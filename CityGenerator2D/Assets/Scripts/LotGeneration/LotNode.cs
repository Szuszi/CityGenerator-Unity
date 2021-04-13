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

        public LotNode(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
