using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class EmployerService : IEmployerService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public EmployerService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public Task<List<Employer>> ListAsync(CancellationToken ct) =>
            _db.Employers.AsNoTracking().OrderByDescending(x => x.EmployerName).ToListAsync(ct);

        public Task<Employer?> GetAsync(Guid id, CancellationToken ct) =>
            _db.Employers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<Employer> CreateAsync(Employer p, CancellationToken ct)
        {
            var utcNow = DateTime.UtcNow;
            p.EmployerNumber = p.EmployerNumber;
            p.EmployerName = p.EmployerName;
            p.Sector=p.Sector;
            p.ContactName=p.ContactName;
            p.ContactEmail=p.ContactEmail;
            p.Phone = p.Phone;
            p.CreatedByUserId = _user.UserId;

            _db.Employers.Add(p);
            await _db.SaveChangesAsync(ct);
            return p;
        }

        public async Task<bool> UpdateAsync(Employer p, CancellationToken ct)
        {
            var existing = await _db.Employers.FirstOrDefaultAsync(x => x.Id == p.Id, ct);
            if (existing is null) return false;

            existing.EmployerNumber = p.EmployerNumber;
            existing.EmployerName   = p.EmployerName;
            existing.Sector = p.Sector; 
            existing.Province = p.Province;
            existing.ContactName = p.ContactName;
            existing.ContactEmail = p.ContactEmail;
            existing.Phone = p.Phone;
          
            existing.Phone = p.Phone;
           

            existing.UpdatedOnUtc = DateTime.UtcNow;
            existing.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var existing = await _db.Providers.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            _db.Providers.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
