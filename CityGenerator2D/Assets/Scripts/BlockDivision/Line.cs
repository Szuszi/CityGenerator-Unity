using System;
using GraphModel;
using Services;
using UnityEngine;

namespace BlockDivision
{
    class Line
    {
        public Node BaseNode { get; private set; }
        public Vector2 NormalVector { get; private set; }

        public Line(Node basePoint, Vector2 normal)
        {
            if (normal.magnitude == 0)
            {
                throw new ArgumentException("Normal cannot be null", nameof(normal));
            }
            
            BaseNode = basePoint;
            NormalVector = normal;
        }

        public Line(Node onePoint, Node otherPoint)
        {
            BaseNode = onePoint;
            NormalVector = VectorService.NodesToNormal(onePoint, otherPoint);
            
            if (NormalVector.magnitude == 0)
            {
                Debug.Log($"first Node: [{onePoint.X}|{onePoint.Y}] " +
                            $"other Node: [{otherPoint.X}|{otherPoint.Y}] ");
                throw new ArgumentException("Normal cannot be null", nameof(NormalVector));
            }
        }

        public Line CalculatePerpendicularLine(Node nodeToCross)
        {
            return new Line(nodeToCross, VectorService.DirectionToNormal(NormalVector));
        }

        // https://www.topcoder.com/thrive/articles/Geometry%20Concepts%20part%202:%20%20Line%20Intersection%20and%20its%20Applications
        public Node getCrossing(Line otherLine)
        {
            var a1 = NormalVector.x;
            var b1 = NormalVector.y;
            var a2 = otherLine.NormalVector.x;
            var b2 = otherLine.NormalVector.y;
            var c1 = a1 * BaseNode.X + b1 * BaseNode.Y;
            var c2 = a2 * otherLine.BaseNode.X + b2 * otherLine.BaseNode.Y;

            var determinant = a1 * b2 - a2 * b1;
            if (determinant == 0) return null;
            else
            {
                var x = (b2 * c1 - b1 * c2) / determinant;
                var y = (a1 * c2 - a2 * c1) / determinant;
                return new Node(x, y);
            }
        }
        
        private Node getRandomOtherNodeInLine()
        {
            Vector2 dirVector = VectorService.DirectionToNormal(NormalVector);
            return new Node(BaseNode.X + dirVector.x, BaseNode.Y + dirVector.y);
        }

        public bool isNodeInRightSide(Node node)
        {
            var otherNodeInLine = getRandomOtherNodeInLine();
            var crossProduct = (BaseNode.Y - otherNodeInLine.Y) * (node.X - otherNodeInLine.X)
                               - (BaseNode.X - otherNodeInLine.X) * (node.Y - otherNodeInLine.Y);

            return crossProduct > 0;
        }


    }
}