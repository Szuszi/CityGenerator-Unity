using MeshGeneration;
using UnityEngine;

namespace Services
{
    static class MeshCreateService
    {
        public static Mesh GenerateRoadMesh(int mapSize)
        {
            Mesh roadMesh = new Mesh();

            roadMesh.vertices = new[]
            {
                new Vector3(mapSize, 0f, mapSize),
                new Vector3(-mapSize, 0f, mapSize),
                new Vector3(mapSize, 0f, -mapSize),
                new Vector3(-mapSize, 0f, -mapSize)
            };

            roadMesh.triangles = new []
            {
                0, 2, 1,
                2, 3, 1
            };

            roadMesh.RecalculateNormals();

            return roadMesh;
        }

        public static Mesh GenerateBlockMesh(BlockMesh block)
        {
            Mesh lMesh = new Mesh();

            int numTriangles = block.Triangles.Count + block.SideTriangles.Count;

            var vertices = new Vector3[numTriangles * 3];  //Not the most optimized way, because same vertex can be stored more than once
            var triangles = new int[numTriangles * 3];

            //Add front panels
            for (int i = 0; i < block.Triangles.Count; i++)
            {
                //Change the Vectors (Y will be up vector)
                Vector3 A = new Vector3(block.Triangles[i].A.x, block.Height, block.Triangles[i].A.y);
                Vector3 B = new Vector3(block.Triangles[i].B.x, block.Height, block.Triangles[i].B.y);
                Vector3 C = new Vector3(block.Triangles[i].C.x, block.Height, block.Triangles[i].C.y);

                //Add attributes to Mesh
                vertices[3 * i] = A;
                vertices[3 * i + 1] = B;
                vertices[3 * i + 2] = C;

                triangles[3 * i] = 3 * i;
                triangles[3 * i + 1] = 3 * i + 1;
                triangles[3 * i + 2] = 3 * i + 2;
            }

            //Add side panels
            for (int i = block.Triangles.Count; i < numTriangles; i++)
            {
                vertices[3 * i] = block.SideTriangles[i - block.Triangles.Count].A;
                vertices[3 * i + 1] = block.SideTriangles[i - block.Triangles.Count].B;
                vertices[3 * i + 2] = block.SideTriangles[i - block.Triangles.Count].C;

                triangles[3 * i] = 3 * i;
                triangles[3 * i + 1] = 3 * i + 1;
                triangles[3 * i + 2] = 3 * i + 2;
            }

            lMesh.vertices = vertices;
            lMesh.triangles = triangles;

            lMesh.RecalculateNormals();

            return lMesh;
        }
    }
}
