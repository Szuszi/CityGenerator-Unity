using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class CityGenerator : MonoBehaviour
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

        private List<Lot> ConcaveLots;
        private List<Lot> ConvexLots;
        private List<LotMesh> LotMeshes;

        [Header("Mesh Generation")]
        public bool drawConvexLots;
        public bool drawConcaveLots;
        public bool drawTriangulatedMeshes;

        //Event to call, when the generation is ready
        private bool GenReady = false;
        private bool GenDone = false;
        

        void Start()
        {
            rand = new System.Random(seed);
            roadGraph = new Graph();
            Thread t = new Thread(new ThreadStart(ThreadProc));
            t.Start();
        }

        void Update()
        {
            if(GenReady && !GenDone) //This make sure, that this will be only called once
            {
                GenDone = true;
                GenerateGameObjects();
            }
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
                DrawLots(Lots, new Color(0.7f, 0.4f, 0.4f));
            }

            if (drawConvexLots)
            {
                DrawLots(ConvexLots, new Color(0.2f, 0.7f, 0.7f));
            }
            if(drawConcaveLots)
            {
                DrawLots(ConcaveLots, new Color(0.2f, 0.7f, 0.2f));
            }
            if (drawTriangulatedMeshes)
            {
                DrawLotMeshes(LotMeshes, new Color(.8f, .8f, .8f));
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

        private void DrawLots(List<Lot> Lots, Color color)
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

        private void DrawLotMeshes(List<LotMesh> lotMeshes, Color color)
        {
            if (lotMeshes == null) return;

            Gizmos.color = color;
            foreach(LotMesh lotMesh in lotMeshes)
            {
                foreach(Triangle tri in lotMesh.triangles)
                {
                    Gizmos.DrawLine(tri.A, tri.B);
                    Gizmos.DrawLine(tri.A, tri.C);
                    Gizmos.DrawLine(tri.B, tri.C);
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

            //MESH GENERATION
            MeshGenerator meshGen = new MeshGenerator(Lots, seed);
            meshGen.GenerateMeshes();

            ConvexLots = meshGen.ConvexLots;
            ConcaveLots = meshGen.ConcaveLots;
            LotMeshes = meshGen.lotMeshes;

            GenReady = true;
        }

        private void GenerateGameObjects()
        {
            GameObject Separator = new GameObject();
            Separator.name = "===========";

            //Make RoadPlane
            Mesh mesh = new Mesh();
            GameObject RoadPlane = new GameObject();
            RoadPlane.name = "RoadPlane";
            RoadPlane.AddComponent<MeshFilter>();
            RoadPlane.AddComponent<MeshRenderer>();
            RoadPlane.GetComponent<MeshFilter>().mesh = GenerateRoadMesh();

            Material RoadMaterial = Resources.Load<Material>("Material/RoadMaterial");
            RoadPlane.GetComponent<MeshRenderer>().material = RoadMaterial;

            //Make Lots
        }

        private Mesh GenerateRoadMesh()
        {
            Mesh RoadMesh = new Mesh();

            RoadMesh.vertices = new Vector3[]
            {
                new Vector3(mapSize, 0, mapSize),
                new Vector3(-mapSize, 0, mapSize),
                new Vector3(mapSize, 0, -mapSize),
                new Vector3(-mapSize, 0, -mapSize)
            };

            RoadMesh.triangles = new int[]
            {
                0, 2, 1,
                2, 3, 1
            };

            RoadMesh.RecalculateNormals();

            return RoadMesh;
        }
    }
}
