using _Main.Scripts.BallSystem;
using _Main.Scripts.CollectableCoinSystem;
using _Main.Scripts.LevelEditor;
using _Main.Scripts.Pooling;
using _Main.Scripts.WallSystem;
using BaseSystems.CurrencySystem.Scripts;
using DG.Tweening;
using UnityEngine;

namespace _Main.Scripts.GridSystem
{
	public class GridCell : MonoBehaviour, IPoolable
	{
		public Vector2Int Coordinate { get; private set; }
		public bool IsWall { get; private set; }
		public bool HasBall => currentBall != null;
		public bool HasCoin => currentCoin != null;

		public BallController CurrentBall => currentBall;

		[SerializeField] private SpriteRenderer paintSprite;

		private GridManager gridManager;

		private BallController currentBall;
		private WallController currentWall;
		private CoinController currentCoin;

		private bool isPainted;

		public void Initialize(CellData cellData, GridManager owner)
		{
			gridManager = owner;

			Coordinate = cellData.coord;
			IsWall = cellData.isWall;

			isPainted = false;
			if (paintSprite != null)
				paintSprite.enabled = false;

			// Önce temizle (pool reuse)
			ClearContentToPool();

			// İçerik spawn kuralları:
			// Wall varsa ball/coin yok
			if (IsWall)
			{
				SpawnWall();
				return;
			}

			if (cellData.hasBall)
				SpawnBall();

			if (cellData.hasCoin && currentBall == null) 
				SpawnCoin();
		}

		private void SpawnBall()
		{
			currentBall = PoolManager.Instance.SpawnBall(transform);
			currentBall.transform.localPosition = Vector3.zero;
			currentBall.transform.localRotation = Quaternion.identity;

			currentBall.Initialize(this);
		}

		private void SpawnWall()
		{
			currentWall = PoolManager.Instance.SpawnWall(transform);
			currentWall.transform.localPosition = Vector3.zero;
			currentWall.transform.localRotation = Quaternion.identity;
		}

		private void SpawnCoin()
		{
			currentCoin = PoolManager.Instance.SpawnCoin(transform);
			currentCoin.transform.localPosition = Vector3.zero;
			currentCoin.transform.localRotation = Quaternion.identity;
		}

		public void Paint()
		{
			if (IsWall) return;
			if (isPainted) return;

			isPainted = true;

			if (paintSprite != null)
			{
				paintSprite.enabled = true;
				paintSprite.DOColor(Color.red, 0.4f);
			}

			gridManager?.IncreaseCurrentPaintedCount();
		}

		public bool TryCollectCoin(Vector3 collectWorldPos)
		{
			if (currentCoin == null)
				return false;

			// Currency +1
			currentCoin.Collect();
			currentCoin = null;

			return true;
		}

		public void ClearBallReference()
		{
			currentBall = null;
		}

		public void SetBallReference(BallController ball)
		{
			currentBall = ball;
		}

		// GridCell pool reset
		public void OnSpawned()
		{
			//
		}

		public void OnDespawned()
		{
			// reset for returning pool
			gridManager = null;
			isPainted = false;

			if (paintSprite != null)
				paintSprite.enabled = false;

			ClearContentToPool();
		}

		private void ClearContentToPool()
		{
			// Ball
			if (currentBall != null)
			{
				PoolManager.Instance.DespawnBall(currentBall);
				currentBall = null;
			}

			// Wall
			if (currentWall != null)
			{
				PoolManager.Instance.DespawnWall(currentWall);
				currentWall = null;
			}

			// Coin
			if (currentCoin != null)
			{
				PoolManager.Instance.DespawnCoin(currentCoin);
				currentCoin = null;
			}
		}
	}
}
