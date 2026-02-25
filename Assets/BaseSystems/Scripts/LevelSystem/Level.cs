using _Main.Scripts.GridSystem;
using _Main.Scripts.LevelEditor;
using _Main.Scripts.Pooling;
using BaseSystems.Scripts.Utilities;
using UnityEngine;

namespace BaseSystems.Scripts.LevelSystem
{
	public class Level : MonoBehaviour
	{
		[SerializeField] private GridManager gridManager;

		private LevelDataSO currentDataSO;

		[SerializeField] private Transform topBorder;
		[SerializeField] private Transform downBorder;
		[SerializeField] private Transform leftBorder;
		[SerializeField] private Transform rightBorder;

		public void Load(LevelDataSO dataSO)
		{
			currentDataSO = dataSO;
			gameObject.SetActive(true);

			if (gridManager == null)
			{
				Debug.LogError("Level: GridManager missing!");
				return;
			}

			gridManager.Initialize(this, currentDataSO);
			gridManager.PositionBorders(topBorder, downBorder, leftBorder, rightBorder, 0.5f);
			// Camera frame 
			if (CameraController.Instance != null)
			{
				CameraController.Instance.FrameGrid(gridManager.GridWidth, gridManager.GridHeight, gridManager.CellSize,
					paddingWorld: 0.5f, edgePaddingPercent: 0.08f);
			}
		}

		public virtual void Play()
		{
		}

		public virtual void Unload()
		{
			if (gridManager != null)
				gridManager.DespawnAllToPool();

			if (PoolManager.Instance != null)
				PoolManager.Instance.DespawnAllUnder(transform);

			gameObject.SetActive(false);
		}

	}
}