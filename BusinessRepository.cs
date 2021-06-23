using Microsoft.EntityFrameworkCore;
using Staks.Data.Entities;
using Staks.Data.Infrastructure;
using Staks.Data.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Staks.Data.Repositories
{
    public class BusinessRepository : StaksRepository<Business>, IBusinessRepository
    {
        private IQueryable<Business> FullBands
        {
            get
            {
                return _context
                    .Businesses
                    .Include(b => b.Profile)
                    .Include(b => b.Members)
                        .ThenInclude(bm => bm.User)
                            .ThenInclude(u => u.Profile);
            }
        }

        public BusinessRepository(StaksContext context) : base(context)
        {
        }

        public override async Task<Business> GetAsync(int id)
        {
            return await FullBands.SingleOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Business> GetByStaksNameAsync(string staksName)
        {
            return await FullBands.SingleOrDefaultAsync(b => b.Profile.StaksName == staksName);
        }

        public async Task<IEnumerable<int>> GetBandMemberIdsAsync(string staksName)
        {
            return await _context
                .Businesses
                .Where(b => b.Profile.StaksName == staksName)
                .SelectMany(b => b.Members)
                .Select(bm => bm.UserId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Business>> GetByUserAsync(int userId)
        {
            return await _context
                .Members
                .Where(bm => bm.User.Id == userId)
                .Include(m => m.Business)
                    .ThenInclude(b => b.Profile)
                .Include(m => m.Business)
                    .ThenInclude(b => b.Members)
                        .ThenInclude(m => m.User)
                .Select(bm => bm.Business)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<PagedList<Business>> GetByBandNameAsync(string bandName, int skip, int size)
        {
            return await _context
                .Businesses
                .Include(b => b.Profile)
                .Where(p => EF.Functions.Like(p.Name, $"{bandName}%"))
                .OrderBy(band => band.Name)
                .ToPagedListAsync(skip, size);
        }
    }
}
