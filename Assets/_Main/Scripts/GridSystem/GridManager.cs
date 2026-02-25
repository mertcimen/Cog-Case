using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Main.Scripts.BallSystem;
using _Main.Scripts.Datas;
using _Main.Scripts.InputSystem;
using _Main.Scripts.LevelEditor;
using _Main.Scripts.Pooling;
using BaseSystems.Scripts.LevelSystem;
using BaseSystems.Scripts.Managers;
using UnityEngine;

namespace _Main.Scripts.GridSystem
{
	public class GridManager : MonoBehaviour
	{
		[Header("Layout")]
		[SerializeField] private float cellSize = 1f;

		private Level currentLevel;
		private Transform gridRoot;

		private ColorType levelTargetColorForPaint;
		
		private Color targetColor;
		public Color TargetColor => targetColor;
		
		private readonly Dictionary<Vector2Int, GridCell> cellsByCoord = new Dictionary<Vector2Int, GridCell>();

		private readonly List<BallController> activeBalls = new List<BallController>(32);

		private int paintableGridCount = 0;
		private int paintedGridCount = 0;

		private bool isAnyBallMoving;

		private int gridWidth;
		private int gridHeight;

		
		public int GridWidth => gridWidth;
		public int GridHeight => gridHeight;
		public float CellSize => cellSize;
		private void OnEnable()
		{
			if (InputController.Instance != null)
				InputController.Instance.OnSwipe += HandleSwipe;
		}

		private void OnDisable()
		{
			if (InputController.Instance != null)
				InputController.Instance.OnSwipe -= HandleSwipe;
		}

		public void Initialize(Level level, GridLevelAsset levelData)
		{
			currentLevel = level;

			if (levelData == null || levelData.grid == null)
			{
				Debug.LogError("GridManager.Initialize failed: levelData/grid is null");
				return;
			}

			levelTargetColorForPaint = levelData.levelPaintColor;
			var matchedColor = ReferenceManagerSO.Instance.GameParameters.fillColors.FirstOrDefault(x =>
				x.colorType == levelData.levelPaintColor);

			targetColor = matchedColor != null ? matchedColor.color : Color.red;

			var gridData = levelData.grid;
			gridData.EnsureGridStorage();

			gridWidth = gridData.gridSize.x;
			gridHeight = gridData.gridSize.y;

			EnsureRoot();
			ClearSpawned();

			paintableGridCount = 0;
			paintedGridCount = 0;
			isAnyBallMoving = false;

			float spacing = cellSize;
			float xCenterOffset = (gridWidth - 1) * 0.5f;
			float yCenterOffset = (gridHeight - 1) * 0.5f;

			for (int i = 0; i < gridData.cells.Count; i++)
			{
				var cell = gridData.cells[i];

				if (!gridData.IsInside(cell.coord))
					continue;

				float x = (cell.coord.x - xCenterOffset) * spacing;
				float z = (cell.coord.y - yCenterOffset) * spacing;

				GridCell spawned = PoolManager.Instance.SpawnCell(gridRoot);
				spawned.transform.localPosition = new Vector3(x, 0f, z);
				spawned.transform.localRotation = Quaternion.identity;
				spawned.transform.localScale = Vector3.one;

				spawned.Initialize(cell, this);

				if (!cell.isWall)
					IncreaseRequiredPaintCount();

				cellsByCoord[cell.coord] = spawned;
			}

			// Cell Painting For Start if they has ball
			foreach (var kv in cellsByCoord)
			{
				var c = kv.Value;
				if (!c.IsWall && c.CurrentBall != null)
					PaintCell(c.Coordinate);
			}

			RebuildActiveBalls();
			InputController.Instance.SetInputEnabled(true);
		}

