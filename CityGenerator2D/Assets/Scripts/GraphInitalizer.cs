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
        private List<LotNode> LotNodes; //Nodes of the Lots
        private List<Lot> Lots;
        private System.Random rand;

        [Header("Maximum Curve between Roads")]
        [Range(2, 20)]
        public int maxDegree = 2;
        
        [Header("Maximum Number of Roads")]
        public int maxMajorRoad = 1000;
        public int maxMinorRoad = 10000;

        [Header("Thickness of Roads")]
        [Range(0.01f, 0.25f)]
        public float majorThickness = 0.2f;
        [Range(0.01f, 0.25f)]
        public float minorThickness = 0.05f;

        [Header("Seed and Size")]
        public int mapSize = 20;
        public int seed = 7;

        [Header("Gizmos")]
        public bool drawRoadNodes = false;
        public bool drawRoads = true;
        public bool drawLotNodes = false;
        public bool drawLots = true;


        void Start()
        {
            rand = new System.Random(seed);
            roadGraph = new Graph();
            Thread t = new Thread(new ThreadStart(ThreadProc));
            t.Start();
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

            if (drawRoads)
            {
                DrawEdges(roadGraph.MajorEdges, Color.white);
                DrawEdges(roadGraph.MinorEdges, Color.black);
            }

            if (drawRoadNodes)
            {
                DrawNodes(roadGraph.MajorNodes, Color.white, 0.2f);
                DrawNodes(roadGraph.MinorNodes, Color.black, 0.1f);
            }

            if (drawLotNodes)
            {
                DrawLotNodes(LotNodes, Color.red, 0.04f);
            }

            if (drawLots)
            {
                DrawLots(new Color(0.7f, 0.4f, 0.4f));
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

        private void DrawLotNodes(List<LotNode> nodes, Color color, float size)
        {
            if (LotNodes == null) return;

            Gizmos.color = color;
            for (int x = nodes.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while beeing read
            {
                Gizmos.DrawSphere(new Vector3(nodes[x].X, nodes[x].Y, 0f), size);
            }
        }

        private void DrawLots(Color color)
        {
            if (Lots == null) return;

            Gizmos.color = color;
            foreach(Lot lot in Lots)
            {
                for(int i = 0; i < lot.Nodes.Count; i++)
                {
                    if(i == (lot.Nodes.Count - 1))
                    {
                        Vector3 from = new Vector3(lot.Nodes[i].X, lot.Nodes[i].Y, 0f);
                        Vector3 to = new Vector3(lot.Nodes[0].X, lot.Nodes[0].Y, 0f);
                        Gizmos.DrawLine(from, to);
                    }
                    else
                    {
                        Vector3 from = new Vector3(lot.Nodes[i].X, lot.Nodes[i].Y, 0f);
                        Vector3 to = new Vector3(lot.Nodes[i + 1].X, lot.Nodes[i + 1].Y, 0f);
                        Gizmos.DrawLine(from, to);
                    }
                }
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
            Debug.Log("Road generation time taken: " + sw.Elapsed.TotalMilliseconds + " ms");
            Debug.Log(majorGen.GetRoadSegments().Count + " major road generated");
            Debug.Log(minorGen.GetRoadSegments().Count + " minor road generated");

            //BLOCK GENERATION
            LotGenerator blockGen = new LotGenerator(roadGraph, majorThickness, minorThickness);
            blockGen.Generate();
            LotNodes = blockGen.LotNodes;
            Lots = blockGen.Lots;
        }
    }
}
