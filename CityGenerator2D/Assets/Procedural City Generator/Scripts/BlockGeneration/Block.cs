using System.Collections.Generic;

namespace BlockGeneration
{
    class Block
    {
        public List<BlockNode> Nodes { get; private set; }
        public float Height { get; set; }
        public bool IsPark { get; set; }

        public Block()
        {
            Nodes = new List<BlockNode>();
            Height = 0;
            IsPark = false;
        }
    }
}
