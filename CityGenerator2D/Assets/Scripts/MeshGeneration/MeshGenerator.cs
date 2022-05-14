using System;
using System.Collections.Generic;
using BlockGeneration;
using UnityEngine;

namespace MeshGeneration
{
    class MeshGenerator
    {
        private readonly List<Block> blocks;
        private readonly float blockHeight;

        public List<Block> ConvexBlocks { get; private set; }
        public List<Block> ConcaveBlocks { get; private set; }
        public readonly List<BlockMesh> BlockMeshes;

        public MeshGenerator
        (
            List<Block> blockToTriangulate,
            float blockDepth
        )
        {
            blocks = blockToTriangulate;
            blockHeight = blockDepth;
            ConvexBlocks = new List<Block>();
            ConcaveBlocks = new List<Block>();
            BlockMeshes = new List<BlockMesh>();
        }

        public void GenerateMeshes()
        {
            TriangulateBlocks();
            BlockSideGeneration();
        }

        private void TriangulateBlocks() //Triangulate Blocks, and make meshes out of them
        {
            //Make BlockMeshes
            foreach(Block block in blocks)
            {
                if (BlockIsConvex(block))
                {
                    ConvexBlocks.Add(block); //For visualization
                    ConvexTriangulation(block);
                }
                else
                {
                    ConcaveBlocks.Add(block); //For visualization
                    block.Height = blockHeight; //Concave lots should be flat
                    ConcaveTriangulation(block);
                }
            }

            //Make sure the triangles are oriented clockwise
            foreach (BlockMesh blockMesh in BlockMeshes)
            {
                foreach(Triangle t in blockMesh.Triangles)
                {
                    if(!IsTriangleOrientedClockwise(t.A, t.B, t.C))
                    {
                        (t.B, t.C) = (t.C, t.B);
                    }
                }
            }
        }

        private void BlockSideGeneration()
        {
            foreach(BlockMesh blockMesh in BlockMeshes)
            {
                for(int i = 0; i < blockMesh.Block.Nodes.Count; i++)
                {
                    float height = blockMesh.Height;

                    BlockNode A;
                    BlockNode B;

                    if(i == blockMesh.Block.Nodes.Count - 1)
                    {
                        A = blockMesh.Block.Nodes[i];
                        B = blockMesh.Block.Nodes[0];
                    }
                    else
                    {
                        A = blockMesh.Block.Nodes[i];
                        B = blockMesh.Block.Nodes[i+1];
                    }

                    Vector3 vecA = new Vector3(A.X, height, A.Y);
                    Vector3 vecB = new Vector3(B.X, height, B.Y);
                    Vector3 vecAA = new Vector3(A.X, 0.0f, A.Y);
                    Vector3 vecBB = new Vector3(B.X, 0.0f, B.Y);

                    Triangle tri1;
                    Triangle tri2;

                    if (IsCounterClockwise(blockMesh.Block))
                    {
                        tri1 = new Triangle(vecA, vecBB, vecAA);
                        tri2 = new Triangle(vecA, vecB, vecBB);
                    }
                    else
                    {
                        tri1 = new Triangle(vecB, vecAA, vecBB);
                        tri2 = new Triangle(vecB, vecA, vecAA);
                    }

                    blockMesh.SideTriangles.Add(tri1);
                    blockMesh.SideTriangles.Add(tri2);
                }
            }
        }



        /**
         *  FUNCTION USED FOR TRIANGULATION
         */

        //This function returns if the shape of the Block is convex or not
        //IMPORTANT NOTE: Block has to be non intersected with himself
        //Method is from the link: http://csharphelper.com/blog/2014/07/determine-whether-a-polygon-is-convex-in-c/
        private bool BlockIsConvex(Block block)
        {
            // For each set of three adjacent points find the cross product. 
            // If the sign of all the cross products is the same, the angles are all positive or negative (depending on the order in which we visit them) so the polygon is convex.

            bool gotNegative = false;
            bool gotPositive = false;

            int numPoints = block.Nodes.Count;

            int B, C;

            for (int A = 0; A < numPoints; A++)
            {
                B = (A + 1) % numPoints;
                C = (B + 1) % numPoints;

                float crossProduct = CrossProductLength(
                        block.Nodes[A].X, block.Nodes[A].Y,
                        block.Nodes[B].X, block.Nodes[B].Y,
                        block.Nodes[C].X, block.Nodes[C].Y);

                if (crossProduct < 0)
                {
                    gotNegative = true;
                }
                else if (crossProduct > 0)
                {
                    gotPositive = true;
                }

                if (gotNegative && gotPositive) return false;
            }

            return true;
        }

        //Return the cross product AB x BC.
        //Method is from the link: http://csharphelper.com/blog/2014/07/determine-whether-a-polygon-is-convex-in-c/
        private float CrossProductLength(float Ax, float Ay, float Bx, float By, float Cx, float Cy)
        {
            // Get the vectors' coordinates.
            float BAx = Ax - Bx;
            float BAy = Ay - By;
            float BCx = Cx - Bx;
            float BCy = Cy - By;

            return (BAx * BCy - BAy * BCx);
        }

