using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Confugations;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class BeneficiaryInviteService : IBeneficiaryInviteService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSenderService _email;
        private readonly ISmsSenderService _sms;
        private readonly IConfiguration _cfg;
        private readonly SmsOptions _smsOpt;
        private readonly IAuditService _audit;

        public BeneficiaryInviteService(
            ApplicationDbContext db,
            IEmailSenderService email,
            ISmsSenderService sms,
            IConfiguration cfg,
            IOptions<SmsOptions> smsOpt,
            IAuditService audit)
        {
            _db = db;
            _email = email;
            _sms = sms;
            _cfg = cfg;
            _smsOpt = smsOpt.Value;
            _audit = audit;
        }

        // ------------------------------------------------------------
        // REGISTRATION INVITE (Controller-based)
        // Sends: /register/claim?token=...
        // Stores: BeneficiaryInvites (TokenHash)
        // ------------------------------------------------------------
        public async Task<(bool ok, string? error)> SendInviteAsync(
            Guid beneficiaryId,
            bool sendEmail,
            bool sendSms,
            CancellationToken ct)
        {
            var ben = await _db.Beneficiaries.FirstOrDefaultAsync(x => x.Id == beneficiaryId, ct);
            if (ben is null)
            {
                await _audit.LogErrorAsync("RegistrationInviteSendFailed", "Beneficiary", beneficiaryId.ToString(),
                    "Beneficiary not found.", ct: ct);
                return (false, "Beneficiary not found.");
            }

            if (!sendEmail && !sendSms)
                return (false, "Select at least one channel (Email/SMS).");

            // Registration single submission rule
            if (ben.RegistrationSubmittedAt != null ||
                ben.RegistrationStatus >= BeneficiaryRegistrationStatus.RegistrationSubmitted)
                return (false, "Beneficiary has already submitted Registration. Cannot send again.");

            // Create new token every time (revokes old invites)
            await RevokeOpenInvitesAsync(beneficiaryId, ct);

            var token = GenerateToken();
            var tokenHash = HashToken(token);

            var channel = sendEmail && sendSms ? InviteChannel.Both
                        : sendSms ? InviteChannel.Sms
                        : InviteChannel.Email;

            var inv = new BeneficiaryInvite
            {
                Id = Guid.NewGuid(),
                BeneficiaryId = beneficiaryId,
                TokenHash = tokenHash,
                Channel = channel,
                Status = InviteStatus.Created,
                CreatedAt = DateTime.UtcNow
            };

            _db.BeneficiaryInvites.Add(inv);

            var publicBaseUrl = (_cfg["App:PublicBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                inv.LastError = "Missing App:PublicBaseUrl in config.";
                await _db.SaveChangesAsync(ct);

                await _audit.LogErrorAsync("RegistrationInviteSendFailed", "Beneficiary", beneficiaryId.ToString(),
                    "Missing App:PublicBaseUrl in config.", note: $"Channel={channel}", ct: ct);

                return (false, "Missing App:PublicBaseUrl in config.");
            }

            var link = BuildRegistrationInviteLink(publicBaseUrl, token);

            // Try both channels; do not stop early
            var errors = new List<string>();

            // Email
            if (sendEmail)
            {
                if (string.IsNullOrWhiteSpace(ben.Email))
                {
                    errors.Add("Beneficiary email is missing.");
                }
                else
                {
                    var subject = "HWSETA Registration Link";
                    var body = $@"
                        <p>Hello {WebUtility.HtmlEncode(ben.FirstName)} {WebUtility.HtmlEncode(ben.LastName)},</p>
                        <p>Please complete your registration using the link below:</p>
                        <p><a href=""{link}"">{link}</a></p>
                        <p>Thank you.</p>";

                    var (ok, err) = await _email.SendAsync(ben.Email!, subject, body, ct);

                    _db.OutboundMessageLogs.Add(new OutboundMessageLog
                    {
                        Id = Guid.NewGuid(),
                        BeneficiaryId = ben.Id,
                        Channel = MessageChannel.Email,
                        Status = ok ? MessageDeliveryStatus.Sent : MessageDeliveryStatus.Failed,
                        To = ben.Email!,
                        Subject = subject,
                        Body = body,
                        SentAt = ok ? DateTime.UtcNow : null,
                        Error = err
                    });

                    if (!ok) errors.Add("Email: " + (err ?? "Unknown error"));
                }
            }

            // SMS
            if (sendSms)
            {
                var mobile = ben.MobileNumber?.Trim();
                if (string.IsNullOrWhiteSpace(mobile))
                {
                    errors.Add("Beneficiary mobile number is missing.");
                }
                else
                {
                    var smsText = $"HWSETA registration link: {link}";
                    var (ok, msgId, err) = await _sms.SendAsync(mobile, smsText, ct);

                    _db.OutboundMessageLogs.Add(new OutboundMessageLog
                    {
                        Id = Guid.NewGuid(),
                        BeneficiaryId = ben.Id,
                        Channel = MessageChannel.Sms,
                        Status = ok ? MessageDeliveryStatus.Sent : MessageDeliveryStatus.Failed,
                        To = mobile,
                        Subject = "SMS",
                        Body = smsText,
                        SentAt = ok ? DateTime.UtcNow : null,
                        ProviderMessageId = msgId,
                        Error = err
                    });

                    if (!ok) errors.Add("SMS: " + (err ?? "Unknown error"));
                }
            }

            // Persist invite + ben status regardless, but return failure if any channel failed
            inv.SentAt = DateTime.UtcNow;

            if (errors.Count == 0)
            {
                inv.Status = InviteStatus.Sent;

                ben.RegistrationStatus = BeneficiaryRegistrationStatus.InviteSent;
                ben.InvitedAt ??= DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    actionType: "RegistrationInviteSent",
                    entityName: "Beneficiary",
                    entityId: ben.Id.ToString(),
                    note: $"Channel={channel}; Link=/register/claim; InviteId={inv.Id}",
                    ct: ct);

                return (true, null);
            }
            else
            {
                inv.Status = InviteStatus.Created;
                inv.LastError = string.Join(" | ", errors);

                await _db.SaveChangesAsync(ct);

                await _audit.LogErrorAsync(
                    actionType: "RegistrationInviteSendFailed",
                    entityName: "Beneficiary",
                    entityId: ben.Id.ToString(),
                    errorMessage: string.Join(" | ", errors),
                    note: $"Channel={channel}; InviteId={inv.Id}",
                    ct: ct);

                return (false, string.Join(" | ", errors));
            }
        }

        public async Task<(bool ok, Guid beneficiaryId, string? error)> ValidateTokenAsync(string token, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, Guid.Empty, "Token is required.");

            var hash = HashToken(token);

            var inv = await _db.BeneficiaryInvites
                .AsNoTracking()
                .Where(x => x.TokenHash == hash && x.Status != InviteStatus.Revoked)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (inv is null)
                return (false, Guid.Empty, "Invalid token.");

            // Optional audit (commented out to avoid noise):
            // await _audit.LogViewAsync("BeneficiaryInvite", inv.Id.ToString(), "Registration token validated", ct);

            return (true, inv.BeneficiaryId, null);
        }

        public async Task MarkPasswordSetAsync(Guid beneficiaryId, CancellationToken ct)
        {
            var ben = await _db.Beneficiaries.FirstAsync(x => x.Id == beneficiaryId, ct);
            ben.RegistrationStatus = BeneficiaryRegistrationStatus.PasswordSet;
            ben.PasswordSetAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync("RegistrationPasswordSet", "Beneficiary", beneficiaryId.ToString(),
                note: "Password set step completed", ct: ct);
        }

        public async Task MarkLocationCapturedAsync(Guid beneficiaryId, decimal lat, decimal lon, CancellationToken ct)
        {
            var ben = await _db.Beneficiaries.FirstAsync(x => x.Id == beneficiaryId, ct);
            ben.Latitude = lat;
            ben.Longitude = lon;
            ben.LocationCapturedAt = DateTime.UtcNow;
            ben.RegistrationStatus = BeneficiaryRegistrationStatus.LocationCaptured;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync("RegistrationLocationCaptured", "Beneficiary", beneficiaryId.ToString(),
                note: $"Lat={lat}; Lon={lon}", ct: ct);
        }

        private async Task RevokeOpenInvitesAsync(Guid beneficiaryId, CancellationToken ct)
        {
            var open = await _db.BeneficiaryInvites
                .Where(x => x.BeneficiaryId == beneficiaryId &&
                            x.Status != InviteStatus.Used &&
                            x.Status != InviteStatus.Revoked)
                .ToListAsync(ct);

            foreach (var i in open)
            {
                i.Status = InviteStatus.Revoked;
                i.RevokedAt = DateTime.UtcNow;
            }

            // No SaveChanges here by design (caller does it).
        }

        // ------------------------------------------------------------
        // SEND CENTER (Unified)
        // - Registration uses SendInviteAsync (/register/claim)
        // - Other forms use FormPublish + BeneficiaryFormInvites (/f/{publicToken}?invite=...)
        // ------------------------------------------------------------
        public async Task<(bool ok, string? error)> SendOneAsync(
            Guid formTemplateId,
            Guid beneficiaryId,
            bool sendEmail,
            bool sendSms,
            string baseUrl,
            string? sentByUserId,
            CancellationToken ct)
        {
            var tpl = await _db.FormTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == formTemplateId && t.IsActive && t.Status == FormStatus.Published, ct);

            if (tpl == null)
                return (false, "Template not found or not published.");

            var ben = await _db.Beneficiaries.FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.IsActive, ct);
            if (ben == null)
                return (false, "Beneficiary not found.");

            if (!sendEmail && !sendSms)
                return (false, "Select at least one channel (Email/SMS).");

            if (sendEmail && string.IsNullOrWhiteSpace(ben.Email))
                return (false, "Beneficiary has no email address.");

            if (sendSms && string.IsNullOrWhiteSpace(ben.MobileNumber))
                return (false, "Beneficiary has no mobile number.");

            // ✅ REGISTRATION: controller-based flow (SendInviteAsync already audits)
            if (tpl.Purpose == FormPurpose.Registration)
            {
                var reg = await SendInviteAsync(beneficiaryId, sendEmail, sendSms, ct);
                return reg.ok ? (true, null) : (false, reg.error ?? "Failed to send registration invite.");
            }

            // ✅ NON-REGISTRATION: dynamic public form flow
            var pub = await _db.FormPublishes
                .FirstOrDefaultAsync(p => p.FormTemplateId == formTemplateId && p.IsPublished, ct);

            if (pub == null)
                return (false, "Form is not published. Publish it first.");

            var errors = new List<string>();

            var cleanBaseUrl = (baseUrl ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(cleanBaseUrl))
                cleanBaseUrl = (_cfg["App:PublicBaseUrl"] ?? "").TrimEnd('/');

            if (string.IsNullOrWhiteSpace(cleanBaseUrl))
                return (false, "Missing baseUrl and App:PublicBaseUrl.");

            if (sendEmail)
            {
                var r = await EnsureInviteRowAsync(pub.Id, ben.Id, InviteChannel.Email, sentByUserId, ct);
                var link = BuildPublicFormInviteLink(cleanBaseUrl, pub.PublicToken, r.InviteToken);

                var subject = $"{tpl.Title} - Please complete the form";
                var bodyHtml = BuildEmailHtml(ben.FirstName, tpl.Title, link);

                var sendResult = await TrySendEmailAsync(ben.Email!, subject, bodyHtml, ct);
                await UpdateInviteAfterSendAsync(r.Id, sendResult.ok, sendResult.error, ct);

                if (!sendResult.ok) errors.Add($"Email failed: {sendResult.error}");
            }

            if (sendSms)
            {
                var r = await EnsureInviteRowAsync(pub.Id, ben.Id, InviteChannel.Sms, sentByUserId, ct);
                var link = BuildPublicFormInviteLink(cleanBaseUrl, pub.PublicToken, r.InviteToken);

                var smsText = $"{tpl.Title}: Please complete the form: {link}";
                var sendResult = await TrySendSmsAsync(ben.MobileNumber, smsText, ct);
                await UpdateInviteAfterSendAsync(r.Id, sendResult.ok, sendResult.error, ct);

                if (!sendResult.ok) errors.Add($"SMS failed: {sendResult.error}");
            }

            if (errors.Count > 0)
            {
                await _audit.LogErrorAsync(
                    actionType: "FormInviteSendFailed",
                    entityName: "FormTemplate",
                    entityId: formTemplateId.ToString(),
                    errorMessage: string.Join(" | ", errors),
                    note: $"BeneficiaryId={beneficiaryId}; Channels={(sendEmail && sendSms ? "Both" : sendEmail ? "Email" : "SMS")}",
                    ct: ct);

                return (false, string.Join(" | ", errors));
            }

            await _audit.LogAsync(
                actionType: "FormInviteSent",
                entityName: "FormTemplate",
                entityId: formTemplateId.ToString(),
                note: $"BeneficiaryId={beneficiaryId}; PublishId={pub.Id}; Channels={(sendEmail && sendSms ? "Both" : sendEmail ? "Email" : "SMS")}",
                ct: ct);

            return (true, null);
        }

        public async Task<(bool ok, string? error, int sent, int failed)> SendBulkAsync(
            FormSendBulkVm vm,
            string baseUrl,
            string? sentByUserId,
            CancellationToken ct)
        {
            if (vm.TemplateId == Guid.Empty)
                return (false, "TemplateId is required.", 0, 0);

            var tpl = await _db.FormTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == vm.TemplateId && t.IsActive && t.Status == FormStatus.Published, ct);

            if (tpl == null)
                return (false, "Template not found or not published.", 0, 0);

            if (!vm.SendEmail && !vm.SendSms)
                return (false, "Select at least one channel (Email/SMS).", 0, 0);

            // For NON-registration forms, enforce publish once
            if (tpl.Purpose != FormPurpose.Registration)
            {
                var pub = await _db.FormPublishes.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.FormTemplateId == vm.TemplateId && p.IsPublished, ct);

                if (pub == null)
                    return (false, "Form is not published. Publish it first.", 0, 0);
            }

            IQueryable<Beneficiary> q = _db.Beneficiaries.Where(b => b.IsActive);

            if (vm.Status.HasValue)
                q = q.Where(b => b.RegistrationStatus == vm.Status.Value);

            if (vm.OnlyNotInvitedYet)
                q = q.Where(b => b.InvitedAt == null);

            if (vm.OnlyMissingPasswordOrUser)
                q = q.Where(b => string.IsNullOrWhiteSpace(b.UserId) || b.PasswordSetAt == null);

            // Legacy string filters (keep if your Beneficiary has these fields)
            if (!string.IsNullOrWhiteSpace(vm.Programme))
                q = q.Where(b => b.Programme == vm.Programme);

            if (!string.IsNullOrWhiteSpace(vm.Provider))
                q = q.Where(b => b.TrainingProvider == vm.Provider);

            if (!string.IsNullOrWhiteSpace(vm.Employer))
                q = q.Where(b => b.Employer == vm.Employer);

            if (!string.IsNullOrWhiteSpace(vm.Province))
                q = q.Where(b => b.Province == vm.Province);

            if (!string.IsNullOrWhiteSpace(vm.Search))
            {
                var s = vm.Search.Trim();
                q = q.Where(b =>
                    (b.FirstName + " " + b.LastName).Contains(s) ||
                    (b.Email ?? "").Contains(s) ||
                    (b.MobileNumber ?? "").Contains(s) ||
                    (b.IdentifierValue ?? "").Contains(s));
            }

            if (vm.SendEmail)
                q = q.Where(b => b.Email != null && b.Email != "");

            if (vm.SendSms)
                q = q.Where(b => b.MobileNumber != null && b.MobileNumber != "");

            // Registration: exclude those already submitted
            if (tpl.Purpose == FormPurpose.Registration)
            {
                q = q.Where(b => b.RegistrationSubmittedAt == null &&
                                 b.RegistrationStatus < BeneficiaryRegistrationStatus.RegistrationSubmitted);
            }

            var ids = await q.Select(b => b.Id).ToListAsync(ct);

            int sent = 0, failed = 0;

            foreach (var benId in ids)
            {
                var r = await SendOneAsync(vm.TemplateId, benId, vm.SendEmail, vm.SendSms, baseUrl, sentByUserId, ct);
                if (r.ok) sent++;
                else failed++;
            }

            await _audit.LogAsync(
                actionType: "BulkInviteCompleted",
                entityName: "FormTemplate",
                entityId: vm.TemplateId.ToString(),
                note: $"Purpose={tpl.Purpose}; Channels={(vm.SendEmail && vm.SendSms ? "Both" : vm.SendEmail ? "Email" : "SMS")}; Targeted={ids.Count}; Sent={sent}; Failed={failed}",
                ct: ct);

            return (true, null, sent, failed);
        }

        public async Task<(bool ok, string? error, BeneficiaryFormInvite? invite)> GetInviteAsync(string inviteToken, CancellationToken ct)
        {
            inviteToken = (inviteToken ?? "").Trim();
            if (string.IsNullOrWhiteSpace(inviteToken))
                return (false, "Invalid invite token.", null);

            var inv = await _db.BeneficiaryFormInvites.AsNoTracking()
                .Include(x => x.FormPublish)
                .FirstOrDefaultAsync(x => x.InviteToken == inviteToken && x.IsActive, ct);

            if (inv == null)
                return (false, "Invite not found.", null);

            // Optional audit:
            // await _audit.LogViewAsync("BeneficiaryFormInvite", inv.Id.ToString(), "Invite opened", ct);

            return (true, null, inv);
        }

        // ------------------------------------------------------------
        // Internal helpers (Dynamic Forms)
        // ------------------------------------------------------------
        private async Task<BeneficiaryFormInvite> EnsureInviteRowAsync(
            Guid formPublishId,
            Guid beneficiaryId,
            InviteChannel channel,
            string? sentByUserId,
            CancellationToken ct)
        {
            var existing = await _db.BeneficiaryFormInvites
                .FirstOrDefaultAsync(x =>
                    x.FormPublishId == formPublishId &&
                    x.BeneficiaryId == beneficiaryId &&
                    x.Channel == channel &&
                    x.IsActive, ct);

            if (existing != null)
            {
                existing.Attempts += 1;
                existing.LastAttemptAtUtc = DateTime.UtcNow;
                existing.SentByUserId = sentByUserId;
                await _db.SaveChangesAsync(ct);
                return existing;
            }

            var row = new BeneficiaryFormInvite
            {
                FormPublishId = formPublishId,
                BeneficiaryId = beneficiaryId,
                Channel = channel,
                InviteToken = NewInviteToken(),
                DeliveryStatus = InviteDeliveryStatus.Pending,
                Attempts = 1,
                LastAttemptAtUtc = DateTime.UtcNow,
                SentByUserId = sentByUserId,
                IsActive = true
            };

            _db.BeneficiaryFormInvites.Add(row);
            await _db.SaveChangesAsync(ct);
            return row;
        }

        private async Task UpdateInviteAfterSendAsync(Guid inviteId, bool ok, string? error, CancellationToken ct)
        {
            var inv = await _db.BeneficiaryFormInvites.FirstOrDefaultAsync(x => x.Id == inviteId, ct);
            if (inv == null) return;

            if (ok)
            {
                inv.DeliveryStatus = InviteDeliveryStatus.Sent;
                inv.SentAtUtc = DateTime.UtcNow;
                inv.LastError = null;
            }
            else
            {
                inv.DeliveryStatus = InviteDeliveryStatus.Failed;
                inv.LastError = error;
            }

            await _db.SaveChangesAsync(ct);
        }

        private static string BuildPublicFormInviteLink(string baseUrl, string publicToken, string inviteToken)
            => $"{baseUrl.TrimEnd('/')}/f/{publicToken}?invite={Uri.EscapeDataString(inviteToken)}";

        private static string BuildRegistrationInviteLink(string baseUrl, string inviteToken)
            => $"{baseUrl.TrimEnd('/')}/register/claim?token={Uri.EscapeDataString(inviteToken)}";

        private static string NewInviteToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(18);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string BuildEmailHtml(string firstName, string formTitle, string link)
        {
            return $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222'>
                    <p>Hello {WebUtility.HtmlEncode(firstName)},</p>
                    <p>Please complete the form: <b>{WebUtility.HtmlEncode(formTitle)}</b></p>
                    <p><a href='{link}'>Click here to open the form</a></p>
                    <p>Thank you.</p>
                </div>";
        }

        private async Task<(bool ok, string? error)> TrySendEmailAsync(string toEmail, string subject, string bodyHtml, CancellationToken ct)
        {
            try
            {
                await _email.SendAsync(toEmail, subject, bodyHtml, ct);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<(bool ok, string? error)> TrySendSmsAsync(string toNumber, string message, CancellationToken ct)
        {
            try
            {
                await _sms.SendAsync(toNumber, message, ct);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}