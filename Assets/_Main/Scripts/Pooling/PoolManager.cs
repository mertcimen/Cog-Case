using _Main.Scripts.BallSystem;
using _Main.Scripts.CollectableCoinSystem;
using _Main.Scripts.Datas;
using _Main.Scripts.GridSystem;
using _Main.Scripts.WallSystem;
using UnityEngine;

namespace _Main.Scripts.Pooling
{
	[DefaultExecutionOrder(-10)]
	public class PoolManager : MonoBehaviour
	{
		public static PoolManager Instance { get; private set; }

		[Header("Prefabs (optional, can fallback to ReferenceManagerSO)")]
		[SerializeField] private GridCell gridCellPrefab;
		[SerializeField] private BallController ballPrefab;
		[SerializeField] private WallController wallPrefab;
		[SerializeField] private CoinController coinPrefab;

		[Header("Prewarm")]
		[SerializeField] private int prewarmCells = 150;
		[SerializeField] private int prewarmBalls = 20;
		[SerializeField] private int prewarmWalls = 50;
		[SerializeField] private int prewarmCoins = 50;

		private Transform cellsRoot;
		private Transform ballsRoot;
		private Transform wallsRoot;
		private Transform coinsRoot;

		private ComponentPool<GridCell> cellPool;
		private ComponentPool<BallController> ballPool;
		private ComponentPool<WallController> wallPool;
		private ComponentPool<CoinController> coinPool;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;

			// Rootlar
			cellsRoot = CreateRoot("CellsPool");
			ballsRoot = CreateRoot("BallsPool");
			wallsRoot = CreateRoot("WallsPool");
			coinsRoot = CreateRoot("CoinsPool");

			ResolvePrefabs();

			cellPool = new ComponentPool<GridCell>(gridCellPrefab, cellsRoot, prewarmCells);
			ballPool = new ComponentPool<BallController>(ballPrefab, ballsRoot, prewarmBalls);
			wallPool = new ComponentPool<WallController>(wallPrefab, wallsRoot, prewarmWalls);
			coinPool = new ComponentPool<CoinController>(coinPrefab, coinsRoot, prewarmCoins);
		}

		private void ResolvePrefabs()
		{
			// Cell prefab mutlaka inspector’dan gelsin (genelde level sistemine özel olur)
			// ama ball/wall/coin için ReferenceManagerSO fallback yapalım
			if (ballPrefab == null && ReferenceManagerSO.Instance != null)
				ballPrefab = ReferenceManagerSO.Instance.BallPrefab;

			if (wallPrefab == null && ReferenceManagerSO.Instance != null)
				wallPrefab = ReferenceManagerSO.Instance.WallPrefab;
		}

		private Transform CreateRoot(string name)
		{
			var go = new GameObject(name);
			go.transform.SetParent(transform, false);
			return go.transform;
		}

		// -----------------------
		// SPAWN / DESPAWN API
		// -----------------------

		public GridCell SpawnCell(Transform parent) => cellPool.Spawn(parent, false);
		public void DespawnCell(GridCell cell) => cellPool.Despawn(cell);

		public BallController SpawnBall(Transform parent) => ballPool.Spawn(parent, false);
		public void DespawnBall(BallController ball) => ballPool.Despawn(ball);

		public WallController SpawnWall(Transform parent) => wallPool.Spawn(parent, false);
		public void DespawnWall(WallController wall) => wallPool.Despawn(wall);

		public CoinController SpawnCoin(Transform parent) => coinPool.Spawn(parent, false);
		public void DespawnCoin(CoinController coin) => coinPool.Despawn(coin);
	}
}
