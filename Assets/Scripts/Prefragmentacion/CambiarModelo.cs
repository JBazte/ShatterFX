using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambiarModelo : MonoBehaviour
{
    [SerializeField]
    private GameObject normal_plate;
    [SerializeField]
    private GameObject broken_plate;
    

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 position = normal_plate.transform.position;
        normal_plate.SetActive(false);
        broken_plate.transform.position = position;
        broken_plate.SetActive(true);
        Debug.Log("Colisionado");
    }


}
