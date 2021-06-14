using System;
using UnityEngine;

namespace Tuntenfisch.Generics
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

                    if (s_instance == null)
                    {
                        throw new ArgumentNullException(nameof(s_instance));
                    }
                }

                return s_instance;
            }
        }

        private static T s_instance = default;
    }
}