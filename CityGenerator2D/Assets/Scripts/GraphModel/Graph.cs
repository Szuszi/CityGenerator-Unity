using System.Collections.Generic;

namespace GraphModel
{
    class Graph
    {
        public List<Node> MajorNodes { get; private set; } //For the major roads
        public List<Edge> MajorEdges { get; private set; }
        public List<Node> MinorNodes { get; private set; } //For the minor roads
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
