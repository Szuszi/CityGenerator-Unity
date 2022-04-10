using System.Collections.Generic;
using System.Threading;
using BlockGeneration;
using GraphModel;
using MeshGeneration;
using RoadGeneration;
using BlockDivision;
using Services;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    private Graph roadGraph; //Graph which will be built, and then drawn
    private List<BlockNode> blockNodes; //Nodes of the Blocks
    private List<Block> blocks;
    private List<Block> lots;
    private System.Random rand;

    private List<Block> concaveBlocks;
    private List<Block> convexBlocks;
    private List<BlockMesh> blockMeshes;
    private List<BoundingRectangle> boundingRectangles;
    private float blockHeight = 0.02f;

    [Header("Maximum Curve between Roads")]
    [Range(0, 20)]
    public int maxDegree = 2;
        
    [Header("Maximum Number of Roads")]
    [Header("Maximum Number of Roads")]
    public int maxMajorRoad = 1000;
    public int maxMinorRoad = 10000;

    [Header("Thickness of Roads")]
    [Range(0.1f, 2.5f)]
    public float majorThickness = 2.0f;
    [Range(0.1f, 2.5f)]
    public float minorThickness = 0.5f;

    [Header("Seed and Size")]
    public int mapSize = 200;
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

    [Header("Block subdivision")] 
    public bool drawBoundingBoxes;
    public bool drawLots;
    public float minBuildHeight = 1;
    public float maxBuildHeight = 10;

    //Event to call, when the generation is ready
    private bool genReady;
    private bool genDone;
        

    void Start()
    {
        rand = new System.Random(seed);
        roadGraph = new Graph();
        lots = new List<Block>();

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
        BlockGenerator blockGen = new BlockGenerator(roadGraph, majorThickness, minorThickness, blockHeight);
        blockGen.Generate();
        blockNodes = blockGen.BlockNodes;
        blocks = blockGen.Blocks;
        Debug.Log(blockGen.Blocks.Count + " block generated");
        
        //BLOCK DIVISION
        BlockDivider blockDiv = new BlockDivider(rand, blocks, lots);
        blockDiv.DivideBlocks();
        blockDiv.SetBuildingHeights(minBuildHeight, maxBuildHeight, blockHeight, mapSize);
        boundingRectangles = blockDiv.BoundingRectangles;

        //MESH GENERATION
        MeshGenerator meshGen = new MeshGenerator(rand, lots, blockHeight, minBuildHeight, maxBuildHeight);
        meshGen.GenerateLotMeshes();

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
        roadPlane.GetComponent<MeshFilter>().mesh = MeshCreateService.GenerateRoadMesh(mapSize);

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
            block.GetComponent<MeshFilter>().mesh = MeshCreateService.GenerateBlockMesh(blockMeshes[i]);

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
            GizmoService.DrawEdges(roadGraph.MajorEdges, Color.white);
            GizmoService.DrawEdges(roadGraph.MinorEdges, Color.black);
        }

        if (drawRoadNodes)
        {
            GizmoService.DrawNodes(roadGraph.MajorNodes, Color.white, 2f);
            GizmoService.DrawNodes(roadGraph.MinorNodes, Color.black, 1f);
        }

        if (drawBlockNodes)
        {
            GizmoService.DrawBlockNodes(blockNodes, Color.red, 0.4f);
        }

        if (drawBlocks)
        {
            GizmoService.DrawBlocks(blocks, new Color(0.7f, 0.4f, 0.4f));
        }

        if (drawConvexBlocks && genDone)
        {
            GizmoService.DrawBlocks(convexBlocks, new Color(0.2f, 0.7f, 0.7f));
        }
        if (drawConcaveBlocks && genDone)
        {
            GizmoService.DrawBlocks(concaveBlocks, new Color(0.2f, 0.7f, 0.2f));
        }
        if (drawTriangulatedMeshes && genDone)
        {
            GizmoService.DrawBlockMeshes(blockMeshes, new Color(.8f, .8f, .8f));
        }

        if (drawBoundingBoxes && genDone)
        {
            List<Edge> cutEdges = new List<Edge>();
            
            foreach (var boundingBox in boundingRectangles)
            {
                GizmoService.DrawEdges(boundingBox.Edges, Color.white);   
                cutEdges.Add(boundingBox.GetCutEdge());
            }
            
            GizmoService.DrawEdges(cutEdges, Color.yellow);
        }

        if (drawLots)
        {
            GizmoService.DrawBlocks(lots, new Color(0.2f, 0.7f, 0.7f));
        }
    }
}