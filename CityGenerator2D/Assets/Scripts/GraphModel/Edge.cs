using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    class Edge
    {
        public Node NodeA { get; private set; }
        public Node NodeB { get; private set; }

        public float DirRadianFromA { get; private set; }
        public float DirRadianFromB { get; private set; }

        public Edge(Node first, Node second)
        {
            NodeA = first;
            NodeB = second;

            DirRadianFromA = Mathf.Atan2(second.Y - first.Y, second.X - first.X);
            DirRadianFromB = Mathf.Atan2(first.Y - second.Y, first.X - second.X);

            first.AddEdge(this);
            second.AddEdge(this);
        }
    }
}
