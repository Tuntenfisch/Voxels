using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubicalMarchingSquares
{
    [RequireComponent(typeof(HermiteVolume))]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class CubicalMarchingSquares : MonoBehaviour
    {
        [Header("Voxel Volume")]
        [Range(2, 64)]
        [SerializeField]
        private int m_numberOfVoxelsAlongAxis = 16;
        [Range(0.25f, 4.0f)]
        [SerializeField]
        private float m_voxelSpacing = 0.5f;

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

        public int NumberOfVoxels => m_numberOfVoxelsAlongAxis * m_numberOfVoxelsAlongAxis * m_numberOfVoxelsAlongAxis;

        public int NumberOfHermiteSamplesAlongAxis => m_numberOfVoxelsAlongAxis + 1;

        public int NumberOfHermiteSamples => NumberOfHermiteSamplesAlongAxis * NumberOfHermiteSamplesAlongAxis * NumberOfHermiteSamplesAlongAxis;

        public int MaxNumberOfFlatFeatureVertices => 3 * NumberOfHermiteSamples - 3 * NumberOfHermiteSamplesAlongAxis * NumberOfHermiteSamplesAlongAxis;

        // 2 sharp feature vertices per segment * 2 segments per face * 6 faces per voxel * number of voxels +
        // 7 sharp feature vertices per component * 4 components per voxel * number of voxels
        public int MaxNumberOfSharpFeatureVertices => 2 * 2 * 6 * NumberOfVoxels + 7 * 4 * NumberOfVoxels;

        // 3 indices per triangle * 2 triangles per segment * 2 segments per face * 6 faces per voxel * number of voxels
        public int MaxNumberOfTriangles => 3 * 2 * 2 * 6 * NumberOfVoxels;

        private static readonly int s_hermiteDimensionsID = Shader.PropertyToID("hermiteDimensions");
        private static readonly int s_voxelDimensionsID = Shader.PropertyToID("voxelDimensions");
        private static readonly int s_voxelSpacingID = Shader.PropertyToID("voxelSpacing");
        private static readonly int s_voxelVolumeToWorldSpaceOffsetID = Shader.PropertyToID("voxelVolumeToWorldSpaceOffset");
        private static readonly int s_cosOfSharpFeatureAngleID = Shader.PropertyToID("cosOfSharpFeatureAngle");
        private static readonly int s_maxIterationsID = Shader.PropertyToID("maxIterations");
        private static readonly int s_stepSizeID = Shader.PropertyToID("stepSize");
        private static readonly int s_hermiteVolumeID = Shader.PropertyToID("hermiteVolume");
        private static readonly int s_generatedVerticesID = Shader.PropertyToID("generatedVertices");
        private static readonly int s_flatFeatureVertexIndicesLookupTableID = Shader.PropertyToID("flatFeatureVertexIndicesLookupTable");
        private static readonly int s_generatedTrianglesID = Shader.PropertyToID("generatedTriangles");

        private Mesh m_mesh;
        private MeshFilter m_meshFilter;
        private MeshCollider m_meshCollider;

        private Bounds m_localBounds;

        private HermiteVolume m_hermiteVolume;

        private Vector3Int m_numberOfThreadsKernel0;
        private Vector3Int m_numberOfThreadsKernel1;
        private AsyncComputeBuffer m_hermiteVolumeBuffer;
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

            m_hermiteVolume = GetComponent<HermiteVolume>();
            m_hermiteVolume.OnHermiteVolumeChanged += OnSettingsUpdated;

            CreateBuffers();
        }

        private void Start()
        {
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
            m_hermiteVolumeBuffer = new AsyncComputeBuffer(NumberOfHermiteSamples, 4 * sizeof(float));
            m_vertexBuffer = new AsyncComputeBuffer(MaxNumberOfFlatFeatureVertices + MaxNumberOfSharpFeatureVertices, Vertex.SizeInBytes, ComputeBufferType.Counter);
            m_flatFeatureVertexIndicesLookupBuffer = new AsyncComputeBuffer(NumberOfHermiteSamples, 3 * sizeof(int));
            m_triangleBuffer = new AsyncComputeBuffer(MaxNumberOfTriangles, 3 * sizeof(int), ComputeBufferType.Append);
            m_vertexCountBuffer = new AsyncComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            m_triangleCountBuffer = new AsyncComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }

        private void ReleaseBuffers()
        {
            m_hermiteVolumeBuffer.Release();
            m_hermiteVolumeBuffer = null;
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
                GenerateHermiteVolume(transform.position);
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

            if (m_hermiteVolumeBuffer.Count != NumberOfHermiteSamples)
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

            m_computeShader.SetInts(s_hermiteDimensionsID, NumberOfHermiteSamplesAlongAxis, NumberOfHermiteSamplesAlongAxis, NumberOfHermiteSamplesAlongAxis);
            m_computeShader.SetInts(s_voxelDimensionsID, m_numberOfVoxelsAlongAxis, m_numberOfVoxelsAlongAxis, m_numberOfVoxelsAlongAxis);
            m_computeShader.SetFloat(s_voxelSpacingID, m_voxelSpacing);
            m_computeShader.SetVector(s_voxelVolumeToWorldSpaceOffsetID, transform.position);
            m_computeShader.SetFloat(s_cosOfSharpFeatureAngleID, Mathf.Cos(m_sharpFeatureAngle * Mathf.Deg2Rad));
            m_computeShader.SetInt(s_maxIterationsID, m_maxIterations);
            m_computeShader.SetFloat(s_stepSizeID, m_stepSize);

            m_computeShader.SetBuffer(0, s_hermiteVolumeID, m_hermiteVolumeBuffer);
            m_computeShader.SetBuffer(0, s_generatedVerticesID, m_vertexBuffer);
            m_computeShader.SetBuffer(0, s_flatFeatureVertexIndicesLookupTableID, m_flatFeatureVertexIndicesLookupBuffer);
            m_computeShader.Dispatch
            (
                0,
                Mathf.CeilToInt(NumberOfHermiteSamplesAlongAxis / (float)m_numberOfThreadsKernel0.x),
                Mathf.CeilToInt(NumberOfHermiteSamplesAlongAxis / (float)m_numberOfThreadsKernel0.y),
                Mathf.CeilToInt(NumberOfHermiteSamplesAlongAxis / (float)m_numberOfThreadsKernel0.z)
            );

            m_computeShader.SetBuffer(1, s_hermiteVolumeID, m_hermiteVolumeBuffer);
            m_computeShader.SetBuffer(1, s_generatedVerticesID, m_vertexBuffer);
            m_computeShader.SetBuffer(1, s_flatFeatureVertexIndicesLookupTableID, m_flatFeatureVertexIndicesLookupBuffer);
            m_computeShader.SetBuffer(1, s_generatedTrianglesID, m_triangleBuffer);
            m_computeShader.Dispatch
            (
                1,
                Mathf.CeilToInt(m_numberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel1.x),
                Mathf.CeilToInt(m_numberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel1.y),
                Mathf.CeilToInt(m_numberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel1.z)
            );

            ComputeBuffer.CopyCount(m_vertexBuffer, m_vertexCountBuffer, 0);
            ComputeBuffer.CopyCount(m_triangleBuffer, m_triangleCountBuffer, 0);

            // Retrieve vertex and triangle count asynchronously.
            m_vertexCountBuffer.RequestData();
            m_triangleCountBuffer.RequestData();
        }

        private void OnDisable()
        {
            m_hermiteVolume.OnHermiteVolumeChanged -= OnSettingsUpdated;

            ReleaseBuffers();
        }

        private void OnValidate()
        {
            m_numberOfVoxelsAlongAxis = Mathf.ClosestPowerOfTwo(m_numberOfVoxelsAlongAxis);
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
