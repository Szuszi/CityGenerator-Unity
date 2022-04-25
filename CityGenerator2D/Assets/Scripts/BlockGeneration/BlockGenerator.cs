using System;
using System.Collections.Generic;
using System.Linq;
using GraphModel;
using Services;
using UnityEngine;

namespace BlockGeneration
{
    class BlockGenerator
    {
        private readonly Graph graph;
        public List<BlockNode> BlockNodes { get; private set; }
        public List<Block> Blocks { get; private set; }
        public List<Block> ThinnedBlocks { get; private set; }

        private readonly float majorRoadThickness;
        private readonly float minorRoadThickness;
        private readonly float blockHeight;

        public BlockGenerator(Graph graphToUse, float majorThickness, float minorThickness, float blockHeight)
        {
            graph = graphToUse;
            BlockNodes = new List<BlockNode>();
            Blocks = new List<Block>();
            ThinnedBlocks = new List<Block>();

            majorRoadThickness = majorThickness;
            minorRoadThickness = minorThickness;
            this.blockHeight = blockHeight;
        }

        public void Generate()
        {
            ThickenNodes();
            CreateBlocks();
            DeleteMapEdgeBlocks();
        }
        
        public void ThickenBlocks(float sidewalkThickness)
        {
            foreach (Block block in Blocks)
            {
                var thinnedBlock = GetTinnedBlock(block, sidewalkThickness);
                ThinnedBlocks.Add(thinnedBlock);
            }

            RemoveAbnormalBlocks();
        }

