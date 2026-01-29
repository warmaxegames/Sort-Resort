using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> available = new Queue<T>();
        private readonly HashSet<T> inUse = new HashSet<T>();
        private readonly int maxSize;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;

        public int AvailableCount => available.Count;
        public int InUseCount => inUse.Count;
        public int TotalCount => available.Count + inUse.Count;

        public ObjectPool(T prefab, Transform parent = null, int initialSize = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = maxSize;
            this.onGet = onGet;
            this.onRelease = onRelease;

            Prewarm(initialSize);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count && TotalCount < maxSize; i++)
            {
                var instance = CreateInstance();
                instance.gameObject.SetActive(false);
                available.Enqueue(instance);
            }
        }

        public T Get()
        {
            T instance;

            if (available.Count > 0)
            {
                instance = available.Dequeue();
            }
            else if (TotalCount < maxSize)
            {
                instance = CreateInstance();
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool exhausted for {typeof(T).Name}");
                return null;
            }

            instance.gameObject.SetActive(true);
            inUse.Add(instance);
            onGet?.Invoke(instance);

            return instance;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            var instance = Get();
            if (instance != null)
            {
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null) return;

            if (!inUse.Contains(instance))
            {
                Debug.LogWarning($"[ObjectPool] Trying to release object not from this pool");
                return;
            }

            inUse.Remove(instance);
            onRelease?.Invoke(instance);
            instance.gameObject.SetActive(false);
            available.Enqueue(instance);
        }

        public void ReleaseAll()
        {
            var toRelease = new List<T>(inUse);
            foreach (var instance in toRelease)
            {
                Release(instance);
            }
        }

        public void Clear()
        {
            foreach (var instance in available)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
            }
            available.Clear();

            foreach (var instance in inUse)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
            }
            inUse.Clear();
        }

        private T CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            return instance;
        }
    }

    // Non-generic version for easier inspector use
    public class GameObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 10;
        [SerializeField] private int maxSize = 100;

        private Queue<GameObject> available = new Queue<GameObject>();
        private HashSet<GameObject> inUse = new HashSet<GameObject>();

        public int AvailableCount => available.Count;
        public int InUseCount => inUse.Count;

        private void Awake()
        {
            if (prefab != null)
            {
                Prewarm(initialSize);
            }
        }

        public void Initialize(GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            this.prefab = prefab;
            this.initialSize = initialSize;
            this.maxSize = maxSize;
            Prewarm(initialSize);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count && (available.Count + inUse.Count) < maxSize; i++)
            {
                var instance = Instantiate(prefab, transform);
                instance.SetActive(false);
                available.Enqueue(instance);
            }
        }

        public GameObject Get()
        {
            GameObject instance;

            if (available.Count > 0)
            {
                instance = available.Dequeue();
            }
            else if ((available.Count + inUse.Count) < maxSize)
            {
                instance = Instantiate(prefab, transform);
            }
            else
            {
                Debug.LogWarning("[GameObjectPool] Pool exhausted");
                return null;
            }

            instance.SetActive(true);
            inUse.Add(instance);
            return instance;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var instance = Get();
            if (instance != null)
            {
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null || !inUse.Contains(instance)) return;

            inUse.Remove(instance);
            instance.SetActive(false);
            available.Enqueue(instance);
        }

        public void ReleaseAll()
        {
            var toRelease = new List<GameObject>(inUse);
            foreach (var instance in toRelease)
            {
                Release(instance);
            }
        }
    }
}
