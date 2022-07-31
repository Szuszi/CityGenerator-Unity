using System;
using System.Collections.Generic;
using UnityEngine;
using GraphModel;
using Services;

namespace BlockDivision
{
    class BoundingRectangle
    {
        public List<Node> Nodes { get; set; }
        public List<Edge> Edges { get; set; }
        
        public BoundingRectangle(List<Node> nodesToUse)
        {
            if (nodesToUse.Count != 4) 
                throw new ArgumentException("Parameter doesn't have exactly four nodes", nameof(nodesToUse));

            Nodes = nodesToUse;
            Edges = new List<Edge>();

            // Get the furthest node from base node to calculate the correct edges
            var baseNode = nodesToUse[0];
            var furthestNode = GetFurtherNodeFromFirst(nodesToUse);

            nodesToUse.Remove(baseNode);
            nodesToUse.Remove(furthestNode);

            Edges.Add(new Edge(baseNode, nodesToUse[0]));
            Edges.Add(new Edge(baseNode, nodesToUse[1]));
            Edges.Add(new Edge(furthestNode, nodesToUse[1]));
            Edges.Add(new Edge(furthestNode, nodesToUse[0]));
        }

        private Node GetFurtherNodeFromFirst(List<Node> nodes)
        {
            var nodeToUse = nodes[0];
            var furthestDistance = 0.0f;
            var furthestNodeIdx = 0;
            foreach (var node in nodes)
            {
                var currentDist = nodeToUse.getDistance(node);
                if (currentDist > furthestDistance)
                {
                    furthestDistance = currentDist;
                    furthestNodeIdx = nodes.IndexOf(node);
                }
            }

            return nodes[furthestNodeIdx];
        }

        public Line GetCutLine(System.Random rand)
        {
            var firstEdgeLength = VectorService.NodesToDirection(Edges[0].NodeA, Edges[0].NodeB).magnitude;
            var secondEdgeLength = VectorService.NodesToDirection(Edges[1].NodeA, Edges[1].NodeB).magnitude;
            var longerEdge = firstEdgeLength > secondEdgeLength ? Edges[0] : Edges[1];

            Vector2 nodeAToB = VectorService.NodesToDirection(longerEdge.NodeA, longerEdge.NodeB);
            int randomValue = rand.Next(0, 4);
            float offsetToUse = 0.3f + 0.1f * randomValue;

            var middlePoint = new Node
            (
                (longerEdge.NodeA.X + nodeAToB.x * offsetToUse),
                (longerEdge.NodeA.Y + nodeAToB.y * offsetToUse)
            );

            return new Line(middlePoint, nodeAToB);
        }

        public Edge GetCutEdge()
        {
            var firstEdgeLength = VectorService.NodesToDirection(Edges[0].NodeA, Edges[0].NodeB).magnitude;
            var secondEdgeLength = VectorService.NodesToDirection(Edges[1].NodeA, Edges[1].NodeB).magnitude;
            var longerEdge = firstEdgeLength > secondEdgeLength ? Edges[0] : Edges[1];
            var otherLongerEdge = firstEdgeLength > secondEdgeLength ? Edges[2] : Edges[3];
            
            var middlePoint = new Node
            (
                (longerEdge.NodeA.X + longerEdge.NodeB.X) / 2.0f, 
                (longerEdge.NodeA.Y + longerEdge.NodeB.Y) / 2.0f
            );
            
            var otherMiddlePoint = new Node
            (
                (otherLongerEdge.NodeA.X + otherLongerEdge.NodeB.X) / 2.0f, 
                (otherLongerEdge.NodeA.Y + otherLongerEdge.NodeB.Y) / 2.0f
            );

            return new Edge(middlePoint, otherMiddlePoint);
        }
        public float GetArea()
        {
            var firstEdgeLength = VectorService.NodesToDirection(Edges[0].NodeA, Edges[0].NodeB).magnitude;
            var secondEdgeLength = VectorService.NodesToDirection(Edges[1].NodeA, Edges[1].NodeB).magnitude;

            return firstEdgeLength * secondEdgeLength;
        }
        
        public float GetAspectRatio()
        {
            var firstEdgeLength = VectorService.NodesToDirection(Edges[0].NodeA, Edges[0].NodeB).magnitude;
            var secondEdgeLength = VectorService.NodesToDirection(Edges[1].NodeA, Edges[1].NodeB).magnitude;
            

            return firstEdgeLength >= secondEdgeLength 
                ? firstEdgeLength / secondEdgeLength 
                : secondEdgeLength / firstEdgeLength;
        }
    }
}