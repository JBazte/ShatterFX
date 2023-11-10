using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class GeneratePoints : MonoBehaviour {
    // [SerializeField] private GameObject prefabPoint;
    [SerializeField] private int numPoints;
    private Vector3[] arrayPoints;  //point coordenates array
    private float sizeX;
    private float sizeZ;
    private VoronoiDiagram vd;

    void Start() {
        vd = GetComponent<VoronoiDiagram>();
        Mesh planeMesh = this.GetComponent<MeshFilter>().mesh;
        arrayPoints = new Vector3[numPoints];

        //Get the size of the plane in pixels
        sizeX = this.transform.localScale.x * planeMesh.bounds.size.x;
        Debug.Log(sizeX);
        sizeZ = this.transform.localScale.z * planeMesh.bounds.size.z;
        Debug.Log(sizeZ);


        PlacePoints();
    }


    private void PlacePoints() {
        //intantiate a point numPoint number of times
        for (int i = 0; i < numPoints; i++) {
            float x = Random.Range(this.transform.position.x - sizeX / 2, this.transform.position.x + sizeX / 2);
            float z = Random.Range(this.transform.position.z - sizeZ / 2, this.transform.position.z + sizeZ / 2);

            arrayPoints[i] = new Vector3(x, this.transform.position.y, z);
            Debug.Log(arrayPoints[i]);
        }

        vd.CalculateVoronoiDiagram();
    }


    private void OnDrawGizmos() {
        //this #if #endif is for not throwing an error while playing the demo
#if UNITY_EDITOR
        if (EditorApplication.isPlaying) {
            for (int i = 0; i < numPoints; i++) {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(arrayPoints[i], 0.1f);
            }
        }
#endif

    }

    public Vector3[] GetPoints() {
        return arrayPoints;
    }

    public int GetSizeX() {
        return (int)sizeX;
    }

    public int GetSizeZ() {
        return (int)sizeZ;
    }
}
