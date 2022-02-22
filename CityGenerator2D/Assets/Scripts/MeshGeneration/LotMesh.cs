using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    class LotMesh
    {
        public readonly Lot lot;
        public readonly List<Triangle> triangles;
        public readonly List<Triangle> sideTriangles;

        public LotMesh (Lot lotToUse) 
        {
            lot = lotToUse;
            triangles = new List<Triangle>();
            sideTriangles = new List<Triangle>();
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            triangles.Add(new Triangle(a, b, c));
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
