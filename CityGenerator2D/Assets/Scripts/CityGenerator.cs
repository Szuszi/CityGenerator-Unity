using System.Collections.Generic;
using System.Threading;
using GraphModel;
using LotGeneration;
using MeshGeneration;
using RoadGeneration;
using Services;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    private Graph roadGraph; //Graph which will be built, and then drawn
    private List<LotNode> lotNodes; //Nodes of the Lots
    private List<Lot> lots;
    private System.Random rand;

    private GizmoService gizmoService; //For drawing with gizmos
    private MeshCreateService meshCreator;

    private List<Lot> concaveLots;
    private List<Lot> convexLots;
    private List<LotMesh> lotMeshes;
    private float lotHeight = 0.02f;

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
    private bool genReady = false;
    private bool genDone = false;
        

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
        if (genReady && !genDone) //This make sure, that this will be only called once
        {
            genDone = true;
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
        lotNodes = blockGen.LotNodes;
        lots = blockGen.Lots;

        //MESH GENERATION
        MeshGenerator meshGen = new MeshGenerator(lots, lotHeight);
        meshGen.GenerateMeshes();

        convexLots = meshGen.ConvexLots;
        concaveLots = meshGen.ConcaveLots;
        lotMeshes = meshGen.LotMeshes;

        genReady = true;
    }

    private void GenerateGameObjects()
    {
        GameObject separator = new GameObject();
        separator.name = "===========";

        //Make RoadPlane
        GameObject roadPlane = new GameObject();
        roadPlane.name = "Road Plane";
        roadPlane.AddComponent<MeshFilter>();
        roadPlane.AddComponent<MeshRenderer>();
        roadPlane.GetComponent<MeshFilter>().mesh = meshCreator.GenerateRoadMesh(mapSize);

        Material roadMaterial = Resources.Load<Material>("Material/RoadMaterial");
        roadPlane.GetComponent<MeshRenderer>().material = roadMaterial;

        //Make Lots
        GameObject lotContainer = new GameObject();
        lotContainer.name = "Lot Container";

        Material lotMaterial = Resources.Load<Material>("Material/LotMaterial");
        Material lotGreenMaterial = Resources.Load<Material>("Material/LotGreenMaterial");

        for (int i = 0; i < lotMeshes.Count; i++)
        {
            GameObject lot = new GameObject();
            lot.name = "Lot" + i.ToString();
            lot.transform.parent = lotContainer.transform;
            lot.AddComponent<MeshFilter>();
            lot.AddComponent<MeshRenderer>();
            lot.GetComponent<MeshFilter>().mesh = meshCreator.GenerateLotMesh(lotMeshes[i], lotHeight);

            if (lotMeshes[i].Lot.Nodes.Count > 10) lot.GetComponent<MeshRenderer>().material = lotGreenMaterial;
            else lot.GetComponent<MeshRenderer>().material = lotMaterial;
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
            gizmoService.DrawLotNodes(lotNodes, Color.red, 0.04f);
        }

        if (drawLots)
        {
            gizmoService.DrawLots(lots, new Color(0.7f, 0.4f, 0.4f));
        }

        if (drawConvexLots)
        {
            gizmoService.DrawLots(convexLots, new Color(0.2f, 0.7f, 0.7f));
        }
        if (drawConcaveLots)
        {
            gizmoService.DrawLots(concaveLots, new Color(0.2f, 0.7f, 0.2f));
        }
        if (drawTriangulatedMeshes)
        {
            gizmoService.DrawLotMeshes(lotMeshes, new Color(.8f, .8f, .8f));
        }
    }
}