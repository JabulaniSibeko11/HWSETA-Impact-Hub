using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Enrollment;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class EnrollmentDocumentService : IEnrollmentDocumentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;
        private readonly IWebHostEnvironment _env;

        public EnrollmentDocumentService(ApplicationDbContext db, ICurrentUserService user, IWebHostEnvironment env)
        {
            _db = db;
            _user = user;
            _env = env;
        }

        public Task<List<EnrollmentDocumentRowVm>> ListAsync(Guid enrollmentId, CancellationToken ct) =>
            _db.EnrollmentDocuments.AsNoTracking()
                .Where(x => x.EnrollmentId == enrollmentId)
                .Include(x => x.DocumentType)
                .OrderByDescending(x => x.UploadedOnUtc)
                .Select(x => new EnrollmentDocumentRowVm
                {
                    Id = x.Id,
                    EnrollmentId = x.EnrollmentId,
                    DocumentType = x.DocumentType.Name,
                    FileName = x.FileName,
                    UploadedOnUtc = x.UploadedOnUtc,
                    UploadedByUserId = x.UploadedByUserId
                })
                .ToListAsync(ct);

        public async Task<(bool ok, string? error)> UploadAsync(EnrollmentDocumentUploadVm vm, CancellationToken ct)
        {
            if (vm.File == null || vm.File.Length == 0)
                return (false, "No file uploaded.");

            // Validate enrollment exists
            var enrollmentExists = await _db.Enrollments.AnyAsync(x => x.Id == vm.EnrollmentId, ct);
            if (!enrollmentExists) return (false, "Enrollment not found.");

            // Validate document type exists/active
            var docType = await _db.DocumentTypes.AsNoTracking()
                .Where(x => x.Id == vm.DocumentTypeId && x.IsActive)
                .Select(x => new { x.Id, x.Name })
                .FirstOrDefaultAsync(ct);

            if (docType == null) return (false, "Document type not found.");

            // Storage folder
            var root = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "enrollments", vm.EnrollmentId.ToString("N"));
            Directory.CreateDirectory(root);

            // Safe file name
            var originalName = Path.GetFileName(vm.File.FileName);
            var ext = Path.GetExtension(originalName);
            var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(root, storedName);

            // Write + hash
            string sha256Hex;
            await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (var input = vm.File.OpenReadStream())
            {
                using var sha = SHA256.Create();
                // Compute hash while copying
                var buffer = new byte[81920];
                int read;
                while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read), ct);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                sha256Hex = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
            }

            var relativePath = $"/uploads/enrollments/{vm.EnrollmentId:N}/{storedName}";

            var doc = new EnrollmentDocument
            {
                EnrollmentId = vm.EnrollmentId,
                DocumentTypeId = vm.DocumentTypeId,
                FileName = originalName,
                StoredPath = relativePath,
                Sha256 = sha256Hex,
                UploadedByUserId = _user.UserId,
                UploadedOnUtc = DateTime.UtcNow,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.EnrollmentDocuments.Add(doc);
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }

        public async Task<(bool ok, string? error, string filePath, string downloadName)> GetFileAsync(Guid docId, CancellationToken ct)
        {
            var doc = await _db.EnrollmentDocuments.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == docId, ct);

            if (doc == null) return (false, "Document not found.", "", "");

            // StoredPath is like "/uploads/..."
            var relative = doc.StoredPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(_env.WebRootPath ?? "wwwroot", relative);

            if (!File.Exists(full)) return (false, "File missing on disk.", "", "");

            return (true, null, full, doc.FileName);
        }

        public async Task<(bool ok, string? error)> DeleteAsync(Guid docId, CancellationToken ct)
        {
            var doc = await _db.EnrollmentDocuments.FirstOrDefaultAsync(x => x.Id == docId, ct);
            if (doc == null) return (false, "Document not found.");

            // Delete file if exists
            var relative = doc.StoredPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(_env.WebRootPath ?? "wwwroot", relative);

            if (File.Exists(full))
                File.Delete(full);

            _db.EnrollmentDocuments.Remove(doc);
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }
    }
}
