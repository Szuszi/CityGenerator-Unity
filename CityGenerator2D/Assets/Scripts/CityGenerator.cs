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
    private List<Block> thinnedBlocks;
    private List<Block> lots;
    private System.Random rand;

    private List<Block> concaveBlocks;
    private List<Block> convexBlocks;
    private List<BlockMesh> blockMeshes;
    private List<BlockMesh> lotMeshes;
    private List<BoundingRectangle> boundingRectangles;
    private float blockHeight = 0.02f;

    [Header("Major Road generation")]
    [Range(0, 20)]
    public int maxDegreeInCurves = 2;
    [Range(0.03f, 0.1f)]
    public float branchingProbability = 0.075f;
    
    [Header("Minor Road generation")]
    [Range(0.02f, 0.2f)] 
    public float crossingDeletionProbability = 0.1f;

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
    
    [Header("Building generation")] 
    public float minBuildHeight = 1;
    public float maxBuildHeight = 10;

    [Header("Sidewalk generation")]
    [Range(0.1f, 1f)]
    public float sidewalkThickness = 0.5f;

    [Header("Gizmos")]
    public bool drawRoadNodes;
    public bool drawRoads = true;
    public bool drawBlockNodes;
    public bool drawBlocks = true;
    public bool drawThinnedBlocks;
    public bool drawConvexBlocks;
    public bool drawConcaveBlocks;
    public bool drawTriangulatedMeshes;
    public bool drawBoundingBoxes;
    public bool drawLots = true;

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
        MajorGenerator majorGen = new MajorGenerator(
            rand, mapSize, maxMajorRoad, maxDegreeInCurves, branchingProbability, roadGraph);
        majorGen.Run();
        MinorGenerator minorGen = new MinorGenerator(
            rand, mapSize, maxMinorRoad, crossingDeletionProbability,roadGraph, majorGen.GetRoadSegments());
        minorGen.Run();

        //ROAD GENERATION TIME, ROAD COUNT
        sw.Stop();
        Debug.Log("Road generation time taken: " + sw.Elapsed.TotalMilliseconds + " ms");
        Debug.Log(majorGen.GetRoadSegments().Count + " major road generated");
        Debug.Log(minorGen.GetRoadSegments().Count + " minor road generated");
        
        //BLOCK GENERATION
        BlockGenerator blockGen = new BlockGenerator(roadGraph, mapSize, majorThickness, minorThickness, blockHeight);
        blockGen.Generate();
        blockNodes = blockGen.BlockNodes;
        blocks = blockGen.Blocks;
        Debug.Log(blockGen.Blocks.Count + " block generated");
        
        //SIDEWALK GENERATION
        blockGen.ThickenBlocks(sidewalkThickness);
        thinnedBlocks = blockGen.ThinnedBlocks;
        Debug.Log("Sidewalk generation completed");
        
        //BLOCK DIVISION
        sw = System.Diagnostics.Stopwatch.StartNew();
        
        BlockDivider blockDiv = new BlockDivider(rand, thinnedBlocks, lots);
        blockDiv.DivideBlocks();
        blockDiv.SetBuildingHeights(minBuildHeight, maxBuildHeight, blockHeight, mapSize);
        boundingRectangles = blockDiv.BoundingRectangles;
        
        //LOT GENERATION TIME, LOT COUNT
        sw.Stop();
        Debug.Log("Lot generation time taken: " + sw.Elapsed.TotalMilliseconds + " ms");
        Debug.Log(lots.Count + " lot generated");

        //BLOCK MESH GENERATION
        MeshGenerator blockMeshGen = new MeshGenerator(blocks, blockHeight);
        blockMeshGen.GenerateMeshes();
        blockMeshes = blockMeshGen.BlockMeshes;
        
        //LOT MESH GENERATION
        MeshGenerator lotMeshGen = new MeshGenerator(lots, blockHeight);
        lotMeshGen.GenerateMeshes();

        convexBlocks = lotMeshGen.ConvexBlocks;
        concaveBlocks = lotMeshGen.ConcaveBlocks;
        lotMeshes = lotMeshGen.BlockMeshes;

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
        
        //Make Lots
        GameObject lotContainer = new GameObject();
        lotContainer.name = "Lot Container";

        Material lotMaterial = Resources.Load<Material>("Material/BlockMaterial");

        for (int i = 0; i < lotMeshes.Count; i++)
        {
            GameObject lot = new GameObject();
            lot.name = "Lot" + i.ToString();
            lot.transform.parent = blockContainer.transform;
            lot.AddComponent<MeshFilter>();
            lot.AddComponent<MeshRenderer>();
            lot.GetComponent<MeshFilter>().mesh = MeshCreateService.GenerateBlockMesh(lotMeshes[i]);
            lot.GetComponent<MeshRenderer>().material = lotMaterial;
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

        if (drawThinnedBlocks)
        {
            GizmoService.DrawBlocks(thinnedBlocks, new Color(0.7f, 0.4f, 0.4f));
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