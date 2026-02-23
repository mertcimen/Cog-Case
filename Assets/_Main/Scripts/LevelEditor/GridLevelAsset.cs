using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.LevelEditor
{
	[CreateAssetMenu(menuName = "Level/Grid Level Asset", fileName = "GridLevelAsset")]
	public class GridLevelAsset : ScriptableObject
	{
		public int levelTime = 60;
		public GridData grid = new GridData();
	}

	[Serializable]
	public class GridData
	{
		public Vector2Int gridSize = new Vector2Int(10, 10);

		// Walls: true = wall var
		[SerializeField] private List<bool> walls = new List<bool>();

		// Balls: şimdilik tek tip, sadece coord
		public List<BallData> balls = new List<BallData>();

		public void EnsureGridStorage()
		{
			int total = Mathf.Max(0, gridSize.x * gridSize.y);
			if (walls == null) walls = new List<bool>(total);

			if (walls.Count != total)
			{
				var newWalls = new List<bool>(total);
				for (int i = 0; i < total; i++)
				{
					bool old = (i >= 0 && i < walls.Count) ? walls[i] : false;
					newWalls.Add(old);
				}

				walls = newWalls;
			}

			if (balls == null) balls = new List<BallData>();
		}

		public bool IsInside(Vector2Int c)
		{
			return c.x >= 0 && c.y >= 0 && c.x < gridSize.x && c.y < gridSize.y;
		}

		public int CoordToIndex(Vector2Int c)
		{
			return c.y * gridSize.x + c.x;
		}

		public bool HasWall(Vector2Int c)
		{
			if (!IsInside(c)) return false;
			EnsureGridStorage();
			return walls[CoordToIndex(c)];
		}

		public void SetWall(Vector2Int c, bool hasWall)
		{
			if (!IsInside(c)) return;
			EnsureGridStorage();
			walls[CoordToIndex(c)] = hasWall;
		}

		public bool HasBall(Vector2Int c)
		{
			if (balls == null) return false;
			for (int i = 0; i < balls.Count; i++)
			{
				if (balls[i].coord == c) return true;
			}
			return false;
		}

		public void AddBall(Vector2Int c)
		{
			if (!IsInside(c)) return;
			if (HasBall(c)) return;
			balls.Add(new BallData { coord = c });
		}

		public void RemoveBall(Vector2Int c)
		{
			if (balls == null) return;
			balls.RemoveAll(b => b.coord == c);
		}

		public void RemoveOutOfBoundsBalls()
		{
			if (balls == null) return;
			balls.RemoveAll(b => !IsInside(b.coord));
		}
	}

	[Serializable]
	public class BallData
	{
		public Vector2Int coord;
	}
}
