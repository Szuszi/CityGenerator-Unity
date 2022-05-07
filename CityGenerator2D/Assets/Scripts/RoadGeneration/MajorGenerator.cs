using System;
using System.Collections.Generic;
using GraphModel;
using UnityEngine;

namespace RoadGeneration
{
    /**
     *  This class will implement the extended L-system
     */

    class MajorGenerator
    {
        private readonly List<RoadSegment> globalGoalsRoads;
        private readonly List<RoadSegment> queue;
        private readonly List<RoadSegment> segments;

        private readonly System.Random rand;
        private readonly int border;
        private readonly int maxSegment;
        private readonly int maxLean;
        private readonly float branchProbability;

        private readonly Graph graph;
        
        private const int RoadLength = 10;


        public MajorGenerator(System.Random seededRandom, int mapSize, int maxRoad, int maxDegree, float branchingChance, Graph graphToBuild)
        {
            globalGoalsRoads = new List<RoadSegment>();
            queue = new List<RoadSegment>();
            segments = new List<RoadSegment>();

            rand = seededRandom;
            border = mapSize;
            maxSegment = maxRoad;
            maxLean = maxDegree;
            branchProbability = branchingChance;

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

            if (segments.Count == maxSegment) Debug.Log("Major Roads reached maximal amount");
        }

        private bool CheckLocalConstraint(RoadSegment segment)
        {
            foreach (RoadSegment road in segments)
            {
                //If the new segment end is close to another segments Node, Fix it's end to it
                if (IsClose(segment.NodeTo, road.NodeTo))
                {
                    segment.NodeTo = road.NodeTo;
                    segment.EndSegment = true;
                }

                if (segment.IsCrossing(road)) return false; //Check if segment is crossing an other road
            }

            //Check if segment is out of border
            if (segment.NodeFrom.X > border || segment.NodeFrom.X < -border
             || segment.NodeFrom.Y > border || segment.NodeFrom.Y < -border) return false; 

            //Check if segment would come into itself
            if (segment.NodeFrom.X == segment.NodeTo.X && segment.NodeFrom.Y == segment.NodeTo.Y) return false; 

            //nodeTo or nodeFrom has more than 4 edges
            if (segment.NodeTo.Edges.Count >= 4 || segment.NodeFrom.Edges.Count >= 4) return false;

            foreach (Edge edge in segment.NodeTo.Edges)
            {
                //NodeTo already connected to NodeFrom
                if(edge.NodeA == segment.NodeFrom || edge.NodeB == segment.NodeFrom) return false;
            }

            return true;
        }

        private void GlobalGoals(RoadSegment segment)
        {
            if (segment.EndSegment) return;
            globalGoalsRoads.Clear();
            var dirVector = segment.getDirVector();

            //BRANCHING
            int maxInt = (int) Math.Round(3 / branchProbability);
            if (branchProbability > 0.3) //Maximum branch probability is 3 out of 10 (0.3)
            {
                maxInt = 6;
            }
            else if (branchProbability < 0.03) //Minimum branch probability is 3 out of 100 (0.03)
            {
                maxInt = 100;
            }
            int branchRandom = rand.Next(0, maxInt);

            if (branchRandom == 1) //the road branches out to the RIGHT
            {
                var normalVector = new Vector2(dirVector.y, -dirVector.x);
                RoadSegment branchedSegment = CalcNewRoadSegment(segment.NodeTo, normalVector, 0);
                globalGoalsRoads.Add(branchedSegment);
            }
            else if (branchRandom == 2) //The road branches out to the LEFT
            {
                var normalVector = new Vector2(-dirVector.y, dirVector.x);
                RoadSegment branchedSegment =  CalcNewRoadSegment(segment.NodeTo, normalVector, 0);
                globalGoalsRoads.Add(branchedSegment);
            }
            else if (branchRandom == 3) //The road branches out to BOTH DIRECTIONS
            {
                var normalVector1 = new Vector2(dirVector.y, -dirVector.x);
                var normalVector2 = new Vector2(-dirVector.y, dirVector.x);
                RoadSegment branchedSegment1 = CalcNewRoadSegment(segment.NodeTo, normalVector1, 0);
                RoadSegment branchedSegment2 = CalcNewRoadSegment(segment.NodeTo, normalVector2, 0);
                globalGoalsRoads.Add(branchedSegment1);
                globalGoalsRoads.Add(branchedSegment2);
            }

            //ROAD CONTINUE
            globalGoalsRoads.Add(GetContinuingRoadSegment(segment));

            foreach (RoadSegment newSegment in globalGoalsRoads)
            {
               queue.Add(newSegment);
            }
        }

