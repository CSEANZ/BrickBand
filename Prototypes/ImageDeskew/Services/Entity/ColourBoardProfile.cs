using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Entity
{
    public class ColourBoardProfile
    {
        public List<ColourZone> Zones { get; set; }
        public ColourZone BaseColour { get; set; }
    }
}
