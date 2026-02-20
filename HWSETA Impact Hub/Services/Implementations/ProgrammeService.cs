using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class ProgrammeService : IProgrammeService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public ProgrammeService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public Task<List<Programme>> ListAsync(CancellationToken ct) =>
            _db.Programmes.AsNoTracking().OrderByDescending(x => x.StartDate).ToListAsync(ct);

        public Task<Programme?> GetAsync(Guid id, CancellationToken ct) =>
            _db.Programmes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<Programme> CreateAsync(Programme p, CancellationToken ct)
        {
            var utcNow = DateTime.UtcNow;
            p.CreatedOnUtc = utcNow;
            p.CreatedByUserId = _user.UserId;

            _db.Programmes.Add(p);
            await _db.SaveChangesAsync(ct);
            return p;
        }

        public async Task<bool> UpdateAsync(Programme p, CancellationToken ct)
        {
            var existing = await _db.Programmes.FirstOrDefaultAsync(x => x.Id == p.Id, ct);
            if (existing is null) return false;

            existing.ProgrammeName = p.ProgrammeName;
            existing.ProgrammeType = p.ProgrammeType;
            existing.CohortYear = p.CohortYear;
            existing.StartDate = p.StartDate;
            existing.EndDate = p.EndDate;
            existing.Province = p.Province;
            existing.TargetBeneficiaries = p.TargetBeneficiaries;
            existing.Notes = p.Notes;

            existing.UpdatedOnUtc = DateTime.UtcNow;
            existing.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var existing = await _db.Programmes.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            _db.Programmes.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
