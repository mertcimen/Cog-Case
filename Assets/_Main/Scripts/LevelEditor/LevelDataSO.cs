using System;
using System.Collections.Generic;
using _Main.Scripts.Datas;
using UnityEngine;

namespace _Main.Scripts.LevelEditor
{
	[CreateAssetMenu(menuName = "Level/Grid Level Asset", fileName = "GridLevelAsset")]
	public class LevelDataSO : ScriptableObject
	{
		public int levelTime = 60;
		public ColorType levelPaintColor;
		public GridData grid = new GridData();
	}

	[Serializable]
	public class GridData
	{
		public Vector2Int gridSize = new Vector2Int(10, 10);

		public List<CellData> cells = new List<CellData>();

		[NonSerialized] private Dictionary<Vector2Int, int> _indexByCoord;

		public int CellCount => Mathf.Max(0, gridSize.x * gridSize.y);

		public bool IsInside(Vector2Int c)
		{
			return c.x >= 0 && c.y >= 0 && c.x < gridSize.x && c.y < gridSize.y;
		}

		public IEnumerable<Vector2Int> AllCoordinates()
		{
			for (int y = 0; y < gridSize.y; y++)
				for (int x = 0; x < gridSize.x; x++)
					yield return new Vector2Int(x, y);
		}

		public void EnsureGridStorage()
		{
			gridSize.x = Mathf.Max(1, gridSize.x);
			gridSize.y = Mathf.Max(1, gridSize.y);

			if (cells == null)
				cells = new List<CellData>(CellCount);

			// Mapping Cells by Coordinate
			var old = new Dictionary<Vector2Int, CellData>(cells.Count);
			for (int i = 0; i < cells.Count; i++)
				old[cells[i].coord] = cells[i];

			// Overlap Safe when grid size Changed
			var newCells = new List<CellData>(CellCount);
			foreach (var c in AllCoordinates())
			{
				if (old.TryGetValue(c, out var existing))
				{
					existing.coord = c;
					newCells.Add(existing);
				}
				else
				{
					newCells.Add(new CellData { coord = c, isWall = false, hasBall = false, hasCoin = false });
				}
			}

			cells = newCells;
			RebuildCache();
		}

		public void RebuildCache()
		{
			if (cells == null)
				cells = new List<CellData>();

			_indexByCoord = new Dictionary<Vector2Int, int>(cells.Count);
			for (int i = 0; i < cells.Count; i++)
				_indexByCoord[cells[i].coord] = i;
		}

		private void EnsureCache()
		{
			if (_indexByCoord == null || _indexByCoord.Count != (cells?.Count ?? 0))
				RebuildCache();
		}

		public bool TryGetCell(Vector2Int c, out CellData cell)
		{
			cell = default;
			if (!IsInside(c)) return false;

			EnsureCache();
			if (cells == null) return false;

			if (_indexByCoord.TryGetValue(c, out int idx))
			{
				cell = cells[idx];
				return true;
			}

			return false;
		}

		public CellData GetCell(Vector2Int c)
		{
			if (!TryGetCell(c, out var cell))
				throw new ArgumentOutOfRangeException(nameof(c), $"Coord out of range or missing: {c}");

			return cell;
		}

		public void SetCell(CellData cell)
		{
			if (!IsInside(cell.coord))
				return;

			EnsureCache();
			if (_indexByCoord.TryGetValue(cell.coord, out int idx))
			{
				cells[idx] = cell;
			}
			else
			{
				cells.Add(cell);
				RebuildCache();
			}
		}

		public bool HasWall(Vector2Int c)
		{
			return TryGetCell(c, out var cell) && cell.isWall;
		}

		public void SetWall(Vector2Int c, bool isWall)
		{
			if (!TryGetCell(c, out var cell)) return;

			cell.isWall = isWall;

			if (isWall)
			{
				cell.hasBall = false;
				cell.hasCoin = false; 
			}

			SetCell(cell);
		}


		public bool HasCoin(Vector2Int c)
		{
			return TryGetCell(c, out var cell) && cell.hasCoin;
		}

		public void SetCoin(Vector2Int c, bool hasCoin)
		{
			if (!TryGetCell(c, out var cell)) return;
			
			if (cell.isWall || cell.hasBall)
			{
				cell.hasCoin = false;
				SetCell(cell);
				return;
			}

			cell.hasCoin = hasCoin;
			SetCell(cell);
		}

		public void ToggleCoin(Vector2Int c)
		{
			if (!TryGetCell(c, out var cell)) return;
			
			if (cell.isWall || cell.hasBall)
			{
				cell.hasCoin = false;
				SetCell(cell);
				return;
			}

			cell.hasCoin = !cell.hasCoin;
			SetCell(cell);
		}

		public bool HasBall(Vector2Int c)
		{
			return TryGetCell(c, out var cell) && cell.hasBall;
		}

		public void SetBall(Vector2Int c, bool hasBall)
		{
			if (!TryGetCell(c, out var cell)) return;

			if (cell.isWall)
			{
				cell.hasBall = false;
				cell.hasCoin = false; 
				SetCell(cell);
				return;
			}

			cell.hasBall = hasBall;

			if (hasBall)
				cell.hasCoin = false; 

			SetCell(cell);
		}


		public void ToggleBall(Vector2Int c)
		{
			if (!TryGetCell(c, out var cell)) return;

			if (cell.isWall)
			{
				cell.hasBall = false;
				cell.hasCoin = false; // NEW
				SetCell(cell);
				return;
			}

			cell.hasBall = !cell.hasBall;

			if (cell.hasBall)
				cell.hasCoin = false; // NEW

			SetCell(cell);
		}

		// For Level simulation : coord -> index trans. (only for calculate)
		public int CoordToIndex(Vector2Int c)
		{
			return c.y * gridSize.x + c.x;
		}

		public Vector2Int IndexToCoord(int index)
		{
			int x = index % gridSize.x;
			int y = index / gridSize.x;
			return new Vector2Int(x, y);
		}
	}

	[Serializable]
	public struct CellData
	{
		public Vector2Int coord;
		public bool isWall;
		public bool hasBall;
		public bool hasCoin;
	}
}