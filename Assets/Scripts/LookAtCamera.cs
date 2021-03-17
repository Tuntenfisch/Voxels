using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    Camera m_camera;

    void Start()
    {
        m_camera = Camera.main;
    }
 
    void LateUpdate()
    {
        transform.LookAt(m_camera.transform);
        transform.rotation = Quaternion.LookRotation(m_camera.transform.forward);
    }
}
