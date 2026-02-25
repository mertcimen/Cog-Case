using System;
using System.Collections.Generic;
using BaseSystems.Scripts.Managers;
using Fiber.Utilities;
using TriInspector;
using UnityEngine;

namespace BaseSystems.Scripts.Utilities
{
    public class ParticlePooler : Singleton<ParticlePooler>
    {
        [Serializable]
        public class Pool
        {
            [Tooltip("Particle Type of this pool")]
            public ParticleType Type;

            [Tooltip("Prefab of the Particle to be pooled")]
            public GameObject Prefab;

            [Tooltip("The size (count) of the pool")]
            public int Size;

            [Tooltip("Whether the Particle deactivates itself after finished playing")]
            public bool AutoDeactivate;
        }

        [TableList]
        [SerializeField] private List<Pool> pools = new();

        private readonly Dictionary<ParticleType, Queue<ParticleSystem>> poolDictionary
            = new();

        private void Awake()
        {
            InitPool();
        }

        private void InitPool()
        {
            foreach (var pool in pools)
                AddToPool(pool.Type, pool.Prefab, pool.Size, pool.AutoDeactivate);
        }

        private void OnEnable()
        {
            LevelManager.OnLevelUnload += OnLevelUnload;
        }

        private void OnDisable()
        {
            LevelManager.OnLevelUnload -= OnLevelUnload;
        }

        private void OnLevelUnload() => DisableAllPooledObjects();

        private void DisableAllPooledObjects()
        {
            foreach (var pool in poolDictionary.Values)
            {
                foreach (var particle in pool)
                {
                    particle.Stop();
                    particle.transform.SetParent(transform);
                    particle.gameObject.SetActive(false);
                }
            }
        }

        #region Spawn Overloads

        public ParticleSystem Spawn(ParticleType type, Vector3 position)
        {
            var particle = SpawnFromPool(type);
            if (particle == null) return null;

            particle.transform.position = position;
            return particle;
        }

        public ParticleSystem Spawn(ParticleType type, Vector3 position, Quaternion rotation)
        {
            var particle = SpawnFromPool(type);
            if (particle == null) return null;

            particle.transform.SetPositionAndRotation(position, rotation);
            return particle;
        }

        public ParticleSystem Spawn(ParticleType type, Transform parent, bool keepWorldRotation = false)
        {
            var particle = SpawnFromPool(type);
            if (particle == null) return null;

            var t = particle.transform;
            t.SetParent(parent);
            t.localPosition = Vector3.zero;

            if (!keepWorldRotation)
                t.forward = parent.forward;

            return particle;
        }

        public ParticleSystem Spawn(ParticleType type, Vector3 position, Transform parent)
        {
            var particle = SpawnFromPool(type);
            if (particle == null) return null;

            var t = particle.transform;
            t.position = position;
            t.SetParent(parent);
            return particle;
        }

        #endregion

        private ParticleSystem SpawnFromPool(ParticleType type)
        {
            if (!poolDictionary.TryGetValue(type, out var queue))
            {
                Debug.LogError($"{type} pool does not exist!");
                return null;
            }

            var particle = queue.Dequeue();
            particle.gameObject.SetActive(true);
            particle.Play();

            queue.Enqueue(particle);

            return particle;
        }

        public void AddToPool(ParticleType type, GameObject prefab, int count, bool deactivate = true)
        {
            if (poolDictionary.ContainsKey(type))
            {
                Debug.LogWarning($"{type} pool already exists! Skipped.");
                return;
            }

            var queue = new Queue<ParticleSystem>();

            for (int i = 0; i < count; i++)
            {
                var particle = Instantiate(prefab, transform).GetComponent<ParticleSystem>();

                if (deactivate)
                {
                    var main = particle.main;
                    main.stopAction = ParticleSystemStopAction.Disable;
                }

                particle.gameObject.SetActive(false);
                queue.Enqueue(particle);
            }

            poolDictionary.Add(type, queue);
        }
    }
    
    public enum ParticleType
    {
        None = 0,
        Smoke = 1,
       
    }

}
