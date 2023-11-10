using System.Collections.Generic;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour {
    [SerializeField] private GeneratePoints pointGenerator;
    [SerializeField] private int regionColorAmount;

    public void CalculateVoronoiDiagram() {
        pointGenerator = GetComponent<GeneratePoints>();
        Vector3[] points = pointGenerator.GetPoints();

        Color[] regionColors = new Color[regionColorAmount];

        for (int i = 0; i < regionColorAmount; i++) {
            regionColors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        }
        Color[] pixelColors = new Color[pointGenerator.GetSizeX() * pointGenerator.GetSizeZ()];

        for (int z = 0; z < pointGenerator.GetSizeZ(); z++) {
            for (int x = 0; x < pointGenerator.GetSizeX(); x++) {
                float distance = float.MaxValue;
                int value = 0;

                for (int i = 0; i < points.Length; i++) {
                    if (Vector3.Distance(new Vector3(x, 0, z), points[i]) < distance) {
                        distance = Vector3.Distance(new Vector3(x, 0, z), points[i]);
                        value = i;
                    }
                }

                pixelColors[x + z * pointGenerator.GetSizeZ()] = regionColors[value % regionColorAmount];
            }

            Texture2D texture = new Texture2D(pointGenerator.GetSizeX(), pointGenerator.GetSizeZ());
            texture.SetPixels(pixelColors);
            texture.Apply();

            GetComponent<Renderer>().material.mainTexture = texture;
        }
    }
}
