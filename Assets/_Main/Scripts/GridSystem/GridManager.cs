using System;
using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.BallSystem;
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
		[SerializeField] private GridCell cellPrefab;
		[SerializeField] private BallController ballPrefab; // şimdilik kullanılmıyor ama bırakıyorum

		[Header("Layout")]
		[SerializeField] private float cellSize = 1f;
		[SerializeField] private float cellGap = 0f;

		private Level currentLevel;
		private Transform gridRoot;

		private readonly Dictionary<Vector2Int, GridCell> cellsByCoord = new Dictionary<Vector2Int, GridCell>();

		private int paintableGridCount = 0;
		private int paintedGridCount = 0;

		private bool isAnyBallMoving;

		private int gridWidth;
		private int gridHeight;

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

			var gridData = levelData.grid;
			gridData.EnsureGridStorage();

			gridWidth = gridData.gridSize.x;
			gridHeight = gridData.gridSize.y;

			EnsureRoot();
			ClearSpawned();

			paintableGridCount = 0;
			paintedGridCount = 0;
			isAnyBallMoving = false;

			float spacing = cellSize + cellGap;
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

				cmd.movement.MoveAlongPath(cmd.path, CoordToCell, fromCell, toCell, () => pending--);
			}

			while (pending > 0)
				yield return null;

			CommitCommands(commands);

			isAnyBallMoving = false;
			if (InputController.Instance != null)
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

		// ✅ extra safety: Editor stop / destroy gibi senaryolarda da çalışsın
		private void OnDestroy()
		{
			// LevelManager zaten çağıracak; ama garanti olsun:
			if (PoolManager.Instance != null)
				DespawnAllToPool();
		}

		// ---------------------------------------------------------
		// Movement planning (segment sıkıştırma)
		// ---------------------------------------------------------

		private struct MoveCommand
		{
			public BallController ball;
			public BallMovementController movement;
			public Vector2Int from;
			public Vector2Int to;
			public List<Vector2Int> path; // from..to inclusive
		}

		private List<MoveCommand> BuildMoveCommands(SwipeDirection dir)
		{
			var commands = new List<MoveCommand>(32);

			bool horizontal = dir == SwipeDirection.Left || dir == SwipeDirection.Right;

			if (horizontal)
			{
				for (int y = 0; y < gridHeight; y++)
				{
					int x = 0;
					while (x < gridWidth)
					{
						while (x < gridWidth && IsWall(new Vector2Int(x, y))) x++;
						if (x >= gridWidth) break;

						int segStart = x;
						while (x < gridWidth && !IsWall(new Vector2Int(x, y))) x++;
						int segEnd = x - 1;

						var balls = CollectBallsInRowSegment(y, segStart, segEnd);
						if (balls.Count == 0) continue;

						if (dir == SwipeDirection.Left)
							balls.Sort((a, b) => a.Coordinate.x.CompareTo(b.Coordinate.x));
						else
							balls.Sort((a, b) => b.Coordinate.x.CompareTo(a.Coordinate.x));

						for (int i = 0; i < balls.Count; i++)
						{
							var fromCell = balls[i];
							int targetX = (dir == SwipeDirection.Left) ? (segStart + i) : (segEnd - i);
							var to = new Vector2Int(targetX, y);

							var ball = fromCell.CurrentBall;
							if (ball == null) continue;

							var mv = ball.MovementController;
							commands.Add(new MoveCommand
							{
								ball = ball,
								movement = mv,
								from = fromCell.Coordinate,
								to = to,
								path = BuildStraightPath(fromCell.Coordinate, to)
							});
						}
					}
				}
			}
			else
			{
				for (int x = 0; x < gridWidth; x++)
				{
					int y = 0;
					while (y < gridHeight)
					{
						while (y < gridHeight && IsWall(new Vector2Int(x, y))) y++;
						if (y >= gridHeight) break;

						int segStart = y;
						while (y < gridHeight && !IsWall(new Vector2Int(x, y))) y++;
						int segEnd = y - 1;

						var balls = CollectBallsInColSegment(x, segStart, segEnd);
						if (balls.Count == 0) continue;

						if (dir == SwipeDirection.Down)
							balls.Sort((a, b) => a.Coordinate.y.CompareTo(b.Coordinate.y));
						else
							balls.Sort((a, b) => b.Coordinate.y.CompareTo(a.Coordinate.y));

						for (int i = 0; i < balls.Count; i++)
						{
							var fromCell = balls[i];
							int targetY = (dir == SwipeDirection.Down) ? (segStart + i) : (segEnd - i);
							var to = new Vector2Int(x, targetY);

							var ball = fromCell.CurrentBall;
							if (ball == null) continue;

							var mv = ball.MovementController;
							commands.Add(new MoveCommand
							{
								ball = ball,
								movement = mv,
								from = fromCell.Coordinate,
								to = to,
								path = BuildStraightPath(fromCell.Coordinate, to)
							});
						}
					}
				}
			}

			return commands;
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

		private bool HasAnyBall()
		{
			foreach (var kv in cellsByCoord)
				if (kv.Value.CurrentBall != null)
					return true;
			return false;
		}

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