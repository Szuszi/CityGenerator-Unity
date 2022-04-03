using System.Collections.Generic;
using BlockGeneration;
using GraphModel;
using MeshGeneration;
using UnityEngine;

namespace Services
{
    class GizmoService
    {
        public static void DrawNodes(List<Node> nodes, Color color, float size)
        {
            for (int x = nodes.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while being read
            {
                Gizmos.color = color;
                Gizmos.DrawSphere(new Vector3(nodes[x].X, nodes[x].Y, 0f), size);
            }
        }

        public static void DrawBlockNodes(List<BlockNode> nodes, Color color, float size)
        {
            if (nodes == null) return;

            Gizmos.color = color;
            for (int x = nodes.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while being read
            {
                Gizmos.DrawSphere(new Vector3(nodes[x].X, nodes[x].Y, 0f), size);
            }
        }

        public static void DrawBlocks(List<Block> blocks, Color color)
        {
            if (blocks == null) return;

            Gizmos.color = color;
            foreach (Block block in blocks)
            {
                for (int i = 0; i < block.Nodes.Count; i++)
                {
                    if (i == (block.Nodes.Count - 1))
                    {
                        Vector3 from = new Vector3(block.Nodes[i].X, block.Nodes[i].Y, 0f);
                        Vector3 to = new Vector3(block.Nodes[0].X, block.Nodes[0].Y, 0f);
                        Gizmos.DrawLine(from, to);
                    }
                    else
                    {
                        Vector3 from = new Vector3(block.Nodes[i].X, block.Nodes[i].Y, 0f);
                        Vector3 to = new Vector3(block.Nodes[i + 1].X, block.Nodes[i + 1].Y, 0f);
                        Gizmos.DrawLine(from, to);
                    }
                }
            }
        }

        public static void DrawBlockMeshes(List<BlockMesh> blockMeshes, Color color)
        {
            if (blockMeshes == null) return;

            Gizmos.color = color;
            foreach (BlockMesh blockMesh in blockMeshes)
            {
                foreach (Triangle tri in blockMesh.Triangles)
                {
                    Gizmos.DrawLine(tri.A, tri.B);
                    Gizmos.DrawLine(tri.A, tri.C);
                    Gizmos.DrawLine(tri.B, tri.C);
                }
            }
        }

        public static void DrawEdges(List<Edge> edges, Color color)
        {
            if (edges == null || edges.Count == 0) return;
            
            for (int x = edges.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while being read
            {
                Gizmos.color = color;
                Vector3 from = new Vector3(edges[x].NodeA.X, edges[x].NodeA.Y, 0f);
                Vector3 to = new Vector3(edges[x].NodeB.X, edges[x].NodeB.Y, 0f);
                Gizmos.DrawLine(from, to);
            }
        }
    }
}
