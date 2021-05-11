using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    class LotMesh
    {
        public readonly List<Triangle> triangles; 

        public LotMesh () 
        {
            triangles = new List<Triangle>();
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
