using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    class RoadSegment
    {
        public Node NodeFrom { get; set; }
        public Node NodeTo { get; set; }

        public int LeanIteration { get; set; }
        public bool LeanRight { get; set; }
        public bool LeanLeft { get; set; }

        public bool EndSegmen { get; set; }

        public RoadSegment(Node from, Node to, int leanNumb)
        {
            NodeFrom = from;
            NodeTo = to;
            LeanIteration = leanNumb;

            LeanLeft = false;
            LeanRight = false;
            EndSegmen = false;
        }

        public bool IsCrossing(RoadSegment road) //Check if the given road intersects with this road
        {
            //if (NodeFrom == road.NodeFrom || NodeFrom == road.NodeTo && NodeTo == road.NodeFrom || NodeTo == road.NodeTo) return true; //Two roadsegment is overlapping each other
            if (NodeFrom == road.NodeFrom || NodeFrom == road.NodeTo || NodeTo == road.NodeFrom || NodeTo == road.NodeTo) return false; //One of the Nodes is the same (it doesnt count as an intersection now)

            int o1 = Orientation(NodeFrom, NodeTo, road.NodeTo);
            int o2 = Orientation(NodeFrom, NodeTo, road.NodeFrom);
            int o3 = Orientation(road.NodeFrom, road.NodeTo, NodeFrom);
            int o4 = Orientation(road.NodeFrom, road.NodeTo, NodeTo);

            //General case
            if (o1 != o2 && o3 != o4)
                return true;

            //Special case (rare, but need to handle) - happens if two segments are collinear - we need to check if they are overlap or not
            if (o1 == 0 && o2 == 0 && o3 == 0 && o4 == 0)
            {
                if (o1 == 0 && OnSegment(NodeFrom, NodeTo, road.NodeTo)) return true;
                if (o2 == 0 && OnSegment(NodeFrom, NodeTo, road.NodeFrom)) return true;
                if (o3 == 0 && OnSegment(road.NodeFrom, road.NodeTo, NodeFrom)) return true;
                if (o4 == 0 && OnSegment(road.NodeFrom, road.NodeTo, NodeTo)) return true;
            }

            return false;
        }

        private int Orientation(Node BaseNode1, Node BaseNode2, Node TestNode) //Oriantation can be calculated with the cross product of two Vectors made from the 3 Nodes
        {
            //float val = (TestNode.Y - BaseNode1.Y) * (BaseNode2.X - TestNode.X) - (TestNode.X - BaseNode1.X) * (BaseNode2.Y - TestNode.Y); //cross product calculation
            float val = (BaseNode1.Y - TestNode.Y) * (BaseNode2.X - TestNode.X) - (BaseNode1.X - TestNode.X) * (BaseNode2.Y - TestNode.Y); //cross product calculation

            if (val > 0.00001f) return 1; //clockwise
            if (val < -0.00001f) return -1; //anticlockwise
            else return 0; //collinear
        }

        private bool OnSegment(Node BaseNode1, Node BaseNode2, Node TestNode) //Check if TestNode is between BaseNodes (we only call this if the 3 points are collinear)
        {
            //If X and Y coordinates are between the BaseNodes X and Y coordinates, then TestNode overlappes
            if (TestNode.X <= Math.Max(BaseNode1.X, BaseNode2.X) && TestNode.X >= Math.Min(BaseNode1.X, BaseNode2.X) && 
                TestNode.Y <= Math.Max(BaseNode1.Y, BaseNode2.Y) && TestNode.Y >= Math.Min(BaseNode1.Y, BaseNode2.Y)) return true;

            return false;
        }
    }
}
