using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using UnityEngine.AI;
using System.IO;

public class VoronoiTexture_v4 : MonoBehaviour {     

    [SerializeField] private int size;
    [SerializeField] private int numPoints;
    [SerializeField] private Material material;

    // Array to store pixel colors for the texture
    private Color[] pixelColors;
    private Color[] regionColors;

    //list to store the pixel vertexes
    private List<Vector3>[] areaVertexes;
    List<Vector3>[] areaVertexesClean;

    List<Vector3>[] listVerticesArea;

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
        
        listVerticesArea = new List<Vector3>[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            listVerticesArea[i] = new List<Vector3>();
        }

        CreateVoronoi();
        orderVerticesForNewMesh_v3();
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

        //we color de (0,0) red
        pixelColors[0 + 0 * size] = Color.red;

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
        Vector3[] vertices
        Vector3[] uvs
        int[] triangles
    
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
            newMesh.name = "mesh_" + v;
            newMesh.vertices = listVerticesArea[v].ToArray();
            int auxVertices = 1;
            List<int> aux = new List<int>();
            //StreamWriter fichero = new StreamWriter("C:\\Users\\User\\UNITY\\ShatterFX\\Assets\\Scripts\\Voronoi_v3\\vertices.txt");

            if(newMesh.vertices.Length >= 3) //There needs to be at least 3 vertices for a triangle
            {
                newMesh.triangles = new int[(newMesh.vertices.Length - 2) * 6];
                for (int j = 1; j <= newMesh.vertices.Length - 2; j++) //The number of triangles is the number of vertices minus 2
                {
                    //fichero.WriteLine("Vertice 0:" + newMesh.vertices[0] + " Vertice "  + (auxVertices+1) + ":" + " " + newMesh.vertices[auxVertices+1]  + " Vértice " + auxVertices + ":"+ " " + newMesh.vertices[auxVertices]);
                    aux.Add(0);
                    aux.Add(auxVertices++);
                    aux.Add(auxVertices);
                }
                //fichero.Close();
            }

            newMesh.triangles = aux.ToArray();

            /*for (int i = 0; i < newMesh.vertices.Length; i++)
            {
                Debug.Log("Vertice " + i + ": " + newMesh.vertices[i]);
            }

            for (int i = 0; i < newMesh.triangles.Length; i++)
            {
                Debug.Log("Vertice: " + newMesh.triangles[i] + ", Coord: " + newMesh.vertices[newMesh.triangles[i]]);
            }*/

            GameObject a = new GameObject(name = "piece_" + v);
            a.AddComponent<MeshFilter>();
            a.AddComponent<MeshRenderer>();
            a.transform.GetComponent<MeshFilter>().mesh = newMesh;

            a.transform.localScale = new Vector3 (0.01245685f, 0.01245685f, 0.01245685f);
            a.transform.rotation = Quaternion.Euler(90, 0, -45);

