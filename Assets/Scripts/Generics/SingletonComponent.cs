using UnityEngine;
using UnityEngine.Assertions;

namespace Generics
{
    public abstract class SingletonComponent<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindObjectOfType<T>();
                    Assert.IsNotNull(s_instance, $"Scene is missing an instance of {typeof(T)}!");
                }

                return s_instance;
            }
        }

        private static T s_instance = default;
    }
}