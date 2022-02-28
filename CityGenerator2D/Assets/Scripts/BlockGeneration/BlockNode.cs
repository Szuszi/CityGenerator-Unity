using System.Collections.Generic;
using GraphModel;

namespace BlockGeneration
{
    class BlockNode
    {
        public float X { get; private set; }
        public float Y { get; private set; }

        public List<Edge> Edges { get; private set; } //Usually have two, but in special cases, it can be only one

        public Block Block { get; set; }

        public BlockNode(float x, float y)
        {
            X = x;
            Y = y;

            Edges = new List<Edge>();
        }
    }
}

