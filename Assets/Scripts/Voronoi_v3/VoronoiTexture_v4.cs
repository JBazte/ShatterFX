using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using UnityEngine.AI;

public class VoronoiTexture_v4 : MonoBehaviour {     

    [SerializeField] private int size;
    [SerializeField] private int numPoints;

    // Array to store pixel colors for the texture
    private Color[] pixelColors;
    private Color[] regionColors;

    //list to store the pixel vertexes
    private List<Vector3>[] areaVertexes;
    List<Vector3>[] areaVertexesClean;
    

private Mesh originalMesh;

    void Start()
    {
        areaVertexes = new List<Vector3>[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            areaVertexes[i] = new List<Vector3>();
        }

        areaVertexesClean = new List<Vector3>[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            areaVertexesClean[i] = new List<Vector3>();
        }

        pixelColors = new Color[size * size];
        originalMesh = transform.GetComponent<MeshFilter>().mesh;
        regionColors = new Color[numPoints];
        CreateVoronoi();
        breakMesh();
    }

    public void CreateVoronoi()
    {
        // Arrays to store Voronoi points and corresponding region colors
        Vector3[] points = new Vector3[numPoints];
        

        // Generate random points and colors
        //V3: we supose the order of 'regionColors[]' tells us which point of 'points[]' is assigned to each color area
        for (int i = 0; i < numPoints; i++)
        {
            points[i] = new Vector3(UnityEngine.Random.Range(0, size), 0, UnityEngine.Random.Range(0, size));
        }

        for (int i = 0; i < numPoints; i++)
        {
            regionColors[i] = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1f);
        }

        // Generate Voronoi diagram by assigning colors based on the nearest point
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = float.MaxValue;
                int value = 0;

                // Find the nearest point for each pixel
                for (int i = 0; i < numPoints; i++)
                {
                    if (Vector3.Distance(new Vector3(x, 0, z), points[i]) < distance)
                    {
                        distance = Vector3.Distance(new Vector3(x, 0, z), points[i]);
                        value = i;
                    }
                }