        private RoadSegment GetContinuingRoadSegment(RoadSegment segment)
        {
            var dirVector = segment.getDirVector();
            
            if (maxLean < 1) return CalcNewRoadSegment(segment.NodeTo, dirVector, 0);
            
            if (segment.LeanIteration == 3) //Check if we need a new lean. If yes, calculate the next RoadSegment
            {
                var randomNumber = rand.Next(0, 3);

                if (randomNumber == 1)
                {
                    dirVector = RotateVector(dirVector, GetRandomAngle(2, maxLean * 2));
                    RoadSegment newSegment = CalcNewRoadSegment(segment.NodeTo, dirVector, 0);
                    newSegment.LeanLeft = true;
                    return newSegment;
                }
                else if (randomNumber == 2)
                {
                    dirVector = RotateVector(dirVector, GetRandomAngle(-2, -maxLean * 2));
                    RoadSegment newSegment = CalcNewRoadSegment(segment.NodeTo, dirVector, 0);
                    newSegment.LeanRight = true;
                    return newSegment;
                }
                else
                {
                    return CalcNewRoadSegment(segment.NodeTo, dirVector, 0);
                }
            }
            else //if not, grow the new segment following the lean
            {
                if (segment.LeanLeft)
                {
                    dirVector = RotateVector(dirVector, GetRandomAngle(2, maxLean));
                    RoadSegment segment1 = CalcNewRoadSegment(segment.NodeTo, dirVector, segment.LeanIteration + 1);
                    segment1.LeanLeft = true;
                    return segment1;
                }
                else if (segment.LeanRight)
                {
                    dirVector = RotateVector(dirVector, GetRandomAngle(-2, -maxLean));
                    RoadSegment segment1 = CalcNewRoadSegment(segment.NodeTo, dirVector, segment.LeanIteration + 1);
                    segment1.LeanRight = true;
                    return segment1;
                }
                else
                {
                    return CalcNewRoadSegment(segment.NodeTo, dirVector, segment.LeanIteration + 1);
                }
            }
        }

        private void GenerateStartSegments()
        {
            //First Generate a number nearby the middle quarter of the map
            int sampleX = rand.Next(0, (border * 100));
            int sampleY = rand.Next(0, (border * 100));
            float starterX = (sampleX / 100.0f) - (float)border/3;
            float starterY = (sampleY / 100.0f) - (float)border/3;
            var startNode = new Node(starterX, starterY);

            //Secondly Generate a vector which determines the two starting directions
            int randomDirX = rand.Next(-100, 100);
            int randomDirY = rand.Next(-100, 100);
            var startDir = new Vector2(randomDirX, randomDirY);
            var starterNodeTo1 = new Node(startNode.X + startDir.normalized.x * RoadLength, starterY + startDir.normalized.y * RoadLength);
            var starterNodeTo2 = new Node(startNode.X - startDir.normalized.x * RoadLength, starterY - startDir.normalized.y * RoadLength);

            //Thirdly We make two starting RoadSegment from these
            var starterSegment1 = new RoadSegment(startNode, starterNodeTo1, 0);
            var starterSegment2 = new RoadSegment(startNode, starterNodeTo2, 0);
            queue.Add(starterSegment1);
            queue.Add(starterSegment2);
        }

        private void AddToGraph(RoadSegment road)
        {
            if (!graph.MajorNodes.Contains(road.NodeFrom)) graph.MajorNodes.Add(road.NodeFrom);
            if (!graph.MajorNodes.Contains(road.NodeTo)) graph.MajorNodes.Add(road.NodeTo);
            graph.MajorEdges.Add(new Edge(road.NodeFrom, road.NodeTo));
        }

        private float GetRandomAngle(int a, int b) //Calculates an Angle between a and b, returns it in radian
        {
            //First we make 'a' smaller, and generate a random number in the range
            if(b < a)
            {
                (b, a) = (a, b);
            }
            int range = Math.Abs(b - a);
            int rotation = rand.Next(0, range) + a;

            //then we make it to radian, and return it
            float rotationAngle = (float)(Math.PI / 180) * rotation;
            return rotationAngle;
        }

        private Vector2 RotateVector(Vector2 dirVector, float rotationAngle)
        {
            //This works like a rotation matrix
            dirVector.x = ((float)Math.Cos(rotationAngle) * dirVector.x) - ((float)Math.Sin(rotationAngle) * dirVector.y);
            dirVector.y = ((float)Math.Sin(rotationAngle) * dirVector.x) + ((float)Math.Cos(rotationAngle) * dirVector.y);

            return dirVector;
        }

        private bool IsClose(Node A, Node B)
        {
            float idealRadius = RoadLength * 0.8f;
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
