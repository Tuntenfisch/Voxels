using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    public float m_translateSensitivity = 0.01f;
    [Range(0.0f, 5.0f)]
    public float m_rotateSensitivity = 2.0f;
    [Range(0.0f, 1.0f)]
    public float m_zoomSensitivity = 0.25f;

    private Vector3 m_lastMousePosition;

    private void Update()
    {
        RotateCamera();
        TranslateCamera();
        ZoomCamera();
    }

    private void RotateCamera()
    {
        if (Input.GetMouseButton(1))
        {
            transform.Rotate(new Vector3(-Input.GetAxis("Mouse Y") * m_rotateSensitivity, Input.GetAxis("Mouse X") * m_rotateSensitivity, 0.0f));
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0.0f);
        }
    }

    private void TranslateCamera()
    {
        if (Input.GetMouseButtonDown(2))
        {
            m_lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 delta = -Input.mousePosition + m_lastMousePosition;
            transform.Translate(delta.x * m_translateSensitivity, delta.y * m_translateSensitivity, 0.0f);
            m_lastMousePosition = Input.mousePosition;
        }
    }

    private void ZoomCamera()
    {
        float mouseScrollDelta = Input.mouseScrollDelta.y;

        if (mouseScrollDelta == 0.0f)
        {
            return;
        }

        transform.Translate(new Vector3(0.0f, 0.0f, mouseScrollDelta * m_zoomSensitivity));
    }
}
