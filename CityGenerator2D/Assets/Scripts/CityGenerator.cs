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

        private GizmoService gizmoService; //For drawing with gizmos

        private List<Lot> ConcaveLots;
        private List<Lot> ConvexLots;
        private List<LotMesh> LotMeshes;
        private float LotHeight = 0.02f;

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
            gizmoService = new GizmoService();
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
                gizmoService.DrawEdges(roadGraph.MajorEdges, Color.white);
                gizmoService.DrawEdges(roadGraph.MinorEdges, Color.black);
            }

            if (drawRoadNodes)
            {
                gizmoService.DrawNodes(roadGraph.MajorNodes, Color.white, 0.2f);
                gizmoService.DrawNodes(roadGraph.MinorNodes, Color.black, 0.1f);
            }

            if (drawLotNodes)
            {
                gizmoService.DrawLotNodes(LotNodes, Color.red, 0.04f);
            }

            if (drawLots)
            {
                gizmoService.DrawLots(Lots, new Color(0.7f, 0.4f, 0.4f));
            }

            if (drawConvexLots)
            {
                gizmoService.DrawLots(ConvexLots, new Color(0.2f, 0.7f, 0.7f));
            }
            if(drawConcaveLots)
            {
                gizmoService.DrawLots(ConcaveLots, new Color(0.2f, 0.7f, 0.2f));
            }
            if (drawTriangulatedMeshes)
            {
                gizmoService.DrawLotMeshes(LotMeshes, new Color(.8f, .8f, .8f));
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
