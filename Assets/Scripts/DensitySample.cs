using UnityEngine;
using UnityEngine.UI;

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
            m_text.color = color;
        }
    }

    public Material m_lineMaterial;
    [HideInInspector]
    public Vector3 m_normal;

    private float m_density;
    private Renderer m_renderer;
    private Text m_text;

    public void Initialize(string text)
    {
        m_text.text = text;
    }

    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        m_text = GetComponentInChildren<Text>();
    }

    private void OnRenderObject()
    {
        DrawNormal();
    }

    private void DrawNormal()
    {
        m_lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(Color.green);
        GL.Vertex(Vector3.zero);
        GL.Vertex(2.0f * m_normal);
        GL.End();
        GL.PopMatrix();
    }
}