		public void PositionBorders(Transform top, Transform down, Transform left, Transform right, float offset = 0.5f)
		{
			if (gridRoot == null) return;

			// Grid local-space bounds (gridRoot local)
			float halfW = (gridWidth - 1) * 0.5f * cellSize;
			float halfH = (gridHeight - 1) * 0.5f * cellSize;

			// Center in local space is (0,0,0) by design.
			Vector3 centerLocal = Vector3.zero;

			// Helper: keep border's current Y (height), move only X/Z in grid plane.
			void SetBorder(Transform t, float xLocal, float zLocal)
			{
				if (t == null) return;

				Vector3 world = gridRoot.TransformPoint(new Vector3(xLocal, 0f, zLocal));
				Vector3 p = t.position;
				p.x = world.x;
				p.z = world.z;
				t.position = p;
			}

			// Right / Left: Z = center, X = edge +/- offset
			SetBorder(right, +halfW + offset, centerLocal.z);
			SetBorder(left, -halfW - offset, centerLocal.z);

			// Top / Down: X = center, Z = edge +/- offset
			SetBorder(top, centerLocal.x, +halfH + offset);
			SetBorder(down, centerLocal.x, -halfH - offset);
		}

		private void RebuildActiveBalls()
		{
			activeBalls.Clear();

			foreach (var kv in cellsByCoord)
			{
				var cell = kv.Value;
				if (cell != null && cell.CurrentBall != null)
					activeBalls.Add(cell.CurrentBall);
			}
		}

		private void ClearSpawned()
		{
			foreach (var kv in cellsByCoord)
			{
				if (kv.Value != null)
					PoolManager.Instance.DespawnCell(kv.Value);
			}

			cellsByCoord.Clear();
		}

		private void UpdateProgressUI()
		{
			if (UIManager.Instance == null) return;
			if (UIManager.Instance.InGameUI == null) return;

			UIManager.Instance.InGameUI.SetProgress(paintedGridCount, paintableGridCount);
		}

		private void HandleSwipe(SwipeDirection dir)
		{
			if (isAnyBallMoving) return;
			if (!HasAnyBall()) return;

			StartCoroutine(ApplySwipeRoutine(dir));
		}

		private IEnumerator ApplySwipeRoutine(SwipeDirection dir)
		{
			isAnyBallMoving = true;

			if (InputController.Instance != null)
				InputController.Instance.SetInputEnabled(false);

			List<MoveCommand> commands = BuildMoveCommands(dir);

			bool anyMove = false;
			for (int i = 0; i < commands.Count; i++)
			{
				if (commands[i].path != null && commands[i].path.Count > 1)
				{
					anyMove = true;
					break;
				}
			}

			if (!anyMove)
			{
				isAnyBallMoving = false;
				if (InputController.Instance != null)
					InputController.Instance.SetInputEnabled(true);
				yield break;
			}

			int pending = 0;

			// aynı anda hareket
			for (int i = 0; i < commands.Count; i++)
			{
				var cmd = commands[i];
				if (cmd.ball == null) continue;
				if (cmd.movement == null) continue;
				if (cmd.path == null || cmd.path.Count <= 1) continue;

				pending++;

				var fromCell = cellsByCoord[cmd.from];
				var toCell = cellsByCoord[cmd.to];

				cmd.movement.MoveAlongPath(cmd.path, CoordToCell, fromCell, toCell, dir, () => pending--);
			}

			while (pending > 0)
				yield return null;

			CommitCommands(commands);

			isAnyBallMoving = false;
			if (InputController.Instance != null && paintedGridCount < paintableGridCount)
				InputController.Instance.SetInputEnabled(true);
		}

		private GridCell CoordToCell(Vector2Int coord)
		{
			cellsByCoord.TryGetValue(coord, out var cell);
			return cell;
		}

		private void IncreaseRequiredPaintCount()
		{
			paintableGridCount++;
			UpdateProgressUI();
		}

		public void IncreaseCurrentPaintedCount()
		{
			paintedGridCount++;
			UpdateProgressUI();

			if (paintedGridCount >= paintableGridCount && paintableGridCount > 0)
			{
				InputController.Instance.SetInputEnabled(false);
				StartCoroutine(Delay());

				IEnumerator Delay()
				{
					yield return new WaitForSeconds(2f);

					LevelManager.Instance.Win();
				}
			}
		}

		private Vector3 CoordToWorld(Vector2Int coord)
		{
			if (cellsByCoord.TryGetValue(coord, out var cell))
				return cell.transform.position;

			// safety fallback
			return transform.position;
		}

		private void PaintCell(Vector2Int coord)
		{
			if (!cellsByCoord.TryGetValue(coord, out var cell)) return;
			if (cell.IsWall) return;

			cell.Paint();
		}

