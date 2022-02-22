using MeshGeneration;
using UnityEngine;

namespace Services
{
    class MeshCreateService
    {
        public Mesh GenerateRoadMesh(int mapSize)
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

        public Mesh GenerateLotMesh(LotMesh lot, float lotHeight)
        {
            Mesh lMesh = new Mesh();

            int numTriangles = lot.Triangles.Count + lot.SideTriangles.Count;

            var vertices = new Vector3[numTriangles * 3];  //Not the most optimized way, because same vertex can be stored more than once
            var triangles = new int[numTriangles * 3];

            //Add front panels
            for (int i = 0; i < lot.Triangles.Count; i++)
            {
                //Change the Vectors (Y will be up vector)
                Vector3 A = new Vector3(lot.Triangles[i].A.x, lotHeight, lot.Triangles[i].A.y);
                Vector3 B = new Vector3(lot.Triangles[i].B.x, lotHeight, lot.Triangles[i].B.y);
                Vector3 C = new Vector3(lot.Triangles[i].C.x, lotHeight, lot.Triangles[i].C.y);

                //Add attributes to Mesh
                vertices[3 * i] = A;
                vertices[3 * i + 1] = B;
                vertices[3 * i + 2] = C;

                triangles[3 * i] = 3 * i;
                triangles[3 * i + 1] = 3 * i + 1;
                triangles[3 * i + 2] = 3 * i + 2;
            }

            //Add side panels
            for (int i = lot.Triangles.Count; i < numTriangles; i++)
            {
                vertices[3 * i] = lot.SideTriangles[i - lot.Triangles.Count].A;
                vertices[3 * i + 1] = lot.SideTriangles[i - lot.Triangles.Count].B;
                vertices[3 * i + 2] = lot.SideTriangles[i - lot.Triangles.Count].C;

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
