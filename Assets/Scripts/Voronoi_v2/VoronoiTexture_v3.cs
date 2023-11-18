using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class VoronoiTexture_v3 : MonoBehaviour {     

    [SerializeField] private int size;
    [SerializeField] private int numPoints;

    // Array to store pixel colors for the texture
    private Color[] pixelColors;

    //list to store the pixel vertexes
    private List<Vector3> vertexes;

    void Start()
    {
        vertexes = new List<Vector3>();
        pixelColors = new Color[size * size];
        CreateVoronoi();
    }

    public void CreateVoronoi()
    {
        // Arrays to store Voronoi points and corresponding region colors
        Vector3[] points = new Vector3[numPoints];
        Color[] regionColors = new Color[numPoints];

        // Generate random points and colors
        //V3: we supose the order of 'regionColors[]' tells us which point of 'points[]' is assigned to each color area
        for (int i = 0; i < numPoints; i++)
        {
            points[i] = new Vector3(Random.Range(0, size), 0, Random.Range(0, size));
        }

        for (int i = 0; i < numPoints; i++)
        {
            regionColors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
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
        vertexes.Add(new Vector3(0, 0, 0));
        vertexes.Add(new Vector3(0, 0, size-1));
        vertexes.Add(new Vector3(size-1, 0, 0));
        vertexes.Add(new Vector3(size-1, 0, size-1));

        //we color the vertexes
        for (int i=0; i < vertexes.Count; i++){

            int x = (int)vertexes[i].x;
            int z = (int)vertexes[i].z;
            pixelColors[x + z * size] = Color.cyan;
            
        }

        // Create and apply the texture to the GameObject's material
        Texture2D texture = new Texture2D(size, size);
        texture.SetPixels(pixelColors);
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;
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

        for (int i=0; i < pointsDistances.Count; i++)
        {
            for (int j = 0; j < pointsDistances.Count; j++)
            {
                //We'll not compare the same line
                if (i!=j)
                {
                    //First we check if they aren't parallel lines
                    if (mNormals[i] != mNormals[j])
                    {
                        //Then we calculate the intersection point of both line ecuations
                        Vector3 intersectionPoint = CalculateIntersectionPoint(mNormals[i], bNormals[i], mNormals[j], bNormals[j]);

                        //We have to make sure that the intersection point is within the bounds of the plane size * size
                        if (intersectionPoint.x <= size && intersectionPoint.z <= size && intersectionPoint.x >=0 && intersectionPoint.z >= 0)
                        {
                            
                            //see of which color is the intersection point
                            Color interPointColor = pixelColors[(int)intersectionPoint.x + (int)intersectionPoint.z * size];

                            //if it has the same color as the area of the main point, this intersection point is a vertex of this area
                            if (interPointColor == regionColors[indexArea])
                            {
                                //we have to make sure it isn't already in the vertex list
                                bool alreadyAdded = false;
                                for (int k = 0; k < vertexes.Count; k++)
                                {
                                    if (vertexes[k] == intersectionPoint)
                                    {
                                        alreadyAdded = true;
                                    }
                                }

                                //if it isn't, we add it
                                if (!alreadyAdded)
                                {
                                    vertexes.Add(intersectionPoint);
                                }
                            }
                        }
                    }
                }
            }
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

        for (int i = 0; i < pointsDistances.Count; i++)
        {
            //For each line ecuation we'll compare with the borders (also line ecuations)
            for(int j = 0; j < limitLines.Count; j++)
            {
                if (mNormals[i] != limitLines[j])
                {
                    Vector3 intersectionPoint = new Vector3(0, 0, 0);

                    //impar: x   par: y
                    if (j % 2 == 0)
                    {
                        intersectionPoint = CalculateHorizontalLines(mNormals[i], bNormals[i], limitLines[j]);

                    }
                    else
                    {
                        intersectionPoint = CalculateVerticalLines(mNormals[i], bNormals[i], limitLines[j]);
                    }



                    if (intersectionPoint.x < size && intersectionPoint.z < size && intersectionPoint.x >= 0 && intersectionPoint.z >= 0)
                    {
                        Debug.Log(intersectionPoint);
                        //see of which color is the intersection point
                        Color interPointColor = pixelColors[(int)intersectionPoint.x + (int)intersectionPoint.z * size];

                        //if it has the same color as the area of the main point, this intersection point is a vertex of this area
                        if (interPointColor == regionColors[indexArea])
                        {
                            //we have to make sure it isn't already in the vertex list
                            bool alreadyAdded = false;
                            for (int k = 0; k < vertexes.Count; k++)
                            {
                                if (vertexes[k] == intersectionPoint)
                                {
                                    alreadyAdded = true;
                                }
                            }

                            //if it isn't, we add it
                            if (!alreadyAdded)
                            {
                                vertexes.Add(intersectionPoint);
                            }
                        }
                    }
                }
                
            }
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

}
