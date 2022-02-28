using System.Collections.Generic;

namespace BlockGeneration
{
    class Block
    {
        public List<BlockNode> Nodes { get; private set; }

        public Block()
        {
            Nodes = new List<BlockNode>();
        }
    }
}
