using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoronoiTexture_v2 : MonoBehaviour {

    [SerializeField] private int size;
    [SerializeField] private int numPoints;

    //private Vector3[] arrayVertex; 

    void Start() {
        CreateVoronoi();
    }

    public void CreateVoronoi()
    {
        // Arrays to store Voronoi points and corresponding region colors
        Vector3[] points = new Vector3[numPoints];
        Color[] regionColors = new Color[numPoints];

        //arrayVertex = new Vector3[1];
        //int indexVertex = 0;

        // Generate random points and colors
        for (int i = 0; i < numPoints; i++)
        {
            points[i] = new Vector3(UnityEngine.Random.Range(0, size), 0, UnityEngine.Random.Range(0, size));
        }

        for (int i = 0; i < numPoints; i++)
        {
            regionColors[i] = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1f);
        }

        // Array to store pixel colors for the texture
        Color[] pixelColors = new Color[size * size];

        // Generate Voronoi diagram by assigning colors based on the nearest point
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {

                //float[] distancesAux = new float[numPoints]; //we need an array if the main points
                float distance = float.MaxValue;
                int value = 0;

                // Find the nearest point for each pixel
                for (int i = 0; i < numPoints; i++)
                {
                    if (Vector3.Distance(new Vector3(x, 0, z), points[i]) < distance)
                    {

                        distance = Vector3.Distance(new Vector3(x, 0, z), points[i]);
                        value = i;
                        Debug.Log("Distance: " + distance);
                        //distancesAux[i] = distance; //we save the distance from that pixel to every main point
                    }
                }

                //Debug.Log(isVertex(distancesAux));
                //we see if this pixel is a vertex
                /*if (isVertex(distancesAux))
                {
                    arrayVertex[indexVertex] = new Vector3(x, 0, z);
                    Array.Resize(ref arrayVertex, arrayVertex.Length + 1);
                    indexVertex++;
                }*/

                // Assign the color of the nearest point to the pixel
                pixelColors[x + z * size] = regionColors[value % numPoints];
            }
        }

        /*foreach (Vector3 point in arrayVertex)
        {
            int x = (int)point.x;
            int z = (int)point.z;
            pixelColors[x + z * size] = Color.cyan;
        }*/

        // Set specific pixels to black to highlight the Voronoi points
        foreach (Vector3 point in points)
        {
            int x = (int)point.x;
            int z = (int)point.z;
            pixelColors[x + z * size] = Color.black;
        }

        // Create and apply the texture to the GameObject's material
        Texture2D texture = new Texture2D(size, size);
        texture.SetPixels(pixelColors);
        texture.Apply();

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                if(isVertexColor(texture, x, z))
                {
                    pixelColors[x + z * size] = Color.cyan;
                }
            } 
        }
        texture.SetPixels(pixelColors);
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;

    }

   /* private bool isVertex(float[] arrayDistances)
    {
        bool isVertex = false;
        float min1, min2, min3;

        min1 = float.MaxValue;
        min2 = float.MaxValue;
        min3 = float.MaxValue;

        //We need to do this 3 times, because a vertex has at least 3 areas colliding
        foreach (int value in arrayDistances)
        {
            if (value < min1) {

                min1 = value;

            } 
        }

        int index = Array.IndexOf(arrayDistances, min1);
        //we overwrite the original array excluding index of min1
        arrayDistances = arrayDistances.Where((e, i) => i != index).ToArray();

        foreach (int value in arrayDistances)
        {
            if (value < min2)
            {
                min2 = value;
            }
        }

        index = Array.IndexOf(arrayDistances, min2);
        //we overwrite the original array excluding index of min2
        arrayDistances = arrayDistances.Where((e, i) => i != index).ToArray();

        foreach (int value in arrayDistances)
        {
            if (value < min3)
            {
                min3 = value;
            }
        }

        //Debug.Log("Min1: " + min1 + " Min2: " + min2 + " Min3: " + min3);
        //min1 is the smallest distance, so we have to compare if min2 is much bigger than min1
        //if so, then this pixel is not a vertex

        float error1 = min2 - min1;
        float error2 = min3 - min1;
        //Debug.Log("Error1: " + error1 + " Error2: " + error2);

        //if error1 is small enough 
        if (error1 >= 0 && error1 <= 0.01)
        {
            if (error2 >= 0 && error2 <= 0.01)
            {
                isVertex = true;
            }
        }


        return isVertex;
    }*/

    private bool isVertexColor(Texture2D texture, int x, int z)
    {
        bool isVertex = false;
        
        Color mainPixelColor = texture.GetPixel(x,z);
        int differentColors = 0;

        //for each pixel, we need to 
        Color leftPixelColor = texture.GetPixel(x - 1, z);
        Color rightPixelColor = texture.GetPixel(x + 1, z);
        Color upPixelColor = texture.GetPixel(x, z + 1);
        Color downPixelColor = texture.GetPixel(x, z - 1);

        if (mainPixelColor != leftPixelColor) differentColors++;
        if (mainPixelColor != rightPixelColor) differentColors++;
        if (mainPixelColor != upPixelColor) differentColors++;
        if (mainPixelColor != downPixelColor) differentColors++;

        if (differentColors > 2)
        {
            isVertex = true;
        }

        return isVertex;
    }

}