            a.GetComponent<MeshRenderer>().material = material;
            a.AddComponent<MeshCollider>();
            a.GetComponent<MeshCollider>().convex = true;
            a.AddComponent<Rigidbody>();
        }
    }

    private void orderVerticesForNewMesh_v3()
    {
        //1º Hacer la Z media entre la Z máxima y la Z mínima
        //2º Hacer 2 grupos de vértices (los que etsán por encima de la Z media y los que están por debajo) y asignar los vértices según sus Z
        //3º Ordenar ambos grupos para que los vértices etsén ordenados en sentido antihorario (por encima--> de mayor a menor; por debajo --> de menor a mayor)
        //4º Juntarlos

        //Por cada área, tenemos que calcular el orden de sus vértices
        List<Vector3> arrayVertices;

        for (int v=0; v < listVerticesArea.Length; v++)
        {
            //array of vertices of an area
            arrayVertices = new List<Vector3>();
            Vector3[] auxVertices = areaVertexesClean[v].ToArray();

            //1º Hacer la Z media entre la Z máxima y la Z mínima
            float maxZ = float.MinValue;
            float minZ = float.MaxValue;

            //We calculate max z
            for (int i = 0; i < auxVertices.Length; i++)
            {
                if (auxVertices[i].z > maxZ)
                {
                    maxZ = auxVertices[i].z;
                }
            }
            //We calculate min z
            for (int i = 0; i < auxVertices.Length; i++)
            {
                if (auxVertices[i].z < minZ)
                {
                    minZ = auxVertices[i].z;
                }
            }

            Debug.Log("Max Z: " + maxZ);
            Debug.Log("Min Z: " + minZ);

            float mediumZ = (maxZ + minZ) / 2;

            //2º Hacer 2 grupos de vértices (los que etsán por encima de la Z media y los que están por debajo) y asignar los vértices según sus Z
            List<Vector3> topVertices = new List<Vector3>();
            List<Vector3> bottomVertices = new List<Vector3>();

            for (int i = 0; i < auxVertices.Length; i++)
            {
                if (auxVertices[i].z > mediumZ)
                {
                    topVertices.Add(auxVertices[i]);
                }
                else if (auxVertices[i].z <= mediumZ)
                {
                    bottomVertices.Add(auxVertices[i]);
                }
            }

            //3º Ordenar ambos grupos para que los vértices estén ordenados en sentido horario
            //COMPROBAR TAMBIÉN LA Z
            //por debajo --> de mayor a menor
            for (int j = 0; j < bottomVertices.Count - 1; j++)
            {
                for (int k = 0; k < bottomVertices.Count - j - 1; k++)
                    if (bottomVertices[k].x < bottomVertices[k + 1].x)
                    {
                        var tempVar = bottomVertices[k];
                        bottomVertices[k] = bottomVertices[k + 1];
                        bottomVertices[k + 1] = tempVar;
                    }
            }

            //por encima--> de menor a mayor
            for (int j = 0; j < topVertices.Count - 1; j++)
            {
                for (int k = 0; k < topVertices.Count - j - 1; k++)
                    if (topVertices[k].x > topVertices[k + 1].x)
                    {
                        var tempVar = topVertices[k];
                        topVertices[k] = topVertices[k + 1];
                        topVertices[k + 1] = tempVar;
                    }
            }

            Debug.Log("Orden vértices de arriba:\n");
            for (int i = 0; i < topVertices.Count; i++)
            {
                Debug.Log(topVertices[i]);
            }
            Debug.Log("Orden vértices de abajo:\n");
            for (int i = 0; i < bottomVertices.Count; i++)
            {
                Debug.Log(bottomVertices[i]);
            }

            //4º Juntarlos
            int indexMainArray = 0;
            for (int i = 0; i < topVertices.Count; i++)
            {
                arrayVertices.Add(topVertices[i]);
                indexMainArray++;
            }

            for (int i = 0; i < bottomVertices.Count; i++)
            {
                arrayVertices.Add(bottomVertices[i]);
                indexMainArray++;
            }

            Debug.Log("Orden del array");
            for (int i = 0; i < arrayVertices.Count; i++)
            {
                Debug.Log(arrayVertices[i]);
            }

           /* //COLOREAMOS PIXELES
            for (int i = 0; i < arrayVertices.Count; i++)
            {
                pixelColors[(int)arrayVertices[i].x + (int)arrayVertices[i].z * size] = Color.black;
            }*/

            listVerticesArea[v] = arrayVertices;
        }

        //PROVISIONAL, PARA VISUALIZARLO MEJOR
        // Create and apply the texture to the GameObject's material
       /* Texture2D texture = new Texture2D(size, size);
        texture.SetPixels(pixelColors);
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;*/

    }
}
//Vector3(0.551999927,-0.795000017,-0.556999922) -- Vector3(270,135,90)
//Vector3(0.551999986,-2.4690001,-0.556999981)