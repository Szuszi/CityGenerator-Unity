using System.Collections.Generic;
using LotGeneration;
using UnityEngine;

namespace MeshGeneration
{
    class MeshGenerator
    {
        private readonly List<Lot> lots;
        private float lotHeight;

        public List<Lot> ConvexLots { get; private set; }
        public List<Lot> ConcaveLots { get; private set; }
        public readonly List<LotMesh> LotMeshes;

        public MeshGenerator(List<Lot> lotToTriangulate, float lotDepht)
        {
            lots = lotToTriangulate;
            lotHeight = lotDepht;
            ConvexLots = new List<Lot>();
            ConcaveLots = new List<Lot>();
            LotMeshes = new List<LotMesh>();

        }

        public void GenerateMeshes()
        {
            TriangulateLots();
            LotSideGeneration();
        }

        private void TriangulateLots() //Triangulate Lots, and make meshes out of them
        {
            //Make LotMeshes
            foreach(Lot lot in lots)
            {
                if (LotIsConvex(lot))
                {
                    ConvexLots.Add(lot); //For visualization
                    ConvexTriangulation(lot);
                }
                else
                {
                    ConcaveLots.Add(lot); //For visualization
                    ConcaveTriangulation(lot);
                }
            }

            //Make sure the triangles are oriented clockwise
            foreach (LotMesh lotMesh in LotMeshes)
            {
                foreach(Triangle t in lotMesh.Triangles)
                {
                    if(!IsTriangleOrientedClockwise(t.A, t.B, t.C))
                    {
                        Vector3 temp = t.B;
                        t.B = t.C;
                        t.C = temp;
                    }
                }
            }
        }

        private void LotSideGeneration()
        {
            foreach(LotMesh lotMesh in LotMeshes)
            {
                for(int i = 0; i < lotMesh.Lot.Nodes.Count; i++)
                {
                    LotNode A;
                    LotNode B;

                    if(i == lotMesh.Lot.Nodes.Count - 1)
                    {
                        A = lotMesh.Lot.Nodes[i];
                        B = lotMesh.Lot.Nodes[0];
                    }
                    else
                    {
                        A = lotMesh.Lot.Nodes[i];
                        B = lotMesh.Lot.Nodes[i+1];
                    }

                    Vector3 vecA = new Vector3(A.X, lotHeight, A.Y);
                    Vector3 vecB = new Vector3(B.X, lotHeight, B.Y);
                    Vector3 vecAA = new Vector3(A.X, 0.0f, A.Y);
                    Vector3 vecBB = new Vector3(B.X, 0.0f, B.Y);

                    Triangle tri1;
                    Triangle tri2;

                    if (IsCounterClockwise(lotMesh.Lot))
                    {
                        tri1 = new Triangle(vecA, vecBB, vecAA);
                        tri2 = new Triangle(vecA, vecB, vecBB);
                    }
                    else
                    {
                        tri1 = new Triangle(vecB, vecAA, vecBB);
                        tri2 = new Triangle(vecB, vecA, vecAA);
                    }

                    lotMesh.SideTriangles.Add(tri1);
                    lotMesh.SideTriangles.Add(tri2);
                }
            }
        }



        /**
         *  FUNCTION USED FOR TRIANGULATION
         */

        //This function returns if the shape of the Lot is convex or not
        //IMPORTANT NOTE: Lot has to be non intersected with himself
        //Method is from the link: http://csharphelper.com/blog/2014/07/determine-whether-a-polygon-is-convex-in-c/
        private bool LotIsConvex(Lot lot)
        {
            // For each set of three adjacent points find the cross product. 
            // If the sign of all the cross products is the same, the angles are all positive or negative (depending on the order in which we visit them) so the polygon is convex.

            bool gotNegative = false;
            bool gotPositive = false;

            int numPoints = lot.Nodes.Count;

            int B, C;

            for (int A = 0; A < numPoints; A++)
            {
                B = (A + 1) % numPoints;
                C = (B + 1) % numPoints;

                float crossProduct = CrossProductLength(
                        lot.Nodes[A].X, lot.Nodes[A].Y,
                        lot.Nodes[B].X, lot.Nodes[B].Y,
                        lot.Nodes[C].X, lot.Nodes[C].Y);

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

        //This function is responsible for making a LotMesh from the given convex Lot
        //Method is from the link: https://www.habrador.com/tutorials/math/10-triangulation/
        private void ConvexTriangulation(Lot lot)
        {
            LotMesh convexMesh = new LotMesh(lot);
            
            for (int i = 2; i < lot.Nodes.Count; i++)
            {
                Vector3 a = new Vector3(lot.Nodes[0].X, lot.Nodes[0].Y, 0); //For gizmos, we make the Vectors like this now, later Y goes to Z (Y is up)
                Vector3 b = new Vector3(lot.Nodes[i-1].X, lot.Nodes[i-1].Y, 0);
                Vector3 c = new Vector3(lot.Nodes[i].X, lot.Nodes[i].Y, 0);

                convexMesh.AddTriangle(a, b, c);
            }

            LotMeshes.Add(convexMesh);
        }

        //This function is responsible for making a LotMesh from the given concave Lot
        //The points on the polygon should be ordered counter-clockwise, Triangulation is made with ear-clipping
        //Method is from the link: https://www.habrador.com/tutorials/math/10-triangulation/
        private void ConcaveTriangulation(Lot lot) 
        {
            //Step 0. Check if the lot stores the vertexes counter clockwise or not
            bool counterClockwise = IsCounterClockwise(lot);

            //The list with triangles, that the ear clipping algorithm will generate
            List<Triangle> triangles = new List<Triangle>();

            //Step 1. Store the vertices in a list and we also need to know the next and prev vertex
            List<Vertex> vertices = new List<Vertex>();

            if (counterClockwise)
            {
                for (int i = 0; i < lot.Nodes.Count; i++)
                {
                    vertices.Add(new Vertex(new Vector3(lot.Nodes[i].X, 0f, lot.Nodes[i].Y)));
                }
            }
            else //The method requires the verex list to be counter-clockwise
            {
                for (int i = lot.Nodes.Count - 1; i > -1 ; i--)
                {
                    vertices.Add(new Vertex(new Vector3(lot.Nodes[i].X, 0f, lot.Nodes[i].Y)));
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

            //Step4 insert the triangles inside the LotMesh
            LotMesh concaveMesh = new LotMesh(lot);

            foreach(Triangle tri in triangles)
            {
                Vector3 A = new Vector3(tri.A.x, tri.A.z, tri.A.y); //We want to draw the lotMeshes as gizmos right now! (up is z right now in the 2d model)
                Vector3 B = new Vector3(tri.B.x, tri.B.z, tri.B.y);
                Vector3 C = new Vector3(tri.C.x, tri.C.z, tri.C.y);

                concaveMesh.Triangles.Add(new Triangle(A, B, C));
            }
            
            LotMeshes.Add(concaveMesh);
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
        //p is the testpoint, and the other points are corners in the triangle
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

        //returns true if a given lot stores the nodes counter clockwise
        //Method is from: https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order
        private bool IsCounterClockwise(Lot lot)
        {
            float sum = 0.0f;
            for(int i = 0; i < lot.Nodes.Count; i++)
            {
                if(i == lot.Nodes.Count - 1) sum += (lot.Nodes[i].X * lot.Nodes[0].Y - lot.Nodes[i].Y * lot.Nodes[0].X); //Last node reached
                else sum += (lot.Nodes[i].X * lot.Nodes[i+1].Y - lot.Nodes[i].Y * lot.Nodes[i+1].X);
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
