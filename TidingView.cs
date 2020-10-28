using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tsg.Entity.Tidings
{
    public class TidingView
    {
        public long? Id { get; set; } = null;
        public string Name { get; set; }
        public string Subject { get; set; }
        public long? CompanyId { get; set; } = null;
        public DateTime CreateDate { get; set; }
        public long? NewsTotal { get; set; }
        public ICollection<TidingTargetView> tidingtargets { get; set; }
        public TidingView() { }
        public TidingView(Tiding entity, long total, bool includetargets)
        {
            Id = entity.Id;
            Name = entity.Name;
            Subject = entity.Subject;
            CompanyId = entity.CompanyId;
            CreateDate = entity.CreateDate;
            NewsTotal = total;
            if (includetargets)
                tidingtargets = entity.tidingtargets != null ?
                    entity.tidingtargets.Select(_ => new TidingTargetView(_)).ToList() : null;
        }
    }
}
