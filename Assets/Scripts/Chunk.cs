using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(DensityVolume))]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    [Header("Chunk")]
    [Range(1, 64)]
    public int m_numberOfVoxelsAlongAxis = 16;
    [Range(0.25f, 4.0f)]
    public float m_voxelSpacing = 0.5f;
    [Range(0, 6)]
    public int m_LODLevel;

    [Header("Shader")]
    public ComputeShader m_shader;
    [Range(0, 50)]
    public int m_maxIterations = 10;
    [Range(0.0f, 0.5f)]
    public float m_stepSize = 0.2f;
    [Range(5.0f, 180.0f)]
    public float m_sharpFeatureAngle = 40.0f;
    public bool m_flatShading = true;
    public bool m_asyncReadback = false;

    [Header("Editor")]
    public bool m_showBounds = false;
    public Color m_gizmoColor = Color.green;

    private int NumberOfVoxels
    {
        get
        {
            return m_numberOfVoxelsAlongAxis * m_numberOfVoxelsAlongAxis * m_numberOfVoxelsAlongAxis;
        }
    }

    private int NumberOfDensitySamplesAlongAxis
    {
        get
        {
            return m_numberOfVoxelsAlongAxis + 1;
        }
    }

    private int NumberOfDensitySamples
    {
        get
        {
            return NumberOfDensitySamplesAlongAxis * NumberOfDensitySamplesAlongAxis * NumberOfDensitySamplesAlongAxis;
        }
    }

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
            // 3 indices per triangle * 2 triangles per segment * 2 segments per face * 6 faces per voxel * number of voxels
            return 3 * 2 * 2 * 6 * NumberOfVoxels;
        }
    }

    private Mesh m_mesh;
    private MeshFilter m_meshFilter;

    private Bounds m_localBounds;

    private DensityVolume m_densityVolumeManipulator;
    private ComputeBuffer m_densityVolumeBuffer;
    private ComputeBuffer m_densityGradientBuffer;
    private ComputeBuffer m_vertexBuffer;
    private ComputeBuffer m_triangleBuffer;
    private ComputeBuffer m_vertexCountBuffer;
    private ComputeBuffer m_triangleCountBuffer;

    private AsyncGPUReadbackRequest m_vertexCountRequest;
    private AsyncGPUReadbackRequest m_triangleCountRequest;
    private AsyncGPUReadbackRequest m_vertexRequest;
    private AsyncGPUReadbackRequest m_triangleRequest;

    private ChunkFlags m_flags;

    public void OnDensitiesChanged()
    {
        m_flags |= ChunkFlags.SettingsUpdated;
    }

    private void Start()
    {
        CreateBuffers();
        InitializeMeshComponents();
        m_densityVolumeManipulator = GetComponent<DensityVolume>();
        GenerateDensityVolume(transform.position);
        CalculateLocalBounds();
        UpdateMesh();
    }

    private void GenerateDensityVolume(Vector3 offset)
    {
        m_densityVolumeManipulator.Generate
        (
            m_densityVolumeBuffer,
            m_densityGradientBuffer,
            NumberOfDensitySamplesAlongAxis,
            m_numberOfVoxelsAlongAxis,
            m_voxelSpacing,
            offset
        );
    }

    private void CalculateLocalBounds()
    {
        m_localBounds = new Bounds
        (
            Vector3.zero, m_voxelSpacing * new Vector3(m_numberOfVoxelsAlongAxis, m_numberOfVoxelsAlongAxis, m_numberOfVoxelsAlongAxis)
        );
    }

    private void CreateBuffers()
    {
        m_densityVolumeBuffer = new ComputeBuffer(NumberOfDensitySamples, sizeof(float));
        m_densityGradientBuffer = new ComputeBuffer(NumberOfDensitySamples, 3 * sizeof(float));
        m_vertexBuffer = new ComputeBuffer(MaxNumberOfVertices, 6 * sizeof(float), ComputeBufferType.Counter);
        m_triangleBuffer = new ComputeBuffer(MaxNumberOfTriangles, 3 * sizeof(int), ComputeBufferType.Append);
        m_vertexCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        m_triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }

    private void ReleaseBuffers()
    {
        m_densityVolumeBuffer.Release();
        m_densityGradientBuffer.Release();
        m_vertexBuffer.Release();
        m_triangleBuffer.Release();
        m_vertexCountBuffer.Release();
        m_triangleCountBuffer.Release();
    }
    private void InitializeMeshComponents()
    {
        m_mesh = new Mesh
        {
            indexFormat = IndexFormat.UInt32
        };
        m_meshFilter = GetComponent<MeshFilter>();
        m_meshFilter.sharedMesh = m_mesh;
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            GenerateDensityVolume(transform.position);
            UpdateMesh();
        }

        if (m_flags.HasFlag(ChunkFlags.RetrievingVertexAndTriangleCount) && (m_vertexCountRequest.done && m_triangleCountRequest.done || !m_asyncReadback))
        {
            OnVertexAndTriangleCountRetrieved();
        }

        if (m_flags.HasFlag(ChunkFlags.RetrievingMeshData) && (m_vertexRequest.done && m_triangleRequest.done || !m_asyncReadback))
        {
            OnMeshDataRetrieved();
        }

        if (m_flags.HasFlag(ChunkFlags.SettingsUpdated))
        {
            OnSettingsUpdated();
        }
    }

    private void OnVertexAndTriangleCountRetrieved()
    {
        m_flags &= ~ChunkFlags.RetrievingVertexAndTriangleCount;

        m_vertexCountRequest.WaitForCompletion();
        m_triangleCountRequest.WaitForCompletion();

        int vertexCount = m_vertexCountRequest.GetData<int>()[0];
        int triangleCount = m_triangleCountRequest.GetData<int>()[0];

        if (triangleCount == 0)
        {
            m_meshFilter.sharedMesh = null;

            return;
        }

        // Retrieve vertices and triangles asynchronously.
        m_vertexRequest = AsyncGPUReadback.Request(m_vertexBuffer, vertexCount * m_vertexBuffer.stride, 0);
        m_triangleRequest = AsyncGPUReadback.Request(m_triangleBuffer, triangleCount * m_triangleBuffer.stride, 0);
        m_flags |= ChunkFlags.RetrievingMeshData;
    }

    private void OnMeshDataRetrieved()
    {
        m_flags &= ~ChunkFlags.RetrievingMeshData;

        m_vertexRequest.WaitForCompletion();
        m_triangleRequest.WaitForCompletion();

        NativeArray<Vertex> vertices = m_vertexRequest.GetData<Vertex>();
        NativeArray<int> triangles = m_triangleRequest.GetData<int>();

        m_mesh.SetVertexBufferParams(vertices.Length, new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
        });
        m_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
        m_mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
        m_mesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);
        m_mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));
        m_mesh.RecalculateBounds();
        m_meshFilter.sharedMesh = m_mesh;
    }

    private void OnSettingsUpdated()
    {
        m_flags &= ~ChunkFlags.SettingsUpdated;

        if (m_flatShading)
        {
            m_shader.EnableKeyword("FLAT_SHADING");
        }
        else
        {
            m_shader.DisableKeyword("FLAT_SHADING");
        }

        ReleaseBuffers();
        CreateBuffers();
        GenerateDensityVolume(transform.position);
        CalculateLocalBounds();
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        m_vertexBuffer.SetCounterValue(0);
        m_triangleBuffer.SetCounterValue(0);

        int stride = 1 << m_LODLevel;

        m_shader.SetInts("densityDimensions", NumberOfDensitySamplesAlongAxis, NumberOfDensitySamplesAlongAxis, NumberOfDensitySamplesAlongAxis);
        m_shader.SetInts("voxelDimensions", m_numberOfVoxelsAlongAxis / stride, m_numberOfVoxelsAlongAxis / stride, m_numberOfVoxelsAlongAxis / stride);
        m_shader.SetInt("stride", stride);
        m_shader.SetFloat("voxelSpacing", m_voxelSpacing);
        m_shader.SetVector("offset", transform.position);
        m_shader.SetInt("maxIterations", m_maxIterations);
        m_shader.SetFloat("stepSize", m_stepSize);
        m_shader.SetFloat("sharpFeatureAngle", m_sharpFeatureAngle * Mathf.Deg2Rad);

        int numberOfThreads = Mathf.CeilToInt((m_numberOfVoxelsAlongAxis / stride) / (float)ThreadCount.c_threadCountCubicalMarchingSquares);

        m_shader.SetBuffer(0, "densityVolume", m_densityVolumeBuffer);
        m_shader.SetBuffer(0, "densityGradient", m_densityGradientBuffer);
        m_shader.SetBuffer(0, "generatedVertices", m_vertexBuffer);
        m_shader.SetBuffer(0, "generatedTriangles", m_triangleBuffer);
        m_shader.Dispatch(0, numberOfThreads, numberOfThreads, numberOfThreads);

        ComputeBuffer.CopyCount(m_vertexBuffer, m_vertexCountBuffer, 0);
        ComputeBuffer.CopyCount(m_triangleBuffer, m_triangleCountBuffer, 0);

        // Retrieve vertex and triangle count asynchronously.
        m_vertexCountRequest = AsyncGPUReadback.Request(m_vertexCountBuffer, m_vertexCountBuffer.stride, 0);
        m_triangleCountRequest = AsyncGPUReadback.Request(m_triangleCountBuffer, m_triangleCountBuffer.stride, 0);
        m_flags |= ChunkFlags.RetrievingVertexAndTriangleCount;
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void OnValidate()
    {
        m_numberOfVoxelsAlongAxis = Mathf.ClosestPowerOfTwo(m_numberOfVoxelsAlongAxis);
        m_LODLevel = Mathf.Clamp(m_LODLevel, 0, Mathf.Max(Mathf.RoundToInt(Mathf.Log(m_numberOfVoxelsAlongAxis, 2.0f)) - 2, 0));
        m_flags |= ChunkFlags.SettingsUpdated;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = m_gizmoColor;

        if (m_showBounds)
        {
            Gizmos.DrawWireCube(transform.position + m_localBounds.center, m_localBounds.size);
        }
    }

    [Flags]
    private enum ChunkFlags
    {
        SettingsUpdated = 1,
        RetrievingVertexAndTriangleCount = 2,
        RetrievingMeshData = 4
    }
}
