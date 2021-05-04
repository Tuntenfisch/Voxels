using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace Generics
{
    [BurstCompile]
    public struct BakeJob : IJob
    {
        private readonly int m_meshID;
        private readonly bool m_convex;

        public BakeJob(int meshID, bool convex = false)
        {
            m_meshID = meshID;
            m_convex = convex;
        }

        public void Execute()
        {
            Physics.BakeMesh(m_meshID, m_convex);
        }
    }
}