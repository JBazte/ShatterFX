using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBreak_v1 : MonoBehaviour
{
    /*
    Mesh: 
        Vector3[] vertices (have them)
        Vector3[] uvs (not important)
        int[] triangles (how to get them?)
    
    To do:
        Get vertices of each shape -- done
        For each shape:
        - Save the original mesh -- done
        - Create a mesh from the given vertices, organize the vertices into triangles
        - Create a gameObject with the new mesh
        - Cut the shape from the original mesh or maybe just deactivate it once done
     */

    [SerializeField]
    private VoronoiTexture_v4 voronoi;

    public void deleteTri(int index)
    {
        Destroy(gameObject.GetComponent<MeshCollider>());
        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
        Mesh newMesh = new Mesh();

        int[] oldTriangles = mesh.triangles;
        int[] newTriangles = new int[mesh.triangles.Length-3];

        Vector3[] vertices = new Vector3[3];

        int i = 0;
        int j = 0;
        while (j < mesh.triangles.Length)
        {
            if(j != index*3)
            {
                newTriangles[i++] = oldTriangles[j++];
                newTriangles[i++] = oldTriangles[j++];
                newTriangles[i++] = oldTriangles[j++];
            }
            else
            {
                vertices[0] = mesh.vertices[oldTriangles[j++]];
                vertices[1] = mesh.vertices[oldTriangles[j++]];
                vertices[2] = mesh.vertices[oldTriangles[j++]];
            }
        }

        newMesh.vertices = vertices;
        newMesh.triangles = new int[] { 0, 1, 2};
        GameObject a = new GameObject();
        a.AddComponent<MeshFilter>();
        a.AddComponent<MeshRenderer>();
        a.transform.GetComponent<MeshFilter>().mesh = newMesh;
        a.AddComponent<MeshCollider>();
        
        transform.GetComponent<MeshFilter>().mesh.triangles = newTriangles;
        gameObject.AddComponent<MeshCollider>();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                deleteTri(hit.triangleIndex);
            }
        }
    }
}
