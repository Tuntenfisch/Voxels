namespace Generics.Pool
{
    public interface IPoolable
    {
        internal void OnAcquire();

        internal void OnRelease();
    }
}