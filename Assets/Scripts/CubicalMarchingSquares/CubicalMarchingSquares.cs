using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubicalMarchingSquares
{
    [RequireComponent(typeof(VoxelVolume))]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class CubicalMarchingSquares : MonoBehaviour
    {
        [Header("Cell Volume")]
        [Range(2, 64)]
        [SerializeField]
        private int m_numberOfCellsAlongAxis = 16;
        [Range(0.25f, 4.0f)]
        [SerializeField]
        private float m_cellSpacing = 0.5f;

        [Header("Shader")]
        [SerializeField]
        private ComputeShader m_computeShader;
        [Range(0, 50)]
        [SerializeField]
        private int m_maxIterations = 10;
        [Range(0.0f, 0.4f)]
        [SerializeField]
        private float m_stepSize = 0.2f;
        [Range(0.1f, 180.0f)]
        [SerializeField]
        private float m_sharpFeatureAngle = 35.0f;
        [SerializeField]
        private bool m_asyncReadback = false;

        [Header("Editor")]
        [SerializeField]
        private bool m_showBounds = false;

        public int NumberOfCells => m_numberOfCellsAlongAxis * m_numberOfCellsAlongAxis * m_numberOfCellsAlongAxis;

        public int NumberOfVoxelsAlongAxis => m_numberOfCellsAlongAxis + 1;

        public int NumberOfVoxels => NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis;

        public int MaxNumberOfFlatFeatureVertices => 3 * NumberOfVoxels - 3 * NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis;

        // 2 sharp feature vertices per segment * 2 segments per face * 6 faces per cell * number of cells +
        // 7 sharp feature vertices per component * 4 components per cell * number of cells
        public int MaxNumberOfSharpFeatureVertices => 2 * 2 * 6 * NumberOfCells + 7 * 4 * NumberOfCells;

        // 3 indices per triangle * 2 triangles per segment * 2 segments per face * 6 faces per cell * number of cells
        public int MaxNumberOfTriangles => 3 * 2 * 2 * 6 * NumberOfCells;

        private static readonly int s_cellDimensionsID = Shader.PropertyToID("cellDimensions");
        private static readonly int s_cellSpacingID = Shader.PropertyToID("cellSpacing");
        private static readonly int s_cellVolumeToWorldSpaceOffsetID = Shader.PropertyToID("cellVolumeToWorldSpaceOffset");
        private static readonly int s_voxelDimensionsID = Shader.PropertyToID("voxelDimensions");
        private static readonly int s_cosOfSharpFeatureAngleID = Shader.PropertyToID("cosOfSharpFeatureAngle");
        private static readonly int s_maxIterationsID = Shader.PropertyToID("maxIterations");
        private static readonly int s_stepSizeID = Shader.PropertyToID("stepSize");
        private static readonly int s_voxelVolumeID = Shader.PropertyToID("voxelVolume");
        private static readonly int s_generatedVerticesID = Shader.PropertyToID("generatedVertices");
        private static readonly int s_flatFeatureVertexIndicesLookupTableID = Shader.PropertyToID("flatFeatureVertexIndicesLookupTable");
        private static readonly int s_generatedTrianglesID = Shader.PropertyToID("generatedTriangles");

        private Mesh m_mesh;
        private MeshFilter m_meshFilter;
        private MeshCollider m_meshCollider;

        private Bounds m_localBounds;

        private VoxelVolume m_voxelVolume;

        private Vector3Int m_numberOfThreadsKernel0;
        private Vector3Int m_numberOfThreadsKernel1;
        private AsyncComputeBuffer m_voxelVolumeBuffer;
        private AsyncComputeBuffer m_vertexBuffer;
        private AsyncComputeBuffer m_flatFeatureVertexIndicesLookupBuffer;
        private AsyncComputeBuffer m_triangleBuffer;
        private AsyncComputeBuffer m_vertexCountBuffer;
        private AsyncComputeBuffer m_triangleCountBuffer;
        private JobHandle m_bakeJobHandle;

        private CubicalMarchingSquaresFlags m_flags;

        private void OnEnable()
        {
            m_computeShader.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
            m_numberOfThreadsKernel0 = new Vector3Int((int)x, (int)y, (int)z);
            m_computeShader.GetKernelThreadGroupSizes(1, out x, out y, out z);
            m_numberOfThreadsKernel1 = new Vector3Int((int)x, (int)y, (int)z);

            m_voxelVolume = GetComponent<VoxelVolume>();
            m_voxelVolume.OnVoxelVolumeChanged += OnSettingsUpdated;

            CreateBuffers();
        }

        private void Start()
        {
            InitializeMeshComponents();
            GenerateVoxelVolume(transform.position);
            CalculateLocalBounds();
            UpdateMesh();
        }

        private void GenerateVoxelVolume(Vector3 localToWorldOffset)
        {
            m_voxelVolume.Generate
            (
                m_voxelVolumeBuffer,
                NumberOfVoxelsAlongAxis,
                m_numberOfCellsAlongAxis,
                m_cellSpacing,
                localToWorldOffset
            );
        }

        private void CalculateLocalBounds()
        {
            m_localBounds = new Bounds
            (
                Vector3.zero, m_cellSpacing * new Vector3(m_numberOfCellsAlongAxis, m_numberOfCellsAlongAxis, m_numberOfCellsAlongAxis)
            );
        }

        private void CreateBuffers()
        {
            m_voxelVolumeBuffer = new AsyncComputeBuffer(NumberOfVoxels, 4 * sizeof(float));
            m_vertexBuffer = new AsyncComputeBuffer(MaxNumberOfFlatFeatureVertices + MaxNumberOfSharpFeatureVertices, Vertex.SizeInBytes, ComputeBufferType.Counter);
            m_flatFeatureVertexIndicesLookupBuffer = new AsyncComputeBuffer(NumberOfVoxels, 3 * sizeof(int));
            m_triangleBuffer = new AsyncComputeBuffer(MaxNumberOfTriangles, 3 * sizeof(int), ComputeBufferType.Append);
            m_vertexCountBuffer = new AsyncComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            m_triangleCountBuffer = new AsyncComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }

        private void ReleaseBuffers()
        {
            m_voxelVolumeBuffer.Release();
            m_voxelVolumeBuffer = null;
            m_vertexBuffer.Release();
            m_vertexBuffer = null;
            m_flatFeatureVertexIndicesLookupBuffer.Release();
            m_flatFeatureVertexIndicesLookupBuffer = null;
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
                GenerateVoxelVolume(transform.position);
                UpdateMesh();
            }

            if (m_vertexCountBuffer.IsDataAvailable(!m_asyncReadback) && m_triangleCountBuffer.IsDataAvailable(!m_asyncReadback))
            {
                OnVertexAndTriangleCountRetrieved();
            }

            if (m_vertexBuffer.IsDataAvailable(!m_asyncReadback) && m_triangleBuffer.IsDataAvailable(!m_asyncReadback))
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
            if (m_vertexCountBuffer.HasError || m_triangleCountBuffer.HasError)
            {
                Debug.Log("GPU readback error detected.");
                return;
            }

            int vertexCount = m_vertexCountBuffer.GetData<int>()[0];
            int triangleCount = m_triangleCountBuffer.GetData<int>()[0];

            if (triangleCount == 0)
            {
                m_meshFilter.sharedMesh = null;
                m_meshCollider.sharedMesh = null;

                return;
            }

            // Retrieve vertices and triangles asynchronously.
            m_vertexBuffer.RequestData(vertexCount);
            m_triangleBuffer.RequestData(triangleCount);
        }

        private void OnMeshDataRetrieved()
        {
            if (m_vertexBuffer.HasError || m_triangleBuffer.HasError)
            {
                Debug.Log("GPU readback error detected.");
                return;
            }

            NativeArray<Vertex> vertices = m_vertexBuffer.GetData<Vertex>();
            NativeArray<int> triangles = m_triangleBuffer.GetData<int>();

            m_mesh.SetVertexBufferParams(vertices.Length, Vertex.Attributes);
            m_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            m_mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
            m_mesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);
            m_mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));
            m_mesh.bounds = m_localBounds;
            m_mesh.RecalculateTangents();
            m_meshFilter.sharedMesh = m_mesh;

            m_bakeJobHandle = new BakeJob(m_mesh.GetInstanceID()).Schedule();
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

            if (m_voxelVolumeBuffer.Count != NumberOfVoxels)
            {
                ReleaseBuffers();
                CreateBuffers();
            }

            GenerateVoxelVolume(transform.position);
            CalculateLocalBounds();
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            m_vertexBuffer.SetCounterValue(0);
            m_triangleBuffer.SetCounterValue(0);

            m_computeShader.SetInts(s_cellDimensionsID, m_numberOfCellsAlongAxis, m_numberOfCellsAlongAxis, m_numberOfCellsAlongAxis);
            m_computeShader.SetFloat(s_cellSpacingID, m_cellSpacing);
            m_computeShader.SetVector(s_cellVolumeToWorldSpaceOffsetID, transform.position);
            m_computeShader.SetInts(s_voxelDimensionsID, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis);
            m_computeShader.SetFloat(s_cosOfSharpFeatureAngleID, Mathf.Cos(m_sharpFeatureAngle * Mathf.Deg2Rad));
            m_computeShader.SetInt(s_maxIterationsID, m_maxIterations);
            m_computeShader.SetFloat(s_stepSizeID, m_stepSize);

            m_computeShader.SetBuffer(0, s_voxelVolumeID, m_voxelVolumeBuffer);
            m_computeShader.SetBuffer(0, s_generatedVerticesID, m_vertexBuffer);
            m_computeShader.SetBuffer(0, s_flatFeatureVertexIndicesLookupTableID, m_flatFeatureVertexIndicesLookupBuffer);
            m_computeShader.Dispatch
            (
                0,
                Mathf.CeilToInt(NumberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel0.x),
                Mathf.CeilToInt(NumberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel0.y),
                Mathf.CeilToInt(NumberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel0.z)
            );

            m_computeShader.SetBuffer(1, s_voxelVolumeID, m_voxelVolumeBuffer);
            m_computeShader.SetBuffer(1, s_generatedVerticesID, m_vertexBuffer);
            m_computeShader.SetBuffer(1, s_flatFeatureVertexIndicesLookupTableID, m_flatFeatureVertexIndicesLookupBuffer);
            m_computeShader.SetBuffer(1, s_generatedTrianglesID, m_triangleBuffer);
            m_computeShader.Dispatch
            (
                1,
                Mathf.CeilToInt(m_numberOfCellsAlongAxis / (float)m_numberOfThreadsKernel1.x),
                Mathf.CeilToInt(m_numberOfCellsAlongAxis / (float)m_numberOfThreadsKernel1.y),
                Mathf.CeilToInt(m_numberOfCellsAlongAxis / (float)m_numberOfThreadsKernel1.z)
            );

            ComputeBuffer.CopyCount(m_vertexBuffer, m_vertexCountBuffer, 0);
            ComputeBuffer.CopyCount(m_triangleBuffer, m_triangleCountBuffer, 0);

            // Retrieve vertex and triangle count asynchronously.
            m_vertexCountBuffer.RequestData();
            m_triangleCountBuffer.RequestData();
        }

        private void OnDisable()
        {
            m_voxelVolume.OnVoxelVolumeChanged -= OnSettingsUpdated;

            ReleaseBuffers();
        }

        private void OnValidate()
        {
            m_numberOfCellsAlongAxis = Mathf.ClosestPowerOfTwo(m_numberOfCellsAlongAxis);
            m_flags |= CubicalMarchingSquaresFlags.SettingsUpdated;
        }

        private void OnDrawGizmos()
        {
            if (m_showBounds)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position + m_localBounds.center, m_localBounds.size);
            }
        }

        [Flags]
        private enum CubicalMarchingSquaresFlags
        {
            SettingsUpdated = 1,
            BakingMesh = 2
        }
    }
}
