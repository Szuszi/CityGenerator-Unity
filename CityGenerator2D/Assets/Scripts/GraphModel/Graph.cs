using System.Collections.Generic;

namespace Assets.Scripts
{
    class Graph
    {
        public List<Node> MajorNodes { get; private set; } //Főutakhoz
        public List<Edge> MajorEdges { get; private set; }
        public List<Node> MinorNodes { get; private set; } //Mellékutakhoz
        public List<Edge> MinorEdges { get; private set; }

        public Graph()
        {
            MajorNodes = new List<Node>();
            MinorNodes = new List<Node>();

            MajorEdges = new List<Edge>();
            MinorEdges = new List<Edge>();
        }
    }
}
