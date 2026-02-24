using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Pooling
{
	public class ComponentPool<T> where T : Component
	{
		private readonly Queue<T> pool = new Queue<T>(64);
		private readonly T prefab;
		private readonly Transform root;

		public ComponentPool(T prefab, Transform root, int prewarmCount)
		{
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
				Despawn(item);
			}
		}

		public T Spawn(Transform parent, bool worldPositionStays = false)
		{
			T item = pool.Count > 0 ? pool.Dequeue() : CreateNew();

			item.transform.SetParent(parent, worldPositionStays);
			item.gameObject.SetActive(true);

			if (item.TryGetComponent<IPoolable>(out var poolable))
				poolable.OnSpawned();

			return item;
		}

		public void Despawn(T item)
		{
			if (item == null) return;

			if (item.TryGetComponent<IPoolable>(out var poolable))
				poolable.OnDespawned();

			item.transform.SetParent(root, false);
			item.gameObject.SetActive(false);

			pool.Enqueue(item);
		}

		private T CreateNew()
		{
			var item = Object.Instantiate(prefab, root);
			item.gameObject.SetActive(false);
			return item;
		}
	}
}