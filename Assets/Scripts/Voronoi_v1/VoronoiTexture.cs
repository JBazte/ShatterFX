using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiTexture : MonoBehaviour {
    [SerializeField] private int size;
    [SerializeField] private int numPoints;

    void Start() {
        CreateVoronoi();
    }

    public void CreateVoronoi() {
        // Arrays to store Voronoi points and corresponding region colors
        Vector3[] points = new Vector3[numPoints];
        Color[] regionColors = new Color[numPoints];

        // Generate random points and colors
        for (int i = 0; i < numPoints; i++) {
            points[i] = new Vector3(Random.Range(0, size), 0, Random.Range(0, size));
        }

        for (int i = 0; i < numPoints; i++) {
            regionColors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        }

        // Array to store pixel colors for the texture
        Color[] pixelColors = new Color[size * size];

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

        // Create and apply the texture to the GameObject's material
        Texture2D texture = new Texture2D(size, size);
        texture.SetPixels(pixelColors);
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;
    }
}
