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
        private MeshCreateService meshCreator;

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
            meshCreator = new MeshCreateService();

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
            RoadPlane.name = "Road Plane";
            RoadPlane.AddComponent<MeshFilter>();
            RoadPlane.AddComponent<MeshRenderer>();
            RoadPlane.GetComponent<MeshFilter>().mesh = meshCreator.GenerateRoadMesh(mapSize);

            Material RoadMaterial = Resources.Load<Material>("Material/RoadMaterial");
            RoadPlane.GetComponent<MeshRenderer>().material = RoadMaterial;

            //Make Lots
            GameObject LotContainer = new GameObject();
            LotContainer.name = "Lot Container";

            Material LotMaterial = Resources.Load<Material>("Material/LotMaterial");
            Material LotGreenMaterial = Resources.Load<Material>("Material/LotGreenMaterial");

            for (int i = 0; i < LotMeshes.Count; i++)
            {
                GameObject Lot = new GameObject();
                Lot.name = "Lot" + i.ToString();
                Lot.transform.parent = LotContainer.transform;
                Lot.AddComponent<MeshFilter>();
                Lot.AddComponent<MeshRenderer>();
                Lot.GetComponent<MeshFilter>().mesh = meshCreator.GenerateLotMesh(LotMeshes[i], LotHeight);

                if (LotMeshes[i].lot.Nodes.Count > 10) Lot.GetComponent<MeshRenderer>().material = LotGreenMaterial;
                else Lot.GetComponent<MeshRenderer>().material = LotMaterial;
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
            if (drawConcaveLots)
            {
                gizmoService.DrawLots(ConcaveLots, new Color(0.2f, 0.7f, 0.2f));
            }
            if (drawTriangulatedMeshes)
            {
                gizmoService.DrawLotMeshes(LotMeshes, new Color(.8f, .8f, .8f));
            }
        }
    }
}
