using UnityEngine;

namespace Generics
{
    public abstract class SingletonComponent<T> : SingletonComponent where T : MonoBehaviour
    {
        public static T Instance
        {
            get
            {
                return s_instance;
            }
        }

        private static T s_instance = default;

        protected override void Setup()
        {
            if (s_instance != this)
            {
                s_instance = this as T;
            }
        }

        protected override void Clear()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }

    public abstract class SingletonComponent : MonoBehaviour
    {
        protected abstract void Setup();

        protected abstract void Clear();

        protected virtual void Awake()
        {
            Setup();
        }

        protected virtual void OnDestroy()
        {
            Clear();
        }
    }
}
