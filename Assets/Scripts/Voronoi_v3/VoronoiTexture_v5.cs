using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using System.IO;

public class VoronoiTexture_v5 : MonoBehaviour {

    [SerializeField] private int size;
    [SerializeField] private int numPoints;
    [SerializeField] private Material material;

    // Array to store pixel colors for the texture
    private Color[] pixelColors;
    private Color[] regionColors;

    // List to store the pixel vertexes
    private List<Vector3>[] areaVertexes;
    List<Vector3>[] areaVertexesClean;

    List<Vector3>[] listVerticesArea;

    void Start() {
        // Increase the number of points by 10 for the collision area
        numPoints += 20;
        areaVertexes = new List<Vector3>[numPoints];
        for (int i = 0; i < numPoints; i++) {
            areaVertexes[i] = new List<Vector3>();
        }

        areaVertexesClean = new List<Vector3>[numPoints];
        for (int i = 0; i < numPoints; i++) {
            areaVertexesClean[i] = new List<Vector3>();
        }

        pixelColors = new Color[size * size];
        regionColors = new Color[numPoints];

        listVerticesArea = new List<Vector3>[numPoints];
        for (int i = 0; i < numPoints; i++) {
            listVerticesArea[i] = new List<Vector3>();
        }
    }

    public void CreateVoronoi(Vector3 collisionPoint) {
        Debug.Log("COL:" + collisionPoint);
        // Arrays to store Voronoi points and corresponding region colors
        Vector3[] points = new Vector3[numPoints];

        // Generate random points and colors
        //V3: we supose the order of 'regionColors[]' tells us which point of 'points[]' is assigned to each color area
        for (int i = 0; i < numPoints - 20; i++) {
            points[i] = new Vector3(UnityEngine.Random.Range(0, size), 0, UnityEngine.Random.Range(0, size));
        }

        for (int i = numPoints - 20; i < numPoints; i++) {
            points[i] = new Vector3(collisionPoint.x + UnityEngine.Random.Range(1f, 100f), 0, collisionPoint.z + UnityEngine.Random.Range(1f, 100f));
        }

        for (int i = 0; i < numPoints; i++) {
            regionColors[i] = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1f);
        }

        // Generate Voronoi diagram by assigning colors based on the nearest point
        for (int z = 0; z < size; z++) {
            for (int x = 0; x < size; x++) {
                float distance = float.MaxValue;
                int value = 0;

                // Find the nearest point for each pixel
                for (int i = 0; i < numPoints; i++) {
                    if (Vector3.Distance(new Vector3(x, 0, z), points[i]) < distance) {
                        distance = Vector3.Distance(new Vector3(x, 0, z), points[i]);
                        value = i;
                    }
                }

                // Assign the color of the nearest point to the pixel
                pixelColors[x + z * size] = regionColors[value % numPoints];
            }
        }

        // Set specific pixels to black to highlight the Voronoi points
        foreach (Vector3 point in points) {
            int x = (int)point.x;
            int z = (int)point.z;
            pixelColors[x + z * size] = Color.black;
        }

        // We check the vertex of each area
        for (int i = 0; i < points.Length; i++) {
            checkVertexes(points, i, regionColors);
        }

        // We'll add to the vertexes array the corners of the plane
        AddCorners(pixelColors, regionColors);

        // We remove the duplicate values of every list for each area
        bool isIn = false;
        for (int i = 0; i < numPoints; i++) {
            List<Vector3> aux = new List<Vector3>();
            for (int j = 0; j < areaVertexes[i].Count; j++) {
                isIn = false;
                for (int k = 0; k < aux.Count; k++) {
                    if (areaVertexes[i][j] == aux[k]) {
                        isIn = true;
                    }
                }
                if (!isIn) {
                    aux.Add(areaVertexes[i][j]);
                }
            }
            areaVertexesClean[i] = aux;
        }

        // We provisionally organize the vertices 
        for (int i = 0; i < areaVertexesClean.Length; i++) {
            for (int j = 0; j < areaVertexesClean[i].Count - 1; j++) {
                for (int k = 0; k < areaVertexesClean[i].Count - j - 1; k++) {
                    if (areaVertexesClean[i][k].x > areaVertexesClean[i][k + 1].x && areaVertexesClean[i][k].z > areaVertexesClean[i][k + 1].z) {
                        var tempVar = areaVertexesClean[i][k];
                        areaVertexesClean[i][k] = areaVertexesClean[i][k + 1];
                        areaVertexesClean[i][k + 1] = tempVar;
                    }
                }
            }
        }