		public void DespawnAllToPool()
		{
			foreach (var kv in cellsByCoord)
			{
				if (kv.Value != null)
					PoolManager.Instance.DespawnCell(kv.Value);
			}

			cellsByCoord.Clear();
		}

		private void OnDestroy()
		{
			if (PoolManager.Instance != null)
				DespawnAllToPool();
		}

		// ---------------------------------------------------------
		// Movement planning 
		// ---------------------------------------------------------

		private struct SegmentKey : IEquatable<SegmentKey>
		{
			public bool horizontal; // true=row, false=col
			public int fixedIndex; // row y OR col x
			public int segStart; // start x or y
			public int segEnd; // end x or y

			public SegmentKey(bool horizontal, int fixedIndex, int segStart, int segEnd)
			{
				this.horizontal = horizontal;
				this.fixedIndex = fixedIndex;
				this.segStart = segStart;
				this.segEnd = segEnd;
			}

			public bool Equals(SegmentKey other)
			{
				return horizontal == other.horizontal && fixedIndex == other.fixedIndex && segStart == other.segStart &&
				       segEnd == other.segEnd;
			}

			public override bool Equals(object obj) => obj is SegmentKey other && Equals(other);

			public override int GetHashCode()
			{
				unchecked
				{
					int hash = (horizontal ? 1 : 0);
					hash = (hash * 397) ^ fixedIndex;
					hash = (hash * 397) ^ segStart;
					hash = (hash * 397) ^ segEnd;
					return hash;
				}
			}
		}

		private List<MoveCommand> BuildMoveCommands(SwipeDirection dir)
		{
			var commands = new List<MoveCommand>(activeBalls.Count);

			bool horizontal = dir == SwipeDirection.Left || dir == SwipeDirection.Right;

			// 1) Segment gruplama (yalnızca aktif toplar üzerinden)
			var groups = new Dictionary<SegmentKey, List<GridCell>>(64);

			for (int i = 0; i < activeBalls.Count; i++)
			{
				var ball = activeBalls[i];
				if (ball == null) continue;

				var cell = ball.CurrentCell;
				if (cell == null) continue;
				if (cell.IsWall) continue;

				Vector2Int c = cell.Coordinate;

				if (!TryFindSegmentBounds(c, horizontal, out int segStart, out int segEnd))
					continue;

				int fixedIndex = horizontal ? c.y : c.x;
				var key = new SegmentKey(horizontal, fixedIndex, segStart, segEnd);

				if (!groups.TryGetValue(key, out var list))
				{
					list = new List<GridCell>(4);
					groups.Add(key, list);
				}

				list.Add(cell);
			}

			// 2) Her segmentte sıkıştırma ve komut üretimi
			foreach (var kv in groups)
			{
				var key = kv.Key;
				var ballsCells = kv.Value;
				if (ballsCells == null || ballsCells.Count == 0)
					continue;

				if (horizontal)
				{
					int y = key.fixedIndex;

					if (dir == SwipeDirection.Left)
						ballsCells.Sort((a, b) => a.Coordinate.x.CompareTo(b.Coordinate.x));
					else
						ballsCells.Sort((a, b) => b.Coordinate.x.CompareTo(a.Coordinate.x));

					for (int i = 0; i < ballsCells.Count; i++)
					{
						var fromCell = ballsCells[i];
						int targetX = (dir == SwipeDirection.Left) ? (key.segStart + i) : (key.segEnd - i);
						var to = new Vector2Int(targetX, y);

						var ball = fromCell.CurrentBall;
						if (ball == null) continue;

						commands.Add(new MoveCommand
						{
							ball = ball,
							movement = ball.MovementController,
							from = fromCell.Coordinate,
							to = to,
							path = BuildStraightPath(fromCell.Coordinate, to)
						});
					}
				}
				else
				{
					int x = key.fixedIndex;

					if (dir == SwipeDirection.Down)
						ballsCells.Sort((a, b) => a.Coordinate.y.CompareTo(b.Coordinate.y));
					else
						ballsCells.Sort((a, b) => b.Coordinate.y.CompareTo(a.Coordinate.y));

					for (int i = 0; i < ballsCells.Count; i++)
					{
						var fromCell = ballsCells[i];
						int targetY = (dir == SwipeDirection.Down) ? (key.segStart + i) : (key.segEnd - i);
						var to = new Vector2Int(x, targetY);

						var ball = fromCell.CurrentBall;
						if (ball == null) continue;

						commands.Add(new MoveCommand
						{
							ball = ball,
							movement = ball.MovementController,
							from = fromCell.Coordinate,
							to = to,
							path = BuildStraightPath(fromCell.Coordinate, to)
						});
					}
				}
			}

			return commands;
		}

