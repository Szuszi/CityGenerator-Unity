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

        private List<Lot> ConcaveLots;
        private List<Lot> ConvexLots;
        private List<LotMesh> LotMeshes;
        private float LotHeight = 0.1f;

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
            MeshGenerator meshGen = new MeshGenerator(Lots, LotHeight);
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
            GameObject RoadPlane = new GameObject();
            RoadPlane.name = "RoadPlane";
            RoadPlane.AddComponent<MeshFilter>();
            RoadPlane.AddComponent<MeshRenderer>();
            RoadPlane.GetComponent<MeshFilter>().mesh = GenerateRoadMesh();

            Material RoadMaterial = Resources.Load<Material>("Material/RoadMaterial");
            RoadPlane.GetComponent<MeshRenderer>().material = RoadMaterial;

            //Make Lots
            Material LotMaterial = Resources.Load<Material>("Material/LotMaterial");
            Material LotGreenMaterial = Resources.Load<Material>("Material/LotGreenMaterial");

            for (int i = 0; i < LotMeshes.Count; i++)
            {
                GameObject Lot = new GameObject();
                Lot.name = "Lot" + i.ToString();
                Lot.AddComponent<MeshFilter>();
                Lot.AddComponent<MeshRenderer>();
                Lot.GetComponent<MeshFilter>().mesh = GenerateLotMesh(LotMeshes[i]);

                if (LotMeshes[i].lot.Nodes.Count > 10) Lot.GetComponent<MeshRenderer>().material = LotGreenMaterial;
                else Lot.GetComponent<MeshRenderer>().material = LotMaterial;
            }

        }

        private Mesh GenerateRoadMesh()
        {
            Mesh RoadMesh = new Mesh();

            RoadMesh.vertices = new Vector3[]
            {
                new Vector3(mapSize, 0f, mapSize),
                new Vector3(-mapSize, 0f, mapSize),
                new Vector3(mapSize, 0f, -mapSize),
                new Vector3(-mapSize, 0f, -mapSize)
            };

            RoadMesh.triangles = new int[]
            {
                0, 2, 1,
                2, 3, 1
            };

            RoadMesh.RecalculateNormals();

            return RoadMesh;
        }

        private Mesh GenerateLotMesh(LotMesh lot)
        {
            Mesh lMesh = new Mesh();

            int numTriangles = lot.triangles.Count + lot.sideTriangles.Count;

            var vertices = new Vector3[numTriangles * 3];  //Not the most optimized way, because same vertex can be stored more than once
            var triangles = new int[numTriangles * 3];

            //Add front panels
            for(int i = 0; i < lot.triangles.Count; i++) 
            {
                //Change the Vectors (Y will be up vector)
                Vector3 A = new Vector3(lot.triangles[i].A.x, LotHeight, lot.triangles[i].A.y);
                Vector3 B = new Vector3(lot.triangles[i].B.x, LotHeight, lot.triangles[i].B.y);
                Vector3 C = new Vector3(lot.triangles[i].C.x, LotHeight, lot.triangles[i].C.y);

                //Add attributes to Mesh
                vertices[3 * i] = A;
                vertices[3 * i + 1] = B;
                vertices[3 * i + 2] = C;

                triangles[3 * i] = 3 * i;
                triangles[3 * i + 1] = 3 * i + 1;
                triangles[3 * i + 2] = 3 * i + 2;
            }

            //Add side panels
            for(int i = lot.triangles.Count; i < numTriangles; i++)
            {
                vertices[3 * i] = lot.sideTriangles[i - lot.triangles.Count].A;
                vertices[3 * i + 1] = lot.sideTriangles[i - lot.triangles.Count].B;
                vertices[3 * i + 2] = lot.sideTriangles[i - lot.triangles.Count].C;

                triangles[3 * i] = 3 * i;
                triangles[3 * i + 1] = 3 * i + 1;
                triangles[3 * i + 2] = 3 * i + 2;
            }

            lMesh.vertices = vertices;
            lMesh.triangles = triangles;

            lMesh.RecalculateNormals();

            return lMesh;
        }
    }
}
