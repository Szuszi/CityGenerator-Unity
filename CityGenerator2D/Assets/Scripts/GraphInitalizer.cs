using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class GraphInitalizer : MonoBehaviour
    {
        private Graph roadGraph; //Graph which will be built, and then drawn
        private Graph blockGraph;
        private System.Random rand;

        public int maxDegree = 2;
        public int maxMajorRoad = 1000;
        public int maxMinorRoad = 10000;
        public int mapSize = 20;
        public int seed = 7;
        public bool drawNodes = false;
        public bool drawThickRoads = false;
        public bool drawBlockNodes = false;

        void Start()
        {
            rand = new System.Random(seed);
            roadGraph = new Graph();
            blockGraph = new Graph();
            Thread t = new Thread(new ThreadStart(ThreadProc));
            t.Start();

            /*
            Node A = new Node(-1.0f, 0.0f);
            Node B = new Node(1.01f, 0.0f);

            Node C = new Node(1.0f, 0.0f);
            Node D = new Node(3.0f, 0.0f);

            graph.MajorNodes.Add(A);
            graph.MajorNodes.Add(B);
            graph.MajorNodes.Add(C);
            graph.MajorNodes.Add(D);

            graph.MajorEdges.Add(new Edge(A, C));
            graph.MajorEdges.Add(new Edge(B, D));

            RoadSegment road1 = new RoadSegment(A, C, 0);
            RoadSegment road2 = new RoadSegment(B, D, 0);

            Debug.Log(road1.IsCrossing(road2));
            */


        }

        void Update()
        {
            //not used
        }

        private void OnDrawGizmos()
        {
            if (roadGraph == null)
            {
                return;
            }

            if (drawThickRoads)
            {
                DrawThickEdges(roadGraph.MinorEdges, Color.black, 3);
                DrawThickEdges(roadGraph.MajorEdges, Color.white, 3);
                return;
            }

            DrawEdges(roadGraph.MajorEdges, Color.white);
            DrawEdges(roadGraph.MinorEdges, Color.black);

            if (drawNodes)
            {
                DrawNodes(roadGraph.MajorNodes, Color.white, 0.2f);
                DrawNodes(roadGraph.MinorNodes, Color.black, 0.1f);
            }

            if (drawBlockNodes)
            {
                DrawNodes(blockGraph.MajorNodes, Color.red, 0.04f);
                DrawNodes(blockGraph.MinorNodes, Color.red, 0.02f);
            }
        }

        private void DrawNodes(List<Node> nodes, Color color, float size)
        {
            for (int x = nodes.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while beeing read
            {
                Gizmos.color = color;
                Gizmos.DrawSphere(new Vector3(nodes[x].X, nodes[x].Y, 0f), size);
            }
        }

        private void DrawThickEdges(List<Edge> edges, Color color, int thickness)
        {
            for (int x = edges.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while beeing read
            {
                Vector3 from = new Vector3(edges[x].NodeA.X, edges[x].NodeA.Y, 0f);
                Vector3 to = new Vector3(edges[x].NodeB.X, edges[x].NodeB.Y, 0f);
                Handles.DrawBezier(from, to, from, to, color, null, thickness);
            }
        }

        private void DrawEdges(List<Edge> edges, Color color)
        {
            for (int x = edges.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while beeing read
            {
                Gizmos.color = color;
                Vector3 from = new Vector3(edges[x].NodeA.X, edges[x].NodeA.Y, 0f);
                Vector3 to = new Vector3(edges[x].NodeB.X, edges[x].NodeB.Y, 0f);
                Gizmos.DrawLine(from, to);
            }
        }

        private void ThreadProc()
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            //ROAD GENERATION
            MajorGenerator majorGen = new MajorGenerator(rand, mapSize, maxMajorRoad, maxDegree, roadGraph);
            majorGen.Run();
            MinorGenerator minorGen = new MinorGenerator(rand, mapSize, maxMinorRoad, roadGraph, majorGen.GetRoadSegments());
            minorGen.Run();

            //GENERATION TIME, ROAD COUNT
            sw.Stop();
            Debug.Log("Time taken: " + sw.Elapsed.TotalMilliseconds + "ms");
            Debug.Log(majorGen.GetRoadSegments().Count + " major road generated");
            Debug.Log(minorGen.GetRoadSegments().Count + " minor road generated");

            //BLOCK GENERATION
            LotGenerator blockGen = new LotGenerator(roadGraph, blockGraph);
            blockGen.Generate();
        }
    }
}
