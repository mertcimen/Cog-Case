using _Main.Scripts.GridSystem;
using _Main.Scripts.LevelEditor;
using UnityEngine;

namespace BaseSystems.Scripts.LevelSystem
{
	public class Level : MonoBehaviour
	{
		[SerializeField] private GridManager gridManager;

		private GridLevelAsset currentAsset;

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