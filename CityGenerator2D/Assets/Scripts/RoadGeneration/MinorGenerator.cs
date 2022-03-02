using System;
using System.Collections.Generic;
using GraphModel;
using UnityEngine;

namespace RoadGeneration
{
    /**
     * This L-system is responsible for generating minor roads from a given Highway graph
     */

    class MinorGenerator
    {
        private readonly List<RoadSegment> globalGoalsRoads;
        private readonly List<RoadSegment> queue;
        private readonly List<RoadSegment> segments;
        private readonly List<RoadSegment> hwSegments;

        private readonly System.Random rand;
        private readonly int border;
        private readonly int maxSegment;

        private readonly Graph graph;
        
        private const int RoadLength = 10;

        public MinorGenerator(System.Random seededRandom, int mapSize, int maxRoad, Graph graphToBuild, List<RoadSegment> majorSegments)
        {
            globalGoalsRoads = new List<RoadSegment>();
            queue = new List<RoadSegment>();
            segments = new List<RoadSegment>();

            hwSegments = majorSegments;

            rand = seededRandom;
            border = mapSize;
            maxSegment = maxRoad;

            graph = graphToBuild;
        }

        public void Run()
        {
            GenerateStartSegments();

            while (queue.Count != 0 && segments.Count < maxSegment)
            {
                RoadSegment current = queue[0];
                queue.RemoveAt(0);

                if (!CheckLocalConstraint(current)) continue;
                segments.Add(current);
                AddToGraph(current);

                GlobalGoals(current);
            }

            if (segments.Count == maxSegment) Debug.Log("Minor Roads reached maximal amount");

            DeleteSomeNodes();
            DeleteInsideLeaves();
            DeleteAloneEdges();
            DeleteAloneNodes();
        }

        private bool CheckLocalConstraint(RoadSegment segment)
        {
            //TRANSFORMATION
            bool stretched = false;

            foreach (RoadSegment road in hwSegments) //first check majorNodes
            {
                if (IsClose(segment.NodeTo, road.NodeTo))
                {
                    segment.NodeTo = road.NodeTo;
                    segment.EndSegment = true;
                    stretched = true;
                    break;
                }
            }

            if (!stretched)
            {
                foreach (RoadSegment road in segments) //then check minorNodes
                {
                    if (IsClose(segment.NodeTo, road.NodeTo))
                    {
                        segment.NodeTo = road.NodeTo;
                        segment.EndSegment = true;
                        break;
                    }
                }
            }

            //CHECKING CROSSING
            foreach (RoadSegment road in hwSegments) //first check majorNodes
            {
                if (segment.IsCrossing(road)) return false;
            }

            foreach (RoadSegment road in segments) //then check minorNodes
            {
                if (segment.IsCrossing(road)) return false;
            }
            
            //CHECKING OTHER CONSTRAINTS
            //Check if segment is out of border
            if (segment.NodeFrom.X > border || segment.NodeFrom.X < -border 
             || segment.NodeFrom.Y > border || segment.NodeFrom.Y < -border) return false;

            //Check if segment would come into itself
            if (segment.NodeFrom.X == segment.NodeTo.X && segment.NodeFrom.Y == segment.NodeTo.Y) return false;

            //nodeTo or nodeFrom has more than 4 edges
            if (segment.NodeTo.Edges.Count >= 4 || segment.NodeFrom.Edges.Count >= 4) return false;

            if (!segment.NodeFrom.IsFree(
            Mathf.Atan2(segment.NodeTo.Y - segment.NodeFrom.Y, segment.NodeTo.X - segment.NodeFrom.X)
            )) return false;  //direction is not free from NodeFrom

            if (!segment.NodeTo.IsFree(
            Mathf.Atan2(segment.NodeFrom.Y - segment.NodeTo.Y, segment.NodeFrom.X - segment.NodeTo.X)
            )) return false;  //direction is not free from NodeTo

            return true;
        }

