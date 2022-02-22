using System.Collections.Generic;
using LotGeneration;
using UnityEngine;

namespace MeshGeneration
{
    class LotMesh
    {
        public readonly Lot Lot;
        public readonly List<Triangle> Triangles;
        public readonly List<Triangle> SideTriangles;

        public LotMesh (Lot lotToUse) 
        {
            Lot = lotToUse;
            Triangles = new List<Triangle>();
            SideTriangles = new List<Triangle>();
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
