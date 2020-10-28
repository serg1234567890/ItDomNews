using System;
using System.Collections.Generic;
using System.Text;
using Tsg.Entity.Base;

namespace Tsg.Entity.Tidings
{
    public class Tiding : BaseEntityForExcel
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public long? CompanyId { get; set; } = null;
        public DateTime? StartDate { get; set; } = DateTime.UtcNow;
        public ICollection<TidingTarget> tidingtargets { get; set; }
    }
}
