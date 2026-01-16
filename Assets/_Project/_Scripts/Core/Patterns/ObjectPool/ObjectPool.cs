using System.Collections.Generic;
using UnityEngine;

namespace DeepDig.Core.Patterns.Pooling
{
    public interface IPoolable
    {
        void OnRecycledPool();
    }

    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> pool;

        public int Count => pool.Count;

        public ObjectPool(T prefab, int initialSize = 0, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            pool = new Queue<T>();

            WarmUpPool(initialSize);
        }
        
        // 预热池子
        public void WarmUpPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = CreateNewObject();
                RecyclePool(obj);
            }
        }
        
        // 从池子取对象
        public T GetFromPool()
        {
            if (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                obj.gameObject.SetActive(true);
                return obj;
            }

            return CreateNewObject();
        }

        // 回收对象
        public void RecyclePool(T obj)
        {
            if (obj is IPoolable resettable)
                resettable.OnRecycledPool();

            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
        
        // 创建新对象
        private T CreateNewObject()
        {
            var obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }
}
