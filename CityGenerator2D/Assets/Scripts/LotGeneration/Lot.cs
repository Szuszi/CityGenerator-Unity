using System.Collections.Generic;

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