        //This function is responsible for making a BlockMesh from the given convex Block
        //Method is from the link: https://www.habrador.com/tutorials/math/10-triangulation/
        private void ConvexTriangulation(Block block)
        {
            BlockMesh convexMesh = new BlockMesh(block);
            
            for (int i = 2; i < block.Nodes.Count; i++)
            {
                Vector3 a = new Vector3(block.Nodes[0].X, block.Nodes[0].Y, 0); //For gizmos, we make the Vectors like this now, later Y goes to Z (Y is up)
                Vector3 b = new Vector3(block.Nodes[i-1].X, block.Nodes[i-1].Y, 0);
                Vector3 c = new Vector3(block.Nodes[i].X, block.Nodes[i].Y, 0);

                convexMesh.AddTriangle(a, b, c);
            }

            BlockMeshes.Add(convexMesh);
        }

        //This function is responsible for making a BlockMesh from the given concave Block
        //The points on the polygon should be ordered counter-clockwise, Triangulation is made with ear-clipping
        //Method is from the link: https://www.habrador.com/tutorials/math/10-triangulation/
        private void ConcaveTriangulation(Block block) 
        {
            //Step 0. Check if the block stores the vertexes counter clockwise or not
            bool counterClockwise = IsCounterClockwise(block);

            //The list with triangles, that the ear clipping algorithm will generate
            List<Triangle> triangles = new List<Triangle>();

            //Step 1. Store the vertices in a list and we also need to know the next and prev vertex
            List<Vertex> vertices = new List<Vertex>();

            if (counterClockwise)
            {
                for (int i = 0; i < block.Nodes.Count; i++)
                {
                    vertices.Add(new Vertex(new Vector3(block.Nodes[i].X, 0f, block.Nodes[i].Y)));
                }
            }
            else //The method requires the vertex list to be counter-clockwise
            {
                for (int i = block.Nodes.Count - 1; i > -1 ; i--)
                {
                    vertices.Add(new Vertex(new Vector3(block.Nodes[i].X, 0f, block.Nodes[i].Y)));
                }
            }

            //Find the next and previous vertex
            vertices[0].PrevVertex = vertices[vertices.Count - 1];
            vertices[0].NextVertex = vertices[1];

            vertices[vertices.Count - 1].PrevVertex = vertices[vertices.Count - 2];
            vertices[vertices.Count - 1].NextVertex = vertices[0];

            for (int i = 1; i < vertices.Count - 1; i++)
            {
                vertices[i].PrevVertex = vertices[i-1];
                vertices[i].NextVertex = vertices[i+1];
            }

            //Step 2. Find the reflex (concave) and convex vertices, and ear vertices
            for (int i = 0; i < vertices.Count; i++)
            {
                CheckIfReflexOrConvex(vertices[i]);
            }

            //Have to find the ears after we have found if the vertex is reflex or convex
            List<Vertex> earVertices = new List<Vertex>();

            for (int i = 0; i < vertices.Count; i++)
            {
                IsVertexEar(vertices[i], vertices, earVertices);
            }

            //Step 3. Triangulate!
            while (true)
            {
                //This means we have just one triangle left
                if (vertices.Count == 3)
                {
                    //The final triangle
                    triangles.Add(new Triangle(vertices[0].Position, vertices[0].PrevVertex.Position, vertices[0].NextVertex.Position));

                    break;
                }

                if(earVertices.Count == 0)
                {
                    Debug.Log("earVertices not found, Triangulation failed.");
                    break;
                }

                //Make a triangle of the first ear
                Vertex earVertex = earVertices[0];

                Vertex earVertexPrev = earVertex.PrevVertex;
                Vertex earVertexNext = earVertex.NextVertex;

                Triangle newTriangle = new Triangle(earVertex.Position, earVertexPrev.Position, earVertexNext.Position);

                triangles.Add(newTriangle);

                //Remove the vertex from the lists
                earVertices.Remove(earVertex);
                vertices.Remove(earVertex);

                //Update the previous vertex and next vertex
                earVertexPrev.NextVertex = earVertexNext;
                earVertexNext.PrevVertex = earVertexPrev;

                //...see if we have found a new ear by investigating the two vertices that was part of the ear
                CheckIfReflexOrConvex(earVertexPrev);
                CheckIfReflexOrConvex(earVertexNext);

                earVertices.Remove(earVertexPrev);
                earVertices.Remove(earVertexNext);

                IsVertexEar(earVertexPrev, vertices, earVertices);
                IsVertexEar(earVertexNext, vertices, earVertices);
            }

            //Step4 insert the triangles inside the BlockMesh
            BlockMesh concaveMesh = new BlockMesh(block);

            foreach(Triangle tri in triangles)
            {
                Vector3 A = new Vector3(tri.A.x, tri.A.z, tri.A.y); //We want to draw the blockMeshes as gizmos right now! (up is z right now in the 2d model)
                Vector3 B = new Vector3(tri.B.x, tri.B.z, tri.B.y);
                Vector3 C = new Vector3(tri.C.x, tri.C.z, tri.C.y);

                concaveMesh.Triangles.Add(new Triangle(A, B, C));
            }
            
            BlockMeshes.Add(concaveMesh);
        }

