using System;
using System.Collections.Generic;
using System.Text;
using Tsg.Entity.Base;

namespace Tsg.Entity.Tidings
{
    public class TidingTarget : BaseEntityForExcel
    {
        public long? TidingId { get; set; } = null;
        public long? CompanyId { get; set; } = null;
        public long? ObjectId { get; set; } = null;
        public long? Podezd { get; set; } = null;
        public long? PlacementId { get; set; } = null;
        public string Title { get; set; } = null;
    }
}
