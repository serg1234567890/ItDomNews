using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Tsg.Entity.Tidings
{
    public class TidingPush : TidingTarget
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public DateTime? StartDate { get; set; }
        List<long?> placements { get; set; }
        public TidingPush() 
        {
            placements = new List<long?>();
        }
        public void AddPlacements(ICollection<long?> range)
        {
            placements.AddRange(range);
        }
        public IEnumerable<long?> GetPlacements()
        {
            return placements.Distinct();
        }
    }
}
