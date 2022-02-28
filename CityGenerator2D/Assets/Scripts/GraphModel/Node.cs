using System.Collections.Generic;
using BlockGeneration;
using UnityEngine;

namespace GraphModel
{
    class Node
    {
        public float X { get; private set; }
        public float Y { get; private set; }

        public List<Edge> Edges { get; private set; } //The node knows, which edges includes it -> The Node knows it's neighbours
        public List<BlockNode> BlockNodes { get; private set; } //The node knows which blockNodes connected to it. The size depends on the number of edges

        public Node(float x, float y)
        {
            X = x;
            Y = y;

            Edges = new List<Edge>();
            BlockNodes = new List<BlockNode>();
        }

        public void AddEdge(Edge edge)
        {
            Edges.Add(edge);
        }


        //This function check if in a direction's surroundings (+-60 degrees) , there is any edges already
        public bool IsFree(float dirRad)
        {
            foreach (Edge edge in Edges)
            {
                if (edge.NodeA == this)
                {
                    if (Mathf.Abs(edge.DirRadianFromA - dirRad) < (Mathf.PI / 3))
                    {
                        return false;
                    }
                    if (dirRad > (2.5f * Mathf.PI / 3) && edge.DirRadianFromA < (-2.5f * Mathf.PI / 3) || dirRad < (-2.5f * Mathf.PI / 3) && edge.DirRadianFromA > (2.5f * Mathf.PI / 3)) //Special case, when the two radians are around PI and -PI
                    {
                        return false;
                    }
                }
                else if (edge.NodeB == this)
                {
                    if (Mathf.Abs(edge.DirRadianFromB - dirRad) < (Mathf.PI / 3))
                    {
                        return false;
                    }
                    if (dirRad > (2.5f * Mathf.PI / 3) && edge.DirRadianFromB < (-2.5f * Mathf.PI / 3) || dirRad < (-2.5f * Mathf.PI / 3) && edge.DirRadianFromB > (2.5f * Mathf.PI / 3)) //Special case, when the two radians are around PI and -PI
                    {
                        return false;
                    }
                }
                else
                {
                    Debug.Log("This shouldn't happen");
                    return false;
                }
            }
            return true;
        }
    }
}
