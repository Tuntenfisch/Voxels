using UnityEngine;

[RequireComponent(typeof(Chunk))]
public class DensityVolume : MonoBehaviour
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

    private ComputeBuffer m_octaveOffsetsBuffer;
    private Texture2D m_heightMapScalingTexture;
    private IMeshifier m_meshifier;
    private DensityVolumeManipulatorFlags m_flags;

    private void Awake()
    {
        m_meshifier = GetComponent<IMeshifier>();

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
    }

    private void Update()
    {
        if (m_flags.HasFlag(DensityVolumeManipulatorFlags.SettingsUpdated))
        {
            OnSettingsUpdated();
        }
    }

    private void OnSettingsUpdated()
    {
        m_flags &= ~DensityVolumeManipulatorFlags.SettingsUpdated;

        ReleaseBuffers();
        CreateBuffers();
        CalculateOctaveOffsets();
        CalculateHeightMapScalingTexture();

        if (m_meshifier != null)
        {
            m_meshifier.OnDensitiesChanged();
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
        ComputeBuffer densityVolumeBuffer,
        ComputeBuffer densityGradientBuffer,
        int numberOfDensitySamplesAlongAxis,
        int numberOfVoxelsAlongAxis,
        float voxelSpacing,
        Vector3 offset
    )
    {
        int stride = 1;

        m_shader.SetInts("densityDimensions", numberOfDensitySamplesAlongAxis, numberOfDensitySamplesAlongAxis, numberOfDensitySamplesAlongAxis);
        m_shader.SetInts("voxelDimensions", numberOfVoxelsAlongAxis, numberOfVoxelsAlongAxis, numberOfVoxelsAlongAxis);
        m_shader.SetInt("stride", stride);
        m_shader.SetFloat("voxelSpacing", voxelSpacing);
        m_shader.SetVector("offset", offset);
        m_shader.SetFloat("noiseScale", m_noiseScale);
        m_shader.SetInt("octaves", m_octaves);
        m_shader.SetFloat("initialAmplitude", m_initialAmplitude);
        m_shader.SetFloat("persistence", m_persistence);
        m_shader.SetFloat("initialFrequency", m_initialFrequency);
        m_shader.SetFloat("lacunarity", m_lacunarity);
        m_shader.SetFloat("sharpness", m_sharpness);
        m_shader.SetFloat("centralDifferenceSpacing", m_centralDifferenceSpacing);

        int numberOfThreads = Mathf.CeilToInt((numberOfDensitySamplesAlongAxis / stride) / (float)ThreadCount.c_threadCountDensityVolumeManipulator);

        m_shader.SetBuffer(0, "densityVolume", densityVolumeBuffer);
        m_shader.SetBuffer(0, "densityGradient", densityGradientBuffer);
        m_shader.SetBuffer(0, "octaveOffsets", m_octaveOffsetsBuffer);
        m_shader.SetTexture(0, "heightMapScaling", m_heightMapScalingTexture);
        m_shader.Dispatch(0, numberOfThreads, numberOfThreads, numberOfThreads);
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void OnValidate()
    {
        m_flags |= DensityVolumeManipulatorFlags.SettingsUpdated;
    }

    private enum DensityVolumeManipulatorFlags
    {
        SettingsUpdated = 1
    }
}