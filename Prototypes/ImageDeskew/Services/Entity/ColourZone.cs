using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Entity
{
    public class ColourZone
    {
        public int Column { get; set; }
        public byte B { get; set; }
        public byte G { get; set; }
        public byte R { get; set; }
        public int Average { get; set; }
    }
}
