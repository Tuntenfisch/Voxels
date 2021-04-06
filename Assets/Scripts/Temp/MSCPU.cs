using System.Collections.Generic;
using UnityEngine;

public class MSCPU : MonoBehaviour
{
    [Header("Shader")]
    public Material m_lineMaterial;

    [Header("Other")]
    public GameObject m_densitySamplePrefab;
    public bool m_subSample;

    [HideInInspector]
    public DensitySample[] m_densities;

    // 3---------2
    // |         |
    // |         |
    // |         |
    // 0---------1
    private static readonly Vector3Int[] s_corners =
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0)
    };

    // +----2----+
    // |         |
    // 3         1
    // |         |
    // +----0----+
    private static readonly int[][] s_segments =
    {
        new int[] { -1, -1, -1, -1 },
        new int[] {  3,  0, -1, -1 },
        new int[] {  0,  1, -1, -1 },
        new int[] {  3,  1, -1, -1 },
        new int[] {  1,  2, -1, -1 },
        new int[] {  3,  2,  0,  1 },
        new int[] {  0,  2, -1, -1 },
        new int[] {  3,  2, -1, -1 },
        new int[] {  2,  3, -1, -1 },
        new int[] {  2,  0, -1, -1 },
        new int[] {  3,  0,  2,  1 },
        new int[] {  2,  1, -1, -1 },
        new int[] {  1,  3, -1, -1 },
        new int[] {  1,  0, -1, -1 },
        new int[] {  0,  3, -1, -1 },
        new int[] { -1, -1, -1, -1 }
    };

    private static readonly Vector2Int[] s_edges =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 2),
        new Vector2Int(2, 3),
        new Vector2Int(3, 0),
    };

    // 6----5----4
    // |         |
    // 7    8    3
    // |         |
    // 0----1----2
    private static readonly int[][] s_subSampleCentralDensitySummationTerms =
    {
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 },
        new int[] { 0, 0 }
    };

    private static readonly int[][] s_densitiesToSubSampleDensitiesMapping =
    {
        new int[] { 0, 1, 8, 7 },
        new int[] { 1, 2, 3, 8 },
        new int[] { 7, 8, 5, 6 },
        new int[] { 8, 3, 4, 5 }
    };

    private int NumberOfDensitySamplesAlongAxis
    {
        get
        {
            return m_numberOfVoxelsAlongAxis + 1;
        }
    }

    private int m_numberOfVoxelsAlongAxis = 2;
    private List<Vector3> m_lines;
    private List<Color> m_colors;

    private void Start()
    {
        m_densities = new DensitySample[NumberOfDensitySamplesAlongAxis * NumberOfDensitySamplesAlongAxis];
        m_lines = new List<Vector3>();
        m_colors = new List<Color>();

        Vector3Int id = Vector3Int.zero;

        for (id.x = 0; id.x < NumberOfDensitySamplesAlongAxis; id.x++)
        {
            for (id.y = 0; id.y < NumberOfDensitySamplesAlongAxis; id.y++)
            {
                GameObject gameObject = Instantiate(m_densitySamplePrefab, CalculateLocalPosition(id), Quaternion.identity, transform);
                m_densities[CalculateDensityIndex(id)] = gameObject.GetComponent<DensitySample>();
            }
        }
    }

    private void Update()
    {
        m_lines.Clear();
        m_colors.Clear();

        UpdateMarchingSquares(2, Color.red);

        if (!m_subSample)
        {
            return;
        }

        UpdateSubSampledMarchingSquares(1, Color.cyan);
    }

    private int CalculateDensityIndex(Vector3Int position)
    {
        return position.x + position.y * NumberOfDensitySamplesAlongAxis;
    }

    private Vector3 CalculateLocalPosition(Vector3 position)
    {
        return position - 0.5f * new Vector3(m_numberOfVoxelsAlongAxis, m_numberOfVoxelsAlongAxis, 0.0f);
    }

    private Vector3 CreateVertexAlongEdge(Vector3Int id, Vector2Int edge, int stride)
    {
        Vector3Int cornerA = s_corners[edge.x];
        Vector3Int cornerB = s_corners[edge.y];

        float sampleA = m_densities[CalculateDensityIndex(stride * (id + cornerA))].Density;
        float sampleB = m_densities[CalculateDensityIndex(stride * (id + cornerB))].Density;
        float interpolant = -sampleA / (sampleB - sampleA);

        return Vector3.Lerp(cornerA, cornerB, interpolant);
    }

    private void UpdateMarchingSquares(int stride, Color color)
    {
        Vector3Int id = Vector3Int.zero;

        for (id.x = 0; id.x < m_numberOfVoxelsAlongAxis / stride; id.x++)
        {
            for (id.y = 0; id.y < m_numberOfVoxelsAlongAxis / stride; id.y++)
            {
                int segmentsIndex = 0;

                for (int index = 0; index < s_corners.Length; index++)
                {
                    segmentsIndex |= (m_densities[CalculateDensityIndex(stride * (id + s_corners[index]))].Density < 0.0 ? 1 : 0) << index;
                }

                int[] segments = s_segments[segmentsIndex];

                for (int index = 0; index < segments.Length; index += 2)
                {
                    if (segments[index] == -1)
                    {
                        break;
                    }

                    Vector2Int edgeA = s_edges[segments[index + 0]];
                    Vector2Int edgeB = s_edges[segments[index + 1]];

                    Vector3 vertexA = stride * CreateVertexAlongEdge(id, edgeA, stride) + CalculateLocalPosition(id);
                    Vector3 vertexB = stride * CreateVertexAlongEdge(id, edgeB, stride) + CalculateLocalPosition(id);

                    m_lines.Add(vertexA);
                    m_lines.Add(vertexB);
                    m_colors.Add(color);
                }
            }
        }
    }

    private Vector3 CreateSubSampleVertexAlongEdge(Vector2Int edge, float[] densities)
    {
        Vector3Int cornerA = s_corners[edge.x];
        Vector3Int cornerB = s_corners[edge.y];

        float sampleA = densities[edge.x];
        float sampleB = densities[edge.y];
        float interpolant = -sampleA / (sampleB - sampleA);

        return Vector3.Lerp(cornerA, cornerB, interpolant);
    }

    private void UpdateSubSampledMarchingSquares(int stride, Color color)
    {
        Vector3Int id = Vector3Int.zero;

        for (id.x = 0; id.x < m_numberOfVoxelsAlongAxis / stride; id.x++)
        {
            for (id.y = 0; id.y < m_numberOfVoxelsAlongAxis / stride; id.y++)
            {
                Vector3Int subSampleID = id / 2;
                int subSampleStride = 2 * stride;

                float[] subSampleDensities = new float[9];

                for (int index = 0; index < 8; index +=2)
                {
                    subSampleDensities[index] = m_densities[CalculateDensityIndex(subSampleStride * (subSampleID + s_corners[index / 2]))].Density;
                }

                for (int index = 1; index < 8; index += 2)
                {
                    subSampleDensities[index] = 0.5f * (subSampleDensities[index - 1] + subSampleDensities[(index + 1) % 8]);
                }

                int subSampleCentralDensitySummationTermsIndex = 0;

                for (int index = 0; index < s_corners.Length; index += 2)
                {
                    subSampleCentralDensitySummationTermsIndex |= (subSampleDensities[index] < 0.0 ? 1 : 0) << index;
                }

                int[] summationTerms = s_subSampleCentralDensitySummationTerms[subSampleCentralDensitySummationTermsIndex];

                subSampleDensities[8] = 0.5f * (subSampleDensities[summationTerms[0]] + subSampleDensities[summationTerms[1]]);

                Vector3Int localID = id - subSampleID;

                float[] densities = new float[4];

                for (int index = 0; index < densities.Length; index++)
                {
                    densities[index] = subSampleDensities[s_densitiesToSubSampleDensitiesMapping[localID.x + 2 * localID.y][index]];
                }

                int segmentsIndex = 0;

                for (int index = 0; index < densities.Length; index++)
                {
                    segmentsIndex |= (densities[index] < 0.0 ? 1 : 0) << index;
                }

                int[] segments = s_segments[segmentsIndex];

                for (int index = 0; index < segments.Length; index += 2)
                {
                    if (segments[index] == -1)
                    {
                        break;
                    }

                    Vector2Int edgeA = s_edges[segments[index + 0]];
                    Vector2Int edgeB = s_edges[segments[index + 1]];

                    Vector3 vertexA = stride * CreateSubSampleVertexAlongEdge(edgeA, densities) + CalculateLocalPosition(id);
                    Vector3 vertexB = stride * CreateSubSampleVertexAlongEdge(edgeB, densities) + CalculateLocalPosition(id);

                    m_lines.Add(vertexA);
                    m_lines.Add(vertexB);
                    m_colors.Add(color);
                }
            }
        }
    }

    private void OnRenderObject()
    {
        m_lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);

        for (int x = 0; x < m_numberOfVoxelsAlongAxis + 1; x++)
        {
            GL.Color(x % 2 == 1 ? Color.gray : Color.black);
            GL.Vertex(CalculateLocalPosition(new Vector3(x, 0.0f, 0.0f)));
            GL.Vertex(CalculateLocalPosition(new Vector3(x, m_numberOfVoxelsAlongAxis, 0.0f)));
        }

        for (int y = 0; y < m_numberOfVoxelsAlongAxis + 1; y++)
        {
            GL.Color(y % 2 == 1 ? Color.gray : Color.black);
            GL.Vertex(CalculateLocalPosition(new Vector3(0.0f, y, 0.0f)));
            GL.Vertex(CalculateLocalPosition(new Vector3(m_numberOfVoxelsAlongAxis, y, 0.0f)));
        }

        for (int index = 0; index < m_lines.Count; index += 2)
        {
            GL.Color(m_colors[index / 2]);
            GL.Vertex(m_lines[index + 0]);
            GL.Vertex(m_lines[index + 1]);

            GL.Color(Color.green);

            GL.Vertex(m_lines[index + 0]);
            GL.Vertex(m_lines[index + 0] + 0.1f * Vector3.up);

            GL.Vertex(m_lines[index + 1]);
            GL.Vertex(m_lines[index + 1] + 0.1f * Vector3.up);
        }

        GL.End();
        GL.PopMatrix();
    }
}
