using UnityEngine;

namespace Assets.Scripts
{
    class MeshCreateService
    {
        public Mesh GenerateRoadMesh(int mapSize)
        {
            Mesh RoadMesh = new Mesh();

            RoadMesh.vertices = new Vector3[]
            {
                new Vector3(mapSize, 0f, mapSize),
                new Vector3(-mapSize, 0f, mapSize),
                new Vector3(mapSize, 0f, -mapSize),
                new Vector3(-mapSize, 0f, -mapSize)
            };

            RoadMesh.triangles = new int[]
            {
                0, 2, 1,
                2, 3, 1
            };

            RoadMesh.RecalculateNormals();

            return RoadMesh;
        }

        public Mesh GenerateLotMesh(LotMesh lot, float lotHeight)
        {
            Mesh lMesh = new Mesh();

            int numTriangles = lot.triangles.Count + lot.sideTriangles.Count;

            var vertices = new Vector3[numTriangles * 3];  //Not the most optimized way, because same vertex can be stored more than once
            var triangles = new int[numTriangles * 3];

            //Add front panels
            for (int i = 0; i < lot.triangles.Count; i++)
            {
                //Change the Vectors (Y will be up vector)
                Vector3 A = new Vector3(lot.triangles[i].A.x, lotHeight, lot.triangles[i].A.y);
                Vector3 B = new Vector3(lot.triangles[i].B.x, lotHeight, lot.triangles[i].B.y);
                Vector3 C = new Vector3(lot.triangles[i].C.x, lotHeight, lot.triangles[i].C.y);

                //Add attributes to Mesh
                vertices[3 * i] = A;
                vertices[3 * i + 1] = B;
                vertices[3 * i + 2] = C;

                triangles[3 * i] = 3 * i;
                triangles[3 * i + 1] = 3 * i + 1;
                triangles[3 * i + 2] = 3 * i + 2;
            }

            //Add side panels
            for (int i = lot.triangles.Count; i < numTriangles; i++)
            {
                vertices[3 * i] = lot.sideTriangles[i - lot.triangles.Count].A;
                vertices[3 * i + 1] = lot.sideTriangles[i - lot.triangles.Count].B;
                vertices[3 * i + 2] = lot.sideTriangles[i - lot.triangles.Count].C;

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