		private bool TryFindSegmentBounds(Vector2Int start, bool horizontal, out int segStart, out int segEnd)
		{
			segStart = 0;
			segEnd = 0;

			if (!cellsByCoord.TryGetValue(start, out var startCell) || startCell == null || startCell.IsWall)
				return false;

			if (horizontal)
			{
				int y = start.y;

				int xMin = start.x;
				while (xMin - 1 >= 0 && !IsWall(new Vector2Int(xMin - 1, y)))
					xMin--;

				int xMax = start.x;
				while (xMax + 1 < gridWidth && !IsWall(new Vector2Int(xMax + 1, y)))
					xMax++;

				segStart = xMin;
				segEnd = xMax;
				return true;
			}
			else
			{
				int x = start.x;

				int yMin = start.y;
				while (yMin - 1 >= 0 && !IsWall(new Vector2Int(x, yMin - 1)))
					yMin--;

				int yMax = start.y;
				while (yMax + 1 < gridHeight && !IsWall(new Vector2Int(x, yMax + 1)))
					yMax++;

				segStart = yMin;
				segEnd = yMax;
				return true;
			}
		}

		private struct MoveCommand
		{
			public BallController ball;
			public BallMovementController movement;
			public Vector2Int from;
			public Vector2Int to;
			public List<Vector2Int> path; // from..to inclusive
		}

		private List<GridCell> CollectBallsInRowSegment(int y, int startX, int endX)
		{
			var result = new List<GridCell>(4);
			for (int x = startX; x <= endX; x++)
			{
				var c = new Vector2Int(x, y);
				if (cellsByCoord.TryGetValue(c, out var cell) && cell.CurrentBall != null)
					result.Add(cell);
			}

			return result;
		}

		private List<GridCell> CollectBallsInColSegment(int x, int startY, int endY)
		{
			var result = new List<GridCell>(4);
			for (int y = startY; y <= endY; y++)
			{
				var c = new Vector2Int(x, y);
				if (cellsByCoord.TryGetValue(c, out var cell) && cell.CurrentBall != null)
					result.Add(cell);
			}

			return result;
		}

		public bool TryGetCell(Vector2Int coord, out GridCell cell) => cellsByCoord.TryGetValue(coord, out cell);

		private void CommitCommands(List<MoveCommand> commands)
		{
			// 1) tüm cell ball ref’lerini temizle
			foreach (var kv in cellsByCoord)
				kv.Value.ClearBallReference();

			// 2) yeni konumlara ata
			for (int i = 0; i < commands.Count; i++)
			{
				var cmd = commands[i];
				if (cmd.ball == null) continue;

				if (!cellsByCoord.TryGetValue(cmd.to, out var toCell))
					continue;

				toCell.SetBallReference(cmd.ball);
				cmd.ball.SetCurrentCell(toCell);

				// final world pos
				cmd.ball.transform.position = toCell.transform.position;
			}
		}

		private List<Vector2Int> BuildStraightPath(Vector2Int from, Vector2Int to)
		{
			var path = new List<Vector2Int>(8) { from };

			Vector2Int d = to - from;
			int stepX = d.x == 0 ? 0 : (d.x > 0 ? 1 : -1);
			int stepY = d.y == 0 ? 0 : (d.y > 0 ? 1 : -1);

			var cur = from;
			while (cur != to)
			{
				cur = new Vector2Int(cur.x + stepX, cur.y + stepY);
				path.Add(cur);
			}

			return path;
		}

		private bool HasAnyBall() => activeBalls.Count > 0;

		private bool IsWall(Vector2Int c)
		{
			return cellsByCoord.TryGetValue(c, out var cell) && cell.IsWall;
		}

		// ---------------------------------------------------------
		// Root / clear
		// ---------------------------------------------------------

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
	}
}