using UnityEngine;

public static class DensityFunctions
{
    public static float Sphere(Vector3 position, float radius)
    {
        return radius - position.magnitude;
    }

    public static Vector3 SphereGradient(Vector3 position)
    {
        return position.normalized;
    }

    public static float Noise(Vector3 position)
    {
        return Mathf.PerlinNoise(position.x, position.z) - position.y;
    }

    public static Vector3 NoiseGradient(Vector3 position)
    {
        Vector3 gradient;

        gradient.x = Noise(position + 0.1f * Vector3.left) - Noise(position + 0.1f * Vector3.right);
        gradient.y = Noise(position + 0.1f * Vector3.down) - Noise(position + 0.1f * Vector3.up);
        gradient.z = Noise(position + 0.1f * Vector3.back) - Noise(position + 0.1f * Vector3.forward);

        return gradient;
    }
}
