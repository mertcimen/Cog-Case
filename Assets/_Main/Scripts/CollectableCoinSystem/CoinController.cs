using _Main.Scripts.Pooling;
using BaseSystems.CurrencySystem.Scripts;
using UnityEngine;

namespace _Main.Scripts.CollectableCoinSystem
{
	public class CoinController : MonoBehaviour, IPoolable
	{
		public void Collect()
		{
			var screenPos = Camera.main.WorldToScreenPoint(transform.position);
			CurrencyManager.Money.AddCurrency(1, screenPos, false);

			PoolManager.Instance.DespawnCoin(this);
		}

		public void OnSpawned()
		{
			transform.localScale = Vector3.one;
		}

		public void OnDespawned()
		{
			transform.localScale = Vector3.one;
		}
	}
}