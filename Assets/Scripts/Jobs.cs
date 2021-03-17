using Unity.Jobs;
using UnityEngine;

public struct BakeJob : IJob
{
    public int m_meshID;
    public bool m_convex;

    public void Execute()
    {
        Physics.BakeMesh(m_meshID, m_convex);
    }
}