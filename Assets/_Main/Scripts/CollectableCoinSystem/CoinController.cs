using System.Collections;
using _Main.Scripts.Pooling;
using BaseSystems.CurrencySystem.Scripts;
using DG.Tweening;
using UnityEngine;

namespace _Main.Scripts.CollectableCoinSystem
{
	public class CoinController : MonoBehaviour, IPoolable
	{
		private WaitForSeconds waitForSecond = new WaitForSeconds(0.7f);

		public void Collect()
		{
			StartCoroutine(CollectSequence());
		}

		private IEnumerator CollectSequence()
		{
			transform.DOScale(Vector3.one + Vector3.up * 1.4f, 0.3f).OnComplete((() =>
			{
				transform.DOScale(0, 0.4f);
			}));

			yield return waitForSecond;
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