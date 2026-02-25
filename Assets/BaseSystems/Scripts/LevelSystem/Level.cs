using _Main.Scripts.GridSystem;
using _Main.Scripts.LevelEditor;
using UnityEngine;

namespace BaseSystems.Scripts.LevelSystem
{
	public class Level : MonoBehaviour
	{
		[SerializeField] private GridManager gridManager;

		private GridLevelAsset currentAsset;

		[SerializeField] private Transform topBorder;
		[SerializeField] private Transform downBorder;
		[SerializeField] private Transform leftBorder;
		[SerializeField] private Transform rightBorder;
		
		
		public void Load(GridLevelAsset asset)
		{
			currentAsset = asset;
			gameObject.SetActive(true);

			if (gridManager == null)
			{
				Debug.LogError("Level: GridManager missing!");
				return;
			}

			gridManager.Initialize(this, currentAsset);
			gridManager.PositionBorders(topBorder, downBorder, leftBorder, rightBorder, 0.5f);
		}

		public virtual void Play()
		{
		}

		public virtual void Unload()
		{
			// GridManager zaten OnDestroy ile despawn ediyor
			// ama destroy öncesi temiz istersen direkt çağırabilirsin:
			if (gridManager != null)
				gridManager.DespawnAllToPool();
		}
	}
}