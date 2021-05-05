using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace World
{
    public class LodGrid
    {
        public float3 Dimensions => 0.0f;

        private float m_resolution;
        private float m_numberOfLods;

        public LodGrid(float resolution, int numberOfLods)
        {
            if (resolution <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Resolution has to be greater than 0!");
            }

            if (numberOfLods < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfLods), numberOfLods, "Number of level of details has to be non-negative!");
            }

            m_resolution = resolution;
            m_numberOfLods = numberOfLods;
        }

        public IEnumerable<Bounds> Update(float3 viewerPosition)
        {
            float3 viewerCoordinate = math.round(viewerPosition / (2.0f * m_resolution));

            if (m_numberOfLods == 0)
            {
                yield return new Bounds(m_resolution * viewerCoordinate, m_resolution * new float3(1.0f, 1.0f, 1.0f));
            }
        }
    }
}