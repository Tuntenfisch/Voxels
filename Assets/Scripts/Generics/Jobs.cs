using Unity.Jobs;
using UnityEngine;

namespace Generics
{
    public struct BakeJob : IJob
    {
        private int m_meshID;
        private bool m_convex;

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
