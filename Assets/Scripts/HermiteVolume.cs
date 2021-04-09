using System;
using UnityEngine;
using UnityEngine.Profiling;

public class HermiteVolume : MonoBehaviour
{
    public event Action OnHermiteVolumeChanged;

    [Header("Noise Parameters")]
    public int m_seed;
    public Vector3 m_offset;


    [Header("Height Map")]
    [Range(0.0f, 100.0f)]
    public float m_height = 15.0f;
    [Range(0.0f, 100.0f)]
    public float m_wavelength = 35.0f;

    [Header("FBM")]
    [Range(1, 16)]
    public int m_numberOfOctaves = 4;
    [Range(0.0f, 1.0f)]
    public float m_persistence = 0.5f;
    [Range(1.0f, 4.0f)]
    public float m_lacunarity = 2.0f;

    [Header("Shader")]
    public ComputeShader m_computeShader;

    private static readonly int s_hermiteDimensionsID = Shader.PropertyToID("hermiteDimensions");
    private static readonly int s_voxelDimensionsID = Shader.PropertyToID("voxelDimensions");
    private static readonly int s_voxelStrideID = Shader.PropertyToID("voxelStride");
    private static readonly int s_voxelSpacingID = Shader.PropertyToID("voxelSpacing");
    private static readonly int s_voxelVolumeToWorldSpaceOffsetID = Shader.PropertyToID("voxelVolumeToWorldSpaceOffset");
    private static readonly int s_wavelengthID = Shader.PropertyToID("wavelength");
    private static readonly int s_numberOfOctavesID = Shader.PropertyToID("numberOfOctaves");
    private static readonly int s_persistenceID = Shader.PropertyToID("persistence");
    private static readonly int s_lacunarityID = Shader.PropertyToID("lacunarity");
    private static readonly int s_heightID = Shader.PropertyToID("height");
    private static readonly int s_hermiteVolumeID = Shader.PropertyToID("hermiteVolume");
    private static readonly int s_octaveOffsetsID = Shader.PropertyToID("octaveOffsets");

    private Vector3Int m_numberOfThreads;
    private ComputeBuffer m_octaveOffsetsBuffer;
    private HermiteVolumeFlags m_flags;

    private void OnEnable()
    {
        m_computeShader.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
        m_numberOfThreads = new Vector3Int((int)x, (int)y, (int)z);
        CreateBuffers();
        CalculateOctaveOffsets();
    }

    private void CreateBuffers()
    {
        m_octaveOffsetsBuffer = new ComputeBuffer(m_numberOfOctaves, 3 * sizeof(float));
    }

    private void ReleaseBuffers()
    {
        m_octaveOffsetsBuffer.Release();
        m_octaveOffsetsBuffer = null;
    }

    private void Update()
    {
        if (m_flags.HasFlag(HermiteVolumeFlags.SettingsUpdated))
        {
            OnSettingsUpdated();
        }
    }

    private void OnSettingsUpdated()
    {
        m_flags &= ~HermiteVolumeFlags.SettingsUpdated;

        if (m_octaveOffsetsBuffer.count != m_numberOfOctaves)
        {
            ReleaseBuffers();
            CreateBuffers();
        }

        CalculateOctaveOffsets();

        OnHermiteVolumeChanged();
    }

    private void CalculateOctaveOffsets()
    {
        System.Random random = new System.Random(m_seed);
        Vector3[] octaveOffsets = new Vector3[m_numberOfOctaves];

        for (int index = 0; index < m_numberOfOctaves; index++)
        {
            octaveOffsets[index] = new Vector3(random.Next(-100000, 100000), random.Next(-100000, 100000), random.Next(-100000, 100000)) + m_offset;
        }
        m_octaveOffsetsBuffer.SetData(octaveOffsets);
    }

    public void Generate
    (
        ComputeBuffer hermiteVolumeBuffer,
        int numberOfHermiteSamplesAlongAxis,
        int numberOfVoxelsAlongAxis,
        float voxelSpacing,
        Vector3 localToWorldOffset
    )
    {
        Profiler.BeginSample("HermiteVolume.Generate");

        int voxelStride = 1;

        m_computeShader.SetInts(s_hermiteDimensionsID, numberOfHermiteSamplesAlongAxis, numberOfHermiteSamplesAlongAxis, numberOfHermiteSamplesAlongAxis);
        m_computeShader.SetInts(s_voxelDimensionsID, numberOfVoxelsAlongAxis / voxelStride, numberOfVoxelsAlongAxis / voxelStride, numberOfVoxelsAlongAxis / voxelStride);
        m_computeShader.SetInt(s_voxelStrideID, voxelStride);
        m_computeShader.SetFloat(s_voxelSpacingID, voxelSpacing);
        m_computeShader.SetVector(s_voxelVolumeToWorldSpaceOffsetID, localToWorldOffset);
        m_computeShader.SetInt(s_numberOfOctavesID, m_numberOfOctaves);
        m_computeShader.SetFloat(s_wavelengthID, m_wavelength);
        m_computeShader.SetFloat(s_persistenceID, m_persistence);
        m_computeShader.SetFloat(s_lacunarityID, m_lacunarity);
        m_computeShader.SetFloat(s_heightID, m_height);
        m_computeShader.SetBuffer(0, s_hermiteVolumeID, hermiteVolumeBuffer);
        m_computeShader.SetBuffer(0, s_octaveOffsetsID, m_octaveOffsetsBuffer);
        m_computeShader.Dispatch
        (
            0,
            Mathf.CeilToInt((numberOfHermiteSamplesAlongAxis / voxelStride) / (float)m_numberOfThreads.x),
            Mathf.CeilToInt((numberOfHermiteSamplesAlongAxis / voxelStride) / (float)m_numberOfThreads.y),
            Mathf.CeilToInt((numberOfHermiteSamplesAlongAxis / voxelStride) / (float)m_numberOfThreads.z)
        );

        Profiler.EndSample();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnValidate()
    {
        m_flags |= HermiteVolumeFlags.SettingsUpdated;
    }

    private enum HermiteVolumeFlags
    {
        SettingsUpdated = 1
    }
}