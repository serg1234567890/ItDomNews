using System;
using System.Collections.Generic;
using System.Text;

namespace Tsg.Entity.Tidings
{
    public class TidingTargetView
    {
        public long? Id { get; set; }
        public long? TidingId { get; set; }
        public long? CompanyId { get; set; }
        public long? ObjectId { get; set; }
        public long? Podezd { get; set; }
        public long? PlacementId { get; set; }
        public string Title { get; set; } = null;
        public TidingTargetView() { }
        public TidingTargetView(TidingTarget entity)
        {
            Id = entity.Id;
            TidingId = entity.TidingId;
            CompanyId = entity.CompanyId;
            ObjectId = entity.ObjectId;
            Podezd = entity.Podezd;
            PlacementId = entity.PlacementId;
            Title = entity.Title;
        }
    }
}
