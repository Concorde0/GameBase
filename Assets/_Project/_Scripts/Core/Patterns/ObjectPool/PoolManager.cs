using System.Collections.Generic;
using UnityEngine;

namespace GameBase
{
    public class PoolManager : MonoBehaviour
    {
        private static readonly Dictionary<Component, object> pools = new();

        public static ObjectPool<T> GetPool<T>(T prefab, int warmUp = 0) where T : Component
        {
            if (!pools.TryGetValue(prefab, out var poolObj))
            {
                var pool = new ObjectPool<T>(prefab, warmUp);
                pools[prefab] = pool;
                return pool;
                
            }
            return poolObj as ObjectPool<T>;
        }
    }
}