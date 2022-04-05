using System.Collections.Generic;
using BlockGeneration;
using UnityEngine;

namespace MeshGeneration
{
    class BlockMesh
    {
        public readonly Block Block;
        public readonly List<Triangle> Triangles;
        public readonly List<Triangle> SideTriangles;
        public readonly float Height;

        public BlockMesh (Block blockToUse, float blockHeight) 
        {
            Block = blockToUse;
            Triangles = new List<Triangle>();
            SideTriangles = new List<Triangle>();
            Height = blockHeight;
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Triangles.Add(new Triangle(a, b, c));
        }
    }

    class Triangle
    {
        public Vector3 A { get; set; }
        public Vector3 B { get; set; }
        public Vector3 C { get; set; }

        public Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            A = a;
            B = b;
            C = c;
        }
    }
}
