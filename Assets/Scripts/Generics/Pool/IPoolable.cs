namespace Tuntenfisch.Generics.Pool
{
    public interface IPoolable
    {
        public void OnAcquire();

        public void OnRelease();
    }
}