using System.Collections.Generic;
using BlockDivision;
using BlockGeneration;
using GraphModel;

namespace Services
{
    class BoundingService
    {
        public static BoundingRectangle GetMinBoundingRectangle(Block block)
        {
            var candidateRectangles = new List<BoundingRectangle>();

            for (int i = 0; i < block.Nodes.Count - 1; i++)
            {
                candidateRectangles.Add(CalculateBoundingRectangle(block, i, i+1));
            }
            candidateRectangles.Add(
                CalculateBoundingRectangle(block, block.Nodes.Count - 1, 0)
            );

            return GetSmallestBoundingRectangle(candidateRectangles);
        }

        private static BoundingRectangle CalculateBoundingRectangle(Block block, int firstNodeIndex, int otherNodeIndex)
        {
            var baseLine = new Line(block.Nodes[firstNodeIndex].GetNodeForm(),
                                    block.Nodes[otherNodeIndex].GetNodeForm());
            
            var sideLines = GetMinMaxPerpendicularLine(baseLine, block);
            var otherSideLines = GetMinMaxPerpendicularLine(sideLines[0], block);

            var boundingNodes = new List<Node>();
            boundingNodes.Add(sideLines[0].getCrossing(otherSideLines[0]));
            boundingNodes.Add(sideLines[0].getCrossing(otherSideLines[1]));
            boundingNodes.Add(sideLines[1].getCrossing(otherSideLines[0]));
            boundingNodes.Add(sideLines[1].getCrossing(otherSideLines[1]));

            return new BoundingRectangle(boundingNodes);
        }

        private static List<Line> GetMinMaxPerpendicularLine(Line baseLine, Block block)
        {
            var candidateEdgeLines = new List<Line>();
            
            foreach (var blockNode in block.Nodes)
            {
                var baseNode = new Node(blockNode.X, blockNode.Y);
                Line perpendicularLine = baseLine.CalculatePerpendicularLine(baseNode);
                candidateEdgeLines.Add(perpendicularLine);
            }
            
            int currentMinIdx = 0;
            int currentMaxIdx = 0;
            
            // Special case: Normal Vector of the line has X as 0
            if (baseLine.NormalVector.x == 0)
            {
                float currentMin = baseLine.getCrossing(candidateEdgeLines[0]).Y;
                float currentMax = currentMin;
                foreach (var line in candidateEdgeLines)
                {
                    Node crossingNode = baseLine.getCrossing(line);
                    if (crossingNode == null) continue;

                    if (currentMin > crossingNode.Y)
                    {
                        currentMin = crossingNode.Y;
                        currentMinIdx = candidateEdgeLines.IndexOf(line);
                    }

                    if (currentMax < crossingNode.Y)
                    {
                        currentMax = crossingNode.Y;
                        currentMaxIdx = candidateEdgeLines.IndexOf(line);
                    }
                }
            }
            else
            {
                float currentMin = baseLine.getCrossing(candidateEdgeLines[0]).X;
                float currentMax = currentMin;
                foreach (var line in candidateEdgeLines)
                {
                    Node crossingNode = baseLine.getCrossing(line);
                    if (crossingNode == null) continue;

                    if (currentMin > crossingNode.X)
                    {
                        currentMin = crossingNode.X;
                        currentMinIdx = candidateEdgeLines.IndexOf(line);
                    }

                    if (currentMax < crossingNode.X)
                    {
                        currentMax = crossingNode.X;
                        currentMaxIdx = candidateEdgeLines.IndexOf(line);
                    }
                }
            }
            
            return new List<Line> {candidateEdgeLines[currentMinIdx] , candidateEdgeLines[currentMaxIdx]};
        }

        private static BoundingRectangle GetSmallestBoundingRectangle(List<BoundingRectangle> boundingRectangles)
        {
            var smallestRectangleArea = boundingRectangles[0].GetArea();
            var smallestRectangleIdx = 0;

            foreach (var rectangle in boundingRectangles)
            {
                var currentArea = rectangle.GetArea();
                if (smallestRectangleArea > currentArea)
                {
                    smallestRectangleArea = currentArea;
                    smallestRectangleIdx = boundingRectangles.IndexOf(rectangle);
                }
            }

            return boundingRectangles[smallestRectangleIdx];
        }
    }
}