                // Assign the color of the nearest point to the pixel
                pixelColors[x + z * size] = regionColors[value % numPoints];
            }
        }

        // Set specific pixels to black to highlight the Voronoi points
        foreach (Vector3 point in points)
        {
            int x = (int)point.x;
            int z = (int)point.z;
            pixelColors[x + z * size] = Color.black;
        }

        //We check the vertex of each area
         for (int i=0; i<points.Length; i++)
         {
             checkVertexes(points, i, regionColors);
         }

        //we'll add to the vertexes array the corners of the plane
        AddCorners(pixelColors, regionColors);

        //We remove the duplicate values of every list for each area
        bool isIn = false;
        for (int i = 0; i < numPoints; i++)
        {
            List<Vector3> aux = new List<Vector3>();
            for (int j = 0; j < areaVertexes[i].Count; j++)
            {
                isIn = false;
                for (int k = 0; k < aux.Count; k++)
                {
                    if (areaVertexes[i][j] == aux[k])
                    {
                        isIn = true;
                    }
                }

                if(!isIn)
                {
                    aux.Add(areaVertexes[i][j]);
                    //Debug.Log(areaVertexes[i][j]);
                }
            }
            areaVertexesClean[i] = aux;
            //Debug.Log("num " + i + ": " +  aux.Count);
        }

        //for (int i = 0; i < areaVertexesClean.Length; i++)
        //{
        //    Debug.Log(areaVertexesClean[i].Count);
        //}

        for (int i = 0; i < areaVertexesClean.Length; i++)
        {
            for (int j = 0; j < areaVertexesClean[i].Count - 1; j++)
            {
                for (int k = 0; k < areaVertexesClean[i].Count - j -1; k++)
                    if (areaVertexesClean[i][k].x > areaVertexesClean[i][k + 1].x && areaVertexesClean[i][k].z > areaVertexesClean[i][k + 1].z)
                    {
                        var tempVar = areaVertexesClean[i][k];
                        areaVertexesClean[i][k] = areaVertexesClean[i][k + 1];
                        areaVertexesClean[i][k + 1] = tempVar;
                    }
            }
        }

        //we color the vertexes
        for (int i = 0; i < areaVertexesClean.Length; i++)
        {
            for (int j = 0; j < areaVertexesClean[i].Count; j++)
            {
                //Debug.Log(ColorUtility.ToHtmlStringRGB(regionColors[i]) + ": " + areaVertexesClean[i][j]);
                int x = (int)areaVertexesClean[i][j].x;
                int z = (int)areaVertexesClean[i][j].z;
                pixelColors[x + z * size] = Color.cyan;
            }
        }

        // Create and apply the texture to the GameObject's material
        Texture2D texture = new Texture2D(size, size);
        texture.SetPixels(pixelColors);
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;

        Debug.Log(ColorUtility.ToHtmlStringRGB(regionColors[0]) + " " + areaVertexesClean[0].Count);
    }

    private void checkVertexes(Vector3[] points, int indexArea, Color[] regionColors)
    {

        //We'll use ecuations of the line, normal lines and intersection points to find the vertexes in which different area colors converge

        //We'll check the vertexes of an specific area
        Vector3 actualPoint = points[indexArea];
        //We use a dictionary to store the distance between the main point and the others
        Dictionary<Vector3, float> pointsDistances = new Dictionary<Vector3, float>();

        //auxiliar arrays to store the variables of each the normal line ecuation 
        float[] mNormals = new float[points.Length - 1];
        float[] bNormals = new float[points.Length - 1]; 
        int indexPoints = 0;

        //1º We calculate the distance between the point and the others
        for (int i=0; i<points.Length; i++)
        {
            if (i != indexArea)
            {
                pointsDistances.Add(points[i],Vector3.Distance(actualPoint, points[i]));
            }
            
        }

        //We order it from min to max distance value (Value)
        pointsDistances = pointsDistances.OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        //2º From shorter to longer distance, we find the normal line to each pair of points
        foreach (var kvp in pointsDistances){

            //Calculate the slope of the line, and the normal
            float m = (kvp.Key.z - actualPoint.z) / (kvp.Key.x - actualPoint.x);
            float mNormal = -1 / m;

            //we use the middle point of the line to find the normal line ecuation
            Vector3 middlePoint = new Vector3((actualPoint.x + kvp.Key.x)/2, 0, (actualPoint.z + kvp.Key.z) / 2);
            float bNormal = middlePoint.z - mNormal * middlePoint.x;

            //We store the variables of the normal ecuations, the indexPoint tells us of which point
            mNormals[indexPoints] = mNormal;
            bNormals[indexPoints] = bNormal;

            indexPoints++;
        }

        //3º Once we have all the normals, we have to check where they converge
        int indexI = 0;
        int indexJ = 0;
        foreach (var i in pointsDistances)
        {
            //For each line ecuation we'll compare with the borders (also line ecuations)
            foreach (var j in pointsDistances)
            {
                //We'll not compare the same line
                if (indexI!=indexJ)
                {
                    //First we check if they aren't parallel lines
                    if (mNormals[indexI] != mNormals[indexJ])
                    {
                        //Then we calculate the intersection point of both line ecuations
                        Vector3 intersectionPoint = CalculateIntersectionPoint(mNormals[indexI], bNormals[indexI], mNormals[indexJ], bNormals[indexJ]);

                        //We have to make sure that the intersection point is within the bounds of the plane size * size
                        if (intersectionPoint.x <= size && intersectionPoint.z <= size && intersectionPoint.x >=0 && intersectionPoint.z >= 0)
                        {
                            
                            //see of which color is the intersection point
                            Color interPointColor = pixelColors[(int)intersectionPoint.x + (int)intersectionPoint.z * size];

                            //if it has the same color as the area of the main point, this intersection point is a vertex of this area
                            if (interPointColor == regionColors[indexArea])
                            {
                                Vector3 pointA = i.Key;
                                Vector3 pointB = j.Key;
                                int indexAinPoints = 0;
                                int indexBinPoints = 0;

                                for(int k = 0; k < points.Length; k++)
                                {
                                    if (points[k] == pointA)
                                    {
                                        indexAinPoints = k;
                                    }
                                    else if (points[k] == pointB)
                                    {
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



        //4º We do the same but with the limits of the plane

        List<float> limitLines = new List<float>();
        //impar: x   par: y
        limitLines.Add(0);
        limitLines.Add(0);
        limitLines.Add(size-1);
        limitLines.Add(size-1);

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
         Recta DB: x = size - 1 */
        indexI = 0;
        foreach (var i in pointsDistances)
        {
            //For each line ecuation we'll compare with the borders (also line ecuations)
            for(int j = 0; j < limitLines.Count; j++)
            {
                if (mNormals[indexI] != limitLines[j])
                {
                    Vector3 intersectionPoint = new Vector3(0, 0, 0);

                    //impar: x   par: y
                    if (j % 2 == 0)
                    {
                        intersectionPoint = CalculateHorizontalLines(mNormals[indexI], bNormals[indexI], limitLines[j]);

                    }
                    else
                    {
                        intersectionPoint = CalculateVerticalLines(mNormals[indexI], bNormals[indexI], limitLines[j]);
                    }



                    if (intersectionPoint.x < size && intersectionPoint.z < size && intersectionPoint.x >= 0 && intersectionPoint.z >= 0)
                    {
                        //Debug.Log(intersectionPoint);
                        //see of which color is the intersection point
                        Color interPointColor = pixelColors[(int)intersectionPoint.x + (int)intersectionPoint.z * size];

                        //if it has the same color as the area of the main point, this intersection point is a vertex of this area
                        if (interPointColor == regionColors[indexArea])
                        {
                            Vector3 pointA = i.Key;
                            int indexAinPoints = 0;

                            for (int k = 0; k < points.Length; k++)
                            {
                                if (points[k] == pointA)
                                {
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

    private Vector3 CalculateIntersectionPoint(float m1, float b1, float m2, float b2)
    {
        float x = (b2 - b1) / (m1 - m2);
        float y = m1 * x + b1;

        return new Vector3(x, 0, y);
    }

    private Vector3 CalculateHorizontalLines(float m1, float b1, float y)
    {
        // Calcular x utilizando la ecuación y = mx + b
        float xIntersection = (y - b1) / m1;

        return new Vector3(xIntersection, 0, y);
    }

    private Vector3 CalculateVerticalLines(float m1, float b1, float x)
    {
        // Punto de intersección con la recta vertical x = size
        float yIntersection = m1 * x + b1;

        return new Vector3(x, 0, yIntersection);
    }

    private void AddCorners(Color[] pixelColors, Color[] regionColors)
    {
        for (int i = 0; i < regionColors.Length; i++)
        { //x + z * size
            if (pixelColors[0] == regionColors[i]) //x=0, z=0
            {
                areaVertexes[i].Add(new Vector3(0, 0, 0));
            }
            else if (pixelColors[size - 1] == regionColors[i]) //x=size - 1, z=0
            {
                areaVertexes[i].Add(new Vector3(size - 1, 0, 0));
            }
            else if (pixelColors[(size - 1) * size] == regionColors[i]) //x=0, z=size - 1
            {
                areaVertexes[i].Add(new Vector3(0, 0, size - 1));
            }
            else if (pixelColors[size - 1 + (size - 1) * size] == regionColors[i]) //x=size - 1, z=size - 1
            {
                areaVertexes[i].Add(new Vector3(size - 1, 0, size - 1));
            }
        }
        
        
        
    }


    /*
    Mesh: 
        Vector3[] vertices (have them)
        Vector3[] uvs (not important)
        int[] triangles (how to get them?)
    
    To do:
        Get vertices of each shape
        For each shape:
        - Save the original mesh
        - Create a mesh from the given vertices --> organize the vertices into triangles (3 vert, 1 tri, 0 1 2; 4 vert, 2 tri, 0 1 2 0 2 3; 5 vert, 3 tri, 0 1 2 0 2 3 0 3 4)
        - Create a gameObject with the new mesh
        - Cut the shape from the original mesh or maybe just deactivate it once done
     */

    private void breakMesh()
    {
        for (int v = 0; v < numPoints; v++) //Do this for each resulting area
        {
            Mesh newMesh = new Mesh();
            newMesh.vertices = areaVertexesClean[v].ToArray();
            int auxVertices = 1;
            List<int> aux = new List<int>();

            if(newMesh.vertices.Length >= 3) //There needs to be at least 3 vertices for a triangle
            {
                newMesh.triangles = new int[(newMesh.vertices.Length -2) *3];
                for (int j = 1; j <= newMesh.vertices.Length - 2; j++) //The number of triangles is the number of vertices minus 2
                {
                    aux.Add(0);
                    aux.Add(auxVertices++);
                    aux.Add(auxVertices);
                }
            }

            newMesh.triangles = aux.ToArray();

            for (int i = 0; i < newMesh.vertices.Length; i++)
            {
                Debug.Log("Vertice " + i + ": " + newMesh.vertices[i]);
            }

            for (int i = 0; i < newMesh.triangles.Length; i++)
            {
                Debug.Log("Vertice: " + newMesh.triangles[i] + ", Coord: " + newMesh.vertices[newMesh.triangles[i]]);
            }

            /*Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(0.0f, 0.0f, 0.0f);
            vertices[1] = new Vector3(1.0f, 0.0f, 0.0f);
            vertices[2] = new Vector3(0.0f, 0.0f, 1.0f);
            vertices[3] = new Vector3(1.0f, 0.0f, 1.0f);

            newMesh.vertices = vertices;

            int[] triangles = new int[6];
            triangles[0] = 0;
            triangles[1] = 2;
            triangles[2] = 1;
            triangles[3] = 1;
            triangles[4] = 2;
            triangles[5] = 3;
        
            newMesh.triangles = triangles;

            for (int i = 0; i < newMesh.vertices.Length; i++)
            {
                Debug.Log("Vertice " + i + ": " + newMesh.vertices[i]);
            }

            for (int i = 0; i < newMesh.triangles.Length; i++)
            {
                Debug.Log("Vertice: " + newMesh.triangles[i] + ", Coord: " + newMesh.vertices[newMesh.triangles[i]]);
            }
    */
            GameObject a = new GameObject();
            a.AddComponent<MeshFilter>();
            a.AddComponent<MeshRenderer>();
            a.transform.GetComponent<MeshFilter>().mesh = newMesh;

            /*Material material = new Material(Shader.Find("Specular"));
            material.color = regionColors[v];

            a.GetComponent<MeshRenderer>().material = material;*/
            //a.AddComponent<MeshCollider>();
        }
    }
}
