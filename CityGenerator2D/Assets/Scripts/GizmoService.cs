using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Assets.Scripts
{
    class GizmoService
    {
        public void DrawNodes(List<Node> nodes, Color color, float size)
        {
            for (int x = nodes.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while beeing read
            {
                Gizmos.color = color;
                Gizmos.DrawSphere(new Vector3(nodes[x].X, nodes[x].Y, 0f), size);
            }
        }

        public void DrawLotNodes(List<LotNode> nodes, Color color, float size)
        {
            if (nodes == null) return;

            Gizmos.color = color;
            for (int x = nodes.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while beeing read
            {
                Gizmos.DrawSphere(new Vector3(nodes[x].X, nodes[x].Y, 0f), size);
            }
        }

        public void DrawLots(List<Lot> Lots, Color color)
        {
            if (Lots == null) return;

            Gizmos.color = color;
            foreach (Lot lot in Lots)
            {
                for (int i = 0; i < lot.Nodes.Count; i++)
                {
                    if (i == (lot.Nodes.Count - 1))
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

        public void DrawLotMeshes(List<LotMesh> lotMeshes, Color color)
        {
            if (lotMeshes == null) return;

            Gizmos.color = color;
            foreach (LotMesh lotMesh in lotMeshes)
            {
                foreach (Triangle tri in lotMesh.triangles)
                {
                    Gizmos.DrawLine(tri.A, tri.B);
                    Gizmos.DrawLine(tri.A, tri.C);
                    Gizmos.DrawLine(tri.B, tri.C);
                }
            }
        }

        public void DrawEdges(List<Edge> edges, Color color)
        {
            for (int x = edges.Count - 1; x > -1; x--) //for loop start from backwards, because the list is getting new elements while beeing read
            {
                Gizmos.color = color;
                Vector3 from = new Vector3(edges[x].NodeA.X, edges[x].NodeA.Y, 0f);
                Vector3 to = new Vector3(edges[x].NodeB.X, edges[x].NodeB.Y, 0f);
                Gizmos.DrawLine(from, to);
            }
        }
    }
}
