using _Main.Scripts.GridSystem;
using _Main.Scripts.LevelEditor;
using UnityEngine;

namespace BaseSystems.Scripts.LevelSystem
{
	public class Level : MonoBehaviour
	{
		[SerializeField] private GridManager gridManager;

		[SerializeField] private GridLevelAsset gridLevelAsset;

		public virtual void Load()
		{
			gameObject.SetActive(true);

			gridManager.Initialize(this, gridLevelAsset);
		}

		public virtual void Play()
		{
		}
	}
}