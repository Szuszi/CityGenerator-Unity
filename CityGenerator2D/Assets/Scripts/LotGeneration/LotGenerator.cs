using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    class LotGenerator
    {
        private readonly Graph graph;
        public List<LotNode> LotNodes { get; private set; }

        private float majorRoadThickness;
        private float minorRoadThickness;

        public LotGenerator(Graph graphToUse, float majorThickness, float minorThickness)
        {
            graph = graphToUse;
            LotNodes = new List<LotNode>();

            majorRoadThickness = majorThickness;
            minorRoadThickness = minorThickness;
        }

        public void Generate()
        {
            ThickenNodes();
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
            }

            //Then Thicken minor nodes
            foreach (Node node in graph.MinorNodes)
            {
                if (node.Edges.Count == 1) OneEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 2) TwoEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 3) ThreeEdgedThickening(node, minorRoadThickness);
                else if (node.Edges.Count == 4) FourEdgedThickening(node, minorRoadThickness);                
                else if (node.Edges.Count > 4)  throw new ArgumentException("Node has more than four edges", nameof(node));
            }
        }



        //THICKENER FUNCTIONS

        private void OneEdgedThickening(Node node, float thickness)
        {
            if (node.Edges.Count != 1) throw new ArgumentException("Parameter node doesn't have exactly one edges", nameof(node)); //this only work for nodes, which has only one edge

            float dirRadian;
            if (node.Edges[0].NodeA == node) dirRadian = node.Edges[0].DirRadianFromA;
            else dirRadian = node.Edges[0].DirRadianFromB;

            Vector2 leftForward = new Vector2(Mathf.Cos(dirRadian + 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4), Mathf.Sin(dirRadian + 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4));
            Vector2 rightForward = new Vector2(Mathf.Cos(dirRadian - 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4), Mathf.Sin(dirRadian - 1.5f * Mathf.PI / 2) * thickness * 1f / Mathf.Sin(Mathf.PI / 4));

            LotNodes.Add(new LotNode(node.X + leftForward.x, node.Y + leftForward.y));
            LotNodes.Add(new LotNode(node.X + rightForward.x, node.Y + rightForward.y));

            return;
        }

        private void TwoEdgedThickening(Node node, float thickness)
        {
            if (node.Edges.Count != 2) throw new ArgumentException("Parameter node doesn't have exactly two edges", nameof(node)); //this only work for nodes, which has two edges

            float avarageRad1 = AvarageRadianFromTwoEdges(node.Edges[0], node.Edges[1], node);
            float radianDiff1 = RadianDifferenceFromTwoEdges(node.Edges[0], node.Edges[1], node);

            float avarageRad2 = avarageRad1 + Mathf.PI;
            float radianDiff2 = 2f * Mathf.PI - radianDiff1;

            Vector2 vec1 = new Vector2(Mathf.Cos(avarageRad1) * thickness * (1f / Mathf.Sin(radianDiff1 / 2)), Mathf.Sin(avarageRad1) * thickness * (1f / Mathf.Sin(radianDiff1 / 2)));
            Vector2 vec2 = new Vector2(Mathf.Cos(avarageRad2) * thickness * (1f / Mathf.Sin(radianDiff2 / 2)), Mathf.Sin(avarageRad2) * thickness * (1f / Mathf.Sin(radianDiff2 / 2)));

            LotNodes.Add(new LotNode(node.X + vec1.x, node.Y + vec1.y));
            LotNodes.Add(new LotNode(node.X + vec2.x, node.Y + vec2.y));
            
            return;
        }

        private void ThreeEdgedThickening(Node node, float thickness)
        {
            if (node.Edges.Count != 3) throw new ArgumentException("Parameter node doesn't have exactly three edges", nameof(node)); //this only work for nodes, which has three edges

            Edge edge1 = node.Edges[0];
            Edge edge2 = node.Edges[1];
            Edge edge3 = node.Edges[2];

            float avarageRad12 = AvarageRadianFromTwoEdges(edge1, edge2, node);
            float avarageRad13 = AvarageRadianFromTwoEdges(edge1, edge3, node);
            float avarageRad23 = AvarageRadianFromTwoEdges(edge2, edge3, node);

            float radianDiff12 = RadianDifferenceFromTwoEdges(edge1, edge2, node);
            float radianDiff13 = RadianDifferenceFromTwoEdges(edge1, edge3, node);
            float radianDiff23 = RadianDifferenceFromTwoEdges(edge2, edge3, node);

            if (!CorrectThreeEdgedAvarage(avarageRad12, edge3, node)) avarageRad12 += Mathf.PI;
            if (!CorrectThreeEdgedAvarage(avarageRad13, edge2, node)) avarageRad13 += Mathf.PI;
            if (!CorrectThreeEdgedAvarage(avarageRad23, edge1, node)) avarageRad23 += Mathf.PI;

            Vector2 vec12 = new Vector2(Mathf.Cos(avarageRad12) * thickness * (1f / Mathf.Sin(radianDiff12/2)), Mathf.Sin(avarageRad12) * thickness * (1f / Mathf.Sin(radianDiff12/2)));
            Vector2 vec13 = new Vector2(Mathf.Cos(avarageRad13) * thickness * (1f / Mathf.Sin(radianDiff13/2)), Mathf.Sin(avarageRad13) * thickness * (1f / Mathf.Sin(radianDiff13/2)));
            Vector2 vec23 = new Vector2(Mathf.Cos(avarageRad23) * thickness * (1f / Mathf.Sin(radianDiff23/2)), Mathf.Sin(avarageRad23) * thickness * (1f / Mathf.Sin(radianDiff23/2)));

            LotNodes.Add(new LotNode(node.X + vec12.x, node.Y + vec12.y));
            LotNodes.Add(new LotNode(node.X + vec13.x, node.Y + vec13.y));
            LotNodes.Add(new LotNode(node.X + vec23.x, node.Y + vec23.y));

            return;
        }

        private void FourEdgedThickening(Node node, float thickness) //TODO: Base edge, common edge selection not perfect, sometimes getting wrong results
        {
            if (node.Edges.Count != 4) throw new ArgumentException("Parameter node doesn't have exactly four edges", nameof(node)); //this only work for nodes, which has four edges

            Edge edge1 = node.Edges[0];
            Edge edge2 = node.Edges[1];
            Edge edge3 = node.Edges[2];
            Edge edge4 = node.Edges[3];

            //radians from node
            float radian1 = edge1.NodeA == node ? edge1.DirRadianFromA : edge1.DirRadianFromB;
            float radian2 = edge2.NodeA == node ? edge2.DirRadianFromA : edge2.DirRadianFromB;
            float radian3 = edge3.NodeA == node ? edge3.DirRadianFromA : edge3.DirRadianFromB;
            float radian4 = edge4.NodeA == node ? edge4.DirRadianFromA : edge4.DirRadianFromB;

            //Base edges are two edges furthest to each other. Common edges are the rest
            Edge baseEdge1 = edge1;
            Edge baseEdge2;
            Edge commonEdge1;
            Edge commonEdge2;

            //Get The furthest edge from edge1
            if (RadianDifference(radian1, radian2) > RadianDifference(radian1, radian3))
            {
                if (RadianDifference(radian1, radian2) > RadianDifference(radian1, radian4))
                {
                    baseEdge2 = edge2;
                    commonEdge1 = edge3;
                    commonEdge2 = edge4;
                }
                else
                {
                    baseEdge2 = edge4;
                    commonEdge1 = edge3;
                    commonEdge2 = edge2;
                }
            }
            else
            {
                if (RadianDifference(radian1, radian3) > RadianDifference(radian1, radian4))
                {
                    baseEdge2 = edge3;
                    commonEdge1 = edge2;
                    commonEdge2 = edge4;
                }
                else
                {
                    baseEdge2 = edge4;
                    commonEdge1 = edge3;
                    commonEdge2 = edge2;
                }
            }

            //4 final vectors to use for thickening
            Vector2 vec1 = new Vector2(Mathf.Cos(AvarageRadianFromTwoEdges(baseEdge1, commonEdge1, node)) * thickness * (1f / Mathf.Sin(RadianDifferenceFromTwoEdges(baseEdge1, commonEdge1, node) / 2)),
                                       Mathf.Sin(AvarageRadianFromTwoEdges(baseEdge1, commonEdge1, node)) * thickness * (1f / Mathf.Sin(RadianDifferenceFromTwoEdges(baseEdge1, commonEdge1, node) / 2)));

            Vector2 vec2 = new Vector2(Mathf.Cos(AvarageRadianFromTwoEdges(baseEdge1, commonEdge2, node)) * thickness * (1f / Mathf.Sin(RadianDifferenceFromTwoEdges(baseEdge1, commonEdge2, node) / 2)),
                                       Mathf.Sin(AvarageRadianFromTwoEdges(baseEdge1, commonEdge2, node)) * thickness * (1f / Mathf.Sin(RadianDifferenceFromTwoEdges(baseEdge1, commonEdge2, node) / 2)));

            Vector2 vec3 = new Vector2(Mathf.Cos(AvarageRadianFromTwoEdges(baseEdge2, commonEdge1, node)) * thickness * (1f / Mathf.Sin(RadianDifferenceFromTwoEdges(baseEdge2, commonEdge1, node) / 2)),
                                       Mathf.Sin(AvarageRadianFromTwoEdges(baseEdge2, commonEdge1, node)) * thickness * (1f / Mathf.Sin(RadianDifferenceFromTwoEdges(baseEdge2, commonEdge1, node) / 2)));

            Vector2 vec4 = new Vector2(Mathf.Cos(AvarageRadianFromTwoEdges(baseEdge2, commonEdge2, node)) * thickness * (1f / Mathf.Sin(RadianDifferenceFromTwoEdges(baseEdge2, commonEdge2, node) / 2)),
                                       Mathf.Sin(AvarageRadianFromTwoEdges(baseEdge2, commonEdge2, node)) * thickness * (1f / Mathf.Sin(RadianDifferenceFromTwoEdges(baseEdge2, commonEdge2, node) / 2)));

            LotNodes.Add(new LotNode(node.X + vec1.x, node.Y + vec1.y));
            LotNodes.Add(new LotNode(node.X + vec2.x, node.Y + vec2.y));
            LotNodes.Add(new LotNode(node.X + vec3.x, node.Y + vec3.y));
            LotNodes.Add(new LotNode(node.X + vec4.x, node.Y + vec4.y));

            return;
        }



        //HELPER FUNCTIONS

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

        //This function check the avarage radiant of edge1 and edge2, and if it's not correct returns a false
        private bool CorrectThreeEdgedAvarage(float avarage, Edge edgeNotIncluded, Node node)
        {
            float radiantFromNode;
            if (edgeNotIncluded.NodeA == node) radiantFromNode = edgeNotIncluded.DirRadianFromA;
            else radiantFromNode = edgeNotIncluded.DirRadianFromB;

            if (RadianDifference(radiantFromNode, avarage) < RadianDifference(radiantFromNode, avarage + Mathf.PI)) return false;
            return true;
        }

        //Takes two radiant, returns the true difference
        public float RadianDifference(float rad1, float rad2)
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
            if ((rad2 + 2 * (float)Math.PI - rad1) < (rad1 - rad2)) return rad2 + 2 * (float)Math.PI - rad1;
            else return rad1 - rad2;
        }
    }
}
