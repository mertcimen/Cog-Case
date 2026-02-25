using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Pooling
{
	public interface IComponentPool
	{
		int PoolId { get; }
		System.Type ElementType { get; }
		Transform Root { get; }
		Component Spawn(Transform parent, bool worldPositionStays);
		void Despawn(Component item);
		void Prewarm(int count);
	}

	public sealed class ComponentPool<T> : IComponentPool where T : Component
	{
		private readonly Queue<T> pool = new Queue<T>(64);
		private readonly T prefab;
		private readonly Transform root;
		private readonly PoolManager owner;
		private readonly int poolId;

		public int PoolId => poolId;
		public System.Type ElementType => typeof(T);
		public Transform Root => root;

		public ComponentPool(PoolManager owner, int poolId, T prefab, Transform root, int prewarmCount)
		{
			this.owner = owner;
			this.poolId = poolId;
			this.prefab = prefab;
			this.root = root;

			if (prewarmCount > 0)
				Prewarm(prewarmCount);
		}

		public void Prewarm(int count)
		{
			for (int i = 0; i < count; i++)
			{
				var item = CreateNew();
				DespawnTyped(item); 
			}
		}

		public T SpawnTyped(Transform parent, bool worldPositionStays = false)
		{
			T item = pool.Count > 0 ? pool.Dequeue() : CreateNew();

			item.transform.SetParent(parent, worldPositionStays);
			item.gameObject.SetActive(true);

			if (item.TryGetComponent<IPoolable>(out var poolable))
				poolable.OnSpawned();

			return item;
		}

		public void DespawnTyped(T item)
		{
			if (item == null) return;

			if (item.TryGetComponent<IPoolable>(out var poolable))
				poolable.OnDespawned();

			item.transform.SetParent(root, false);
			item.gameObject.SetActive(false);

			pool.Enqueue(item);
		}

		Component IComponentPool.Spawn(Transform parent, bool worldPositionStays) =>
			SpawnTyped(parent, worldPositionStays);

		void IComponentPool.Despawn(Component item)
		{
			if (item is T typed)
				DespawnTyped(typed);
		}

		private T CreateNew()
		{
			var item = Object.Instantiate(prefab, root);
			item.gameObject.SetActive(false);

			// Marking  instance 
			if (!item.TryGetComponent<PooledObject>(out var tag))
				tag = item.gameObject.AddComponent<PooledObject>();

			tag.Setup(owner, poolId);

			return item;
		}
	}
}