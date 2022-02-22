using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    class LotGenerator
    {
        private readonly Graph graph;
        public List<LotNode> LotNodes { get; private set; }
        public List<Lot> Lots { get; private set; }

        private float majorRoadThickness;
        private float minorRoadThickness;

        public LotGenerator(Graph graphToUse, float majorThickness, float minorThickness)
        {
            graph = graphToUse;
            LotNodes = new List<LotNode>();
            Lots = new List<Lot>();

            majorRoadThickness = majorThickness;
            minorRoadThickness = minorThickness;
        }

        public void Generate()
        {
            ThickenNodes();
            CreateLots();
            DeleteMapEdgeLots();
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
                else if(node.Edges.Count > 4) throw new ArgumentException("Node has more than four edges", nameof(node));
                else throw new ArgumentException("Node has no edges", nameof(node));
            }

            //Then Thicken minor nodes
            foreach (Node node in graph.MinorNodes)
            {
                if (node.Edges.Count == 1) OneEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 2) TwoEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 3) ThreeEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 4) FourEdgedThickening(node, minorRoadThickness);                
                else if (node.Edges.Count > 4)  throw new ArgumentException("Node has more than four edges", nameof(node));
                else throw new ArgumentException("Node has no edges", nameof(node));
            }
        }

        //Connects the LotNodes into one Lot
        private void CreateLots()
        {
            foreach(LotNode lotNode in LotNodes)
            {
                if(lotNode.Lot == null)
                {
                    Lot newLot = new Lot();
                    Lots.Add(newLot);
                    if (!FormLot(lotNode, lotNode.Edges[0], newLot, 1))
                    {
                        Lots.Remove(newLot); //If the new lot is corrupt, remove it
                    }
                }
            }
        }

        //Delete Lots which are in the edge of the map, or not formed correctly
        private void DeleteMapEdgeLots()
        {
            List<Lot> removable = new List<Lot>();

            foreach(Lot lot in Lots)
            {
                //First Check if the First and Last node has the same edge.
                bool sameEdgeFound = false;
                foreach (Edge edge1 in lot.Nodes[0].Edges)
                {
                    foreach(Edge edge2 in lot.Nodes[lot.Nodes.Count - 1].Edges)
                    {
                        if (edge1 == edge2) sameEdgeFound = true;
                    }
                }

                if (!sameEdgeFound) removable.Add(lot);

                //We also remove Lots, which doesn't have at least 3 nodes
                else if (lot.Nodes.Count <= 2) removable.Add(lot);

                else //There are also Edge Lots which first and last LotNode is in the same Node
                {
                    Node firstNode = lot.Nodes[0].Edges[0].NodeA.LotNodes.Contains(lot.Nodes[0]) ? lot.Nodes[0].Edges[0].NodeA : lot.Nodes[0].Edges[0].NodeB;
                    Node lastNode = lot.Nodes[lot.Nodes.Count - 1].Edges[0].NodeA.LotNodes.Contains(lot.Nodes[lot.Nodes.Count - 1]) ? lot.Nodes[lot.Nodes.Count - 1].Edges[0].NodeA : lot.Nodes[lot.Nodes.Count - 1].Edges[0].NodeB;

                    if (firstNode == lastNode) removable.Add(lot);
                }
            }

            foreach(Lot lot in removable)
            {
                Lots.Remove(lot);
            }
        }



        /**
         * THICKENER FUNCTIONS
         */

        private void OneEdgedThickening(Node node, float thickness)
        {
            if (node.Edges.Count != 1) throw new ArgumentException("Parameter node doesn't have exactly one edges", nameof(node)); //this only work for nodes, which has only one edge

            //Calculate
            float dirRadian;
            if (node.Edges[0].NodeA == node) dirRadian = node.Edges[0].DirRadianFromA;
            else dirRadian = node.Edges[0].DirRadianFromB;

            Vector2 leftForward = new Vector2(Mathf.Cos(dirRadian + 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4), Mathf.Sin(dirRadian + 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4));
            Vector2 rightForward = new Vector2(Mathf.Cos(dirRadian - 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4), Mathf.Sin(dirRadian - 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4));


            //Then store it
            LotNode lotNode1 = new LotNode(node.X + leftForward.x, node.Y + leftForward.y);
            LotNode lotNode2 = new LotNode(node.X + rightForward.x, node.Y + rightForward.y);

            lotNode1.Edges.Add(node.Edges[0]);
            lotNode2.Edges.Add(node.Edges[0]);

            node.LotNodes.Add(lotNode1);
            node.LotNodes.Add(lotNode2);

            LotNodes.Add(lotNode1);
            LotNodes.Add(lotNode2);

            return;
        }

        private void TwoEdgedThickening(Node node, float thickness)
        {
            if (node.Edges.Count != 2) throw new ArgumentException("Parameter node doesn't have exactly two edges", nameof(node)); //this only work for nodes, which has two edges

            //Calculate
            float avarageRad1 = AvarageRadianFromTwoEdges(node.Edges[0], node.Edges[1], node);
            float radianDiff1 = RadianDifferenceFromTwoEdges(node.Edges[0], node.Edges[1], node);

            float avarageRad2 = avarageRad1 + Mathf.PI;
            float radianDiff2 = 2f * Mathf.PI - radianDiff1;

            Vector2 vec1 = new Vector2(Mathf.Cos(avarageRad1) * thickness * (1f / Mathf.Sin(radianDiff1 / 2)), Mathf.Sin(avarageRad1) * thickness * (1f / Mathf.Sin(radianDiff1 / 2)));
            Vector2 vec2 = new Vector2(Mathf.Cos(avarageRad2) * thickness * (1f / Mathf.Sin(radianDiff2 / 2)), Mathf.Sin(avarageRad2) * thickness * (1f / Mathf.Sin(radianDiff2 / 2)));


            //Then store it
            LotNode lotNode1 = new LotNode(node.X + vec1.x, node.Y + vec1.y);
            LotNode lotNode2 = new LotNode(node.X + vec2.x, node.Y + vec2.y);

            lotNode1.Edges.Add(node.Edges[0]);
            lotNode1.Edges.Add(node.Edges[1]);
            lotNode2.Edges.Add(node.Edges[0]);
            lotNode2.Edges.Add(node.Edges[1]);

            node.LotNodes.Add(lotNode1);
            node.LotNodes.Add(lotNode2);

            LotNodes.Add(lotNode1);
            LotNodes.Add(lotNode2);

            return;
        }

        private void ThreeEdgedThickening(Node node, float thickness)
        {
            if (node.Edges.Count != 3) throw new ArgumentException("Parameter node doesn't have exactly three edges", nameof(node)); //this only work for nodes, which has three edges
            
            bool majorWithMinor = false;

            //First Calculate
            Edge edge1 = node.Edges[0];
            Edge edge2 = node.Edges[1];
            Edge edge3 = node.Edges[2];

            if (!(graph.MajorEdges.Contains(edge1) && graph.MajorEdges.Contains(edge2) && graph.MajorEdges.Contains(edge3)) && !(graph.MinorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge2) && graph.MinorEdges.Contains(edge3))) majorWithMinor = true;

            float avarageRad12 = AvarageRadianFromTwoEdges(edge1, edge2, node);
            float avarageRad13 = AvarageRadianFromTwoEdges(edge1, edge3, node);
            float avarageRad23 = AvarageRadianFromTwoEdges(edge2, edge3, node);

            float radianDiff12 = RadianDifferenceFromTwoEdges(edge1, edge2, node);
            float radianDiff13 = RadianDifferenceFromTwoEdges(edge1, edge3, node);
            float radianDiff23 = RadianDifferenceFromTwoEdges(edge2, edge3, node);

            if (radianDiff12 > Mathf.PI / 2)
            {
                if (!CorrectThreeEdgedAvarage(avarageRad12, edge3, node)) avarageRad12 += Mathf.PI;
            }
            if (radianDiff13 > Mathf.PI / 2)
            {
                if (!CorrectThreeEdgedAvarage(avarageRad13, edge2, node)) avarageRad13 += Mathf.PI;
            }
            if (radianDiff23 > Mathf.PI / 2)
            {
                if (!CorrectThreeEdgedAvarage(avarageRad23, edge1, node)) avarageRad23 += Mathf.PI;
            }

            Vector2 vec12;
            Vector2 vec13;
            Vector2 vec23;

            //If one of the crossing is a Major-Minor crossing, make the vectors in a different way.
            if ((graph.MajorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge2)) || (graph.MajorEdges.Contains(edge2) && graph.MinorEdges.Contains(edge1))) vec12 = MajorMinorCrossingThickening(edge1, edge2, node, radianDiff12);
            else
            {
                
                if(majorWithMinor && graph.MinorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge2))
                {
                    vec12 = new Vector2(Mathf.Cos(avarageRad12) * thickness/2 * (1f / Mathf.Sin(radianDiff12 / 2)), Mathf.Sin(avarageRad12) * thickness/2 * (1f / Mathf.Sin(radianDiff12 / 2)));
                }
                else vec12 = new Vector2(Mathf.Cos(avarageRad12) * thickness * (1f / Mathf.Sin(radianDiff12 / 2)), Mathf.Sin(avarageRad12) * thickness * (1f / Mathf.Sin(radianDiff12 / 2)));
            }

            if ((graph.MajorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge3)) || (graph.MajorEdges.Contains(edge3) && graph.MinorEdges.Contains(edge1))) vec13 = MajorMinorCrossingThickening(edge1, edge3, node, radianDiff13);
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge3))
                {
                    vec13 = new Vector2(Mathf.Cos(avarageRad13) * thickness/2 * (1f / Mathf.Sin(radianDiff13 / 2)), Mathf.Sin(avarageRad13) * thickness/2 * (1f / Mathf.Sin(radianDiff13 / 2)));
                }
                else vec13 = new Vector2(Mathf.Cos(avarageRad13) * thickness * (1f / Mathf.Sin(radianDiff13 / 2)), Mathf.Sin(avarageRad13) * thickness * (1f / Mathf.Sin(radianDiff13 / 2)));
            }

            if ((graph.MajorEdges.Contains(edge2) && graph.MinorEdges.Contains(edge3)) || (graph.MajorEdges.Contains(edge3) && graph.MinorEdges.Contains(edge2))) vec23 = MajorMinorCrossingThickening(edge2, edge3, node, radianDiff23);
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(edge2) && graph.MinorEdges.Contains(edge3))
                {
                    vec23 = new Vector2(Mathf.Cos(avarageRad23) * thickness/2 * (1f / Mathf.Sin(radianDiff23 / 2)), Mathf.Sin(avarageRad23) * thickness/2 * (1f / Mathf.Sin(radianDiff23 / 2)));
                }
                else vec23 = new Vector2(Mathf.Cos(avarageRad23) * thickness * (1f / Mathf.Sin(radianDiff23 / 2)), Mathf.Sin(avarageRad23) * thickness * (1f / Mathf.Sin(radianDiff23 / 2)));
            }

            //Then store it
            LotNode lotNode12 = new LotNode(node.X + vec12.x, node.Y + vec12.y);
            LotNode lotNode13 = new LotNode(node.X + vec13.x, node.Y + vec13.y);
            LotNode lotNode23 = new LotNode(node.X + vec23.x, node.Y + vec23.y);

            lotNode12.Edges.Add(edge1);
            lotNode12.Edges.Add(edge2);
            lotNode13.Edges.Add(edge1);
            lotNode13.Edges.Add(edge3);
            lotNode23.Edges.Add(edge2);
            lotNode23.Edges.Add(edge3);

            node.LotNodes.Add(lotNode12);
            node.LotNodes.Add(lotNode13);
            node.LotNodes.Add(lotNode23);

            LotNodes.Add(lotNode12);
            LotNodes.Add(lotNode13);
            LotNodes.Add(lotNode23);

            return;
        }

        private void FourEdgedThickening(Node node, float thickness)
        {
            if (node.Edges.Count != 4) throw new ArgumentException("Parameter node doesn't have exactly four edges", nameof(node)); //this only work for nodes, which has four edges

            bool majorWithMinor = false;

            //FIRST CALCULATE
            Edge edge1 = node.Edges[0];
            Edge edge2 = node.Edges[1];
            Edge edge3 = node.Edges[2];
            Edge edge4 = node.Edges[3];

            if (!(graph.MajorEdges.Contains(edge1) && graph.MajorEdges.Contains(edge2) && graph.MajorEdges.Contains(edge3) && graph.MajorEdges.Contains(edge4)) && 
                !(graph.MinorEdges.Contains(edge1) && graph.MinorEdges.Contains(edge2) && graph.MinorEdges.Contains(edge3) && graph.MinorEdges.Contains(edge4))) majorWithMinor = true;

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

            float avarageRadBase1Common1 = AvarageRadianFromTwoEdges(baseEdge1, commonEdge1, node);
            float avarageRadBase1Common2 = AvarageRadianFromTwoEdges(baseEdge1, commonEdge2, node);
            float avarageRadBase2Common1 = AvarageRadianFromTwoEdges(baseEdge2, commonEdge1, node);
            float avarageRadBase2Common2 = AvarageRadianFromTwoEdges(baseEdge2, commonEdge2, node);

            if (!CorrectFourEdgedAvarage(avarageRadBase1Common1, baseEdge1, baseEdge2, commonEdge2, node)) avarageRadBase1Common1 += Mathf.PI;
            if (!CorrectFourEdgedAvarage(avarageRadBase1Common2, baseEdge1, baseEdge2, commonEdge1, node)) avarageRadBase1Common2 += Mathf.PI;
            if (!CorrectFourEdgedAvarage(avarageRadBase2Common1, baseEdge2, baseEdge1, commonEdge2, node)) avarageRadBase2Common1 += Mathf.PI;
            if (!CorrectFourEdgedAvarage(avarageRadBase2Common2, baseEdge2, baseEdge1, commonEdge1, node)) avarageRadBase2Common2 += Mathf.PI;

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
            if ((graph.MajorEdges.Contains(baseEdge1) && graph.MinorEdges.Contains(commonEdge1)) || (graph.MajorEdges.Contains(commonEdge1) && graph.MinorEdges.Contains(baseEdge1)))
                vecB1C1 = MajorMinorCrossingThickening(baseEdge1, commonEdge1, node, radianDiffB1C1);
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(baseEdge1) && graph.MinorEdges.Contains(commonEdge1))
                     vecB1C1 = new Vector2(Mathf.Cos(avarageRadBase1Common1) * thickness / 2 * (1f / Mathf.Sin(radianDiffB1C1 / 2)), Mathf.Sin(avarageRadBase1Common1) * thickness / 2 * (1f / Mathf.Sin(radianDiffB1C1 / 2)));            
                else vecB1C1 = new Vector2(Mathf.Cos(avarageRadBase1Common1) * thickness * (1f / Mathf.Sin(radianDiffB1C1 / 2)), Mathf.Sin(avarageRadBase1Common1) * thickness * (1f / Mathf.Sin(radianDiffB1C1 / 2)));
            }

            if ((graph.MajorEdges.Contains(baseEdge1) && graph.MinorEdges.Contains(commonEdge2)) || (graph.MajorEdges.Contains(commonEdge2) && graph.MinorEdges.Contains(baseEdge1)))
                vecB1C2 = MajorMinorCrossingThickening(baseEdge1, commonEdge2, node, radianDiffB1C2);
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(baseEdge1) && graph.MinorEdges.Contains(commonEdge2))
                     vecB1C2 = new Vector2(Mathf.Cos(avarageRadBase1Common2) * thickness/2 * (1f / Mathf.Sin(radianDiffB1C2 / 2)), Mathf.Sin(avarageRadBase1Common2) * thickness/2 * (1f / Mathf.Sin(radianDiffB1C2 / 2)));
                else vecB1C2 = new Vector2(Mathf.Cos(avarageRadBase1Common2) * thickness * (1f / Mathf.Sin(radianDiffB1C2 / 2)), Mathf.Sin(avarageRadBase1Common2) * thickness * (1f / Mathf.Sin(radianDiffB1C2 / 2)));
            }

            if ((graph.MajorEdges.Contains(baseEdge2) && graph.MinorEdges.Contains(commonEdge1)) || (graph.MajorEdges.Contains(commonEdge1) && graph.MinorEdges.Contains(baseEdge2)))
                vecB2C1 = MajorMinorCrossingThickening(baseEdge2, commonEdge1, node, radianDiffB2C1);
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(baseEdge2) && graph.MinorEdges.Contains(commonEdge1))
                     vecB2C1 = new Vector2(Mathf.Cos(avarageRadBase2Common1) * thickness/2 * (1f / Mathf.Sin(radianDiffB2C1 / 2)), Mathf.Sin(avarageRadBase2Common1) * thickness/2 * (1f / Mathf.Sin(radianDiffB2C1 / 2)));
                else vecB2C1 = new Vector2(Mathf.Cos(avarageRadBase2Common1) * thickness * (1f / Mathf.Sin(radianDiffB2C1 / 2)), Mathf.Sin(avarageRadBase2Common1) * thickness * (1f / Mathf.Sin(radianDiffB2C1 / 2)));
            }

            if ((graph.MajorEdges.Contains(baseEdge2) && graph.MinorEdges.Contains(commonEdge2)) || (graph.MajorEdges.Contains(commonEdge2) && graph.MinorEdges.Contains(baseEdge2)))
                vecB2C2 = MajorMinorCrossingThickening(baseEdge2, commonEdge2, node, radianDiffB2C2);
            else
            {
                if (majorWithMinor && graph.MinorEdges.Contains(baseEdge2) && graph.MinorEdges.Contains(commonEdge2))
                     vecB2C2 = new Vector2(Mathf.Cos(avarageRadBase2Common2) * thickness/2 * (1f / Mathf.Sin(radianDiffB2C2 / 2)), Mathf.Sin(avarageRadBase2Common2) * thickness/2 * (1f / Mathf.Sin(radianDiffB2C2 / 2)));
                else vecB2C2 = new Vector2(Mathf.Cos(avarageRadBase2Common2) * thickness * (1f / Mathf.Sin(radianDiffB2C2 / 2)), Mathf.Sin(avarageRadBase2Common2) * thickness * (1f / Mathf.Sin(radianDiffB2C2 / 2)));
            }

            //THEN STORE IT
            LotNode lotNodeB1C1 = new LotNode(node.X + vecB1C1.x, node.Y + vecB1C1.y);
            LotNode lotNodeB1C2 = new LotNode(node.X + vecB1C2.x, node.Y + vecB1C2.y);
            LotNode lotNodeB2C1 = new LotNode(node.X + vecB2C1.x, node.Y + vecB2C1.y);
            LotNode lotNodeB2C2 = new LotNode(node.X + vecB2C2.x, node.Y + vecB2C2.y);

            lotNodeB1C1.Edges.Add(baseEdge1);
            lotNodeB1C1.Edges.Add(commonEdge1);
            lotNodeB1C2.Edges.Add(baseEdge1);
            lotNodeB1C2.Edges.Add(commonEdge2);
            lotNodeB2C1.Edges.Add(baseEdge2);
            lotNodeB2C1.Edges.Add(commonEdge1);
            lotNodeB2C2.Edges.Add(baseEdge2);
            lotNodeB2C2.Edges.Add(commonEdge2);

            node.LotNodes.Add(lotNodeB1C1);
            node.LotNodes.Add(lotNodeB1C2);
            node.LotNodes.Add(lotNodeB2C1);
            node.LotNodes.Add(lotNodeB2C2);

            LotNodes.Add(lotNodeB1C1);
            LotNodes.Add(lotNodeB1C2);
            LotNodes.Add(lotNodeB2C1);
            LotNodes.Add(lotNodeB2C2);

            return;
        }



        /**
         * LOT FORMING
         */

        //Recursive function, which forms a new Lot from a starting LotNode. Returns true if forming finished. Returns false if forming failed.
        private bool FormLot(LotNode node, Edge edgeToSearch, Lot lotToBuild, int iteration)
        {
            if (lotToBuild.Nodes.Contains(node)) return true; //We got around

            if (node.Lot != null) return false; //Node already has a Lot
            if (iteration > 100) return false; //Recursive function iteration is too high

            lotToBuild.Nodes.Add(node); //First add current node to the Lot
            node.Lot = lotToBuild;

            Node nextNode = edgeToSearch.NodeA.LotNodes.Contains(node) ? edgeToSearch.NodeB : edgeToSearch.NodeA;

            //Special case: One edged node
            if(node.Edges.Count == 1)
            {
                Node currentNode = edgeToSearch.NodeA.LotNodes.Contains(node) ? edgeToSearch.NodeA : edgeToSearch.NodeB;
                LotNode otherLotNode = currentNode.LotNodes[0] == node ? currentNode.LotNodes[1] : currentNode.LotNodes[0];

                if (!lotToBuild.Nodes.Contains(otherLotNode))
                {
                    if (FormLot(otherLotNode, edgeToSearch, lotToBuild, iteration + 1)) //If it's the first lotnode, go for 2nd node, if it's the second, operate normally
                    {
                        return true;
                    }
                    else return false; 
                }
            }

            //Check which is the next LotNode
            LotNode nextLotNode = null; 
            int currentLotOrientationToEdge = Orientation(edgeToSearch, node);
            foreach(LotNode lotNode in nextNode.LotNodes)
            {
                if (lotNode.Edges.Contains(edgeToSearch))
                {
                    if (Orientation(edgeToSearch, lotNode) == currentLotOrientationToEdge) nextLotNode = lotNode;
                }
            }

            if(nextLotNode == null) //Next LotNode not found, forming failed
            {
                return false;
            }

            Edge nextEdgeToSearch;

            if (nextLotNode.Edges.Count == 1) //Special case: One eged node
            {
                nextEdgeToSearch = edgeToSearch;
            }
            else
            {
                nextEdgeToSearch = nextLotNode.Edges[0] == edgeToSearch ? nextLotNode.Edges[1] : nextLotNode.Edges[0];
            }

            //Recursive call to the next node
            if (FormLot(nextLotNode, nextEdgeToSearch, lotToBuild, iteration + 1)) return true;
            else return false;
        }



        /**
         * HELPER FUNCTIONS
         */

        //Returns an avarage radian between two given edge, which has the given common node
        private float AvarageRadianFromTwoEdges(Edge edge1, Edge edge2, Node node)
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
                Vector2 edge2vec = new Vector2(Mathf.Cos(dirRad2) * minorRoadThickness * (1f / Mathf.Sin(radianDiff)), Mathf.Sin(dirRad2) * minorRoadThickness * (1f / Mathf.Sin(radianDiff)));
                Vector2 edge1vec = new Vector2(Mathf.Cos(dirRad1) * majorRoadThickness * (1f / Mathf.Sin(radianDiff)), Mathf.Sin(dirRad1) * majorRoadThickness * (1f / Mathf.Sin(radianDiff)));
                return edge1vec + edge2vec;
            }
            else //Edge1 is the MajorEdge
            {
                Vector2 edge1vec = new Vector2(Mathf.Cos(dirRad1) * minorRoadThickness * (1f / Mathf.Sin(radianDiff)), Mathf.Sin(dirRad1) * minorRoadThickness * (1f / Mathf.Sin(radianDiff)));
                Vector2 edge2vec = new Vector2(Mathf.Cos(dirRad2) * majorRoadThickness * (1f / Mathf.Sin(radianDiff)), Mathf.Sin(dirRad2) * majorRoadThickness * (1f / Mathf.Sin(radianDiff)));
                return edge1vec + edge2vec;
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

        //This function check the avarage radiant in three edged thickening, and if it's not correct returns a false
        private bool CorrectThreeEdgedAvarage(float avarage, Edge edgeNotIncluded, Node node)
        {
            float radiantFromNode;
            if (edgeNotIncluded.NodeA == node) radiantFromNode = edgeNotIncluded.DirRadianFromA;
            else radiantFromNode = edgeNotIncluded.DirRadianFromB;

            if (RadianDifference(radiantFromNode, avarage) < RadianDifference(radiantFromNode, avarage + Mathf.PI)) return false;
            return true;
        }

        //This function check the avarage radiant in four edged thickening, and if it's not correct returns a false
        private bool CorrectFourEdgedAvarage(float avarage, Edge edgeIncluded,  Edge edgeNotIncluded1, Edge edgeNotIncluded2, Node node)
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

            if ((RadianDifference(avarage, radianFromIncludedNode) > RadianDifference(avarage, radianFromNode1)) || (RadianDifference(avarage, radianFromIncludedNode) > RadianDifference(avarage, radianFromNode2))) return false;
       
            return true;
        }

        //Takes two radiant, returns the true difference
        private float RadianDifference(float rad1, float rad2)
        {
            //Convert the radiants between PI and -PI
            while (rad1 < -Mathf.PI) rad1 += Mathf.PI * 2;
            while (rad1 > Mathf.PI) rad1 -= Mathf.PI * 2;
            while (rad2 < -Mathf.PI) rad2 += Mathf.PI * 2;
            while (rad2 > Mathf.PI) rad2 -= Mathf.PI * 2;

            if (rad1 < rad2) //first make sure rad1 is bigger than rad2
            {
                float temp = rad2;
                rad2 = rad1;
                rad1 = temp;
            }

            //then check if difference is smaller, if we add 2 * PI to rad2, if yes, return that
            if ((rad2 + 2 * Mathf.PI - rad1) < (rad1 - rad2)) return rad2 + 2 * Mathf.PI - rad1;
            else return rad1 - rad2;
        }

        //This function checks the orientation of a LotNode compared to and edge
        private int Orientation(Edge BaseEdge, LotNode TestNode) 
        {
            //Oriantation can be calculated with the cross product of two Vectors made from the 3 Nodes
            float val = (BaseEdge.NodeA.Y - TestNode.Y) * (BaseEdge.NodeB.X - TestNode.X) - (BaseEdge.NodeA.X - TestNode.X) * (BaseEdge.NodeB.Y - TestNode.Y);

            if (val > 0.00001f) return 1; //clockwise
            if (val < -0.00001f) return -1; //anticlockwise
            else
            {
                Debug.Log("LotNode Collinear to it's Edge");
                return 0; //collinear, shouldn't really happen with LotNodes that uses this edge
            }
        }
    }
}
