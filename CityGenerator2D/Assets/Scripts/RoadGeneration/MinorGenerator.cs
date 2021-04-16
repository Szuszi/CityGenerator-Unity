using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    /**
     * This L-system is responsible for generating minor roads from a given Highway graph
     */

    class MinorGenerator
    {
        private List<RoadSegment> globalGoalsRoads;
        private List<RoadSegment> Q;
        private List<RoadSegment> segments;
        private List<RoadSegment> hwSegments;

        private System.Random rand;
        private int border;
        private int maxSegment;

        private Graph graph;

        public MinorGenerator(System.Random seededRandom, int mapSize, int maxRoad, Graph graphToBuild, List<RoadSegment> MajorSegments)
        {
            globalGoalsRoads = new List<RoadSegment>();
            Q = new List<RoadSegment>();
            segments = new List<RoadSegment>();

            hwSegments = MajorSegments;

            rand = seededRandom;
            border = mapSize;
            maxSegment = maxRoad;

            graph = graphToBuild;
        }

        public void Run()
        {
            GenerateStartSegments();

            while (Q.Count() != 0 && segments.Count() < maxSegment)
            {
                RoadSegment current = Q[0];
                Q.RemoveAt(0);

                if (!CheckLocalConstrait(current)) continue;
                segments.Add(current);
                AddToGraph(current);

                GlobalGoals(current);
            }

            if (segments.Count == maxSegment) Debug.Log("Minor Roads reached maximal amount");

            DeleteSomeNodes();
            DeleteAloneEdges();
            DeleteAloneNodes();
        }

        private bool CheckLocalConstrait(RoadSegment segment)
        {
            //TRANSFORMATION
            bool streched = false;

            foreach (RoadSegment road in hwSegments) //first check majorNodes
            {
                if (IsClose(segment.NodeTo, road.NodeTo) && !streched)
                {
                    segment.NodeTo = road.NodeTo;
                    segment.EndSegmen = true;
                    streched = true;
                    break;
                }
       
                if (segment.IsCrossing(road))
                {
                    return false;
                }
                
            }

            if (!streched)
            {
                foreach (RoadSegment road in segments) //then check minorNodes
                {
                    if (IsClose(segment.NodeTo, road.NodeTo) && !streched)
                    {
                        segment.NodeTo = road.NodeTo;
                        segment.EndSegmen = true;
                        streched = true;
                        break;
                    }
                    
                    if (segment.IsCrossing(road))
                    {
                        return false;
                    }
                    
                }
            }


            //CHECKING CONSTRAITS
            if (segment.NodeFrom.X > border || segment.NodeFrom.X < -border || segment.NodeFrom.Y > border || segment.NodeFrom.Y < -border) return false; //out of border

            if (segment.NodeFrom.X == segment.NodeTo.X && segment.NodeFrom.Y == segment.NodeTo.Y) return false; //it comes into itself

            if (segment.NodeTo.Edges.Count >= 4) return false; //nodeTo has more than 4 edges

            if (segment.NodeFrom.Edges.Count >= 4) return false; //nodeFrom has more than 4 edges

            if (!segment.NodeFrom.IsFree(Mathf.Atan2(segment.NodeTo.Y - segment.NodeFrom.Y, segment.NodeTo.X - segment.NodeFrom.X))) return false;  //direction is not free from NodeFrom

            if (!segment.NodeTo.IsFree(Mathf.Atan2(segment.NodeFrom.Y - segment.NodeTo.Y, segment.NodeFrom.X - segment.NodeTo.X))) return false;  //direction is not free from NodeTo

            return true;
        }

        private void GlobalGoals(RoadSegment segment)
        {
            if (segment.EndSegmen) return;

            globalGoalsRoads.Clear();

            //Generate in the 3 other possible direction
            Vector2 dirVector = new Vector2(segment.NodeTo.X - segment.NodeFrom.X, segment.NodeTo.Y - segment.NodeFrom.Y);
            Vector2 normalVector1 = new Vector2(dirVector.y, -dirVector.x);
            Vector2 normalVector2 = new Vector2(-dirVector.y, dirVector.x);

            RoadSegment branchedSegment1 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + dirVector.normalized.x, segment.NodeTo.Y + dirVector.normalized.y), 0);
            RoadSegment branchedSegment2 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + normalVector1.normalized.x, segment.NodeTo.Y + normalVector1.normalized.y), 0);
            RoadSegment branchedSegment3 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + normalVector2.normalized.x, segment.NodeTo.Y + normalVector2.normalized.y), 0);
            globalGoalsRoads.Add(branchedSegment1);
            globalGoalsRoads.Add(branchedSegment2);
            globalGoalsRoads.Add(branchedSegment3);

            foreach (RoadSegment newSegment in globalGoalsRoads)
            {
                Q.Add(newSegment);
            }
        }

        private void AddToGraph(RoadSegment road)
        {
            if (!graph.MajorNodes.Contains(road.NodeFrom) && !graph.MinorNodes.Contains(road.NodeFrom)) graph.MinorNodes.Add(road.NodeFrom);
            if (!graph.MajorNodes.Contains(road.NodeTo) && !graph.MinorNodes.Contains(road.NodeTo)) graph.MinorNodes.Add(road.NodeTo);
            graph.MinorEdges.Add(new Edge(road.NodeFrom, road.NodeTo));
        }

        private void GenerateStartSegments()
        {
            foreach(RoadSegment segment in hwSegments)
            {
                if (segment.EndSegmen) continue;

                Vector2 dirVector = new Vector2(segment.NodeTo.X - segment.NodeFrom.X, segment.NodeTo.Y - segment.NodeFrom.Y);
                Vector2 normalVector1 = new Vector2(dirVector.y, -dirVector.x);
                Vector2 normalVector2 = new Vector2(-dirVector.y, dirVector.x);

                RoadSegment branchedSegment2 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + normalVector1.normalized.x, segment.NodeTo.Y + normalVector1.normalized.y), 0);
                RoadSegment branchedSegment3 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + normalVector2.normalized.x, segment.NodeTo.Y + normalVector2.normalized.y), 0);
                Q.Add(branchedSegment2);
                Q.Add(branchedSegment3);

                if (hwSegments.IndexOf(segment) == 0) //We also branch out from the very first Node!
                {
                    RoadSegment branchedSegment4 = new RoadSegment(segment.NodeFrom, new Node(segment.NodeFrom.X + normalVector1.normalized.x, segment.NodeFrom.Y + normalVector1.normalized.y), 0);
                    RoadSegment branchedSegment5 = new RoadSegment(segment.NodeFrom, new Node(segment.NodeFrom.X + normalVector2.normalized.x, segment.NodeFrom.Y + normalVector2.normalized.y), 0);
                    Q.Add(branchedSegment4);
                    Q.Add(branchedSegment5);
                }
            }
        }

        private void DeleteSomeNodes()
        {
            List<Node> removable = new List<Node>();
            List<Edge> removableEdges = new List<Edge>();

            foreach(Node node in graph.MinorNodes) //Randomly choose nodes to delete, store these nodes and its edges
            {
                if (node.X > (border - 2) || node.X < (-border + 2) || node.Y > (border - 2) || node.Y < (-border + 2)) continue;
                if(rand.Next(0,10) == 1)
                {
                    foreach(Edge edge in node.Edges)
                    {
                        if(!removableEdges.Contains(edge)) removableEdges.Add(edge);
                    }
                    removable.Add(node);
                }
            }

            foreach(Edge edge in removableEdges) //Remove the edges from other nodes and from the minoredges list
            {
                foreach(Node node in graph.MinorNodes)
                {
                    if (node.Edges.Contains(edge)) node.Edges.Remove(edge);
                }
                foreach(Node node in graph.MajorNodes)
                {
                    if (node.Edges.Contains(edge)) node.Edges.Remove(edge);
                }
                graph.MinorEdges.Remove(edge);
            }

            foreach(Node node in removable) //Remove the node from the minornodes list
            {
                graph.MinorNodes.Remove(node);
            }
        }

        private void DeleteAloneEdges()
        {
            List<Edge> removable = new List<Edge>();

            foreach (Edge edge in graph.MinorEdges)
            {
                if(edge.NodeA.Edges.Count == 1 && edge.NodeB.Edges.Count == 1)
                {
                    removable.Add(edge);
                }
            }

            foreach (Edge edge in removable)
            {
                graph.MinorEdges.Remove(edge);
                edge.NodeA.Edges.Remove(edge);
                edge.NodeB.Edges.Remove(edge);
            }

            //Leftover nodes will be deleted by DeleteAloneNodes()
        }

        private void DeleteAloneNodes()
        {
            List<Node> removable = new List<Node>();

            foreach (Node node in graph.MinorNodes)
            {
                if (node.Edges.Count <= 0)
                {
                    removable.Add(node);
                }
            }

            foreach (Node node in removable)
            {
                graph.MinorNodes.Remove(node);
            }
        }

        private bool IsClose(Node A, Node B)
        {
            if (((float)Math.Pow(A.X - B.X, 2) + (float)Math.Pow(A.Y - B.Y, 2)) < (0.7f * 0.7f)) return true; //The two points are closer than 0.7f
            return false;
        }

        public List<RoadSegment> GetRoadSegments()
        {
            return segments;
        }
    }
}