        private void GlobalGoals(RoadSegment segment)
        {
            if (segment.EndSegment) return;

            globalGoalsRoads.Clear();

            //Generate in the 3 other possible direction
            var dirVector = segment.getDirVector();
            var normalVector1 = new Vector2(dirVector.y, -dirVector.x);
            var normalVector2 = new Vector2(-dirVector.y, dirVector.x);

            RoadSegment branchedSegment1 = CalcNewRoadSegment(segment.NodeTo, dirVector, 0);
            RoadSegment branchedSegment2 = CalcNewRoadSegment(segment.NodeTo, normalVector1, 0);
            RoadSegment branchedSegment3 = CalcNewRoadSegment(segment.NodeTo, normalVector2, 0);
            globalGoalsRoads.Add(branchedSegment1);
            globalGoalsRoads.Add(branchedSegment2);
            globalGoalsRoads.Add(branchedSegment3);

            foreach (RoadSegment newSegment in globalGoalsRoads)
            {
                queue.Add(newSegment);
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
                if (segment.EndSegment) continue;

                var dirVector = segment.getDirVector();
                var normalVector1 = new Vector2(dirVector.y, -dirVector.x);
                var normalVector2 = new Vector2(-dirVector.y, dirVector.x);

                RoadSegment branchedSegment2 = CalcNewRoadSegment(segment.NodeTo, normalVector1, 0);
                RoadSegment branchedSegment3 = CalcNewRoadSegment(segment.NodeTo, normalVector2, 0);
                queue.Add(branchedSegment2);
                queue.Add(branchedSegment3);

                if (hwSegments.IndexOf(segment) == 0) //We also branch out from the very first Node!
                {
                    RoadSegment branchedSegment4 = CalcNewRoadSegment(segment.NodeFrom, normalVector1, 0);
                    RoadSegment branchedSegment5 = CalcNewRoadSegment(segment.NodeFrom, normalVector2, 0);
                    queue.Add(branchedSegment4);
                    queue.Add(branchedSegment5);
                }
            }
        }


        //DELETE FUNCTIONS AT THE END

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

            foreach(Edge edge in removableEdges) //Remove the edges from other nodes and from the minorEdges list
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

            foreach(Node node in removable) //Remove the node from the minorNodes list
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


        private void DeleteInsideLeaves()
        {
            List<Node> removableNodes = new List<Node>();
            List<Edge> removableEdges = new List<Edge>();

            foreach (Node node in graph.MinorNodes)
            {
                if (node.Edges.Count == 1 && node.X < (border - 2) && node.X > (-border + 2) && node.Y < (border - 2) && node.Y > (-border + 2)) //We only remove leaves which are not in the edge of the map
                {
                    removableNodes.Add(node);
                    if (!removableEdges.Contains(node.Edges[0])) removableEdges.Add(node.Edges[0]);
                }
            }

            foreach (Edge edge in removableEdges) //Remove the edges from other nodes and from the minorEdges list
            {
                foreach (Node node in graph.MinorNodes)
                {
                    if (node.Edges.Contains(edge)) node.Edges.Remove(edge);
                }
                foreach (Node node in graph.MajorNodes)
                {
                    if (node.Edges.Contains(edge)) node.Edges.Remove(edge);
                }
                graph.MinorEdges.Remove(edge);
            }

            foreach (Node node in removableNodes) //Remove the node from the minorNodes list
            {
                graph.MinorNodes.Remove(node);
            }
        }



        private bool IsClose(Node A, Node B)
        {
            float idealRadius = RoadLength * 0.7f;
            //If the two points are closer than the ideal radius
            if (((float)Math.Pow(A.X - B.X, 2) + (float)Math.Pow(A.Y - B.Y, 2)) < (idealRadius * idealRadius)) return true;
            return false;
        }
        
        private RoadSegment CalcNewRoadSegment(Node nodeFrom, Vector2 dirVector, int leanIteration)
        {
            var newNodeTo = new Node(nodeFrom.X + dirVector.normalized.x * RoadLength, nodeFrom.Y + dirVector.normalized.y * RoadLength);
            return new RoadSegment(nodeFrom, newNodeTo, leanIteration);
        }

        public List<RoadSegment> GetRoadSegments()
        {
            return segments;
        }
    }
}
