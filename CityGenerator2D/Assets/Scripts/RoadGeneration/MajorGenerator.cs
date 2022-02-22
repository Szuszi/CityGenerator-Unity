using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    /**
     *  This class will implement the extended L-system
     */

    class MajorGenerator
    {
        private List<RoadSegment> globalGoalsRoads;
        private List<RoadSegment> Q;
        private List<RoadSegment> segments;

        private System.Random rand;
        private int border;
        private int maxSegment;
        private int maxLean;

        private Graph graph;


        public MajorGenerator(System.Random seededRandom, int mapSize, int maxRoad, int maxDegree, Graph graphToBuild)
        {
            globalGoalsRoads = new List<RoadSegment>();
            Q = new List<RoadSegment>();
            segments = new List<RoadSegment>();

            rand = seededRandom;
            border = mapSize;
            maxSegment = maxRoad;
            maxLean = maxDegree;

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

            if (segments.Count == maxSegment) Debug.Log("Major Roads reached maximal amount");
        }

        private bool CheckLocalConstrait(RoadSegment segment)
        {
            //TRANSFOTMATION
            foreach (RoadSegment road in segments)
            {
                //If the new segment end is close to another segments Node, Fix it's end to it
                if (IsClose(segment.NodeTo, road.NodeTo))
                {
                    segment.NodeTo = road.NodeTo;
                    segment.EndSegmen = true;
                }
            }

            //CHECKING CONSTRAITS
            foreach(RoadSegment road in segments)
            {
                if (segment.IsCrossing(road)) return false; //Check if segment is crossing an other road
            }

            if (segment.NodeFrom.X > border || segment.NodeFrom.X < -border || segment.NodeFrom.Y > border || segment.NodeFrom.Y < -border) return false; //Check if segment is out of border

            if (segment.NodeFrom.X == segment.NodeTo.X && segment.NodeFrom.Y == segment.NodeTo.Y) return false; //Check if segment would come into itself

            if (segment.NodeTo.Edges.Count >= 4) return false; //nodeTo has more than 4 edges

            if (segment.NodeFrom.Edges.Count >= 4) return false; //nodeFrom has more than 4 edges

            foreach (Edge edge in segment.NodeTo.Edges)
            {
                if(edge.NodeA == segment.NodeFrom || edge.NodeB == segment.NodeFrom) return false;  //NodeTo already connected to NodeFrom
            }

            return true;
        }

        private void GlobalGoals(RoadSegment segment)
        {
            if (segment.EndSegmen) return;

            globalGoalsRoads.Clear();

            //First calculate the direction Vector
            Vector2 dirVector = new Vector2(segment.NodeTo.X - segment.NodeFrom.X, segment.NodeTo.Y - segment.NodeFrom.Y);


            //BRANCHING

            int branchRandom = rand.Next(0, 40);

            if (branchRandom == 4) //At every 40th points approx. the road branches out to the RIGHT
            {
                Vector2 normalVector = new Vector2(dirVector.y, -dirVector.x);
                RoadSegment branchedSegment = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + normalVector.normalized.x, segment.NodeTo.Y + normalVector.normalized.y), 0);
                globalGoalsRoads.Add(branchedSegment);
            }
            else if (branchRandom == 5) //At every other 40th points approx. the road branches out to the LEFT
            {
                Vector2 normalVector = new Vector2(-dirVector.y, dirVector.x);
                RoadSegment branchedSegment = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + normalVector.normalized.x, segment.NodeTo.Y + normalVector.normalized.y), 0);
                globalGoalsRoads.Add(branchedSegment);
            }
            else if (branchRandom == 6) //At every another 40th points approx. the road branches out to BOTH DIRECTIONS
            {
                Vector2 normalVector1 = new Vector2(dirVector.y, -dirVector.x);
                Vector2 normalVector2 = new Vector2(-dirVector.y, dirVector.x);
                RoadSegment branchedSegment1 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + normalVector1.normalized.x, segment.NodeTo.Y + normalVector1.normalized.y), 0);
                RoadSegment branchedSegment2 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + normalVector2.normalized.x, segment.NodeTo.Y + normalVector2.normalized.y), 0);
                globalGoalsRoads.Add(branchedSegment1);
                globalGoalsRoads.Add(branchedSegment2);
            }


            //ROAD CONTINUEING

            //Then check if we need to determine a new lean. If yes, calculate the next RoadSegment like that
            if (segment.LeanIteration == 3)
            {
                int randomNumber = rand.Next(0, 3);
                bool Left = false;
                bool Right = false;
                if (randomNumber == 1)
                {
                    Left = true;
                }
                else if (randomNumber == 2)
                {
                    Right = true;
                }

                if (Left == true)
                {
                    dirVector = RotateVector(dirVector, GetRandomAngle(3, maxLean * 2));
                    RoadSegment segment1 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + dirVector.normalized.x, segment.NodeTo.Y + dirVector.normalized.y), 0);
                    segment1.LeanLeft = true;
                    globalGoalsRoads.Add(segment1);
                }
                else if (Right == true)
                {
                    dirVector = RotateVector(dirVector, GetRandomAngle(-3, maxLean * 2));
                    RoadSegment segment1 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + dirVector.normalized.x, segment.NodeTo.Y + dirVector.normalized.y), 0);
                    segment1.LeanRight = true;
                    globalGoalsRoads.Add(segment1);
                }
                else
                {
                    RoadSegment segment1 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + dirVector.normalized.x, segment.NodeTo.Y + dirVector.normalized.y), 0);
                    globalGoalsRoads.Add(segment1);
                }

            }
            else //if not, grow the new segment following the lean
            {
                if (segment.LeanLeft == true)
                {
                    dirVector = RotateVector(dirVector, GetRandomAngle(2, maxLean));
                    RoadSegment segment1 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + dirVector.normalized.x, segment.NodeTo.Y + dirVector.normalized.y), segment.LeanIteration + 1);
                    segment1.LeanLeft = true;
                    globalGoalsRoads.Add(segment1);
                }
                else if (segment.LeanRight == true)
                {
                    dirVector = RotateVector(dirVector, GetRandomAngle(-2, -maxLean));
                    RoadSegment segment1 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + dirVector.normalized.x, segment.NodeTo.Y + dirVector.normalized.y), segment.LeanIteration + 1);
                    segment1.LeanRight = true;
                    globalGoalsRoads.Add(segment1);
                }
                else
                {
                    RoadSegment segment1 = new RoadSegment(segment.NodeTo, new Node(segment.NodeTo.X + dirVector.normalized.x, segment.NodeTo.Y + dirVector.normalized.y), segment.LeanIteration + 1);
                    globalGoalsRoads.Add(segment1);
                }
            }

            foreach (RoadSegment newSegment in globalGoalsRoads)
            {
               Q.Add(newSegment);
            }
        }

        private void GenerateStartSegments()
        {
            //First Generate a number nearby the middle quarter of the map
            int sampleX = rand.Next(0, (border * 100));
            int sampleY = rand.Next(0, (border * 100));
            float starterX = ((float)sampleX / 100.0f) - (float)border/3;
            float starterY = ((float)sampleY / 100.0f) - (float)border/3;
            Node startNode = new Node(starterX, starterY);

            //Secondly Generate a vector which determines the two starting directions
            int randomDirX = rand.Next(-100, 100);
            int randomDirY = rand.Next(-100, 100);
            Vector2 startDir = new Vector2(randomDirX, randomDirY);
            Node starterNodeTo1 = new Node(startNode.X + startDir.normalized.x, starterY + startDir.normalized.y);
            Node starterNodeTo2 = new Node(startNode.X - startDir.normalized.x, starterY - startDir.normalized.y);

            //Thirdly We make two starting RoadSegment from these
            RoadSegment starterSegment1 = new RoadSegment(startNode, starterNodeTo1, 0);
            RoadSegment starterSegment2 = new RoadSegment(startNode, starterNodeTo2, 0);
            Q.Add(starterSegment1);
            Q.Add(starterSegment2);
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
                int temp = b;
                b = a;
                a = temp;
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
            if (((float)Math.Pow(A.X - B.X, 2) + (float)Math.Pow(A.Y - B.Y, 2)) < (0.8f * 0.8f)) return true; //The two points are closer than 0.8f
            return false;
        }

        public List<RoadSegment> GetRoadSegments()
        {
            return segments;
        }
    }
}
