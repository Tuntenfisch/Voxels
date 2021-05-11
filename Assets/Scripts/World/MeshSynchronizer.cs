namespace World
{
    internal class MeshSynchronizer
    {
        public bool Synced => m_numberOfMeshesReady == m_numberOfMeshRequests;

        private int m_numberOfMeshRequests;
        private int m_numberOfMeshesReady;

        public void MeshReady() => m_numberOfMeshesReady++;

        public void IncrementNumberOfMeshRequests() => m_numberOfMeshRequests++;
    }
}