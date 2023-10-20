using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambiarModelo : MonoBehaviour
{
    [SerializeField] private GameObject modelo;
    [SerializeField] private Mesh mesh_platoRoto;
    [SerializeField] private Mesh mesh_platoNormal;

    private void OnCollisionEnter(Collision collision)
    {
        modelo.GetComponent<MeshFilter>().mesh = mesh_platoRoto;
        this.GetComponent<MeshCollider>().sharedMesh = mesh_platoRoto;
        Debug.Log("Colisionado");
    }


}
