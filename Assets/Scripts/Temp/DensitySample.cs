using UnityEngine;

public class DensitySample : MonoBehaviour
{
    public float Density
    {
        get
        {
            return m_density;
        }
        set
        {
            m_density = Mathf.Clamp(value, -1.0f, 1.0f);
            Color color = Color.Lerp(Color.black, Color.white, 0.5f * (m_density + 1.0f));
            m_renderer.material.color = color;
        }
    }


    private float m_density;
    private Renderer m_renderer;

    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        Density = 1.0f;
    }

    private void OnMouseOver()
    {
        float scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta != 0)
        {
            Density += 0.1f * scrollDelta;
        }
    }
}