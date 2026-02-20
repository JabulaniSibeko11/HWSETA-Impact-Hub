using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class ProviderService : IProviderService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public ProviderService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public Task<List<Provider>> ListAsync(CancellationToken ct) =>
            _db.Providers.AsNoTracking().OrderByDescending(x => x.ProviderName).ToListAsync(ct);

        public Task<Provider?> GetAsync(Guid id, CancellationToken ct) =>
            _db.Providers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<Provider> CreateAsync(Provider p, CancellationToken ct)
        {
            var utcNow = DateTime.UtcNow;

            p.AccreditationNo = p.AccreditationNo;
            p.ContactName=p.ContactName;
            p.ContactEmail=p.ContactEmail;
            p.Province=p.Province;
            p.CreatedOnUtc = utcNow;
            p.CreatedByUserId = _user.UserId;

            _db.Providers.Add(p);
            await _db.SaveChangesAsync(ct);
            return p;
        }

        public async Task<bool> UpdateAsync(Provider p, CancellationToken ct)
        {
            var existing = await _db.Providers.FirstOrDefaultAsync(x => x.Id == p.Id, ct);
            if (existing is null) return false;

            existing.ProviderName = p.ProviderName;
            existing.AccreditationNo = p.AccreditationNo;
            existing.Province = p.Province;
            existing.ContactName = p.ContactName;
            existing.ContactEmail = p.ContactEmail;
          
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