        // Create and apply the texture to the GameObject's material
        Texture2D texture = new Texture2D(size, size);
        texture.SetPixels(pixelColors);
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;
    }

    private void checkVertexes(Vector3[] points, int indexArea, Color[] regionColors) {

        //We'll use ecuations of the line, normal lines and intersection points to find the vertexes in which different area colors converge
        //We'll check the vertexes of an specific area
        Vector3 actualPoint = points[indexArea];

        //We use a dictionary to store the distance between the main point and the others
        Dictionary<Vector3, float> pointsDistances = new Dictionary<Vector3, float>();

        // Auxiliary arrays to store the variables of each the normal line ecuations 
        float[] mNormals = new float[points.Length - 1];
        float[] bNormals = new float[points.Length - 1];
        int indexPoints = 0;

        // 1� We calculate the distance between the main point and the others
        for (int i = 0; i < points.Length; i++) {
            if (i != indexArea) {
                pointsDistances.Add(points[i], Vector3.Distance(actualPoint, points[i]));
            }
        }

        // We order it from min to max distance value (Value)
        pointsDistances = pointsDistances.OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // 2� From shorter to longer distance, we find the normal line to each pair of points
        foreach (var kvp in pointsDistances) {

            // Calculate the slope of the line, and the normal
            float m = (kvp.Key.z - actualPoint.z) / (kvp.Key.x - actualPoint.x);
            float mNormal = -1 / m;

            // We use the middle point of the line to find the normal line ecuation
            Vector3 middlePoint = new Vector3((actualPoint.x + kvp.Key.x) / 2, 0, (actualPoint.z + kvp.Key.z) / 2);
            float bNormal = middlePoint.z - mNormal * middlePoint.x;

            // We store the variables of the normal ecuations, the indexPoint tells us which point they belong to
            mNormals[indexPoints] = mNormal;
            bNormals[indexPoints] = bNormal;

            indexPoints++;
        }

        // 3� Once we have all the normals, we check where they converge
        int indexI = 0;
        int indexJ = 0;

        foreach (var i in pointsDistances) {
            // For each line ecuation we'll compare with the borders (also line ecuations)
            foreach (var j in pointsDistances) {
                // We won't compare the same line with itself
                if (indexI != indexJ) {
                    // First we check that they aren't parallel lines
                    if (mNormals[indexI] != mNormals[indexJ]) {
                        // Then we calculate the intersection point of both line ecuations
                        Vector3 intersectionPoint = CalculateIntersectionPoint(mNormals[indexI], bNormals[indexI], mNormals[indexJ], bNormals[indexJ]);

                        // We have to make sure that the intersection point is within the bounds of the plane size * size
                        if (intersectionPoint.x <= size && intersectionPoint.z <= size && intersectionPoint.x >= 0 && intersectionPoint.z >= 0) {
                            // We check the color of the intersection point
                            Color interPointColor = pixelColors[(int)intersectionPoint.x + (int)intersectionPoint.z * size];

                            // If it is the same color as the main point's area, the intersection point is a vertex of said area
                            if (interPointColor == regionColors[indexArea]) {
                                Vector3 pointA = i.Key;
                                Vector3 pointB = j.Key;
                                int indexAinPoints = 0;
                                int indexBinPoints = 0;

                                for (int k = 0; k < points.Length; k++) {
                                    if (points[k] == pointA) {
                                        indexAinPoints = k;
                                    } else if (points[k] == pointB) {
                                        indexBinPoints = k;
                                    }
                                }

                                areaVertexes[indexArea].Add(intersectionPoint);
                                areaVertexes[indexAinPoints].Add(intersectionPoint);
                                areaVertexes[indexBinPoints].Add(intersectionPoint);
                            }
                        }
                    }
                }
                indexJ++;
            }
            indexI++;
            indexJ = 0;
        }

        // 4� We do the same but with the limits of the plane
        List<float> limitLines = new List<float>();

        //We'll add the limits of the plane, 4 in this case
        /*
            A                  B
            --------------------
            |                  |
            |                  |
            |                  |
            |                  |
            |                  |
            |                  |
            |                  |
            |                  |
            --------------------
            C                  D


            Recta CA: x = 0   
            Recta AB: y = size - 1
            Recta CD: y = 0
            Recta DB: x = size - 1
        */

        // impar: x   par: y
        limitLines.Add(0);
        limitLines.Add(0);
        limitLines.Add(size - 1);
        limitLines.Add(size - 1);

        indexI = 0;
        foreach (var i in pointsDistances) {
            // For each line ecuation we'll compare with the borders (also line ecuations)
            for (int j = 0; j < limitLines.Count; j++) {
                if (mNormals[indexI] != limitLines[j]) {
                    Vector3 intersectionPoint = new Vector3(0, 0, 0);

                    // impar: x   par: y
                    if (j % 2 == 0) {
                        intersectionPoint = CalculateHorizontalLines(mNormals[indexI], bNormals[indexI], limitLines[j]);
                    } else {
                        intersectionPoint = CalculateVerticalLines(mNormals[indexI], bNormals[indexI], limitLines[j]);
                    }

                    if (intersectionPoint.x < size && intersectionPoint.z < size && intersectionPoint.x >= 0 && intersectionPoint.z >= 0) {
                        // We check the color of the intersection point
                        Color interPointColor = pixelColors[(int)intersectionPoint.x + (int)intersectionPoint.z * size];

                        // If it is the same color as the main point's area, this intersection point is a vertex of said area
                        if (interPointColor == regionColors[indexArea]) {
                            Vector3 pointA = i.Key;
                            int indexAinPoints = 0;

                            for (int k = 0; k < points.Length; k++) {
                                if (points[k] == pointA) {
                                    indexAinPoints = k;
                                }
                            }
                            areaVertexes[indexArea].Add(intersectionPoint);
                            areaVertexes[indexAinPoints].Add(intersectionPoint);
                        }
                    }
                }
            }
            indexI++;
        }
    }

    private Vector3 CalculateIntersectionPoint(float m1, float b1, float m2, float b2) {
        float x = (b2 - b1) / (m1 - m2);
        float y = m1 * x + b1;

        return new Vector3(x, 0, y);
    }

    private Vector3 CalculateHorizontalLines(float m1, float b1, float y) {
        // Calcular x utilizando la ecuaci�n y = mx + b
        float xIntersection = (y - b1) / m1;

        return new Vector3(xIntersection, 0, y);
    }

    private Vector3 CalculateVerticalLines(float m1, float b1, float x) {
        // Punto de intersecci�n con la recta vertical x = size
        float yIntersection = m1 * x + b1;

        return new Vector3(x, 0, yIntersection);
    }

    private void AddCorners(Color[] pixelColors, Color[] regionColors) {
        // For each of the colors, we check if the corner is the same color as its area
        for (int i = 0; i < regionColors.Length; i++) { // x + z * size
                                                        //If it is, we add the corner to that area's vertexes
            if (pixelColors[0] == regionColors[i]) // x = 0, z = 0
            {
                areaVertexes[i].Add(new Vector3(0, 0, 0));
            } else if (pixelColors[size - 1] == regionColors[i]) // x = size - 1, z = 0
              {
                areaVertexes[i].Add(new Vector3(size - 1, 0, 0));
            } else if (pixelColors[(size - 1) * size] == regionColors[i]) // x = 0, z = size - 1
              {
                areaVertexes[i].Add(new Vector3(0, 0, size - 1));
            } else if (pixelColors[size - 1 + (size - 1) * size] == regionColors[i]) // x = size - 1, z = size - 1
              {
                areaVertexes[i].Add(new Vector3(size - 1, 0, size - 1));
            }
        }
    }

    private void orderVerticesForNewMesh() {
        // Por cada �rea, tenemos que calcular el orden de sus v�rtices
        List<Vector3> arrayVertices;

        for (int v = 0; v < listVerticesArea.Length; v++) {
            // Array of vertices of an area
            arrayVertices = new List<Vector3>();
            Vector3[] auxVertices = areaVertexesClean[v].ToArray();

            // 1� Hacer la Z media entre la Z m�xima y la Z m�nima
            float maxZ = float.MinValue;
            float minZ = float.MaxValue;

            // We calculate max z
            for (int i = 0; i < auxVertices.Length; i++) {
                if (auxVertices[i].z > maxZ) {
                    maxZ = auxVertices[i].z;
                }
            }
            // We calculate min z
            for (int i = 0; i < auxVertices.Length; i++) {
                if (auxVertices[i].z < minZ) {
                    minZ = auxVertices[i].z;
                }
            }
            float mediumZ = (maxZ + minZ) / 2;

            // 2� Hacer 2 grupos de v�rtices (los que ets�n por encima de la Z media y los que est�n por debajo) y asignar los v�rtices seg�n sus Z
            List<Vector3> topVertices = new List<Vector3>();
            List<Vector3> bottomVertices = new List<Vector3>();

            for (int i = 0; i < auxVertices.Length; i++) {
                if (auxVertices[i].z > mediumZ) {
                    topVertices.Add(auxVertices[i]);
                } else if (auxVertices[i].z <= mediumZ) {
                    bottomVertices.Add(auxVertices[i]);
                }
            }

            // 3� Ordenar ambos grupos para que los v�rtices est�n ordenados en sentido horario
            // COMPROBAR TAMBI�N LA Z
            // por debajo --> de mayor a menor
            for (int j = 0; j < bottomVertices.Count - 1; j++) {
                for (int k = 0; k < bottomVertices.Count - j - 1; k++) {
                    if (bottomVertices[k].x < bottomVertices[k + 1].x) {
                        var tempVar = bottomVertices[k];
                        bottomVertices[k] = bottomVertices[k + 1];
                        bottomVertices[k + 1] = tempVar;
                    }
                }
            }

            // por encima --> de menor a mayor
            for (int j = 0; j < topVertices.Count - 1; j++) {
                for (int k = 0; k < topVertices.Count - j - 1; k++) {
                    if (topVertices[k].x > topVertices[k + 1].x) {
                        var tempVar = topVertices[k];
                        topVertices[k] = topVertices[k + 1];
                        topVertices[k + 1] = tempVar;
                    }
                }
            }

            // 4� Juntarlos
            int indexMainArray = 0;
            for (int i = 0; i < topVertices.Count; i++) {
                arrayVertices.Add(topVertices[i]);
                indexMainArray++;
            }

            for (int i = 0; i < bottomVertices.Count; i++) {
                arrayVertices.Add(bottomVertices[i]);
                indexMainArray++;
            }

            listVerticesArea[v] = arrayVertices;
        }
    }

    private void breakMesh() {
        /*
        A mesh is composed of an array of Vector3 for vertices and array of integers for the triangles

        Mesh: 
            Vector3[] vertices
            int[] triangles
        */

        for (int v = 0; v < numPoints; v++) {
            // For each of the points we create a mesh
            Mesh newMesh = new Mesh();
            newMesh.name = "mesh_" + v;

            // We get the the vertices from the listVerticesArea, that has been previously cleaned and organized
            newMesh.vertices = listVerticesArea[v].ToArray();

            int auxVertices = 1;
            List<int> aux = new List<int>();

            // If there are at least 3 vertices, which should always be true, we start to create triangles
            if (newMesh.vertices.Length >= 3) {
                // The number of triangles is the ammount of vertices minus two
                newMesh.triangles = new int[(newMesh.vertices.Length - 2) * 6];

                for (int j = 1; j <= newMesh.vertices.Length - 2; j++) {
                    // As the vertices are already organized, we pivot from the first one like this: (0 1 2), (0 2 3), ...
                    aux.Add(0);
                    aux.Add(auxVertices++);
                    aux.Add(auxVertices);
                }
            }
            newMesh.triangles = aux.ToArray();

            //Create a gameobject for each of the resulting pieces
            GameObject piece = new GameObject(name = "piece_" + v);

            //Add the generated mesh and a glass-like material to the piece
            piece.AddComponent<MeshFilter>();
            piece.AddComponent<MeshRenderer>();
            piece.GetComponent<MeshFilter>().mesh = newMesh;
            piece.GetComponent<MeshRenderer>().material = material;

            //Adjust the position and the scale of the piece
            piece.transform.position = new Vector3(0, -2.39f, 0);
            piece.transform.localScale = new Vector3(0.003120452f, 0.003120452f, 0.003120452f); //Adjusted to size = 512
            piece.transform.rotation = Quaternion.Euler(225, -45, -90);

            //Add the physics elements so that it has a weight and a collider
            piece.AddComponent<MeshCollider>();
            piece.GetComponent<MeshCollider>().convex = true;
            piece.AddComponent<Rigidbody>();
            piece.GetComponent<Rigidbody>().velocity = GetComponent<Rigidbody>().velocity;
            piece.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
            piece.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    private void OnCollisionEnter(Collision other) {
        // If the plane has collided with the floor, we "break" it
        if (other.gameObject.tag == "Suelo") {
            Vector3 collisionPoint = other.collider.ClosestPoint(transform.position);
            CreateVoronoi(collisionPoint);
            orderVerticesForNewMesh();
            breakMesh();
            this.gameObject.SetActive(false);
            Debug.Log("End");
        }
    }
}