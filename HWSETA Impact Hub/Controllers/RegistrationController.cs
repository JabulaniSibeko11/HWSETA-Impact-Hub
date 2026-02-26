using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Registrations;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    [AllowAnonymous]
    public sealed class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBeneficiaryInviteService _invites;
        private readonly IRegistrationService _reg;

        public RegistrationController(ApplicationDbContext db, IBeneficiaryInviteService invites, IRegistrationService reg)
        {
            _db = db;
            _invites = invites;
            _reg = reg;
        }

        // Step 1: claim token -> show password page
        [HttpGet("/register/claim")]
        public async Task<IActionResult> Claim([FromQuery] string token, CancellationToken ct)
        {
            var (ok, beneficiaryId, err) = await _invites.ValidateTokenAsync(token, ct);
            if (!ok) return View("InvalidToken", err);

            var ben = await _db.Beneficiaries.AsNoTracking()
                .FirstAsync(x => x.Id == beneficiaryId, ct);

            var vm = new SetPasswordVm
            {
                Token = token,
                Email = ben.Email ?? ""
            };

            return View("SetPassword", vm);
        }

        // Step 2: set password -> then force location
        [HttpPost("/register/set-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("SetPassword", vm);

            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(vm.Token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            var (ok, err) = await _reg.SetPasswordAsync(beneficiaryId, vm.Email, vm.Password, ct);
            if (!ok)
            {
                ModelState.AddModelError("", err ?? "Failed to set password.");
                return View("SetPassword", vm);
            }

            await _invites.MarkPasswordSetAsync(beneficiaryId, ct);

            return RedirectToAction(nameof(Location), new { token = vm.Token });
        }

        // Step 3: ask location permission then save coords
        [HttpGet("/register/location")]
        public IActionResult Location([FromQuery] string token)
        {
            var vm = new LocationVm { Token = token };
            return View("Location", vm);
        }

        [HttpPost("/register/location")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLocation(LocationVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("Location", vm);

            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(vm.Token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            await _invites.MarkLocationCapturedAsync(beneficiaryId, vm.Latitude, vm.Longitude, ct);

            return RedirectToAction(nameof(RegistrationForm), new { token = vm.Token });
        }

        // Step 4: render SYSTEM registration form (controller-based)
        [HttpGet("/register/form")]
        public async Task<IActionResult> RegistrationForm([FromQuery] string token, CancellationToken ct)
        {
            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            var sys = await _db.FormTemplates.AsNoTracking()
                .Where(x => x.IsActive && x.Purpose == FormPurpose.Registration)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (sys is null)
                return View("InvalidToken", "System 'Beneficiary Registration' template is missing. Please contact admin.");

            var ben = await _db.Beneficiaries.AsNoTracking()
                .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.Id == beneficiaryId, ct);

            if (ben is null)
                return View("InvalidToken", "Beneficiary not found.");

            // ✅ Prevent re-submission
            if (ben.RegistrationSubmittedAt != null ||
                ben.RegistrationStatus >= BeneficiaryRegistrationStatus.RegistrationSubmitted)
            {
                return RedirectToAction(nameof(RegistrationDone));
            }

            var vm = new RegistrationFormVm
            {
                Token = token,
                BeneficiaryId = ben.Id,

                FormName = string.IsNullOrWhiteSpace(sys.Title) ? "Beneficiary Registration" : sys.Title,
                Description = string.IsNullOrWhiteSpace(sys.Description)
                    ? "Please confirm your personal details, accept consent, and provide your programme information."
                    : sys.Description,

                LockIdentifierFields = true,

                IdentifierType = ben.IdentifierType,
                IdentifierValue = ben.IdentifierValue,

                FirstName = ben.FirstName ?? "",
                MiddleName = ben.MiddleName,
                LastName = ben.LastName ?? "",
                DateOfBirth = ben.DateOfBirth,

                GenderId = ben.GenderId,
                RaceId = ben.RaceId,
                CitizenshipStatusId = ben.CitizenshipStatusId,
                DisabilityStatusId = ben.DisabilityStatusId,
                DisabilityTypeId = ben.DisabilityTypeId,
                EducationLevelId = ben.EducationLevelId,
                EmploymentStatusId = ben.EmploymentStatusId,

                Email = ben.Email,
                MobileNumber = ben.MobileNumber ?? "",
                AltNumber = ben.AltNumber,

                ProvinceId = ben.Address?.ProvinceId ?? Guid.Empty,
                City = ben.Address?.City ?? "",
                AddressLine1 = ben.Address?.AddressLine1 ?? "",
                PostalCode = ben.Address?.PostalCode ?? "",

                ConsentGiven = ben.ConsentGiven,
                ConsentDate = (ben.ConsentDate == default ? DateTime.Today : ben.ConsentDate),

                ProgressStatus = "",
                Comment = ""
            };

            await LoadRegistrationDropdownsAsync(vm, ct);

            // ✅ Programmes initially filtered by current QualificationType selection (if any)
            if (vm.QualificationTypeId != Guid.Empty)
            {
                vm.Programmes = await GetProgrammesSelectListAsync(vm.QualificationTypeId, ct);
            }
            else
            {
                vm.Programmes = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            }

            return View("RegistrationForm", vm);
        }

        // Step 4 (POST): persist Section 1 + Section 2 (system registration)
        [HttpPost("/register/form")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrationForm(RegistrationFormVm vm, CancellationToken ct)
        {
            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(vm.Token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            var ben = await _db.Beneficiaries
                .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.Id == beneficiaryId, ct);

            if (ben is null)
                return View("InvalidToken", "Beneficiary not found.");

            // ✅ Prevent re-submission
            if (ben.RegistrationSubmittedAt != null ||
                ben.RegistrationStatus >= BeneficiaryRegistrationStatus.RegistrationSubmitted)
            {
                return RedirectToAction(nameof(RegistrationDone));
            }

            if (!ModelState.IsValid)
            {
                await LoadRegistrationDropdownsAsync(vm, ct);

                // ✅ Programmes must be filtered by chosen QualificationType
                vm.Programmes = vm.QualificationTypeId == Guid.Empty
                    ? new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                    : await GetProgrammesSelectListAsync(vm.QualificationTypeId, ct);

                return View("RegistrationForm", vm);
            }

            // Update Beneficiary (ID/Passport locked)
            ben.FirstName = vm.FirstName.Trim();
            ben.MiddleName = string.IsNullOrWhiteSpace(vm.MiddleName) ? null : vm.MiddleName.Trim();
            ben.LastName = vm.LastName.Trim();
            ben.DateOfBirth = vm.DateOfBirth;

            ben.GenderId = vm.GenderId;
            ben.RaceId = vm.RaceId;
            ben.CitizenshipStatusId = vm.CitizenshipStatusId;
            ben.DisabilityStatusId = vm.DisabilityStatusId;
            ben.DisabilityTypeId = vm.DisabilityTypeId;
            ben.EducationLevelId = vm.EducationLevelId;
            ben.EmploymentStatusId = vm.EmploymentStatusId;

            ben.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
            ben.MobileNumber = vm.MobileNumber.Trim();
            ben.AltNumber = string.IsNullOrWhiteSpace(vm.AltNumber) ? null : vm.AltNumber.Trim();

            if (ben.Address == null)
            {
                ben.Address = new Address
                {
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedByUserId = ben.CreatedByUserId
                };
            }

            ben.Address.ProvinceId = vm.ProvinceId;
            ben.Address.City = vm.City.Trim();
            ben.Address.AddressLine1 = vm.AddressLine1.Trim();
            ben.Address.PostalCode = vm.PostalCode.Trim();
            ben.Address.UpdatedOnUtc = DateTime.UtcNow;

            // Consent
            ben.ConsentGiven = vm.ConsentGiven;
            ben.ConsentDate = vm.ConsentDate;

            // Derive Cohort + Provider in backend (your Cohort is source of truth)
            var cohort = await _db.Cohorts.AsNoTracking()
                .Where(c =>
                    c.IsActive &&
        c.QualificationTypeId == vm.QualificationTypeId &&
                    c.ProgrammeId == vm.ProgrammeId &&
                    c.EmployerId == vm.EmployerId &&
                    c.QualificationTypeId == vm.QualificationTypeId)
                .OrderByDescending(c => c.StartDate)
                .ThenByDescending(c => c.CreatedOnUtc)
                .FirstOrDefaultAsync(ct);

            if (cohort == null)
            {
                ModelState.AddModelError("", "No active cohort found for the selected Qualification Type + Programme + Employer. Please contact support.");
                await LoadRegistrationDropdownsAsync(vm, ct);

                vm.Programmes = vm.QualificationTypeId == Guid.Empty
                    ? new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                    : await GetProgrammesSelectListAsync(vm.QualificationTypeId, ct);

                return View("RegistrationForm", vm);
            }

            vm.DerivedCohortId = cohort.Id;
            vm.DerivedProviderId = cohort.ProviderId;

            EnrollmentStatus status = vm.ProgressStatus switch
            {
                "Completed" => EnrollmentStatus.Completed,
                "DroppedOut" => EnrollmentStatus.DroppedOut,
                _ => EnrollmentStatus.InTraining
            };

            // Upsert enrollment for beneficiary + cohort
            var enrollment = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.BeneficiaryId == beneficiaryId && e.CohortId == cohort.Id, ct);

            if (enrollment == null)
            {
                enrollment = new Enrollment
                {
                    BeneficiaryId = beneficiaryId,
                    CohortId = cohort.Id,
                    CurrentStatus = status,
                    Notes = vm.Comment.Trim(),
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedByUserId = ben.CreatedByUserId
                };

                _db.Enrollments.Add(enrollment);
            }
            else
            {
                enrollment.CohortId = cohort.Id;
                enrollment.CurrentStatus = status;
                enrollment.Notes = vm.Comment.Trim();
                enrollment.UpdatedOnUtc = DateTime.UtcNow;
                enrollment.UpdatedByUserId = ben.CreatedByUserId;
            }

            // Registration timestamps/status
            var now = DateTime.UtcNow;
            ben.RegistrationSubmittedAt = now;
            ben.RegistrationStatus = BeneficiaryRegistrationStatus.RegistrationSubmitted;

            if (status == EnrollmentStatus.Completed)
                ben.RegistrationStatus = BeneficiaryRegistrationStatus.Completed;

            await _db.SaveChangesAsync(ct);

            if (status == EnrollmentStatus.Completed)
                return RedirectToAction(nameof(Proof), new { token = vm.Token });

            return RedirectToAction(nameof(RegistrationDone));
        }

        // Step 5: upload proof of completion (only if status == Completed)
        [HttpGet("/register/proof")]
        public async Task<IActionResult> Proof([FromQuery] string token, CancellationToken ct)
        {
            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            var ben = await _db.Beneficiaries.AsNoTracking()
                .FirstAsync(x => x.Id == beneficiaryId, ct);

            if (ben.RegistrationStatus != BeneficiaryRegistrationStatus.Completed)
                return RedirectToAction(nameof(RegistrationDone));

            return View("UploadProof", new UploadProofVm { Token = token });
        }

        [HttpPost("/register/proof")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProof(UploadProofVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("UploadProof", vm);

            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(vm.Token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            var ben = await _db.Beneficiaries.FirstAsync(x => x.Id == beneficiaryId, ct);

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proof");
            Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(vm.File.FileName);
            var safeName = $"{beneficiaryId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var fullPath = Path.Combine(uploads, safeName);

            await using (var fs = System.IO.File.Create(fullPath))
                await vm.File.CopyToAsync(fs, ct);

            ben.ProofOfCompletionPath = "/uploads/proof/" + safeName;
            ben.ProofUploadedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return RedirectToAction(nameof(RegistrationDone));
        }

        [HttpGet("/register/done")]
        public IActionResult RegistrationDone()
            => View("Done");

        // ✅ AJAX: programmes filtered by QualificationType (Cohort is the source of truth)
        [HttpGet("/register/programmes")]
        public async Task<IActionResult> GetProgrammes([FromQuery] Guid qualificationTypeId, CancellationToken ct)
        {
            if (qualificationTypeId == Guid.Empty)
                return Ok(Array.Empty<object>());

            var items = await _db.Cohorts.AsNoTracking()
                .Where(c => c.IsActive && c.QualificationTypeId == qualificationTypeId)
                .Select(c => new { c.ProgrammeId, c.Programme.ProgrammeName })
                .Distinct()
                .OrderBy(x => x.ProgrammeName)
                .Select(x => new { id = x.ProgrammeId, name = x.ProgrammeName })
                .ToListAsync(ct);

            return Ok(items);
        }
        private async Task<List<SelectListItem>> GetProgrammesSelectListAsync(Guid qualificationTypeId, CancellationToken ct)
        {
            var items = await _db.Cohorts.AsNoTracking()
                .Where(c => c.IsActive && c.QualificationTypeId == qualificationTypeId)
                .Select(c => new { c.ProgrammeId, c.Programme.ProgrammeName })
                .Distinct()
                .OrderBy(x => x.ProgrammeName)
                .ToListAsync(ct);

            return items.Select(x => new SelectListItem
            {
                Value = x.ProgrammeId.ToString(),
                Text = x.ProgrammeName
            }).ToList();
        }
        /// <summary>
        /// Reload dropdowns for RegistrationForm view.
        /// IMPORTANT: call this before returning View() when ModelState invalid.
        /// </summary>
        private async Task LoadRegistrationDropdownsAsync(RegistrationFormVm vm, CancellationToken ct)
        {
            vm.Genders = await _db.Genders.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            vm.Races = await _db.Races.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            vm.CitizenshipStatuses = await _db.CitizenshipStatuses.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            vm.DisabilityStatuses = await _db.DisabilityStatuses.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            vm.DisabilityTypes = await _db.DisabilityTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            vm.EducationLevels = await _db.EducationLevels.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            vm.EmploymentStatuses = await _db.EmploymentStatuses.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            vm.Provinces = await _db.Provinces.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            vm.QualificationTypes = await _db.QualificationTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            // ✅ Programmes will be filtered by QualificationType via AJAX (and server method for postback)
            // keep it empty by default; GET action can optionally fill if QualificationType already selected.
            vm.Programmes = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

            vm.Employers = await _db.Employers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.EmployerName)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.EmployerName })
                .ToListAsync(ct);

            if (vm.ProgressStatuses == null || vm.ProgressStatuses.Count == 0)
            {
                vm.ProgressStatuses = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new() { Value = "Incomplete", Text = "InComplete" },
                    new() { Value = "DroppedOut", Text = "DropOut" },
                    new() { Value = "Completed", Text = "Complete" }
                };
            }

            vm.LockIdentifierFields = true;
        }
    }
}