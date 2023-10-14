using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {
    [SerializeField] private Camera m_Camera;
    [SerializeField] private Transform m_Target;
    private float timeDilation = 1.0f;
    private float originalTimeScale;

    private Vector3 m_PreviousPosition;

    private void Start() {
        originalTimeScale = Time.timeScale;
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            m_PreviousPosition = m_Camera.ScreenToViewportPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0)) {
            Vector3 direction = m_PreviousPosition - m_Camera.ScreenToViewportPoint(Input.mousePosition);

            m_Camera.transform.position = m_Target.position;

            m_Camera.transform.Rotate(new Vector3(1, 0, 0), direction.y * 180);
            m_Camera.transform.Rotate(new Vector3(0, 1, 0), -direction.x * 180, Space.World);
            m_Camera.transform.Translate(new Vector3(0, 0, -3));

            m_PreviousPosition = m_Camera.ScreenToViewportPoint(Input.mousePosition);
        }

        if (Input.GetKey(KeyCode.Z) && Time.timeScale >= 0.2f) {
            timeDilation += 0.1f;
            Time.timeScale = originalTimeScale / timeDilation;
        } else if (Input.GetKey(KeyCode.X) && Time.timeScale <= 1f) {
            timeDilation -= 0.1f;
            Time.timeScale = originalTimeScale / timeDilation;
        }
    }
}
