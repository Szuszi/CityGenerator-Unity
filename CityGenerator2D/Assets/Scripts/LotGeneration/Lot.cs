using System.Collections.Generic;

namespace LotGeneration
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
