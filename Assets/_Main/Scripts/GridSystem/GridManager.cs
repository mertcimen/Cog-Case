using System.Collections.Generic;
using _Main.Scripts.LevelEditor;
using BaseSystems.Scripts.LevelSystem;
using UnityEngine;

namespace _Main.Scripts.GridSystem
{
	public class GridManager : MonoBehaviour
	{
		[SerializeField] private GridCell cellPrefab;

		[Header("Layout")]
		[SerializeField] private float cellSize = 1f; // hücre merkezleri arası temel mesafe
		[SerializeField] private float cellGap = 0f; // ekstra boşluk

		private Level currentLevel;
		private Transform gridRoot;

		private readonly Dictionary<Vector2Int, GridCell> cellsByCoord = new Dictionary<Vector2Int, GridCell>();

		public void Initialize(Level level, GridLevelAsset levelData)
		{
			currentLevel = level;

			if (levelData == null || levelData.grid == null)
			{
				Debug.LogError("GridManager.Initialize failed: levelData/grid is null");
				return;
			}

			var gridData = levelData.grid;
			gridData.EnsureGridStorage();

			EnsureRoot();
			ClearSpawned();

			float spacing = cellSize + cellGap;
			
			float xCenterOffset = (gridData.gridSize.x - 1) * 0.5f;
			float yCenterOffset = (gridData.gridSize.y - 1) * 0.5f;

			for (int i = 0; i < gridData.cells.Count; i++)
			{
				var cell = gridData.cells[i];

				// güvenlik: grid dışı hücreleri geç
				if (!gridData.IsInside(cell.coord))
					continue;

				float x = (cell.coord.x - xCenterOffset) * spacing;
				float z = (cell.coord.y - yCenterOffset) * spacing;

				Vector3 localPos = new Vector3(x, 0f, z);

				GridCell spawned = Instantiate(cellPrefab, gridRoot);
				spawned.transform.localPosition = localPos;
				spawned.transform.localRotation = Quaternion.identity;
				spawned.transform.localScale = Vector3.one;

				// GridCell’in içinde coordinate/wall gibi init fonksiyonların varsa burada setle:
				// spawned.Initialize(cell.coord, cell.isWall, cell.hasBall);
				spawned.Initialize(cell); // <-- cell, CellData struct’ı
				

				cellsByCoord[cell.coord] = spawned;
			}
		}

		private void EnsureRoot()
		{
			if (gridRoot != null) return;

			var existing = transform.Find("GridRoot");
			if (existing != null)
			{
				gridRoot = existing;
				gridRoot.localPosition = Vector3.zero;
				gridRoot.localRotation = Quaternion.identity;
				gridRoot.localScale = Vector3.one;
				return;
			}

			var go = new GameObject("GridRoot");
			gridRoot = go.transform;
			gridRoot.SetParent(transform, false);
			gridRoot.localPosition = Vector3.zero;
			gridRoot.localRotation = Quaternion.identity;
			gridRoot.localScale = Vector3.one;
		}

		private void ClearSpawned()
		{
			// cellsByCoord.Clear();
			//
			// if (gridRoot == null) return;
			//
			// for (int i = gridRoot.childCount - 1; i >= 0; i--)
			// {
			// 	Destroy(gridRoot.GetChild(i).gameObject);
			// }
		}
	}
}