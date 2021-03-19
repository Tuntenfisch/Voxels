using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CMSCPU : MonoBehaviour
{
    private int NumberOfDensitySamples
    {
        get
        {
            return c_hermiteDimensions.x * c_hermiteDimensions.y * c_hermiteDimensions.z;
        }
    }

    private int NumberOfVoxels
    {
        get
        {
            return c_voxelDimensions.x * c_voxelDimensions.y * c_voxelDimensions.z;
        }
    }

    private int MaxNumberOfVertices
    {
        get
        {
            return MaxNumberOfTriangles;
        }
    }

    private int MaxNumberOfTriangles
    {
        get
        {
            // 3 indices per triangle * 2 triangles per segment * 2 segments per face * 6 faces per voxel * number of voxels
            return 3 * s_segments[0].Length * s_faces.Length * NumberOfVoxels;
        }
    }

    private int VoxelStride
    {
        get
        {
            return 1 << m_LODLevel;
        }
    }

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
    private static readonly Vector3Int[] s_voxelCorners =
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)
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
    private static readonly VoxelEdge[] s_voxelEdges =
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

    private static readonly int[][] s_faces =
    {
        new int[] { 2, 11, 6, 10 }, // rear face
        new int[] { 1, 10, 5,  9 }, // right face
        new int[] { 0,  9, 4,  8 }, // front face
        new int[] { 3,  8, 7, 11 }, // left face
        new int[] { 0,  1, 2,  3 }, // bottom face
        new int[] { 4,  5, 6,  7 }  // top face
    };

    private static readonly int[][] s_faceSampleIndices =
    {
        new int[] { 2, 3, 7, 6 },   // rear face
        new int[] { 1, 2, 6, 5 },   // right face
        new int[] { 0, 1, 5, 4 },   // front face
        new int[] { 3, 0, 4, 7 },   // left face
        new int[] { 0, 1, 2, 3 },   // bottom face
        new int[] { 4, 5, 6, 7 }    // top face
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
        new int[] {  3,  2,  1,  0 },  // ambiguous case
        new int[] {  0,  2, -1, -1 },
        new int[] {  3,  2, -1, -1 },
        new int[] {  2,  3, -1, -1 },
        new int[] {  2,  0, -1, -1 },
        new int[] {  3,  0,  2,  1 },  // ambiguous case
        new int[] {  2,  1, -1, -1 },
        new int[] {  1,  3, -1, -1 },
        new int[] {  1,  0, -1, -1 },
        new int[] {  0,  3, -1, -1 },
        new int[] { -1, -1, -1, -1 }
    };

    private static readonly Matrix4x4[] s_normalToFaceTangentMatrices =
    {
        new Matrix4x4(new Vector4(0.0f, 1.0f, 0.0f, 0.0f), new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 1.0f, 0.0f), new Vector4(0.0f, -1.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 1.0f, 0.0f, 0.0f), new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 1.0f, 0.0f), new Vector4(0.0f, -1.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 0.0f, 1.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 0.0f, 1.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f))
    };

    [Header("General")]
    [Range(0, 3)]
    public int m_LODLevel;
    public Faces m_subSampleChunkFaces;

    [Header("Shader")]
    public Material m_lineMaterial;
    [Range(0, 50)]
    public int m_maxIterations = 10;
    [Range(0.0f, 0.4f)]
    public float m_stepSize = 0.2f;
    [Range(0.0f, 180.0f)]
    public float m_sharpFeatureAngle = 40.0f;
    public bool m_flatShading = true;

    [Header("Debug")]
    public bool m_showWireVoxels;
    public bool m_showNormals;

    private static readonly Vector3Int c_voxelDimensions = new Vector3Int(8, 8, 8);
    private static readonly Vector3Int c_hermiteDimensions = c_voxelDimensions + Vector3Int.one;

    private HermiteData[] m_hermiteVolume;
    private Vector3[] m_positions;
    private Vector3[] m_normals;
    private int m_vertexIndex;
    private int[] m_triangles;
    private int m_triangleIndex;
    private Mesh m_mesh;
    private MeshFilter m_meshFilter;
    private CMSCPUFlags m_flags;

    private void Start()
    {
        m_hermiteVolume = new HermiteData[NumberOfDensitySamples];

        GenerateHermiteVolume();

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
        if (transform.hasChanged)
        {
            GenerateHermiteVolume();
            UpdateCubicalMarchingSquares();
            transform.hasChanged = false;
        }

        if (m_flags.HasFlag(CMSCPUFlags.SettingsUpdated))
        {
            m_flags &= ~CMSCPUFlags.SettingsUpdated;
            UpdateCubicalMarchingSquares();
        }
    }

    private Vector3 ClampPositionToVoxel(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, s_voxelCorners[0].x, s_voxelCorners[6].x);
        position.y = Mathf.Clamp(position.y, s_voxelCorners[0].y, s_voxelCorners[6].y);
        position.z = Mathf.Clamp(position.z, s_voxelCorners[0].z, s_voxelCorners[6].z);

        return position;
    }

    private void GenerateHermiteVolume()
    {
        Vector3Int id = Vector3Int.zero;

        for (id.x = 0; id.x < c_hermiteDimensions.x; id.x++)
        {
            for (id.y = 0; id.y < c_hermiteDimensions.y; id.y++)
            {
                for (id.z = 0; id.z < c_hermiteDimensions.z; id.z++)
                {
                    Vector3 worldPosition = CalculateWorldPosition(id);
                    HermiteData hermiteData;
                    hermiteData.m_density = 4.0f - (worldPosition - new Vector3(0.0f, 0.0f, 0.0f)).magnitude;
                    hermiteData.m_normal = (worldPosition - new Vector3(0.0f, 0.0f, 0.0f)).normalized;
                    m_hermiteVolume[CalculateHermiteIndex(id)] = hermiteData;
                }
            }
        }
    }

    private bool IsOnVoxelEdge(Vector3 position)
    {
        bool x = position.x == 0.0 || position.x == 1;
        bool y = position.y == 0.0 || position.y == 1;
        bool z = position.z == 0.0 || position.z == 1;

        return x && (y || z) || (y && z);
    }

    private int CalculateHermiteIndex(Vector3Int position)
    {
        return position.x + position.y * c_hermiteDimensions.x + position.z * c_hermiteDimensions.x * c_hermiteDimensions.y;
    }

    private Vector3 CalculateLocalPosition(Vector3Int position)
    {
        return VoxelStride * (position - 0.5f * (Vector3)c_voxelDimensions / VoxelStride);
    }

    private Vector3 CalculateWorldPosition(Vector3Int position)
    {
        return position - 0.5f * (Vector3)c_voxelDimensions + transform.position;
    }

    private Vertex CalculateVertexAlongEdge(Vector3Int id, VoxelEdge edge)
    {
        Vertex edgeVertex;

        Vector3Int cornerA = s_voxelCorners[edge.m_cornerIndexA];
        Vector3Int cornerB = s_voxelCorners[edge.m_cornerIndexB];

        HermiteData sampleA = m_hermiteVolume[CalculateHermiteIndex(VoxelStride * (id + cornerA))];
        HermiteData sampleB = m_hermiteVolume[CalculateHermiteIndex(VoxelStride * (id + cornerB))];
        float interpolant = -sampleA.m_density / (sampleB.m_density - sampleA.m_density);

        edgeVertex.m_position = Vector3.Lerp(cornerA, cornerB, interpolant);
        edgeVertex.m_normal = Vector3.Lerp(sampleA.m_normal, sampleB.m_normal, interpolant).normalized;

        return edgeVertex;
    }

    private Vertex CalculateSegmentSharpFeatureVertex(Vertex edgeVertexA, Vector3 planeTangentA, Vertex edgeVertexB, Vector3 planeTangentB)
    {
        Vertex sharpFeatureVertex;

        Vector3 lineVec3 = edgeVertexB.m_position - edgeVertexA.m_position;
        Vector3 crossVec1and2 = Vector3.Cross(planeTangentA, planeTangentB);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, planeTangentB);
        float s = Vector3.Dot(crossVec3and2, crossVec1and2) / Vector3.Dot(crossVec1and2, crossVec1and2);
        Vector3 faceSharpFeaturePosition = edgeVertexA.m_position + (planeTangentA * s);

        sharpFeatureVertex.m_position = ClampPositionToVoxel(faceSharpFeaturePosition);
        sharpFeatureVertex.m_normal = (edgeVertexA.m_normal + edgeVertexB.m_normal).normalized;

        return sharpFeatureVertex;
    }

    private void AddTriangle(Vector3Int id, Vertex vertexA, Vertex vertexB, Vertex vertexC)
    {
        m_triangles[m_triangleIndex++] = m_vertexIndex + 0;
        m_triangles[m_triangleIndex++] = m_vertexIndex + 2;
        m_triangles[m_triangleIndex++] = m_vertexIndex + 1;

        Vector3 localPosition = CalculateLocalPosition(id);

        m_positions[m_vertexIndex + 0] = VoxelStride * vertexA.m_position + localPosition;
        m_positions[m_vertexIndex + 1] = VoxelStride * vertexB.m_position + localPosition;
        m_positions[m_vertexIndex + 2] = VoxelStride * vertexC.m_position + localPosition;

        Vector3 normal = -Vector3.Cross(vertexB.m_position - vertexA.m_position, vertexC.m_position - vertexA.m_position).normalized;

        if (m_flatShading)
        {
            m_normals[m_vertexIndex + 0] = normal;
            m_normals[m_vertexIndex + 1] = normal;
            m_normals[m_vertexIndex + 2] = normal;
        }
        else
        {
            m_normals[m_vertexIndex + 0] = vertexA.m_normal;
            m_normals[m_vertexIndex + 1] = vertexB.m_normal;
            m_normals[m_vertexIndex + 2] = vertexC.m_normal;
        }

        m_vertexIndex += 3;
    }

    private void GenerateFaceSegments(Vector3Int id, int faceIndex, ref Segment[] segments, ref int segmentsCount)
    {
        bool subSample = ((int)m_subSampleChunkFaces >> faceIndex & 1) == 1;

        int[] faceSampleIndices = s_faceSampleIndices[faceIndex];

        int segmentsIndex = 0;
        Vector3Int position;
        
        position = VoxelStride * (id + s_voxelCorners[faceSampleIndices[0]]);
        segmentsIndex |= (m_hermiteVolume[CalculateHermiteIndex(position)].m_density >= 0.0 ? 1 : 0) << 0;
        position = VoxelStride * (id + s_voxelCorners[faceSampleIndices[1]]);
        segmentsIndex |= (m_hermiteVolume[CalculateHermiteIndex(position)].m_density >= 0.0 ? 1 : 0) << 1;
        position = VoxelStride * (id + s_voxelCorners[faceSampleIndices[2]]);
        segmentsIndex |= (m_hermiteVolume[CalculateHermiteIndex(position)].m_density >= 0.0 ? 1 : 0) << 2;
        position = VoxelStride * (id + s_voxelCorners[faceSampleIndices[3]]);
        segmentsIndex |= (m_hermiteVolume[CalculateHermiteIndex(position)].m_density >= 0.0 ? 1 : 0) << 3;

        int[] voxelFace = s_faces[faceIndex];
        int[] faceSegments = s_segments[segmentsIndex];

        for (int faceSegmentsIndex = 0; faceSegmentsIndex < faceSegments.Length; faceSegmentsIndex += 2)
        {
            if (faceSegments[faceSegmentsIndex] == -1)
            {
                break;
            }

            int edgeIndexA = voxelFace[faceSegments[faceSegmentsIndex + 0]];
            int edgeIndexB = voxelFace[faceSegments[faceSegmentsIndex + 1]];

            VoxelEdge voxelEdgeA = s_voxelEdges[edgeIndexA];
            VoxelEdge voxelEdgeB = s_voxelEdges[edgeIndexB];

            Vertex edgeVertexA = CalculateVertexAlongEdge(id, voxelEdgeA);
            Vertex edgeVertexB = CalculateVertexAlongEdge(id, voxelEdgeB);

            Vector3 planeTangentA = s_normalToFaceTangentMatrices[faceIndex].MultiplyVector(edgeVertexA.m_normal);
            Vector3 planeTangentB = s_normalToFaceTangentMatrices[faceIndex].MultiplyVector(edgeVertexB.m_normal);
            bool hasSharpFeature = Mathf.Acos(Vector3.Dot(edgeVertexA.m_normal, edgeVertexB.m_normal)) * Mathf.Rad2Deg >= m_sharpFeatureAngle;
            Vertex sharpFeatureVertex = new Vertex { m_position = Vector3.zero, m_normal = Vector3.zero };

            if (hasSharpFeature)
            {
                sharpFeatureVertex = CalculateSegmentSharpFeatureVertex(edgeVertexA, planeTangentA, edgeVertexB, planeTangentB);
            }

            Segment segment = new Segment
            {
                m_endPointA = new SegmentEndPoint
                {
                    m_edgeIndex = edgeIndexA,
                    m_edgeVertex = edgeVertexA
                },

                m_endPointB = new SegmentEndPoint
                {
                    m_edgeIndex = edgeIndexB,
                    m_edgeVertex = edgeVertexB
                },

                m_hasSharpFeatureVertex = hasSharpFeature,
                m_sharpFeatureVertex = sharpFeatureVertex
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
            Vertex edgeVertex = segment.m_endPointA.m_edgeVertex;

            if (IsOnVoxelEdge(edgeVertex.m_position))
            {
                vertex.m_position += edgeVertex.m_position;
                vertex.m_normal += edgeVertex.m_normal;
            }
        }

        vertex.m_position /= index;
        vertex.m_normal = vertex.m_normal.normalized;

        return vertex;
    }

    private unsafe bool HasComponentSharpFeature(Segment[] segments, Component component)
    {
        float cosOfAngle = 1.0f;

        int indexA = 0;

        while (component.m_segmentsIndices[indexA] != -1)
        {
            Segment segmentA = segments[component.m_segmentsIndices[indexA++]];
            Vertex edgeVertexA = segmentA.m_endPointA.m_edgeVertex;

            int indexB = 0;

            while (component.m_segmentsIndices[indexB] != -1)
            {
                Segment segmentB = segments[component.m_segmentsIndices[indexB++]];
                Vertex edgeVertexB = segmentB.m_endPointA.m_edgeVertex;

                if (IsOnVoxelEdge(edgeVertexA.m_position) && IsOnVoxelEdge(edgeVertexB.m_position))
                {
                    float newCosOfAngle = Vector3.Dot(edgeVertexA.m_normal, edgeVertexB.m_normal);
                    cosOfAngle = cosOfAngle > newCosOfAngle ? newCosOfAngle : cosOfAngle;
                }
            }
        }
        return Mathf.Acos(cosOfAngle) >= m_sharpFeatureAngle;
    }

    private unsafe Vector3 CalculateCornerForce(Vector3 corner, Segment[] segments, Component component)
    {
        Vector3 force = Vector3.zero;

        int index = 0;

        while (component.m_segmentsIndices[index] != -1)
        {
            Segment segment = segments[component.m_segmentsIndices[index++]];
            Vertex edgeVertex = segment.m_endPointA.m_edgeVertex;

            if (IsOnVoxelEdge(edgeVertex.m_position))
            {
                float distance = Vector3.Dot(edgeVertex.m_normal, corner - edgeVertex.m_position);
                force -= distance * edgeVertex.m_normal;
            }
        }
        return force;
    }

    private Vector3 CalculateCombinedForce(Vector3 center, Vector3[] forces)
    {
        float alpha = center.z;

        Vector3 force03 = (1.0f - alpha) * forces[0] + alpha * forces[3];
        Vector3 force12 = (1.0f - alpha) * forces[1] + alpha * forces[2];
        Vector3 force47 = (1.0f - alpha) * forces[4] + alpha * forces[7];
        Vector3 force56 = (1.0f - alpha) * forces[5] + alpha * forces[6];

        float beta = center.y;

        Vector3 force0347 = (1.0f - beta) * force03 + beta * force47;
        Vector3 force1256 = (1.0f - beta) * force12 + beta * force56;

        float gamma = center.x;

        return (1.0f - gamma) * force0347 + gamma * force1256;
    }

    private unsafe void CalculateComponentSharpFeature(Segment[] segments, ref Component component)
    {
        component.m_sharpFeatureVertex = CalculateComponentCenterVertex(segments, component);

        if (!HasComponentSharpFeature(segments, component))
        {
            return;
        }

        Vector3[] forces = new Vector3[s_voxelCorners.Length];

        for (int cornerIndex = 0; cornerIndex < forces.Length; cornerIndex++)
        {
            forces[cornerIndex] = CalculateCornerForce(s_voxelCorners[cornerIndex], segments, component);
        }

        for (int iterations = 0; iterations < m_maxIterations; iterations++)
        {
            component.m_sharpFeatureVertex.m_position += m_stepSize * CalculateCombinedForce(component.m_sharpFeatureVertex.m_position, forces);
        }

        component.m_sharpFeatureVertex.m_position = ClampPositionToVoxel(component.m_sharpFeatureVertex.m_position);
    }

    private unsafe void GenerateMesh(Vector3Int id)
    {
        Segment[] segments = new Segment[s_faces.Length * s_segments[0].Length / 2];
        int segmentsCount = 0;

        for (int faceIndex = 0; faceIndex < s_faces.Length; faceIndex++)
        {
            GenerateFaceSegments(id, faceIndex, ref segments, ref segmentsCount);
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
            Component component = components[componentIndex];
            int index = 0;

            while (component.m_segmentsIndices[index] != -1)
            {
                Segment segment = segments[component.m_segmentsIndices[index++]];

                if (segment.m_hasSharpFeatureVertex)
                {
                    AddTriangle(id, component.m_sharpFeatureVertex, segment.m_endPointA.m_edgeVertex, segment.m_sharpFeatureVertex);
                    AddTriangle(id, component.m_sharpFeatureVertex, segment.m_sharpFeatureVertex, segment.m_endPointB.m_edgeVertex);
                }
                else
                {
                    AddTriangle(id, component.m_sharpFeatureVertex, segment.m_endPointA.m_edgeVertex, segment.m_endPointB.m_edgeVertex);
                }
            }
        }
    }

    private unsafe void UpdateCubicalMarchingSquares()
    {
        m_mesh.Clear();

        m_vertexIndex = 0;
        m_triangleIndex = 0;

        Vector3Int id = Vector3Int.zero;

        for (id.x = 0; id.x < c_voxelDimensions.x / VoxelStride; id.x++)
        {
            for (id.y = 0; id.y < c_voxelDimensions.y / VoxelStride; id.y++)
            {
                for (id.z = 0; id.z < c_voxelDimensions.z / VoxelStride; id.z++)
                {
                    GenerateMesh(id);
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

    private void OnRenderObject()
    {
        if (m_showWireVoxels)
        {
            DrawWireVoxels();
        }

        if (m_showNormals)
        {
            DrawNormals();
        }
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

    private void DrawWireVoxels()
    {
        m_lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(Color.grey);

        Vector3Int id = Vector3Int.zero;

        for (id.x = 0; id.x < c_voxelDimensions.x / VoxelStride; id.x++)
        {
            for (id.y = 0; id.y < c_voxelDimensions.y / VoxelStride; id.y++)
            {
                for (id.z = 0; id.z < c_voxelDimensions.z / VoxelStride; id.z++)
                {
                    Vector3 localPosition = CalculateLocalPosition(id);

                    for (int index = 0; index < 4; index++)
                    {
                        GL.Vertex(localPosition + VoxelStride * s_voxelCorners[index]);
                        GL.Vertex(localPosition + VoxelStride * s_voxelCorners[(index + 1) % 4]);
                        GL.Vertex(localPosition + VoxelStride * s_voxelCorners[index]);
                        GL.Vertex(localPosition + VoxelStride * s_voxelCorners[index + 4]);
                        GL.Vertex(localPosition + VoxelStride * s_voxelCorners[index + 4]);
                        GL.Vertex(localPosition + VoxelStride * s_voxelCorners[(index + 1) % 4 + 4]);
                    }
                }
            }
        }

        GL.End();
        GL.PopMatrix();
    }

    private void OnValidate()
    {
        m_flags |= CMSCPUFlags.SettingsUpdated;
    }

    [Flags]
    private enum CMSCPUFlags
    {
        SettingsUpdated = 1
    }

    [Flags]
    public enum Faces
    {
        Left = 8,
        Right = 2,
        Bottom = 16,
        Top = 32,
        Rear = 1,
        Front = 4
    }

    private struct HermiteData
    {
        public float m_density;
        public Vector3 m_normal;
    }

    private struct VoxelEdge
    {
        public int m_cornerIndexA;
        public int m_cornerIndexB;
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