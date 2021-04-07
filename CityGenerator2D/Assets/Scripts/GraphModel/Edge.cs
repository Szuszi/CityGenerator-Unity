using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge
{
    public Node NodeA { get; private set; }
    public Node NodeB { get; private set; }

    public float dirRadianFromA { get; private set; }
    public float dirRadianFromB { get; private set; }

    public Edge(Node first, Node second)
    {
        NodeA = first;
        NodeB = second;

        dirRadianFromA = Mathf.Atan2(second.Y - first.Y, second.X - first.X);
        dirRadianFromB = Mathf.Atan2(first.Y - second.Y, first.X - second.X);

        first.AddEdge(this);
        second.AddEdge(this);
    }
}
