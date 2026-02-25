using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Pooling
{
	[DefaultExecutionOrder(-10)]
	public sealed class PoolManager : MonoBehaviour
	{
		public static PoolManager Instance { get; private set; }

		[Serializable]
		public class PoolDefinition
		{
			[Tooltip("This prefab must be using component for example, Coin,Wall,GridCell...")]
			public Component prefab;

			[Min(0)] public int prewarm = 0;

			[Tooltip("If Root name is empty, using the prefab name.")]
			public string rootName;
		}

		[SerializeField] private List<PoolDefinition> pools = new List<PoolDefinition>();

		// poolId -> pool
		private readonly Dictionary<int, IComponentPool> poolsById = new Dictionary<int, IComponentPool>(32);

		// Type -> poolId (Spawn<T> lookup için)
		private readonly Dictionary<Type, int> poolIdByType = new Dictionary<Type, int>(32);

		private int nextPoolId = 1;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;

			BuildPoolsFromDefinitions();
		}

		private void BuildPoolsFromDefinitions()
		{
			poolsById.Clear();
			poolIdByType.Clear();
			nextPoolId = 1;

			for (int i = 0; i < pools.Count; i++)
			{
				var def = pools[i];
				if (def == null || def.prefab == null)
					continue;

				var type = def.prefab.GetType();
				if (poolIdByType.ContainsKey(type))
				{
					Debug.LogWarning($"PoolManager: Duplicate pool definition for type {type.Name} ignored.");
					continue;
				}

				int poolId = nextPoolId++;
				string rootName = string.IsNullOrWhiteSpace(def.rootName) ? $"{type.Name}Pool" : def.rootName;

				var root = CreateRoot(rootName);

				IComponentPool created = CreateTypedPool(poolId, def.prefab, root, def.prewarm);
				if (created == null)
				{
					Debug.LogError($"PoolManager: Failed to create pool for {type.Name}. Prefab must be a Component.");
					continue;
				}

				poolsById.Add(poolId, created);
				poolIdByType.Add(type, poolId);
			}
		}

		private Transform CreateRoot(string name)
		{
			var go = new GameObject(name);
			go.transform.SetParent(transform, false);
			return go.transform;
		}

		private IComponentPool CreateTypedPool(int poolId, Component prefab, Transform root, int prewarm)
		{
			var type = prefab.GetType();
			var genericPoolType = typeof(ComponentPool<>).MakeGenericType(type);

			try
			{
				// new ComponentPool<T>(PoolManager owner, int poolId, T prefab, Transform root, int prewarmCount)
				return (IComponentPool)Activator.CreateInstance(genericPoolType, this, poolId, prefab, root, prewarm);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return null;
			}
		}

		// -----------------------
		// GENERIC SPAWN / DESPAWN
		// -----------------------

		public T Spawn<T>(Transform parent, bool worldPositionStays = false) where T : Component
		{
			if (!poolIdByType.TryGetValue(typeof(T), out int poolId))
			{
				Debug.LogError($"PoolManager.Spawn<{typeof(T).Name}> failed: No pool definition found. " +
				               $"Add prefab to PoolManager.pools list.");
				return null;
			}

			var pool = poolsById[poolId];
			return pool.Spawn(parent, worldPositionStays) as T;
		}

		public Component Spawn(Type componentType, Transform parent, bool worldPositionStays = false)
		{
			if (componentType == null) return null;

			if (!poolIdByType.TryGetValue(componentType, out int poolId))
			{
				Debug.LogError($"PoolManager.Spawn({componentType.Name}) failed: No pool definition found.");
				return null;
			}

			return poolsById[poolId].Spawn(parent, worldPositionStays);
		}

		public bool Despawn(Component instance)
		{
			if (instance == null) return false;

			if (!instance.TryGetComponent<PooledObject>(out var tag) || tag.Owner != this)
			{
				Debug.LogWarning($"PoolManager.Despawn ignored: {instance.name} is not a pooled instance of this PoolManager.");
				return false;
			}

			if (!poolsById.TryGetValue(tag.PoolId, out var pool))
			{
				Debug.LogWarning($"PoolManager.Despawn ignored: PoolId {tag.PoolId} not found for {instance.name}.");
				return false;
			}

			pool.Despawn(instance);
			return true;
		}

	
		public int DespawnAllUnder(Transform parent)
		{
			if (parent == null) return 0;

			
			var pooled = parent.GetComponentsInChildren<PooledObject>(includeInactive: true);

			int count = 0;
			for (int i = 0; i < pooled.Length; i++)
			{
				var p = pooled[i];
				if (p == null) continue;
				if (p.Owner != this) continue;

				

				if (!poolsById.TryGetValue(p.PoolId, out var pool)) continue;

				var comp = p.GetComponent(pool.ElementType);
				if (comp == null) continue;

				if (Despawn(comp))
					count++;
			}

			return count;
		}

		// -----------------------
		// (OPSİYONEL) Backward-compatible wrapper'lar
		// -----------------------

		public _Main.Scripts.GridSystem.GridCell SpawnCell(Transform parent) => Spawn<_Main.Scripts.GridSystem.GridCell>(parent);
		public void DespawnCell(_Main.Scripts.GridSystem.GridCell cell) => Despawn(cell);

		public _Main.Scripts.BallSystem.BallController SpawnBall(Transform parent) => Spawn<_Main.Scripts.BallSystem.BallController>(parent);
		public void DespawnBall(_Main.Scripts.BallSystem.BallController ball) => Despawn(ball);

		public _Main.Scripts.WallSystem.WallController SpawnWall(Transform parent) => Spawn<_Main.Scripts.WallSystem.WallController>(parent);
		public void DespawnWall(_Main.Scripts.WallSystem.WallController wall) => Despawn(wall);

		public _Main.Scripts.CollectableCoinSystem.CoinController SpawnCoin(Transform parent) => Spawn<_Main.Scripts.CollectableCoinSystem.CoinController>(parent);
		public void DespawnCoin(_Main.Scripts.CollectableCoinSystem.CoinController coin) => Despawn(coin);
	}
}
