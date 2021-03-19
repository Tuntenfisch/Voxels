using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(Chunk))]
public class HermiteVolume : MonoBehaviour
{
    [Header("Noise Parameters")]
    public int m_seed;
    public float m_noiseScale = 10.0f;
    public Vector2 m_noiseOffset;

    [Header("Height Map")]
    [Range(1, 16)]
    public int m_octaves = 3;
    [Range(1.0f, 100.0f)]
    public float m_initialAmplitude = 1.0f;
    [Range(0.0f, 1.0f)]
    public float m_persistence = 0.5f;
    [Range(0.01f, 2.0f)]
    public float m_initialFrequency = 1.0f;
    [Range(1.0f, 4.0f)]
    public float m_lacunarity = 2.0f;
    [Range(-1.0f, 1.0f)]
    public float m_sharpness = 0.0f;
    public AnimationCurve m_scaling;

    [Header("Other")]
    [Range(0.01f, 1.0f)]
    public float m_centralDifferenceSpacing = 0.01f;

    [Header("Shader")]
    public ComputeShader m_shader;

    private static readonly int s_hermiteDimensionsID = Shader.PropertyToID("hermiteDimensions");
    private static readonly int s_voxelDimensionsID = Shader.PropertyToID("voxelDimensions");
    private static readonly int s_voxelStrideID = Shader.PropertyToID("voxelStride");
    private static readonly int s_voxelSpacingID = Shader.PropertyToID("voxelSpacing");
    private static readonly int s_localToWorldOffsetID = Shader.PropertyToID("localToWorldOffset");
    private static readonly int s_noiseScaleID = Shader.PropertyToID("noiseScale");
    private static readonly int s_octavesID = Shader.PropertyToID("octaves");
    private static readonly int s_initialAmplitudeID = Shader.PropertyToID("initialAmplitude");
    private static readonly int s_persistenceID = Shader.PropertyToID("persistence");
    private static readonly int s_initialFrequencyID = Shader.PropertyToID("initialFrequency");
    private static readonly int s_lacunarityID = Shader.PropertyToID("lacunarity");
    private static readonly int s_sharpnessID = Shader.PropertyToID("sharpness");
    private static readonly int s_centralDifferenceSpacingID = Shader.PropertyToID("centralDifferenceSpacing");
    private static readonly int s_hermiteVolumeID = Shader.PropertyToID("hermiteVolume");
    private static readonly int s_octaveOffsetsID = Shader.PropertyToID("octaveOffsets");
    private static readonly int s_heightMapScalingID = Shader.PropertyToID("heightMapScaling");

    private Vector3Int m_numberOfThreads;
    private ComputeBuffer m_octaveOffsetsBuffer;
    private Texture2D m_heightMapScalingTexture;

    private IMeshifier m_meshifier;

    private HermiteVolumeFlags m_flags;

    private void OnEnable()
    {
        m_meshifier = GetComponent<IMeshifier>();
        m_shader.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
        m_numberOfThreads = new Vector3Int((int)x, (int)y, (int)z);
        CreateBuffers();
        CalculateOctaveOffsets();
    }

    private void CreateBuffers()
    {
        m_octaveOffsetsBuffer = new ComputeBuffer(m_octaves, 2 * sizeof(float));
        m_heightMapScalingTexture = new Texture2D(256, 1, TextureFormat.RFloat, false);
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

        ReleaseBuffers();
        CreateBuffers();
        CalculateOctaveOffsets();
        CalculateHeightMapScalingTexture();

        if (m_meshifier != null)
        {
            m_meshifier.OnHermiteVolumeChanged();
        }
    }

    private void CalculateOctaveOffsets()
    {
        System.Random random = new System.Random(m_seed);
        Vector2[] octaveOffsets = new Vector2[m_octaves];

        for (int index = 0; index < m_octaves; index++)
        {
            octaveOffsets[index] = new Vector2(random.Next(-100000, 100000), random.Next(-100000, 100000)) + m_noiseOffset;
        }
        m_octaveOffsetsBuffer.SetData(octaveOffsets);
    }
    private void CalculateHeightMapScalingTexture()
    {
        for (int x = 0; x < m_heightMapScalingTexture.width; x++)
        {
            float value = m_scaling.Evaluate(x / (float)(m_heightMapScalingTexture.width - 1));
            m_heightMapScalingTexture.SetPixel(x, 1, new Color(value, 0.0f, 0.0f));
        }
        m_heightMapScalingTexture.Apply();
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

        m_shader.SetInts(s_hermiteDimensionsID, numberOfHermiteSamplesAlongAxis / voxelStride, numberOfHermiteSamplesAlongAxis / voxelStride, numberOfHermiteSamplesAlongAxis / voxelStride);
        m_shader.SetInts(s_voxelDimensionsID, numberOfVoxelsAlongAxis, numberOfVoxelsAlongAxis, numberOfVoxelsAlongAxis);
        m_shader.SetInt(s_voxelStrideID, voxelStride);
        m_shader.SetFloat(s_voxelSpacingID, voxelSpacing);
        m_shader.SetVector(s_localToWorldOffsetID, localToWorldOffset);
        m_shader.SetFloat(s_noiseScaleID, m_noiseScale);
        m_shader.SetInt(s_octavesID, m_octaves);
        m_shader.SetFloat(s_initialAmplitudeID, m_initialAmplitude);
        m_shader.SetFloat(s_persistenceID, m_persistence);
        m_shader.SetFloat(s_initialFrequencyID, m_initialFrequency);
        m_shader.SetFloat(s_lacunarityID, m_lacunarity);
        m_shader.SetFloat(s_sharpnessID, m_sharpness);
        m_shader.SetFloat(s_centralDifferenceSpacingID, m_centralDifferenceSpacing);

        m_shader.SetBuffer(0, s_hermiteVolumeID, hermiteVolumeBuffer);
        m_shader.SetBuffer(0, s_octaveOffsetsID, m_octaveOffsetsBuffer);
        m_shader.SetTexture(0, s_heightMapScalingID, m_heightMapScalingTexture);
        m_shader.Dispatch
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