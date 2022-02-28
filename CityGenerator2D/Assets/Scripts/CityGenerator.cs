using System.Collections.Generic;
using System.Threading;
using BlockGeneration;
using GraphModel;
using MeshGeneration;
using RoadGeneration;
using Services;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    private Graph roadGraph; //Graph which will be built, and then drawn
    private List<BlockNode> blockNodes; //Nodes of the Blocks
    private List<Block> blocks;
    private System.Random rand;

    private GizmoService gizmoService; //For drawing with gizmos
    private MeshCreateService meshCreator;

    private List<Block> concaveBlocks;
    private List<Block> convexBlocks;
    private List<BlockMesh> blockMeshes;
    private float blockHeight = 0.02f;

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
    public bool drawRoadNodes;
    public bool drawRoads = true;
    public bool drawBlockNodes;
    public bool drawBlocks = true;

    [Header("Mesh Generation")]
    public bool drawConvexBlocks;
    public bool drawConcaveBlocks;
    public bool drawTriangulatedMeshes;

    //Event to call, when the generation is ready
    private bool genReady;
    private bool genDone;
        

    void Start()
    {
        rand = new System.Random(seed);
        roadGraph = new Graph();

        gizmoService = new GizmoService();
        meshCreator = new MeshCreateService();

        Thread t = new Thread(ThreadProc);
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
        BlockGenerator blockGen = new BlockGenerator(roadGraph, majorThickness, minorThickness);
        blockGen.Generate();
        blockNodes = blockGen.BlockNodes;
        blocks = blockGen.Blocks;

        //MESH GENERATION
        MeshGenerator meshGen = new MeshGenerator(blocks, blockHeight);
        meshGen.GenerateMeshes();

        convexBlocks = meshGen.ConvexBlocks;
        concaveBlocks = meshGen.ConcaveBlocks;
        blockMeshes = meshGen.BlockMeshes;

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

        //Make Blocks
        GameObject blockContainer = new GameObject();
        blockContainer.name = "Block Container";

        Material blockMaterial = Resources.Load<Material>("Material/BlockMaterial");
        Material blockGreenMaterial = Resources.Load<Material>("Material/BlockGreenMaterial");

        for (int i = 0; i < blockMeshes.Count; i++)
        {
            GameObject block = new GameObject();
            block.name = "Block" + i.ToString();
            block.transform.parent = blockContainer.transform;
            block.AddComponent<MeshFilter>();
            block.AddComponent<MeshRenderer>();
            block.GetComponent<MeshFilter>().mesh = meshCreator.GenerateBlockMesh(blockMeshes[i], blockHeight);

            if (blockMeshes[i].Block.Nodes.Count > 10) block.GetComponent<MeshRenderer>().material = blockGreenMaterial;
            else block.GetComponent<MeshRenderer>().material = blockMaterial;
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

        if (drawBlockNodes)
        {
            gizmoService.DrawBlockNodes(blockNodes, Color.red, 0.04f);
        }

        if (drawBlocks)
        {
            gizmoService.DrawBlocks(blocks, new Color(0.7f, 0.4f, 0.4f));
        }

        if (drawConvexBlocks)
        {
            gizmoService.DrawBlocks(convexBlocks, new Color(0.2f, 0.7f, 0.7f));
        }
        if (drawConcaveBlocks)
        {
            gizmoService.DrawBlocks(concaveBlocks, new Color(0.2f, 0.7f, 0.2f));
        }
        if (drawTriangulatedMeshes)
        {
            gizmoService.DrawBlockMeshes(blockMeshes, new Color(.8f, .8f, .8f));
        }
    }
}