        //Check if a vertex if reflex or convex, and add to appropriate list
        //Method is from the link: https://www.habrador.com/tutorials/math/10-triangulation/
        private void CheckIfReflexOrConvex(Vertex v)
        {
            v.IsReflex = false;
            v.IsConvex = false;

            //This is a reflex vertex if its triangle is oriented clockwise
            Vector2 a = v.PrevVertex.GetPos2D_XZ();
            Vector2 b = v.GetPos2D_XZ();
            Vector2 c = v.NextVertex.GetPos2D_XZ();

            if (IsTriangleOrientedClockwise(a, b, c))
            {
                v.IsReflex = true;
            }
            else
            {
                v.IsConvex = true;
            }
        }

        //Is a triangle in 2d space oriented clockwise or counter-clockwise
        //https://math.stackexchange.com/questions/1324179/how-to-tell-if-3-connected-points-are-connected-clockwise-or-counter-clockwise
        //https://en.wikipedia.org/wiki/Curve_orientation
        private bool IsTriangleOrientedClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            bool isClockWise = true;

            float determinant = p1.x * p2.y + p3.x * p1.y + p2.x * p3.y - p1.x * p3.y - p3.x * p2.y - p2.x * p1.y;

            if (determinant > 0f)
            {
                isClockWise = false;
            }

            return isClockWise;
        }

        //Check if a vertex is an ear
        private void IsVertexEar(Vertex v, List<Vertex> vertices, List<Vertex> earVertices)
        {
            //A reflex vertex cant be an ear!
            if (v.IsReflex)
            {
                return;
            }

            //This triangle to check point in triangle
            Vector2 a = v.PrevVertex.GetPos2D_XZ();
            Vector2 b = v.GetPos2D_XZ();
            Vector2 c = v.NextVertex.GetPos2D_XZ();

            bool hasPointInside = false;

            for (int i = 0; i < vertices.Count; i++)
            {
                //We only need to check if a reflex vertex is inside of the triangle
                if (vertices[i].IsReflex)
                {
                    Vector2 p = vertices[i].GetPos2D_XZ();

                    //This means inside and not on the hull
                    if (IsPointInTriangle(a, b, c, p))
                    {
                        hasPointInside = true;

                        break;
                    }
                }
            }

            if (!hasPointInside)
            {
                earVertices.Add(v);
            }
        }

        //From http://totologic.blogspot.se/2014/01/accurate-point-in-triangle-test.html
        //p is the test point, and the other points are corners in the triangle
        private bool IsPointInTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p)
        {
            bool isWithinTriangle = false;

            //Based on Barycentric coordinates
            float denominator = ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));

            float a = ((p2.y - p3.y) * (p.x - p3.x) + (p3.x - p2.x) * (p.y - p3.y)) / denominator;
            float b = ((p3.y - p1.y) * (p.x - p3.x) + (p1.x - p3.x) * (p.y - p3.y)) / denominator;
            float c = 1 - a - b;

            //The point is within the triangle
            if (a > 0f && a < 1f && b > 0f && b < 1f && c > 0f && c < 1f)
            {
                isWithinTriangle = true;
            }

            return isWithinTriangle;
        }

        //returns true if a given block stores the nodes counter clockwise
        //Method is from: https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order
        private bool IsCounterClockwise(Block block)
        {
            float sum = 0.0f;
            for(int i = 0; i < block.Nodes.Count; i++)
            {
                if(i == block.Nodes.Count - 1) sum += (block.Nodes[i].X * block.Nodes[0].Y - block.Nodes[i].Y * block.Nodes[0].X); //Last node reached
                else sum += (block.Nodes[i].X * block.Nodes[i+1].Y - block.Nodes[i].Y * block.Nodes[i+1].X);
            }
            return sum >= 0;
        }

        //Class is from the link: https://www.habrador.com/tutorials/math/10-triangulation/
        public class Vertex
        {
            public Vector3 Position;

            //The previous and next vertex this vertex is attached to
            public Vertex PrevVertex;
            public Vertex NextVertex;

            //Properties this vertex may have
            //Reflex is concave
            public bool IsReflex;
            public bool IsConvex;
            //public bool isEar;

            public Vertex(Vector3 position)
            {
                this.Position = position;
            }

            //Get 2d pos of this vertex
            public Vector2 GetPos2D_XZ()
            {
                Vector2 pos_2d_xz = new Vector2(Position.x, Position.z);

                return pos_2d_xz;
            }
        }
    }
}
