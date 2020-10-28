using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tsg.Entity;
using Tsg.Entity.ServiceCategory;
using Tsg.Entity.Tidings;
using Tsg.Entity.Unused;
using Tsg.Entity.Votes;

namespace Tsg.EFStore
{
    public class TidingStoreService : TsgStoreService<Tiding, CommonContext>
    {
        /// //////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Tiding
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////////
        public TidingStoreService(CommonContext context) : base(context) { }

        public async Task<Tiding> GetById(long id)
        {
            return await Query
                .Include(_ => _.tidingtargets)
                .FirstOrDefaultAsync(_ => _.Id == id);
        }

        public async Task<List<Tiding>> GetAllAsync(long companyId)
        {
            return await Query
                .Include(_ => _.tidingtargets)
                .Where(_ => _.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task<List<Tiding>> GetListById(long id)
        {
            return await Query
                .Include(_ => _.tidingtargets)
                .Where(_ => _.Id == id)
                .ToListAsync();
        }
    }
    /// //////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Tiding target
    /// </summary>
    /// //////////////////////////////////////////////////////////////////////////////////////////
    public class TidingTargetStoreService : TsgStoreService<TidingTarget, CommonContext>
    {
        public TidingTargetStoreService(CommonContext context) : base(context) { }

        public async Task<TidingTarget> GetById(long id)
        {
            return await Query
                .FirstOrDefaultAsync(_ => _.Id == id);
        }
        public async Task<List<TidingTarget>> GetByTidingId(long tidingid)
        {
            return await Query
                .Where(_ => _.TidingId == tidingid)
                .ToListAsync();
        }
        public async Task<List<long?>> GetCustomerTarget(long? companyId, long? objectId, long? podezd, long? placementId)
        {
            return await Query
                .Where(_ =>
                    companyId != null && _.CompanyId == companyId && objectId != null && _.ObjectId == objectId &&
                    (
                        (_.Podezd == null && _.PlacementId == null) ||
                        (_.Podezd != null && _.Podezd == podezd && _.PlacementId == null) ||
                        (_.PlacementId != null && _.PlacementId == placementId)
                    )
                ).GroupBy(a => a.TidingId).Select(a => a.Key).ToListAsync();
        }
    }
}
