using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubicalMarchingSquares : MonoBehaviour, IMeshifier
{
    public int MaxNumberOfVertices
    {
        get
        {
            return MaxNumberOfTriangles;
        }
    }

    public int MaxNumberOfTriangles
    {
        get
        {
            return 3 * 2 * Voxel.s_faces.Length * CubicalMarchingSquaresTables.s_segments[0].Length;
        }
    }

    public Material m_lineMaterial;
    [Range(5.0f, 180.0f)]
    public float m_sharpFeatureAngle;
    [Range(0, 16)]
    public int m_maxIterations;
    [Range(0.05f, 0.3f)]
    public float m_stepSize;

    private Voxel m_voxel;
    private Vector3[] m_positions;
    private Vector3[] m_normals;
    private int m_vertexIndex;
    private int[] m_triangles;
    private int m_triangleIndex;
    private Mesh m_mesh;
    private MeshFilter m_meshFilter;
    private CubicalMarchingSquaresFlags m_flags;

    public void OnDensitiesChanged()
    {
        UpdateCubicalMarchingSquares();
    }

    private void Start()
    {
        m_voxel = GetComponent<Voxel>();

        m_positions = new Vector3[MaxNumberOfVertices];
        m_normals = new Vector3[MaxNumberOfVertices];
        m_vertexIndex = 0;
        m_triangles = new int[MaxNumberOfTriangles];
        m_triangleIndex = 0;

        m_mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        m_meshFilter = GetComponent<MeshFilter>();
        m_meshFilter.sharedMesh = m_mesh;
    }

    private void Update()
    {
        if (m_flags.HasFlag(CubicalMarchingSquaresFlags.SettingsUpdated))
        {
            m_flags &= ~CubicalMarchingSquaresFlags.SettingsUpdated;

            UpdateCubicalMarchingSquares();
        }
    }

    private (Vector3, Vector3) GenerateVertexAndNormalAlongVoxelEdge(VoxelEdge voxelEdge)
    {
        float sampleA = m_voxel.m_samples[voxelEdge.m_cornerIndexA].Density;
        float sampleB = m_voxel.m_samples[voxelEdge.m_cornerIndexB].Density;
        float interpolant = sampleA / (sampleA - sampleB);

        Vector3 position = Vector3.Lerp(Voxel.s_corners[voxelEdge.m_cornerIndexA], Voxel.s_corners[voxelEdge.m_cornerIndexB], interpolant);
        Vector3 normal = Vector3.Lerp(m_voxel.m_samples[voxelEdge.m_cornerIndexA].m_normal, m_voxel.m_samples[voxelEdge.m_cornerIndexB].m_normal, interpolant).normalized;

        return (position, normal);
    }

    private Vector3 CalculateSegmentSharpFeature(Vector3 edgeVertexA, Vector3 planeTangentA, Vector3 edgeVertexB, Vector3 planeTangentB)
    {
        Vector3 lineVec3 = edgeVertexB - edgeVertexA;
        Vector3 crossVec1and2 = Vector3.Cross(planeTangentA, planeTangentB);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, planeTangentB);
        float s = Vector3.Dot(crossVec3and2, crossVec1and2) / Vector3.Dot(crossVec1and2, crossVec1and2);
        Vector3 faceSharpFeature = edgeVertexA + (planeTangentA * s);

        return faceSharpFeature;
    }

    private void GenerateFaceSegments(int faceIndex, ref Segment[] segments, ref int segmentsCount)
    {
        int[] voxelFaceSampleIndices = Voxel.s_faceSampleIndices[faceIndex];

        int segmentsIndex = 0;

        for (int sampleIndex = 0; sampleIndex < voxelFaceSampleIndices.Length; sampleIndex++)
        {
            segmentsIndex |= (m_voxel.m_samples[voxelFaceSampleIndices[sampleIndex]].Density >= 0.0 ? 1 : 0) << sampleIndex;
        }

        int[] voxelFace = Voxel.s_faces[faceIndex];
        int[][] faceSegments = CubicalMarchingSquaresTables.s_segments[segmentsIndex];

        for (int faceSegmentsIndex = 0; faceSegmentsIndex < faceSegments.Length; faceSegmentsIndex++)
        {
            if (faceSegments[faceSegmentsIndex][0] == -1)
            {
                break;
            }

            int edgeIndexA = voxelFace[faceSegments[faceSegmentsIndex][0]];
            int edgeIndexB = voxelFace[faceSegments[faceSegmentsIndex][1]];

            VoxelEdge edgeA = Voxel.s_edges[edgeIndexA];
            VoxelEdge edgeB = Voxel.s_edges[edgeIndexB];

            (Vector3 vertexA, Vector3 normalA) = GenerateVertexAndNormalAlongVoxelEdge(edgeA);
            (Vector3 vertexB, Vector3 normalB) = GenerateVertexAndNormalAlongVoxelEdge(edgeB);

            Vector3 planeTangentA = CubicalMarchingSquaresTables.s_normalToFaceTangentMatrices[faceIndex].MultiplyVector(normalA);
            Vector3 planeTangentB = CubicalMarchingSquaresTables.s_normalToFaceTangentMatrices[faceIndex].MultiplyVector(normalB);
            bool hasSharpFeature = Mathf.Acos(Vector3.Dot(planeTangentA, planeTangentB)) * Mathf.Rad2Deg >= m_sharpFeatureAngle;
            Vector3 sharpFeatureVertex = hasSharpFeature ? CalculateSegmentSharpFeature(vertexA, planeTangentA, vertexB, planeTangentB) : Vector3.zero;
            Vector3 sharpFeatureNormal = (normalA + normalB).normalized;

            Segment segment = new Segment
            {
                m_endPointA = new SegmentEndPoint
                {
                    m_edgeIndex = edgeIndexA,
                    m_edgeVertex = new Vertex
                    {
                        m_position = vertexA,
                        m_normal = normalA
                    }
                },

                m_endPointB = new SegmentEndPoint
                {
                    m_edgeIndex = edgeIndexB,
                    m_edgeVertex = new Vertex
                    {
                        m_position = vertexB,
                        m_normal = normalB
                    }
                },

                m_hasSharpFeatureVertex = hasSharpFeature,
                m_sharpFeatureVertex = new Vertex
                {
                    m_position = sharpFeatureVertex,
                    m_normal = sharpFeatureNormal
                }
            };

            segments[segmentsCount++] = segment;
        }
    }

    private unsafe int TraceComponents(ref Segment[] segments, int segmentsCount, ref Component[] components)
    {
        int segmentsAssigned = 0;
        int componentsCount = 0;
        int segmentsIndex = -1;
        int remainingSegmentsToAssign = segmentsCount;

        while (remainingSegmentsToAssign > 0)
        {
            segmentsIndex = (segmentsIndex + 1) % segmentsCount;

            if (((segmentsAssigned >> segmentsIndex) & 1) == 1)
            {
                continue;
            }

            int componentLength = 0;
            int start = segments[segmentsIndex].m_endPointA.m_edgeIndex;
            int end = segments[segmentsIndex].m_endPointB.m_edgeIndex;
            segmentsAssigned |= 1 << segmentsIndex;
            components[componentsCount].m_segmentsIndices[componentLength] = segmentsIndex;
            componentLength++;
            remainingSegmentsToAssign--;

            while (start != end)
            {
                segmentsIndex = (segmentsIndex + 1) % segmentsCount;

                if (((segmentsAssigned >> segmentsIndex) & 1) == 1)
                {
                    continue;
                }

                Segment segment = segments[segmentsIndex];

                if (end == segment.m_endPointA.m_edgeIndex || end == segment.m_endPointB.m_edgeIndex)
                {
                    bool flipped = end == segment.m_endPointB.m_edgeIndex;
                    segments[segmentsIndex].m_endPointA = flipped ? segment.m_endPointB : segment.m_endPointA;
                    segments[segmentsIndex].m_endPointB = flipped ? segment.m_endPointA : segment.m_endPointB;

                    end = segments[segmentsIndex].m_endPointB.m_edgeIndex;
                    segmentsAssigned |= 1 << segmentsIndex;
                    components[componentsCount].m_segmentsIndices[componentLength] = segmentsIndex;
                    componentLength++;
                    remainingSegmentsToAssign--;
                }
            }
            components[componentsCount].m_segmentsIndices[componentLength] = -1;
            componentsCount++;
        }
        return componentsCount;
    }

    private unsafe Vertex CalculateComponentCenterVertex(Segment[] segments, Component component)
    {
        Vertex vertex = new Vertex
        {
            m_position = Vector3.zero,
            m_normal = Vector3.zero
        };

        int index = 0;

        while (component.m_segmentsIndices[index] != -1)
        {
            Segment segment = segments[component.m_segmentsIndices[index++]];
            vertex.m_position += segment.m_endPointA.m_edgeVertex.m_position;
            vertex.m_normal += segment.m_endPointA.m_edgeVertex.m_normal;
        }

        vertex.m_position /= index;
        vertex.m_normal = vertex.m_normal.normalized;

        return vertex;
    }

    private unsafe Vector3 CalculateCornerForce(Vector3 corner, Segment[] segments, Component component)
    {
        Vector3 force = Vector3.zero;

        int index = 0;

        while (component.m_segmentsIndices[index] != -1)
        {
            Segment segment = segments[component.m_segmentsIndices[index++]];
            Vector3 edgeVertex = segment.m_endPointA.m_edgeVertex.m_position;
            Vector3 edgeNormal = segment.m_endPointA.m_edgeVertex.m_normal;
            float distance = Vector3.Dot(edgeNormal, corner - edgeVertex);
            force -= distance * edgeNormal;
        }
        return force;
    }

    private Vector3 CalculateCombinedForce(Vector3 center, Vector3[] forces)
    {
        float alpha = center.z + 0.5f;

        Vector3 force03 = (1.0f - alpha) * forces[0] + alpha * forces[3];
        Vector3 force12 = (1.0f - alpha) * forces[1] + alpha * forces[2];
        Vector3 force47 = (1.0f - alpha) * forces[4] + alpha * forces[7];
        Vector3 force56 = (1.0f - alpha) * forces[5] + alpha * forces[6];

        float beta = center.y + 0.5f;

        Vector3 force0347 = (1.0f - beta) * force03 + beta * force47;
        Vector3 force1256 = (1.0f - beta) * force12 + beta * force56;

        float gamma = center.x + 0.5f;

        return (1.0f - gamma) * force0347 + gamma * force1256;
    }

    private unsafe void CalculateComponentSharpFeature(Segment[] segments, ref Component component)
    {
        Vertex vertex = CalculateComponentCenterVertex(segments, component);
        Vector3[] forces = new Vector3[Voxel.s_corners.Length];

        for (int cornerIndex = 0; cornerIndex < forces.Length; cornerIndex++)
        {
            forces[cornerIndex] = CalculateCornerForce(Voxel.s_corners[cornerIndex], segments, component);
        }

        for (int iterations = 0; iterations < m_maxIterations; iterations++)
        {
            vertex.m_position += m_stepSize * CalculateCombinedForce(vertex.m_position, forces);
        }

        component.m_sharpFeatureVertex = vertex;
    }

    private unsafe void UpdateCubicalMarchingSquares()
    {
        m_mesh.Clear();

        m_vertexIndex = 0;
        m_triangleIndex = 0;

        Segment[] segments = new Segment[Voxel.s_faces.Length * CubicalMarchingSquaresTables.s_segments[0][0].Length];
        int segmentsCount = 0;

        for (int faceIndex = 0; faceIndex < Voxel.s_faces.Length; faceIndex++)
        {
            GenerateFaceSegments(faceIndex, ref segments, ref segmentsCount);
        }

        if (segmentsCount == 0)
        {
            return;
        }

        Component[] components = new Component[4];
        int componentsCount = TraceComponents(ref segments, segmentsCount, ref components);

        for (int componentIndex = 0; componentIndex < componentsCount; componentIndex++)
        {
            CalculateComponentSharpFeature(segments, ref components[componentIndex]);
        }

        for (int componentIndex = 0; componentIndex < componentsCount; componentIndex++)
        {
            Component component = components[componentIndex];
            int index = 0;

            while (component.m_segmentsIndices[index] != -1)
            {
                Segment segment = segments[component.m_segmentsIndices[index++]];

                if (segment.m_hasSharpFeatureVertex)
                {
                    AddTriangle(component.m_sharpFeatureVertex, segment.m_endPointA.m_edgeVertex, segment.m_sharpFeatureVertex);
                    AddTriangle(component.m_sharpFeatureVertex, segment.m_sharpFeatureVertex, segment.m_endPointB.m_edgeVertex);
                }
                else
                {
                    AddTriangle(component.m_sharpFeatureVertex, segment.m_endPointA.m_edgeVertex, segment.m_endPointB.m_edgeVertex);
                }
            }
        }

        if (m_vertexIndex > 0)
        {
            m_mesh.SetVertices(m_positions, 0, m_vertexIndex);
            m_mesh.SetNormals(m_normals, 0, m_vertexIndex);
            m_mesh.SetTriangles(m_triangles, 0, m_triangleIndex, 0);
        }
    }

    private void AddTriangle(Vertex vertexA, Vertex vertexB, Vertex vertexC)
    {
        m_triangles[m_triangleIndex++] = m_vertexIndex + 0;
        m_triangles[m_triangleIndex++] = m_vertexIndex + 2;
        m_triangles[m_triangleIndex++] = m_vertexIndex + 1;

        Vector3 normal = -Vector3.Cross(vertexB.m_position - vertexA.m_position, vertexC.m_position - vertexA.m_position).normalized;

        m_positions[m_vertexIndex] = vertexA.m_position;
        m_normals[m_vertexIndex++] = normal;
        m_positions[m_vertexIndex] = vertexB.m_position;
        m_normals[m_vertexIndex++] = normal;
        m_positions[m_vertexIndex] = vertexC.m_position;
        m_normals[m_vertexIndex++] = normal;
    }

    private void OnRenderObject()
    {
        DrawNormals();
    }

    private void DrawNormals()
    {
        if (m_triangleIndex == 0)
        {
            return;
        }

        m_lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(Color.red);

        for (int triangleIndex = 0; triangleIndex < m_triangleIndex; triangleIndex++)
        {
            GL.Vertex(m_positions[m_triangles[triangleIndex]]);
            GL.Vertex(m_positions[m_triangles[triangleIndex]] + 0.2f * m_normals[m_triangles[triangleIndex]]);
        }

        GL.End();
        GL.PopMatrix();
    }

    private void OnValidate()
    {
        m_flags |= CubicalMarchingSquaresFlags.SettingsUpdated;
    }

    [Flags]
    private enum CubicalMarchingSquaresFlags
    {
        SettingsUpdated = 1
    }

    private unsafe struct Component
    {
        public fixed int m_segmentsIndices[8];
        public Vertex m_sharpFeatureVertex;
    }

    private struct Segment
    {
        public SegmentEndPoint m_endPointA;
        public SegmentEndPoint m_endPointB;

        public bool m_hasSharpFeatureVertex;
        public Vertex m_sharpFeatureVertex;
    }

    private struct SegmentEndPoint
    {
        public int m_edgeIndex;
        public Vertex m_edgeVertex;
    }
}
