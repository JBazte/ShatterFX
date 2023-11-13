using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoronoiTexture_v3 : MonoBehaviour {
      

    //CALCULAR LOS VÉRTICES A TRAVÉS DE RECTAS Y NORMALES
    //1º VER LOS PUNTOS QUE ESTÁN CERCA UNOS DE OTROS
    //2º rECTA ENTRE 2 PUNTOS Y HALLAR LA NORMAL
    //VER DONDE CORTAN, ASÍ TENEMOS LOS VÉRTICES



    [SerializeField] private int size;
    [SerializeField] private int numPoints;

    private Color[,] pixelColors;

    void Start() {

        CreateVoronoi();
    }

    public void CreateVoronoi()
    {
        // Arrays to store Voronoi points and corresponding region colors
        Vector3[] points = new Vector3[numPoints];
        Color[] regionColors = new Color[numPoints];
        pixelColors = new Color[size, size];

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
        // Color[] pixelColors = new Color[size * size];

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
                        //Debug.Log("Distance: " + distance);
                    }
                }

                // Assign the color of the nearest point to the pixel
                pixelColors[x,z] = regionColors[value % numPoints];
            }
        }

        // Set specific pixels to black to highlight the Voronoi points
        foreach (Vector3 point in points)
        {
            int x = (int)point.x;
            int z = (int)point.z;
            pixelColors[x,z] = Color.black;
        }

        // Create and apply the texture to the GameObject's material
        Texture2D texture = new Texture2D(size, size);
        //texture.SetPixels(pixelColors);
        //texture.Apply();

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                if(isVertexColor(x, z))
                {
                    pixelColors[x,z] = Color.cyan;
                }
            } 
        }

        Color[] arrayPixelColors = new Color[size * size];

        for (int i=0; i<size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                arrayPixelColors[i * size + j] = pixelColors[i,j];
            }
        }

        texture.SetPixels(arrayPixelColors);
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;
        Debug.Log("Hemos pintado");

    }

    private bool isVertexColor(int x, int z)
    {
        bool isVertex = false;

        Color mainPixelColor = pixelColors[x,z];

        Color leftPixelColor = Color.white;
        Color upPixelColor = Color.white;
        Color rightPixelColor = Color.white;
        Color downPixelColor = Color.white;

        int differentColors = 0;

        if (x >0 && x < size - 1)
        {
            if (z>0 && z < size - 1)
            {
                leftPixelColor = pixelColors[x-1,z];
                upPixelColor = pixelColors[x,z-1];
                rightPixelColor = pixelColors[x + 1,z];
                downPixelColor = pixelColors[x,z+1];
            }
        }else if (x==0)
        {
            if (z==0)
            {
                rightPixelColor = pixelColors[x + 1,z];
                downPixelColor = pixelColors[x,z + 1];
            }
            else if (z == size - 1)
            {
                rightPixelColor = pixelColors[x + 1,z];
                upPixelColor = pixelColors[x,z - 1];
            }
            else
            {
                rightPixelColor = pixelColors[x + 1,z];
                upPixelColor = pixelColors[x, z - 1];
                downPixelColor = pixelColors[x,z + 1];
            }
        }else if (x == size - 1)
        {
            if (z == 0)
            {
                leftPixelColor = pixelColors[x - 1,z];
                downPixelColor = pixelColors[x,z + 1];
            }
            else if (z == size - 1)
            {
                leftPixelColor = pixelColors[x - 1,z];
                upPixelColor = pixelColors[x,z - 1];
            }
            else
            {
                leftPixelColor = pixelColors[x - 1,z];
                upPixelColor = pixelColors[x,z - 1];
                downPixelColor = pixelColors[x, z + 1];
            }
        }

        //for each pixel, we need to 

        if (mainPixelColor != leftPixelColor) differentColors++;
       // if (mainPixelColor != upLeftPixelColor) differentColors++;
        if (mainPixelColor != upPixelColor) differentColors++;
       // if (mainPixelColor != upRightPixelColor) differentColors++;
        if (mainPixelColor != rightPixelColor) differentColors++;
       // if (mainPixelColor != downRightPixelColor) differentColors++;
        if (mainPixelColor != downPixelColor) differentColors++;
        // if (mainPixelColor != downLeftPixelColor) differentColors++;
        Debug.Log(differentColors);


        if (differentColors > 2)
        {
            isVertex = true;
        }

        return isVertex;
    }

}
