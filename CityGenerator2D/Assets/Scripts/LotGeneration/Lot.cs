using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    class Lot
    {
        public List<LotNode> Nodes { get; private set; }

        public Lot()
        {
            Nodes = new List<LotNode>();
        }
    }
}
