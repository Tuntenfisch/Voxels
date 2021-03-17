using UnityEngine;

public class Voxel : MonoBehaviour
{
    public GameObject m_densitySamplePrefab;
    public Material m_lineMaterial;
    [HideInInspector]
    public DensitySample[] m_samples;

    //     7---------6
    //    /|        /|
    //   / |       / |
    //  /  |      /  |
    // 4---------5   |
    // |   |     |   |
    // |   3-----|---2
    // |  /      |  /
    // | /       | /
    // |/        |/
    // 0---------1
    public static readonly Vector3[] s_corners =
    {
        new Vector3(-0.5f, -0.5f, -0.5f),   // 0
        new Vector3( 0.5f, -0.5f, -0.5f),   // 1
        new Vector3( 0.5f, -0.5f,  0.5f),   // 2
        new Vector3(-0.5f, -0.5f,  0.5f),   // 3
        new Vector3(-0.5f,  0.5f, -0.5f),   // 4
        new Vector3( 0.5f,  0.5f, -0.5f),   // 5
        new Vector3( 0.5f,  0.5f,  0.5f),   // 6
        new Vector3(-0.5f,  0.5f,  0.5f)    // 7
    };

    //     +----6----+
    //    /|        /|
    //   7 |       5 |
    //  /  11     / 10
    // +----4----+   |
    // |   |     |   |
    // |   +----2|---+
    // 8  /      9  /
    // | 3       | 1
    // |/        |/
    // +----0----+
    public static readonly VoxelEdge[] s_edges =
    {
        new VoxelEdge { m_cornerIndexA = 0, m_cornerIndexB = 1 },
        new VoxelEdge { m_cornerIndexA = 1, m_cornerIndexB = 2 },
        new VoxelEdge { m_cornerIndexA = 2, m_cornerIndexB = 3 },
        new VoxelEdge { m_cornerIndexA = 3, m_cornerIndexB = 0 },
        new VoxelEdge { m_cornerIndexA = 4, m_cornerIndexB = 5 },
        new VoxelEdge { m_cornerIndexA = 5, m_cornerIndexB = 6 },
        new VoxelEdge { m_cornerIndexA = 6, m_cornerIndexB = 7 },
        new VoxelEdge { m_cornerIndexA = 7, m_cornerIndexB = 4 },
        new VoxelEdge { m_cornerIndexA = 0, m_cornerIndexB = 4 },
        new VoxelEdge { m_cornerIndexA = 1, m_cornerIndexB = 5 },
        new VoxelEdge { m_cornerIndexA = 2, m_cornerIndexB = 6 },
        new VoxelEdge { m_cornerIndexA = 3, m_cornerIndexB = 7 }
    };

    public static readonly int[][] s_faces =
    {
        new int[] { 2, 11, 6, 10 }, // rear face
        new int[] { 1, 10, 5,  9 }, // right face
        new int[] { 0,  9, 4,  8 }, // front face
        new int[] { 3,  8, 7, 11 }, // left face
        new int[] { 0,  1, 2,  3 }, // bottom face
        new int[] { 4,  5, 6,  7 }  // top face
    };

    public static readonly int[][] s_faceSampleIndices = 
    {
        new int[] { 2, 3, 7, 6 },   // rear face
        new int[] { 1, 2, 6, 5 },   // right face
        new int[] { 0, 1, 5, 4 },   // front face
        new int[] { 3, 0, 4, 7 },   // left face
        new int[] { 0, 1, 2, 3 },   // bottom face
        new int[] { 4, 5, 6, 7 }    // top face
    };

    private IMeshifier m_meshifier;

    private void Start()
    {
        m_samples = new DensitySample[s_corners.Length];

        for (int index = 0; index < s_corners.Length; index++)
        {
            GameObject go = Instantiate(m_densitySamplePrefab, transform.localToWorldMatrix.MultiplyPoint(s_corners[index]), Quaternion.identity, transform);
            DensitySample sample = go.GetComponent<DensitySample>();
            sample.Initialize($"{index}");
            m_samples[index] = sample;
        }
        m_meshifier = GetComponent<IMeshifier>();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            UpdateDensities();
            transform.hasChanged = false;
        }
    }

    private void UpdateDensities()
    {
        foreach (DensitySample sample in m_samples)
        {
            sample.Density = DensityFunctions.Sphere(sample.transform.position + 0.5f * Vector3.one, 5.0f);
            sample.m_normal = DensityFunctions.SphereGradient(sample.transform.position + 0.5f * Vector3.one);
        }
        m_meshifier.OnDensitiesChanged();
    }

    private void OnRenderObject()
    {
        DrawWireVoxel();
    }

    private void DrawWireVoxel()
    {
        m_lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(Color.grey);

        for (int index = 0; index < 4; index++)
        {
            GL.Vertex(s_corners[index]);
            GL.Vertex(s_corners[(index + 1) % 4]);
            GL.Vertex(s_corners[index]);
            GL.Vertex(s_corners[index + 4]);
            GL.Vertex(s_corners[index + 4]);
            GL.Vertex(s_corners[(index + 1) % 4 + 4]);
        }

        GL.End();
        GL.PopMatrix();
    }
}
