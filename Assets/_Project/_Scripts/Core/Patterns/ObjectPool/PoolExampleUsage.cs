using System;
using UnityEngine;

namespace GameBase
{
    
    public class PoolExampleUsage : MonoBehaviour
    {
        public PoolThings prefab;
        private ObjectPool<PoolThings> pool;

        private void Start()
        {
            pool = new ObjectPool<PoolThings>(prefab, 10);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var things = pool.GetFromPool();
                things.transform.position = transform.position;
            }
        }
    }
    
    public class PoolThings : MonoBehaviour,IPoolable
    {
        public void OnRecycledPool()
        {
            //重置位置，速度，状态等等
        }
    }
}