        //Makes the Nodes thicker, by extending them by the given thickness.
        private void ThickenNodes()
        {
            //First Thicken major nodes
            foreach(Node node in graph.MajorNodes)
            {
                if(node.Edges.Count == 1) OneEdgedThickening(node, majorRoadThickness);
                else if(node.Edges.Count == 2) TwoEdgedThickening(node, majorRoadThickness);
                else if(node.Edges.Count == 3) ThreeEdgedThickening(node, majorRoadThickness);
                else if(node.Edges.Count == 4) FourEdgedThickening(node, majorRoadThickness);
                else if(node.Edges.Count > 4)
                    throw new ArgumentException("Node has more than four edges", nameof(node));
                else throw new ArgumentException("Node has no edges", nameof(node));
            }

            //Then Thicken minor nodes
            foreach (Node node in graph.MinorNodes)
            {
                if (node.Edges.Count == 1) OneEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 2) TwoEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 3) ThreeEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 4) FourEdgedThickening(node, minorRoadThickness);                
                else if (node.Edges.Count > 4)
                    throw new ArgumentException("Node has more than four edges", nameof(node));
                else throw new ArgumentException("Node has no edges", nameof(node));
            }
        }

        //Connects the BlockNodes into one Block
        private void CreateBlocks()
        {
            foreach(BlockNode blockNode in BlockNodes)
            {
                if(blockNode.Block == null)
                {
                    var newBlock = new Block();
                    newBlock.Height = blockHeight;
                    
                    Blocks.Add(newBlock);
                    if (!FormBlock(blockNode, blockNode.Edges[0], newBlock, 1))
                    {
                        Blocks.Remove(newBlock); //If the new block is corrupt, remove it
                    }
                }
            }
        }

        //Delete Blocks which are in the edge of the map, or not formed correctly
        private void DeleteMapEdgeBlocks()
        {
            List<Block> removable = new List<Block>();

            foreach(Block block in Blocks)
            {
                //First Check if the First and Last node has the same edge.
                bool sameEdgeFound = false;
                foreach (Edge edge1 in block.Nodes[0].Edges)
                {
                    foreach(Edge edge2 in block.Nodes[block.Nodes.Count - 1].Edges)
                    {
                        if (edge1 == edge2) sameEdgeFound = true;
                    }
                }

                if (!sameEdgeFound) removable.Add(block);

                //We also remove Blocks, which doesn't have at least 3 nodes
                else if (block.Nodes.Count <= 2) removable.Add(block);

                else //There are also Edge Blocks which first and last BlockNode is in the same Node
                {
                    Node firstNode = block.Nodes[0].Edges[0].NodeA.BlockNodes.Contains(block.Nodes[0]) 
                        ? block.Nodes[0].Edges[0].NodeA 
                        : block.Nodes[0].Edges[0].NodeB;
                    Node lastNode = block.Nodes[block.Nodes.Count - 1].Edges[0].NodeA.BlockNodes
                        .Contains(block.Nodes[block.Nodes.Count - 1]) 
                        ? block.Nodes[block.Nodes.Count - 1].Edges[0].NodeA 
                        : block.Nodes[block.Nodes.Count - 1].Edges[0].NodeB;

                    if (firstNode == lastNode) removable.Add(block);
                }
            }

            foreach(Block block in removable)
            {
                Blocks.Remove(block);
            }
        }

        private void RemoveAbnormalBlocks()
        {
            List<int> removableIndexes = new List<int>();
            
            foreach (Block block in ThinnedBlocks)
            {
                if (block.Nodes.Count > 10)
                {
                    removableIndexes.Add(ThinnedBlocks.IndexOf(block));
                    continue;
                }

                var boundingRect = BoundingService.GetMinBoundingRectangle(block);
                if (boundingRect.GetArea() < 10f)
                {
                    removableIndexes.Add(ThinnedBlocks.IndexOf(block));
                }
            }

            for (int i = ThinnedBlocks.Count - 1; i > -1; i--)
            {
                if (removableIndexes.Contains(i))
                {
                    ThinnedBlocks.RemoveAt(i);
                }
            }
        }



        /**
         * THICKENER FUNCTIONS
         */

        private void OneEdgedThickening(Node node, float thickness)
        {
            //this only work for nodes, which has only one edge
            if (node.Edges.Count != 1) 
                throw new ArgumentException("Parameter node doesn't have exactly one edges", nameof(node));

            //Calculate
            float dirRadian;
            if (node.Edges[0].NodeA == node) dirRadian = node.Edges[0].DirRadianFromA;
            else dirRadian = node.Edges[0].DirRadianFromB;

            Vector2 leftForward = new Vector2(
                Mathf.Cos(dirRadian + 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4),
                Mathf.Sin(dirRadian + 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4));
            Vector2 rightForward = new Vector2(
                Mathf.Cos(dirRadian - 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4),
                Mathf.Sin(dirRadian - 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4));


            //Then store it
            BlockNode blockNode1 = new BlockNode(node.X + leftForward.x, node.Y + leftForward.y);
            BlockNode blockNode2 = new BlockNode(node.X + rightForward.x, node.Y + rightForward.y);

            blockNode1.Edges.Add(node.Edges[0]);
            blockNode2.Edges.Add(node.Edges[0]);

            node.BlockNodes.Add(blockNode1);
            node.BlockNodes.Add(blockNode2);

            BlockNodes.Add(blockNode1);
            BlockNodes.Add(blockNode2);
        }

        private void TwoEdgedThickening(Node node, float thickness)
        {
            var newBlockNodes = GetTwoEdgedThickenedNodes(node, thickness);
            var blockNode1 = newBlockNodes[0];
            var blockNode2 = newBlockNodes[1];

            blockNode1.Edges.Add(node.Edges[0]);
            blockNode1.Edges.Add(node.Edges[1]);
            blockNode2.Edges.Add(node.Edges[0]);
            blockNode2.Edges.Add(node.Edges[1]);

            node.BlockNodes.Add(blockNode1);
            node.BlockNodes.Add(blockNode2);

            BlockNodes.Add(blockNode1);
            BlockNodes.Add(blockNode2);
        }

        private List<BlockNode> GetTwoEdgedThickenedNodes(Node node, float thickness)
        {
            //this only work for nodes, which has two edges
            if (node.Edges.Count != 2) 
                throw new ArgumentException("Parameter node doesn't have exactly two edges", nameof(node));
            
            //Calculate
            float averageRad1 = AverageRadianFromTwoEdges(node.Edges[0], node.Edges[1], node);
            float radianDiff1 = RadianDifferenceFromTwoEdges(node.Edges[0], node.Edges[1], node);

            float averageRad2 = averageRad1 + Mathf.PI;
            float radianDiff2 = 2f * Mathf.PI - radianDiff1;

            Vector2 vec1 = new Vector2(
                Mathf.Cos(averageRad1) * thickness * (1f / Mathf.Sin(radianDiff1 / 2)),
                Mathf.Sin(averageRad1) * thickness * (1f / Mathf.Sin(radianDiff1 / 2)));
            Vector2 vec2 = new Vector2(
                Mathf.Cos(averageRad2) * thickness * (1f / Mathf.Sin(radianDiff2 / 2)),
                Mathf.Sin(averageRad2) * thickness * (1f / Mathf.Sin(radianDiff2 / 2)));


            //Then store it
            BlockNode blockNode1 = new BlockNode(node.X + vec1.x, node.Y + vec1.y);
            BlockNode blockNode2 = new BlockNode(node.X + vec2.x, node.Y + vec2.y);

            var returnList = new List<BlockNode>();
            returnList.Add(blockNode1);
            returnList.Add(blockNode2);

            return returnList;
        }

        private void ThreeEdgedThickening(Node node, float thickness)
        {
            //this only work for nodes, which has three edges
            if (node.Edges.Count != 3) 
                throw new ArgumentException("Parameter node doesn't have exactly three edges", nameof(node));
            
            bool majorWithMinor = false;

            //First Calculate
            Edge edge1 = node.Edges[0];
            Edge edge2 = node.Edges[1];
            Edge edge3 = node.Edges[2];

            if (!(
                    graph.MajorEdges.Contains(edge1) &&
                    graph.MajorEdges.Contains(edge2) &&
                    graph.MajorEdges.Contains(edge3))
                && !(
                    graph.MinorEdges.Contains(edge1) &&
                    graph.MinorEdges.Contains(edge2) &&
                    graph.MinorEdges.Contains(edge3)))
            {
                majorWithMinor = true;
            }
            
            float averageRad12 = AverageRadianFromTwoEdges(edge1, edge2, node);
            float averageRad13 = AverageRadianFromTwoEdges(edge1, edge3, node);
            float averageRad23 = AverageRadianFromTwoEdges(edge2, edge3, node);

            float radianDiff12 = RadianDifferenceFromTwoEdges(edge1, edge2, node);
            float radianDiff13 = RadianDifferenceFromTwoEdges(edge1, edge3, node);
            float radianDiff23 = RadianDifferenceFromTwoEdges(edge2, edge3, node);
            
            if (radianDiff12 > Mathf.PI / 2 && !CorrectThreeEdgedAverage(averageRad12, edge3, node))
                averageRad12 += Mathf.PI;
            
            if (radianDiff13 > Mathf.PI / 2 && !CorrectThreeEdgedAverage(averageRad13, edge2, node))
                averageRad13 += Mathf.PI;
            
            if (radianDiff23 > Mathf.PI / 2 && !CorrectThreeEdgedAverage(averageRad23, edge1, node))
                averageRad23 += Mathf.PI;

            Vector2 vec12;
            Vector2 vec13;
            Vector2 vec23;

            //If one of the crossing is a Major-Minor crossing, make the vectors in a different way.
            if ((graph.MajorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge2))
                || (graph.MajorEdges.Contains(edge2) && graph.MinorEdges.Contains(edge1)))
            {
                vec12 = MajorMinorCrossingThickening(edge1, edge2, node, radianDiff12);
            }
            else
            {
                
                if(majorWithMinor && graph.MinorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge2))
                {
                    vec12 = new Vector2(
                        Mathf.Cos(averageRad12) * thickness/2 * (1f / Mathf.Sin(radianDiff12 / 2)),
                        Mathf.Sin(averageRad12) * thickness/2 * (1f / Mathf.Sin(radianDiff12 / 2)));
                }
                else 
                {
                    vec12 = new Vector2(
                        Mathf.Cos(averageRad12) * thickness * (1f / Mathf.Sin(radianDiff12 / 2)),
                        Mathf.Sin(averageRad12) * thickness * (1f / Mathf.Sin(radianDiff12 / 2)));
                }
            }

            if ((graph.MajorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge3))
                || (graph.MajorEdges.Contains(edge3) && graph.MinorEdges.Contains(edge1)))
            {
                vec13 = MajorMinorCrossingThickening(edge1, edge3, node, radianDiff13);
            }
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge3))
                {
                    vec13 = new Vector2(
                        Mathf.Cos(averageRad13) * thickness/2 * (1f / Mathf.Sin(radianDiff13 / 2)),
                        Mathf.Sin(averageRad13) * thickness/2 * (1f / Mathf.Sin(radianDiff13 / 2)));
                }
                else {
                    vec13 = new Vector2(
                        Mathf.Cos(averageRad13) * thickness * (1f / Mathf.Sin(radianDiff13 / 2)),
                        Mathf.Sin(averageRad13) * thickness * (1f / Mathf.Sin(radianDiff13 / 2)));
                }
            }

            if ((graph.MajorEdges.Contains(edge2) && graph.MinorEdges.Contains(edge3))
                || (graph.MajorEdges.Contains(edge3) && graph.MinorEdges.Contains(edge2)))
            {
                vec23 = MajorMinorCrossingThickening(edge2, edge3, node, radianDiff23);
            }
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(edge2) && graph.MinorEdges.Contains(edge3))
                {
                    vec23 = new Vector2(
                        Mathf.Cos(averageRad23) * thickness/2 * (1f / Mathf.Sin(radianDiff23 / 2)),
                        Mathf.Sin(averageRad23) * thickness/2 * (1f / Mathf.Sin(radianDiff23 / 2)));
                }
                else
                {
                    vec23 = new Vector2(
                        Mathf.Cos(averageRad23) * thickness * (1f / Mathf.Sin(radianDiff23 / 2)),
                        Mathf.Sin(averageRad23) * thickness * (1f / Mathf.Sin(radianDiff23 / 2)));
                }
            }

            //Then store it
            BlockNode blockNode12 = new BlockNode(node.X + vec12.x, node.Y + vec12.y);
            BlockNode blockNode13 = new BlockNode(node.X + vec13.x, node.Y + vec13.y);
            BlockNode blockNode23 = new BlockNode(node.X + vec23.x, node.Y + vec23.y);

            blockNode12.Edges.Add(edge1);
            blockNode12.Edges.Add(edge2);
            blockNode13.Edges.Add(edge1);
            blockNode13.Edges.Add(edge3);
            blockNode23.Edges.Add(edge2);
            blockNode23.Edges.Add(edge3);

            node.BlockNodes.Add(blockNode12);
            node.BlockNodes.Add(blockNode13);
            node.BlockNodes.Add(blockNode23);

            BlockNodes.Add(blockNode12);
            BlockNodes.Add(blockNode13);
            BlockNodes.Add(blockNode23);
        }

        private void FourEdgedThickening(Node node, float thickness)
        {
            //this only work for nodes, which has four edges
            if (node.Edges.Count != 4) 
                throw new ArgumentException("Parameter node doesn't have exactly four edges", nameof(node));

            bool majorWithMinor = false;

            //FIRST CALCULATE
            Edge edge1 = node.Edges[0];
            Edge edge2 = node.Edges[1];
            Edge edge3 = node.Edges[2];
            Edge edge4 = node.Edges[3];

            if (!(
                    graph.MajorEdges.Contains(edge1) 
                    && graph.MajorEdges.Contains(edge2)
                    && graph.MajorEdges.Contains(edge3) 
                    && graph.MajorEdges.Contains(edge4))
                && !(
                    graph.MinorEdges.Contains(edge1)
                    && graph.MinorEdges.Contains(edge2)
                    && graph.MinorEdges.Contains(edge3)
                    && graph.MinorEdges.Contains(edge4)))
            {
                majorWithMinor = true;
            }


            //radians from node
            float radian1 = edge1.NodeA == node ? edge1.DirRadianFromA : edge1.DirRadianFromB;
            float radian2 = edge2.NodeA == node ? edge2.DirRadianFromA : edge2.DirRadianFromB;
            float radian3 = edge3.NodeA == node ? edge3.DirRadianFromA : edge3.DirRadianFromB;
            float radian4 = edge4.NodeA == node ? edge4.DirRadianFromA : edge4.DirRadianFromB;

            List<float> radians = new List<float> { radian1, radian2, radian3, radian4 };
            List<Edge> edges = new List<Edge> { edge1, edge2, edge3, edge4 };
            List<Edge> sortedEdges = new List<Edge>();

            for (int i = radians.Count - 1; i > - 1; i--) //Get the edges sorted by their radians
            {
                float smallest = radians.Min();
                int idx = radians.IndexOf(smallest);
                sortedEdges.Add(edges[idx]);
                
                edges.RemoveAt(idx);
                radians.RemoveAt(idx);
            }

            //After sorting, we can determine the role of every edge
            Edge baseEdge1 = sortedEdges[0];
            Edge baseEdge2 = sortedEdges[2];
            Edge commonEdge1 = sortedEdges[1];
            Edge commonEdge2 = sortedEdges[3];

            float averageRadBase1Common1 = AverageRadianFromTwoEdges(baseEdge1, commonEdge1, node);
            float averageRadBase1Common2 = AverageRadianFromTwoEdges(baseEdge1, commonEdge2, node);
            float averageRadBase2Common1 = AverageRadianFromTwoEdges(baseEdge2, commonEdge1, node);
            float averageRadBase2Common2 = AverageRadianFromTwoEdges(baseEdge2, commonEdge2, node);

            if (!CorrectFourEdgedAverage(
                    averageRadBase1Common1, baseEdge1, baseEdge2, commonEdge2, node))
                averageRadBase1Common1 += Mathf.PI;
            if (!CorrectFourEdgedAverage(
                    averageRadBase1Common2, baseEdge1, baseEdge2, commonEdge1, node))
                averageRadBase1Common2 += Mathf.PI;
            if (!CorrectFourEdgedAverage(
                    averageRadBase2Common1, baseEdge2, baseEdge1, commonEdge2, node))
                averageRadBase2Common1 += Mathf.PI;
            if (!CorrectFourEdgedAverage(
                    averageRadBase2Common2, baseEdge2, baseEdge1, commonEdge1, node))
                averageRadBase2Common2 += Mathf.PI;

            //4 final vectors to use for thickening
            Vector2 vecB1C1;
            Vector2 vecB1C2;
            Vector2 vecB2C1;
            Vector2 vecB2C2;

            float radianDiffB1C1 = RadianDifferenceFromTwoEdges(baseEdge1, commonEdge1, node);
            float radianDiffB1C2 = RadianDifferenceFromTwoEdges(baseEdge1, commonEdge2, node);
            float radianDiffB2C1 = RadianDifferenceFromTwoEdges(baseEdge2, commonEdge1, node);
            float radianDiffB2C2 = RadianDifferenceFromTwoEdges(baseEdge2, commonEdge2, node);

            //If one of the crossing is a Major-Minor crossing, make the vectors in a different way:
            if ((graph.MajorEdges.Contains(baseEdge1) && graph.MinorEdges.Contains(commonEdge1))
                || (graph.MajorEdges.Contains(commonEdge1) && graph.MinorEdges.Contains(baseEdge1)))
            {
                vecB1C1 = MajorMinorCrossingThickening(baseEdge1, commonEdge1, node, radianDiffB1C1);
            }
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(baseEdge1) && graph.MinorEdges.Contains(commonEdge1))
                {
                    vecB1C1 = new Vector2(
                         Mathf.Cos(averageRadBase1Common1) * thickness / 2 * (1f / Mathf.Sin(radianDiffB1C1 / 2)),
                         Mathf.Sin(averageRadBase1Common1) * thickness / 2 * (1f / Mathf.Sin(radianDiffB1C1 / 2)));
                    
                }            
                else {
                    vecB1C1 = new Vector2(
                        Mathf.Cos(averageRadBase1Common1) * thickness * (1f / Mathf.Sin(radianDiffB1C1 / 2)),
                        Mathf.Sin(averageRadBase1Common1) * thickness * (1f / Mathf.Sin(radianDiffB1C1 / 2)));
                    
                }
            }

            if ((graph.MajorEdges.Contains(baseEdge1) && graph.MinorEdges.Contains(commonEdge2))
                || (graph.MajorEdges.Contains(commonEdge2) && graph.MinorEdges.Contains(baseEdge1)))
            {
                vecB1C2 = MajorMinorCrossingThickening(baseEdge1, commonEdge2, node, radianDiffB1C2);
            }
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(baseEdge1) && graph.MinorEdges.Contains(commonEdge2))
                {
                    vecB1C2 = new Vector2(
                        Mathf.Cos(averageRadBase1Common2) * thickness/2 * (1f / Mathf.Sin(radianDiffB1C2 / 2)),
                        Mathf.Sin(averageRadBase1Common2) * thickness/2 * (1f / Mathf.Sin(radianDiffB1C2 / 2)));
                }
                else
                {
                    vecB1C2 = new Vector2(
                        Mathf.Cos(averageRadBase1Common2) * thickness * (1f / Mathf.Sin(radianDiffB1C2 / 2)),
                        Mathf.Sin(averageRadBase1Common2) * thickness * (1f / Mathf.Sin(radianDiffB1C2 / 2)));
                }
            }

            if ((graph.MajorEdges.Contains(baseEdge2) && graph.MinorEdges.Contains(commonEdge1))
                || (graph.MajorEdges.Contains(commonEdge1) && graph.MinorEdges.Contains(baseEdge2)))
            {
                vecB2C1 = MajorMinorCrossingThickening(baseEdge2, commonEdge1, node, radianDiffB2C1);
            }
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(baseEdge2) && graph.MinorEdges.Contains(commonEdge1))
                {
                    vecB2C1 = new Vector2(
                        Mathf.Cos(averageRadBase2Common1) * thickness/2 * (1f / Mathf.Sin(radianDiffB2C1 / 2)),
                        Mathf.Sin(averageRadBase2Common1) * thickness/2 * (1f / Mathf.Sin(radianDiffB2C1 / 2)));
                }
                else {
                    vecB2C1 = new Vector2(
                        Mathf.Cos(averageRadBase2Common1) * thickness * (1f / Mathf.Sin(radianDiffB2C1 / 2)),
                        Mathf.Sin(averageRadBase2Common1) * thickness * (1f / Mathf.Sin(radianDiffB2C1 / 2)));
                }
            }

            if ((graph.MajorEdges.Contains(baseEdge2) && graph.MinorEdges.Contains(commonEdge2))
                || (graph.MajorEdges.Contains(commonEdge2) && graph.MinorEdges.Contains(baseEdge2)))
            {
                vecB2C2 = MajorMinorCrossingThickening(baseEdge2, commonEdge2, node, radianDiffB2C2);
            }
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(baseEdge2) && graph.MinorEdges.Contains(commonEdge2))
                {
                    vecB2C2 = new Vector2(
                        Mathf.Cos(averageRadBase2Common2) * thickness/2 * (1f / Mathf.Sin(radianDiffB2C2 / 2)),
                        Mathf.Sin(averageRadBase2Common2) * thickness/2 * (1f / Mathf.Sin(radianDiffB2C2 / 2)));
                }
                else
                {
                    vecB2C2 = new Vector2(
                        Mathf.Cos(averageRadBase2Common2) * thickness * (1f / Mathf.Sin(radianDiffB2C2 / 2)),
                        Mathf.Sin(averageRadBase2Common2) * thickness * (1f / Mathf.Sin(radianDiffB2C2 / 2)));
                }
            }

            //THEN STORE IT
            BlockNode blockNodeB1C1 = new BlockNode(node.X + vecB1C1.x, node.Y + vecB1C1.y);
            BlockNode blockNodeB1C2 = new BlockNode(node.X + vecB1C2.x, node.Y + vecB1C2.y);
            BlockNode blockNodeB2C1 = new BlockNode(node.X + vecB2C1.x, node.Y + vecB2C1.y);
            BlockNode blockNodeB2C2 = new BlockNode(node.X + vecB2C2.x, node.Y + vecB2C2.y);

            blockNodeB1C1.Edges.Add(baseEdge1);
            blockNodeB1C1.Edges.Add(commonEdge1);
            blockNodeB1C2.Edges.Add(baseEdge1);
            blockNodeB1C2.Edges.Add(commonEdge2);
            blockNodeB2C1.Edges.Add(baseEdge2);
            blockNodeB2C1.Edges.Add(commonEdge1);
            blockNodeB2C2.Edges.Add(baseEdge2);
            blockNodeB2C2.Edges.Add(commonEdge2);

            node.BlockNodes.Add(blockNodeB1C1);
            node.BlockNodes.Add(blockNodeB1C2);
            node.BlockNodes.Add(blockNodeB2C1);
            node.BlockNodes.Add(blockNodeB2C2);

            BlockNodes.Add(blockNodeB1C1);
            BlockNodes.Add(blockNodeB1C2);
            BlockNodes.Add(blockNodeB2C1);
            BlockNodes.Add(blockNodeB2C2);
        }



        /**
         * BLOCK FORMING
         */

        //Recursive function, which forms a new Block from a starting BlockNode. Returns true if forming finished. Returns false if forming failed.
        private bool FormBlock(BlockNode node, Edge edgeToSearch, Block blockToBuild, int iteration)
        {
            if (blockToBuild.Nodes.Contains(node)) return true; //We got around

            if (node.Block != null) return false; //Node already has a Block
            if (iteration > 100) return false; //Recursive function iteration is too high

            blockToBuild.Nodes.Add(node); //First add current node to the Block
            node.Block = blockToBuild;

            Node nextNode = edgeToSearch.NodeA.BlockNodes.Contains(node) ? edgeToSearch.NodeB : edgeToSearch.NodeA;

            //Special case: One edged node
            if(node.Edges.Count == 1)
            {
                Node currentNode = edgeToSearch.NodeA.BlockNodes.Contains(node) ? edgeToSearch.NodeA : edgeToSearch.NodeB;
                BlockNode otherBlockNode = currentNode.BlockNodes[0] == node ? currentNode.BlockNodes[1] : currentNode.BlockNodes[0];

                if (!blockToBuild.Nodes.Contains(otherBlockNode))
                {
                    if (FormBlock(otherBlockNode, edgeToSearch, blockToBuild, iteration + 1)) //If it's the first blockNode, go for 2nd node, if it's the second, operate normally
                    {
                        return true;
                    }
                    else return false; 
                }
            }

            //Check which is the next blockNode
            BlockNode nextBlockNode = null; 
            int currentBlockOrientationToEdge = Orientation(edgeToSearch, node);
            foreach(BlockNode blockNode in nextNode.BlockNodes)
            {
                if (blockNode.Edges.Contains(edgeToSearch))
                {
                    if (Orientation(edgeToSearch, blockNode) == currentBlockOrientationToEdge) nextBlockNode = blockNode;
                }
            }

            if(nextBlockNode == null) //Next BlockNode not found, forming failed
            {
                return false;
            }

            Edge nextEdgeToSearch;

            if (nextBlockNode.Edges.Count == 1) //Special case: One edged node
            {
                nextEdgeToSearch = edgeToSearch;
            }
            else
            {
                nextEdgeToSearch = nextBlockNode.Edges[0] == edgeToSearch ? nextBlockNode.Edges[1] : nextBlockNode.Edges[0];
            }

            //Recursive call to the next node
            if (FormBlock(nextBlockNode, nextEdgeToSearch, blockToBuild, iteration + 1)) return true;
            else return false;
        }


        
        /**
         * BlOCK THINNING
         */

        private Block GetTinnedBlock(Block baseBlock, float sideWalkThickness)
        {
            var blockDelegate1 = new Block();
            var blockDelegate2 = new Block();

            for (int i = 0; i < baseBlock.Nodes.Count; i++)
            {
                var currentNode = baseBlock.Nodes[i].GetNodeForm();
                var nextNode = (i == baseBlock.Nodes.Count - 1) 
                    ? baseBlock.Nodes[0].GetNodeForm() 
                    : baseBlock.Nodes[i + 1].GetNodeForm();
                var lastNode = (i == 0)
                    ? baseBlock.Nodes[baseBlock.Nodes.Count - 1].GetNodeForm()
                    : baseBlock.Nodes[i - 1].GetNodeForm();
                
                var currentEdge = new Edge(currentNode, nextNode);
                var lastEdge = new Edge(lastNode, currentNode);
                
                currentNode.Edges.Clear();
                currentNode.Edges.Add(lastEdge);
                currentNode.Edges.Add(currentEdge);

                var newBlockNodes = GetTwoEdgedThickenedNodes(currentNode, sideWalkThickness);
                var newBlockNode1 = newBlockNodes[0];
                var newBlockNode2 = newBlockNodes[1];

                if (Orientation(currentEdge, newBlockNode1) > 0)
                {
                    blockDelegate1.Nodes.Add(newBlockNode1);
                    blockDelegate2.Nodes.Add(newBlockNode2);
                }
                else
                {
                    blockDelegate2.Nodes.Add(newBlockNode1);
                    blockDelegate1.Nodes.Add(newBlockNode2);
                }
            }

            var boundingRect1 = BoundingService.GetMinBoundingRectangle(blockDelegate1);
            var boundingRect2 = BoundingService.GetMinBoundingRectangle(blockDelegate2);

            return boundingRect1.GetArea() > boundingRect2.GetArea() ? blockDelegate2 : blockDelegate1;
        }
        


        /**
         * HELPER FUNCTIONS
         */

        //Returns an average radian between two given edge, which has the given common node
        private float AverageRadianFromTwoEdges(Edge edge1, Edge edge2, Node node)
        {
            if (!node.Edges.Contains(edge1) || !node.Edges.Contains(edge2)) throw new ArgumentException("Parameter node doesn't connect the two edges", nameof(node));

            float dirRad1;
            float dirRad2;

            if (edge1.NodeA == node) dirRad1 = edge1.DirRadianFromA;
            else dirRad1 = edge1.DirRadianFromB;

            if (edge2.NodeA == node) dirRad2 = edge2.DirRadianFromA;
            else dirRad2 = edge2.DirRadianFromB;

            if(RadianDifference((dirRad1 + dirRad2) / 2, dirRad2) > RadianDifference(((dirRad1 + dirRad2) / 2) + Mathf.PI, dirRad2)) return ((dirRad1 + dirRad2) / 2 + Mathf.PI);
            return (dirRad1 + dirRad2) / 2;
        }

        private Vector2 MajorMinorCrossingThickening(Edge edge1, Edge edge2, Node node, float radianDiff)
        {
            float dirRad1;
            float dirRad2;

            if (edge1.NodeA == node) dirRad1 = edge1.DirRadianFromA;
            else dirRad1 = edge1.DirRadianFromB;

            if (edge2.NodeA == node) dirRad2 = edge2.DirRadianFromA;
            else dirRad2 = edge2.DirRadianFromB;

            if (graph.MajorEdges.Contains(edge2)) //Edge2 is the MajorEdge
            {
                Vector2 edge2Vec = new Vector2(Mathf.Cos(dirRad2) * minorRoadThickness * (1f / Mathf.Sin(radianDiff)), Mathf.Sin(dirRad2) * minorRoadThickness * (1f / Mathf.Sin(radianDiff)));
                Vector2 edge1Vec = new Vector2(Mathf.Cos(dirRad1) * majorRoadThickness * (1f / Mathf.Sin(radianDiff)), Mathf.Sin(dirRad1) * majorRoadThickness * (1f / Mathf.Sin(radianDiff)));
                return edge1Vec + edge2Vec;
            }
            else //Edge1 is the MajorEdge
            {
                Vector2 edge1Vec = new Vector2(Mathf.Cos(dirRad1) * minorRoadThickness * (1f / Mathf.Sin(radianDiff)), Mathf.Sin(dirRad1) * minorRoadThickness * (1f / Mathf.Sin(radianDiff)));
                Vector2 edge2Vec = new Vector2(Mathf.Cos(dirRad2) * majorRoadThickness * (1f / Mathf.Sin(radianDiff)), Mathf.Sin(dirRad2) * majorRoadThickness * (1f / Mathf.Sin(radianDiff)));
                return edge1Vec + edge2Vec;
            }
        }

        //Returns the radian difference between the two given edge, which has the given common node
        private float RadianDifferenceFromTwoEdges(Edge edge1, Edge edge2, Node node)
        {
            if (!node.Edges.Contains(edge1) || !node.Edges.Contains(edge2)) throw new ArgumentException("Parameter node doesn't connect the two edges", nameof(node));

            float dirRad1;
            float dirRad2;

            if (edge1.NodeA == node) dirRad1 = edge1.DirRadianFromA;
            else dirRad1 = edge1.DirRadianFromB;

            if (edge2.NodeA == node) dirRad2 = edge2.DirRadianFromA;
            else dirRad2 = edge2.DirRadianFromB;

            return RadianDifference(dirRad1, dirRad2);
        }

        //This function check the average radiant in three edged thickening, and if it's not correct returns a false
        private bool CorrectThreeEdgedAverage(float average, Edge edgeNotIncluded, Node node)
        {
            float radiantFromNode;
            if (edgeNotIncluded.NodeA == node) radiantFromNode = edgeNotIncluded.DirRadianFromA;
            else radiantFromNode = edgeNotIncluded.DirRadianFromB;

            if (RadianDifference(radiantFromNode, average) < RadianDifference(radiantFromNode, average + Mathf.PI)) return false;
            return true;
        }

        //This function check the average radiant in four edged thickening, and if it's not correct returns a false
        private bool CorrectFourEdgedAverage(float average, Edge edgeIncluded,  Edge edgeNotIncluded1, Edge edgeNotIncluded2, Node node)
        {
            float radianFromNode1;
            if (edgeNotIncluded1.NodeA == node) radianFromNode1 = edgeNotIncluded1.DirRadianFromA;
            else radianFromNode1 = edgeNotIncluded1.DirRadianFromB;

            float radianFromNode2;
            if (edgeNotIncluded2.NodeA == node) radianFromNode2 = edgeNotIncluded2.DirRadianFromA;
            else radianFromNode2 = edgeNotIncluded2.DirRadianFromB;

            float radianFromIncludedNode;
            if (edgeIncluded.NodeA == node) radianFromIncludedNode = edgeIncluded.DirRadianFromA;
            else radianFromIncludedNode = edgeIncluded.DirRadianFromB;

            if ((RadianDifference(average, radianFromIncludedNode) > RadianDifference(average, radianFromNode1)) || (RadianDifference(average, radianFromIncludedNode) > RadianDifference(average, radianFromNode2))) return false;
       
            return true;
        }

        //Takes two radiant, returns the true difference
        private float RadianDifference(float rad1, float rad2)
        {
            //Convert the radians between PI and -PI
            while (rad1 < -Mathf.PI) rad1 += Mathf.PI * 2;
            while (rad1 > Mathf.PI) rad1 -= Mathf.PI * 2;
            while (rad2 < -Mathf.PI) rad2 += Mathf.PI * 2;
            while (rad2 > Mathf.PI) rad2 -= Mathf.PI * 2;

            if (rad1 < rad2) //first make sure rad1 is bigger than rad2
            {
                (rad2, rad1) = (rad1, rad2);
            }

            //then check if difference is smaller, if we add 2 * PI to rad2, if yes, return that
            if ((rad2 + 2 * Mathf.PI - rad1) < (rad1 - rad2)) return rad2 + 2 * Mathf.PI - rad1;
            else return rad1 - rad2;
        }

        //This function checks the orientation of a BlockNode compared to and edge
        private int Orientation(Edge baseEdge, BlockNode testNode) 
        {
            //Orientation can be calculated with the cross product of two Vectors made from the 3 Nodes
            float val = (baseEdge.NodeA.Y - testNode.Y) * (baseEdge.NodeB.X - testNode.X) - (baseEdge.NodeA.X - testNode.X) * (baseEdge.NodeB.Y - testNode.Y);

            if (val > 0.00001f) return 1; //clockwise
            if (val < -0.00001f) return -1; //anticlockwise
            else
            {
                Debug.Log("BlockNode Collinear to it's Edge");
                return 0; //collinear, shouldn't really happen with BlockNodes that uses this edge
            }
        }
    }
}
