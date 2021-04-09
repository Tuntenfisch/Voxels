using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(HermiteVolume))]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CubicalMarchingSquares : MonoBehaviour
{
    [Header("Voxel Volume")]
    [Range(2, 128)]
    public int m_numberOfVoxelsAlongAxis = 16;
    [Range(0.25f, 4.0f)]
    public float m_voxelSpacing = 0.5f;
    [Range(0, 7)]
    public int m_LODLevel;

    [Header("Shader")]
    public ComputeShader m_computeShader;
    [Range(0, 50)]
    public int m_maxIterations = 10;
    [Range(0.0f, 0.4f)]
    public float m_stepSize = 0.2f;
    [Range(0.1f, 180.0f)]
    public float m_sharpFeatureAngle = 35.0f;
    public bool m_asyncReadback = false;

    [Header("Editor")]
    public bool m_showBounds = false;
    private int NumberOfVoxels
    {
        get
        {
            return m_numberOfVoxelsAlongAxis * m_numberOfVoxelsAlongAxis * m_numberOfVoxelsAlongAxis;
        }
    }

    private int NumberOfHermiteSamplesAlongAxis
    {
        get
        {
            return m_numberOfVoxelsAlongAxis + 1;
        }
    }

    private int NumberOfHermiteSamples
    {
        get
        {
            return NumberOfHermiteSamplesAlongAxis * NumberOfHermiteSamplesAlongAxis * NumberOfHermiteSamplesAlongAxis;
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

    private static readonly int s_hermiteDimensionsID = Shader.PropertyToID("hermiteDimensions");
    private static readonly int s_voxelDimensionsID = Shader.PropertyToID("voxelDimensions");
    private static readonly int s_voxelStrideID = Shader.PropertyToID("voxelStride");
    private static readonly int s_voxelSpacingID = Shader.PropertyToID("voxelSpacing");
    private static readonly int s_voxelVolumeToWorldSpaceOffsetID = Shader.PropertyToID("voxelVolumeToWorldSpaceOffset");
    private static readonly int s_sharpFeatureAngleID = Shader.PropertyToID("sharpFeatureAngle");
    private static readonly int s_maxIterationsID = Shader.PropertyToID("maxIterations");
    private static readonly int s_stepSizeID = Shader.PropertyToID("stepSize");
    private static readonly int s_hermiteVolumeID = Shader.PropertyToID("hermiteVolume");
    private static readonly int s_generatedVerticesID = Shader.PropertyToID("generatedVertices");
    private static readonly int s_generatedTrianglesID = Shader.PropertyToID("generatedTriangles");

    private Mesh m_mesh;
    private MeshFilter m_meshFilter;
    private MeshCollider m_meshCollider;

    private Bounds m_localBounds;

    private HermiteVolume m_hermiteVolume;

    private Vector3Int m_numberOfThreads;
    private ComputeBuffer m_hermiteVolumeBuffer;
    private ComputeBuffer m_vertexBuffer;
    private ComputeBuffer m_triangleBuffer;
    private ComputeBuffer m_vertexCountBuffer;
    private ComputeBuffer m_triangleCountBuffer;
    private AsyncGPUReadbackRequest m_vertexCountRequest;
    private AsyncGPUReadbackRequest m_triangleCountRequest;
    private AsyncGPUReadbackRequest m_vertexRequest;
    private AsyncGPUReadbackRequest m_triangleRequest;
    private JobHandle m_bakeJobHandle;

    private CubicalMarchingSquaresFlags m_flags;

    private void OnEnable()
    {
        m_hermiteVolume = GetComponent<HermiteVolume>();
        m_hermiteVolume.OnHermiteVolumeChanged += OnSettingsUpdated;

        CreateBuffers();
    }

    private void Start()
    {
        m_computeShader.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
        m_numberOfThreads = new Vector3Int((int)x, (int)y, (int)z);

        InitializeMeshComponents();
        GenerateHermiteVolume(transform.position);
        CalculateLocalBounds();
        UpdateMesh();
    }

    private void GenerateHermiteVolume(Vector3 localToWorldOffset)
    {
        m_hermiteVolume.Generate
        (
            m_hermiteVolumeBuffer,
            NumberOfHermiteSamplesAlongAxis,
            m_numberOfVoxelsAlongAxis,
            m_voxelSpacing,
            localToWorldOffset
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
        m_hermiteVolumeBuffer = new ComputeBuffer(NumberOfHermiteSamples, 4 * sizeof(float));
        m_vertexBuffer = new ComputeBuffer(MaxNumberOfVertices, Vertex.s_sizeInBytes, ComputeBufferType.Counter);
        m_triangleBuffer = new ComputeBuffer(MaxNumberOfTriangles, 3 * sizeof(int), ComputeBufferType.Append);
        m_vertexCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        m_triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }

    private void ReleaseBuffers()
    {
        m_hermiteVolumeBuffer.Release();
        m_hermiteVolumeBuffer = null;
        m_vertexBuffer.Release();
        m_vertexBuffer = null;
        m_triangleBuffer.Release();
        m_triangleBuffer = null;
        m_vertexCountBuffer.Release();
        m_vertexCountBuffer = null;
        m_triangleCountBuffer.Release();
        m_triangleCountBuffer = null;
    }
    private void InitializeMeshComponents()
    {
        m_mesh = new Mesh
        {
            indexFormat = IndexFormat.UInt32
        };
        m_meshFilter = GetComponent<MeshFilter>();
        m_meshFilter.sharedMesh = m_mesh;
        m_meshCollider = GetComponent<MeshCollider>();
        m_meshCollider.sharedMesh = m_mesh;
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            GenerateHermiteVolume(transform.position);
            UpdateMesh();
        }

        if (m_flags.HasFlag(CubicalMarchingSquaresFlags.RetrievingVertexAndTriangleCount) && (m_vertexCountRequest.done && m_triangleCountRequest.done || !m_asyncReadback))
        {
            OnVertexAndTriangleCountRetrieved();
        }

        if (m_flags.HasFlag(CubicalMarchingSquaresFlags.RetrievingMeshData) && (m_vertexRequest.done && m_triangleRequest.done || !m_asyncReadback))
        {
            OnMeshDataRetrieved();
        }

        if (m_flags.HasFlag(CubicalMarchingSquaresFlags.BakingMesh) && m_bakeJobHandle.IsCompleted)
        {
            OnMeshBaked();
        }

        if (m_flags.HasFlag(CubicalMarchingSquaresFlags.SettingsUpdated))
        {
            OnSettingsUpdated();
        }
    }

    private void OnVertexAndTriangleCountRetrieved()
    {
        m_flags &= ~CubicalMarchingSquaresFlags.RetrievingVertexAndTriangleCount;

        m_vertexCountRequest.WaitForCompletion();
        m_triangleCountRequest.WaitForCompletion();

        if (m_vertexCountRequest.hasError || m_triangleCountRequest.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }

        int vertexCount = m_vertexCountRequest.GetData<int>()[0];
        int triangleCount = m_triangleCountRequest.GetData<int>()[0];

        if (triangleCount == 0)
        {
            m_meshFilter.sharedMesh = null;
            m_meshCollider.sharedMesh = null;

            return;
        }

        // Retrieve vertices and triangles asynchronously.
        m_vertexRequest = AsyncGPUReadback.Request(m_vertexBuffer, vertexCount * m_vertexBuffer.stride, 0);
        m_triangleRequest = AsyncGPUReadback.Request(m_triangleBuffer, triangleCount * m_triangleBuffer.stride, 0);
        m_flags |= CubicalMarchingSquaresFlags.RetrievingMeshData;
    }

    private void OnMeshDataRetrieved()
    {
        m_flags &= ~CubicalMarchingSquaresFlags.RetrievingMeshData;

        m_vertexRequest.WaitForCompletion();
        m_triangleRequest.WaitForCompletion();

        if (m_vertexRequest.hasError || m_triangleRequest.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }

        NativeArray<Vertex> vertices = m_vertexRequest.GetData<Vertex>();
        NativeArray<int> triangles = m_triangleRequest.GetData<int>();

        m_mesh.SetVertexBufferParams(vertices.Length, Vertex.s_attributes);
        m_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
        m_mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
        m_mesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);
        m_mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));
        m_mesh.bounds = m_localBounds;
        m_mesh.RecalculateTangents();
        m_meshFilter.sharedMesh = m_mesh;

        m_bakeJobHandle = new BakeJob()
        {
            m_meshID = m_mesh.GetInstanceID(),
            m_convex = false
        }.Schedule();
        m_flags |= CubicalMarchingSquaresFlags.BakingMesh;
    }

    private void OnMeshBaked()
    {
        m_flags &= ~CubicalMarchingSquaresFlags.BakingMesh;
        m_bakeJobHandle.Complete();
        m_meshCollider.sharedMesh = m_mesh;
    }

    private void OnSettingsUpdated()
    {
        m_flags &= ~CubicalMarchingSquaresFlags.SettingsUpdated;

        if (m_hermiteVolumeBuffer.count != NumberOfHermiteSamples)
        {
            ReleaseBuffers();
            CreateBuffers();
        }

        GenerateHermiteVolume(transform.position);
        CalculateLocalBounds();
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        m_vertexBuffer.SetCounterValue(0);
        m_triangleBuffer.SetCounterValue(0);

        int voxelStride = 1 << m_LODLevel;

        m_computeShader.SetInts(s_hermiteDimensionsID, NumberOfHermiteSamplesAlongAxis, NumberOfHermiteSamplesAlongAxis, NumberOfHermiteSamplesAlongAxis);
        m_computeShader.SetInts(s_voxelDimensionsID, m_numberOfVoxelsAlongAxis / voxelStride, m_numberOfVoxelsAlongAxis / voxelStride, m_numberOfVoxelsAlongAxis / voxelStride);
        m_computeShader.SetInt(s_voxelStrideID, voxelStride);
        m_computeShader.SetFloat(s_voxelSpacingID, m_voxelSpacing);
        m_computeShader.SetVector(s_voxelVolumeToWorldSpaceOffsetID, transform.position);
        m_computeShader.SetFloat(s_sharpFeatureAngleID, m_sharpFeatureAngle * Mathf.Deg2Rad);
        m_computeShader.SetInt(s_maxIterationsID, m_maxIterations);
        m_computeShader.SetFloat(s_stepSizeID, m_stepSize);
        m_computeShader.SetBuffer(0, s_hermiteVolumeID, m_hermiteVolumeBuffer);
        m_computeShader.SetBuffer(0, s_generatedVerticesID, m_vertexBuffer);
        m_computeShader.SetBuffer(0, s_generatedTrianglesID, m_triangleBuffer);
        m_computeShader.Dispatch
        (
            0,
            Mathf.CeilToInt((m_numberOfVoxelsAlongAxis / voxelStride) / (float)m_numberOfThreads.x),
            Mathf.CeilToInt((m_numberOfVoxelsAlongAxis / voxelStride) / (float)m_numberOfThreads.y),
            Mathf.CeilToInt((m_numberOfVoxelsAlongAxis / voxelStride) / (float)m_numberOfThreads.z)
        );

        ComputeBuffer.CopyCount(m_vertexBuffer, m_vertexCountBuffer, 0);
        ComputeBuffer.CopyCount(m_triangleBuffer, m_triangleCountBuffer, 0);

        // Retrieve vertex and triangle count asynchronously.
        m_vertexCountRequest = AsyncGPUReadback.Request(m_vertexCountBuffer, m_vertexCountBuffer.stride, 0);
        m_triangleCountRequest = AsyncGPUReadback.Request(m_triangleCountBuffer, m_triangleCountBuffer.stride, 0);
        m_flags |= CubicalMarchingSquaresFlags.RetrievingVertexAndTriangleCount;
    }

    private void OnDisable()
    {
        m_hermiteVolume.OnHermiteVolumeChanged -= OnSettingsUpdated;

        ReleaseBuffers();
    }

    private void OnValidate()
    {
        m_numberOfVoxelsAlongAxis = Mathf.ClosestPowerOfTwo(m_numberOfVoxelsAlongAxis);
        m_LODLevel = Mathf.Clamp(m_LODLevel, 0, Mathf.Max(Mathf.RoundToInt(Mathf.Log(m_numberOfVoxelsAlongAxis, 2.0f)), 0));
        m_flags |= CubicalMarchingSquaresFlags.SettingsUpdated;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (m_showBounds)
        {
            Gizmos.DrawWireCube(transform.position + m_localBounds.center, m_localBounds.size);
        }
    }

    [Flags]
    private enum CubicalMarchingSquaresFlags
    {
        SettingsUpdated = 1,
        RetrievingVertexAndTriangleCount = 2,
        RetrievingMeshData = 4,
        BakingMesh = 8
    }
}
