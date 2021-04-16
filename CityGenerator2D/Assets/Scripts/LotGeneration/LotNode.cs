using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    class LotNode
    {
        public float X { get; private set; }
        public float Y { get; private set; }

        public List<Edge> Edges { get; private set; } //Usually have two, but in special cases, it can be only one

        public Lot Lot { get; set; }

        public LotNode(float x, float y)
        {
            X = x;
            Y = y;

            Edges = new List<Edge>();
        }
    }
}

