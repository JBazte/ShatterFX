using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelSwap : MonoBehaviour {
    [SerializeField]
    private GameObject normalPlate;
    [SerializeField]
    private GameObject brokenPlate;

    private void OnCollisionEnter(Collision collision) {
        Vector3 position = normalPlate.transform.position;
        normalPlate.SetActive(false);
        brokenPlate.transform.position = position;
        brokenPlate.SetActive(true);
    }


}
