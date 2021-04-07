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
        public Graph LotGraph { get; set; }

        public LotGenerator(Graph graphToUse, Graph graphToBuild)
        {
            graph = graphToUse;
            LotGraph = graphToBuild;
        }

        public void Generate()
        {
            ThickenNodes(0.07f, 0.05f);
        }

        //Makes the Nodes thicker, by extending them by the given thickness.
        private void ThickenNodes(float majorRoadThickness, float minorRoadThickness)
        {

            //First Thicken major nodes, generation depends on the number of edges a node has
            foreach(Node node in graph.MajorNodes)
            {
                if(node.Edges.Count == 1)
                {
                    float dirRadian;
                    if (node.Edges[0].NodeA == node) dirRadian = node.Edges[0].dirRadianFromA;
                    else dirRadian = node.Edges[0].dirRadianFromB;

                    Vector2 forward = new Vector2((float)Math.Cos(dirRadian + Mathf.PI) * majorRoadThickness, (float)Math.Sin(dirRadian + Mathf.PI) * majorRoadThickness);
                    Vector2 left = new Vector2((float)Math.Cos(dirRadian + Mathf.PI / 2) * majorRoadThickness, (float)Math.Sin(dirRadian + Mathf.PI / 2) * majorRoadThickness);
                    Vector2 right = new Vector2((float)Math.Cos(dirRadian - Mathf.PI / 2) * majorRoadThickness, (float)Math.Sin(dirRadian - Mathf.PI / 2) * majorRoadThickness);

                    LotGraph.MajorNodes.Add(new Node(node.X + forward.x, node.Y + forward.y));
                    LotGraph.MajorNodes.Add(new Node(node.X + left.x, node.Y + left.y));
                    LotGraph.MajorNodes.Add(new Node(node.X + right.x, node.Y + right.y));
                }

                else if(node.Edges.Count == 2)
                {
                    float avarageRad = AvarageRadianFromTwoEdges(node.Edges[0], node.Edges[1], node);

                    Vector2 vec1 = new Vector2((float)Math.Cos(avarageRad) * majorRoadThickness, (float)Math.Sin(avarageRad) * majorRoadThickness);
                    Vector2 vec2 = new Vector2((-1) * vec1.x, (-1) * vec1.y);

                    LotGraph.MajorNodes.Add(new Node(node.X + vec1.x, node.Y + vec1.y));
                    LotGraph.MajorNodes.Add(new Node(node.X + vec2.x, node.Y + vec2.y));
                }

                else if(node.Edges.Count == 3)
                {
                    Edge edge1 = node.Edges[0];
                    Edge edge2 = node.Edges[1];
                    Edge edge3 = node.Edges[2];

                    float avarageRad12 = AvarageRadianFromTwoEdges(edge1, edge2, node);
                    float avarageRad13 = AvarageRadianFromTwoEdges(edge1, edge3, node);
                    float avarageRad23 = AvarageRadianFromTwoEdges(edge2, edge3, node);

                    if (!CorrectThreeEdgedAvarage(avarageRad12, edge3, node)) avarageRad12 += Mathf.PI;
                    if (!CorrectThreeEdgedAvarage(avarageRad13, edge2, node)) avarageRad13 += Mathf.PI;
                    if (!CorrectThreeEdgedAvarage(avarageRad23, edge1, node)) avarageRad23 += Mathf.PI;

                    Vector2 vec12 = new Vector2((float)Math.Cos(avarageRad12) * majorRoadThickness, (float)Math.Sin(avarageRad12) * majorRoadThickness);
                    Vector2 vec13 = new Vector2((float)Math.Cos(avarageRad13) * majorRoadThickness, (float)Math.Sin(avarageRad13) * majorRoadThickness);
                    Vector2 vec23 = new Vector2((float)Math.Cos(avarageRad23) * majorRoadThickness, (float)Math.Sin(avarageRad23) * majorRoadThickness);

                    LotGraph.MajorNodes.Add(new Node(node.X + vec12.x, node.Y + vec12.y));
                    LotGraph.MajorNodes.Add(new Node(node.X + vec13.x, node.Y + vec13.y));
                    LotGraph.MajorNodes.Add(new Node(node.X + vec23.x, node.Y + vec23.y));
                }

                else if(node.Edges.Count == 4)
                {
                    Edge edge1 = node.Edges[0];
                    Edge edge2 = node.Edges[1];
                    Edge edge3 = node.Edges[2];
                    Edge edge4 = node.Edges[3];

                    //radians from node
                    float radian1 = edge1.NodeA == node ? edge1.dirRadianFromA : edge1.dirRadianFromB;
                    float radian2 = edge2.NodeA == node ? edge2.dirRadianFromA : edge2.dirRadianFromB;
                    float radian3 = edge3.NodeA == node ? edge3.dirRadianFromA : edge3.dirRadianFromB;
                    float radian4 = edge4.NodeA == node ? edge4.dirRadianFromA : edge4.dirRadianFromB;

                    //Base edges are two edges furthest to each other. Common edges are the rest
                    Edge baseEdge1 = edge1;
                    Edge baseEdge2;
                    Edge commonEdge1;
                    Edge commonEdge2;

                    //Get The furthest edge from edge1
                    if (RadianDifference(radian1, radian2) > RadianDifference(radian1, radian3))
                    {
                        if(RadianDifference(radian1, radian2) > RadianDifference(radian1, radian4))
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
                        if(RadianDifference(radian1, radian3) > RadianDifference(radian1, radian4))
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
                    Vector2 vec1 = new Vector2((float)Math.Cos(AvarageRadianFromTwoEdges(baseEdge1, commonEdge1, node)) * majorRoadThickness, 
                                               (float)Math.Sin(AvarageRadianFromTwoEdges(baseEdge1, commonEdge1, node)) * majorRoadThickness);
                    Vector2 vec2 = new Vector2((float)Math.Cos(AvarageRadianFromTwoEdges(baseEdge1, commonEdge2, node)) * majorRoadThickness,
                                               (float)Math.Sin(AvarageRadianFromTwoEdges(baseEdge1, commonEdge2, node)) * majorRoadThickness);
                    Vector2 vec3 = new Vector2((float)Math.Cos(AvarageRadianFromTwoEdges(baseEdge2, commonEdge1, node)) * majorRoadThickness,
                                               (float)Math.Sin(AvarageRadianFromTwoEdges(baseEdge2, commonEdge1, node)) * majorRoadThickness);
                    Vector2 vec4 = new Vector2((float)Math.Cos(AvarageRadianFromTwoEdges(baseEdge2, commonEdge2, node)) * majorRoadThickness,
                                               (float)Math.Sin(AvarageRadianFromTwoEdges(baseEdge2, commonEdge2, node)) * majorRoadThickness);

                    LotGraph.MajorNodes.Add(new Node(node.X + vec1.x, node.Y + vec1.y));
                    LotGraph.MajorNodes.Add(new Node(node.X + vec2.x, node.Y + vec2.y));
                    LotGraph.MajorNodes.Add(new Node(node.X + vec3.x, node.Y + vec3.y));
                    LotGraph.MajorNodes.Add(new Node(node.X + vec4.x, node.Y + vec4.y));
                }

                else if(node.Edges.Count > 4)
                {
                    Debug.Log("A node has more than 4 Edges"); //This shoudln't happen if everything works properly
                }
            }

            //Then Thicken minor nodes, generation depends on the number of edges a node has
            foreach (Node node in graph.MinorNodes)
            {
                if (node.Edges.Count == 1)
                {
                    float dirRadian;
                    if (node.Edges[0].NodeA == node) dirRadian = node.Edges[0].dirRadianFromA;
                    else dirRadian = node.Edges[0].dirRadianFromB;

                    Vector2 forward = new Vector2((float)Math.Cos(dirRadian + Mathf.PI) * minorRoadThickness, (float)Math.Sin(dirRadian + Mathf.PI) * minorRoadThickness);
                    Vector2 left = new Vector2((float)Math.Cos(dirRadian + Mathf.PI / 2) * minorRoadThickness, (float)Math.Sin(dirRadian + Mathf.PI / 2) * minorRoadThickness);
                    Vector2 right = new Vector2((float)Math.Cos(dirRadian - Mathf.PI / 2) * minorRoadThickness, (float)Math.Sin(dirRadian - Mathf.PI / 2) * minorRoadThickness);

                    LotGraph.MinorNodes.Add(new Node(node.X + forward.x, node.Y + forward.y));
                    LotGraph.MinorNodes.Add(new Node(node.X + left.x, node.Y + left.y));
                    LotGraph.MinorNodes.Add(new Node(node.X + right.x, node.Y + right.y));
                }

                else if (node.Edges.Count == 2)
                {
                    float avarageRad = AvarageRadianFromTwoEdges(node.Edges[0], node.Edges[1], node);

                    Vector2 vec1 = new Vector2((float)Math.Cos(avarageRad) * minorRoadThickness, (float)Math.Sin(avarageRad) * minorRoadThickness);
                    Vector2 vec2 = new Vector2((-1) * vec1.x, (-1) * vec1.y);

                    LotGraph.MinorNodes.Add(new Node(node.X + vec1.x, node.Y + vec1.y));
                    LotGraph.MinorNodes.Add(new Node(node.X + vec2.x, node.Y + vec2.y));
                }

                else if (node.Edges.Count == 3)
                {
                    Edge edge1 = node.Edges[0];
                    Edge edge2 = node.Edges[1];
                    Edge edge3 = node.Edges[2];

                    float avarageRad12 = AvarageRadianFromTwoEdges(edge1, edge2, node);
                    float avarageRad13 = AvarageRadianFromTwoEdges(edge1, edge3, node);
                    float avarageRad23 = AvarageRadianFromTwoEdges(edge2, edge3, node);

                    if (!CorrectThreeEdgedAvarage(avarageRad12, edge3, node)) avarageRad12 += Mathf.PI;
                    if (!CorrectThreeEdgedAvarage(avarageRad13, edge2, node)) avarageRad13 += Mathf.PI;
                    if (!CorrectThreeEdgedAvarage(avarageRad23, edge1, node)) avarageRad23 += Mathf.PI;

                    Vector2 vec12 = new Vector2((float)Math.Cos(avarageRad12) * minorRoadThickness, (float)Math.Sin(avarageRad12) * minorRoadThickness);
                    Vector2 vec13 = new Vector2((float)Math.Cos(avarageRad13) * minorRoadThickness, (float)Math.Sin(avarageRad13) * minorRoadThickness);
                    Vector2 vec23 = new Vector2((float)Math.Cos(avarageRad23) * minorRoadThickness, (float)Math.Sin(avarageRad23) * minorRoadThickness);

                    LotGraph.MinorNodes.Add(new Node(node.X + vec12.x, node.Y + vec12.y));
                    LotGraph.MinorNodes.Add(new Node(node.X + vec13.x, node.Y + vec13.y));
                    LotGraph.MinorNodes.Add(new Node(node.X + vec23.x, node.Y + vec23.y));
                }

                else if (node.Edges.Count == 4)
                {
                    Edge edge1 = node.Edges[0];
                    Edge edge2 = node.Edges[1];
                    Edge edge3 = node.Edges[2];
                    Edge edge4 = node.Edges[3];

                    //radians from node
                    float radian1 = edge1.NodeA == node ? edge1.dirRadianFromA : edge1.dirRadianFromB;
                    float radian2 = edge2.NodeA == node ? edge2.dirRadianFromA : edge2.dirRadianFromB;
                    float radian3 = edge3.NodeA == node ? edge3.dirRadianFromA : edge3.dirRadianFromB;
                    float radian4 = edge4.NodeA == node ? edge4.dirRadianFromA : edge4.dirRadianFromB;

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
                    Vector2 vec1 = new Vector2((float)Math.Cos(AvarageRadianFromTwoEdges(baseEdge1, commonEdge1, node)) * minorRoadThickness,
                                               (float)Math.Sin(AvarageRadianFromTwoEdges(baseEdge1, commonEdge1, node)) * minorRoadThickness);
                    Vector2 vec2 = new Vector2((float)Math.Cos(AvarageRadianFromTwoEdges(baseEdge1, commonEdge2, node)) * minorRoadThickness,
                                               (float)Math.Sin(AvarageRadianFromTwoEdges(baseEdge1, commonEdge2, node)) * minorRoadThickness);
                    Vector2 vec3 = new Vector2((float)Math.Cos(AvarageRadianFromTwoEdges(baseEdge2, commonEdge1, node)) * minorRoadThickness,
                                               (float)Math.Sin(AvarageRadianFromTwoEdges(baseEdge2, commonEdge1, node)) * minorRoadThickness);
                    Vector2 vec4 = new Vector2((float)Math.Cos(AvarageRadianFromTwoEdges(baseEdge2, commonEdge2, node)) * minorRoadThickness,
                                               (float)Math.Sin(AvarageRadianFromTwoEdges(baseEdge2, commonEdge2, node)) * minorRoadThickness);

                    LotGraph.MinorNodes.Add(new Node(node.X + vec1.x, node.Y + vec1.y));
                    LotGraph.MinorNodes.Add(new Node(node.X + vec2.x, node.Y + vec2.y));
                    LotGraph.MinorNodes.Add(new Node(node.X + vec3.x, node.Y + vec3.y));
                    LotGraph.MinorNodes.Add(new Node(node.X + vec4.x, node.Y + vec4.y));
                }

                else if (node.Edges.Count > 4)
                {
                    Debug.Log("A node has more than 4 Edges"); //This shoudln't happen if everything works properly
                }
            }
        }

        //Returns an avarage radian between two given edge, which has the given common node
        private float AvarageRadianFromTwoEdges(Edge edge1, Edge edge2, Node node)
        {
            float dirRad1;
            float dirRad2;

            if (edge1.NodeA == node) dirRad1 = edge1.dirRadianFromA;
            else dirRad1 = edge1.dirRadianFromB;

            if (edge2.NodeA == node) dirRad2 = edge2.dirRadianFromA;
            else dirRad2 = edge2.dirRadianFromB;

            if(RadianDifference((dirRad1 + dirRad2) / 2, dirRad2) > RadianDifference(((dirRad1 + dirRad2) / 2) + Mathf.PI, dirRad2)) return ((dirRad1 + dirRad2) / 2 + Mathf.PI);
            return (dirRad1 + dirRad2) / 2;
        }

        //This function check the avarage radiant of edge1 and edge2, and if it's not correct returns a false
        private bool CorrectThreeEdgedAvarage(float avarage, Edge edgeNotIncluded, Node node)
        {
            float radiantFromNode;
            if (edgeNotIncluded.NodeA == node) radiantFromNode = edgeNotIncluded.dirRadianFromA;
            else radiantFromNode = edgeNotIncluded.dirRadianFromB;

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
