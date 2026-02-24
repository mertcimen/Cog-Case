#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using _Main.Scripts.Datas;
using UnityEditor;
using UnityEngine;

namespace _Main.Scripts.LevelEditor
{
	public class GridLevelEditorWindow : EditorWindow
	{
		private const int CellSize = 22;
		private const int CellPadding = 2;

		// State explosion limit
		private const int MaxStatesToExplore = 120000;

		private GridLevelAsset _asset;
		private EditMode _mode = EditMode.WallPaint;

		// Drag paint
		private bool _isDragging;
		private bool _dragPaintValue;
		private Vector2Int _lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);

		private Vector2 _scroll;

		// Win analysis result
		private bool _hasWinAnalysis;
		private WinAnalysisResult _winResult;

		private enum EditMode
		{
			WallPaint,
			BallPlace,
			CoinPlace
		}

		private enum SwipeDir
		{
			Left,
			Right,
			Up,
			Down
		}

		[Serializable]
		private struct WinAnalysisResult
		{
			public bool winnable;
			public int minSwipes;
			public int exploredStates;
			public int uniqueStates;
			public bool hitLimit;
		}

		private struct PaintState
		{
			public List<int> ballsSorted; // index list (coord bazlı datadan runtime mapping)
			public ulong[] paintMask;

			public PaintState(List<int> ballsSorted, ulong[] paintMask)
			{
				this.ballsSorted = ballsSorted;
				this.paintMask = paintMask;
			}
		}

		[MenuItem("--/Level Editor/Grid Level Editor")]
		public static void Open()
		{
			GetWindow<GridLevelEditorWindow>("Grid Level Editor");
		}

		private void OnGUI()
		{
			DrawTopBar();

			if (_asset == null)
			{
				EditorGUILayout.HelpBox("Bir GridLevelAsset seçin ya da oluşturun.", MessageType.Info);
				return;
			}

			if (_asset.grid == null)
				_asset.grid = new GridData();

			_asset.grid.EnsureGridStorage();

			EditorGUILayout.Space(8);
			DrawLevelMeta(_asset);

			EditorGUILayout.Space(8);
			DrawGridSettings(_asset);

			EditorGUILayout.Space(8);
			DrawModeToolbar();

			EditorGUILayout.Space(8);
			DrawWinSimulationPanel(_asset.grid);

			EditorGUILayout.Space(8);
			DrawGrid(_asset.grid);
		}

		private void DrawTopBar()
		{
			EditorGUILayout.BeginVertical("box");

			EditorGUI.BeginChangeCheck();
			_asset = (GridLevelAsset)EditorGUILayout.ObjectField("Level Asset", _asset, typeof(GridLevelAsset), false);
			if (EditorGUI.EndChangeCheck())
			{
				ResetDragState();
				_hasWinAnalysis = false;
				Repaint();
			}

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Create New Asset", GUILayout.Height(22)))
				CreateNewAsset();

			GUILayout.FlexibleSpace();

			if (_asset != null && GUILayout.Button("Ping Asset", GUILayout.Height(22), GUILayout.Width(120)))
			{
				EditorGUIUtility.PingObject(_asset);
				Selection.activeObject = _asset;
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}

		private void CreateNewAsset()
		{
			string path = EditorUtility.SaveFilePanelInProject("Create Grid Level Asset", "GridLevelAsset", "asset",
				"Yeni level asset için isim seçin.");

			if (string.IsNullOrEmpty(path))
				return;

			var asset = ScriptableObject.CreateInstance<GridLevelAsset>();
			asset.grid = new GridData();
			asset.grid.EnsureGridStorage();

			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();

			_asset = asset;
			EditorUtility.SetDirty(_asset);

			_hasWinAnalysis = false;
			Repaint();
		}

		private void DrawLevelMeta(GridLevelAsset asset)
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);

			// Level Time
			EditorGUI.BeginChangeCheck();
			int newTime = EditorGUILayout.IntField("Level Time", asset.levelTime);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(asset, "Edit Level Time");
				asset.levelTime = Mathf.Max(0, newTime);
				MarkDirty();
				_hasWinAnalysis = false;
				Repaint();
			}

			// Paint Color (ALWAYS visible)
			EditorGUI.BeginChangeCheck();
			var newPaintColorType = (ColorType)EditorGUILayout.EnumPopup("Paint Color", asset.levelPaintColor);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(asset, "Edit Paint Color");
				asset.levelPaintColor = newPaintColorType;
				MarkDirty();
				_hasWinAnalysis = false;
				Repaint();
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawGridSettings(GridLevelAsset asset)
		{
			var grid = asset.grid;

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();

			Vector2Int newSize = EditorGUILayout.Vector2IntField("Grid Size", grid.gridSize);
			newSize.x = Mathf.Max(1, newSize.x);
			newSize.y = Mathf.Max(1, newSize.y);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(asset, "Edit Grid Size");

				grid.gridSize = newSize;
				grid.EnsureGridStorage();

				MarkDirty();
				ResetDragState();
				_hasWinAnalysis = false;
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawModeToolbar()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Edit Mode", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			_mode = (EditMode)GUILayout.Toolbar((int)_mode, new[] { "Wall Paint", "Ball Place", "Coin Place" });

			if (EditorGUI.EndChangeCheck())
			{
				ResetDragState();
				_hasWinAnalysis = false;
			}

			EditorGUILayout.HelpBox(
				_mode == EditMode.WallPaint
					?
					"Wall Paint: Sol tık basılı tutup sürükleyerek duvar ekle/sil. İlk tıklama ekleme mi silme mi olacağını belirler."
					: _mode == EditMode.BallPlace
						? "Ball Place: Hücreye tıklayınca top ekler/siler. Duvar üstüne top konulamaz."
						: "Coin Place: Hücreye tıklayınca coin ekler/siler. Duvar veya top olan hücreye coin konulamaz.",
				MessageType.Info);

			EditorGUILayout.EndVertical();
		}

		// =========================================================
		// WIN SIMULATION (Paint All Non-Wall Cells)
		// =========================================================

		private void DrawWinSimulationPanel(GridData grid)
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Simulation (Paint All)", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox(
				"Amaç: Duvar olmayan tüm hücrelerin üzerinden en az bir kez top geçirmek.\n" +
				"Bu buton, swipe kombinasyonlarını BFS ile gezerek level kazanılabilir mi kontrol eder.",
				MessageType.Info);

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Check Winnable (Paint All)", GUILayout.Height(28)))
			{
				_winResult = CheckWinnablePaintAll(grid, MaxStatesToExplore);
				_hasWinAnalysis = true;
				Repaint();
			}

			using (new EditorGUI.DisabledScope(!_hasWinAnalysis))
			{
				if (GUILayout.Button("Clear Result", GUILayout.Height(28), GUILayout.Width(120)))
					_hasWinAnalysis = false;
			}

			EditorGUILayout.EndHorizontal();

			if (_hasWinAnalysis)
			{
				EditorGUILayout.Space(6);
				EditorGUILayout.LabelField($"Result: {(_winResult.winnable ? "WINNABLE ✅" : "NOT WINNABLE ❌")}",
					EditorStyles.boldLabel);

				if (_winResult.winnable)
					EditorGUILayout.LabelField($"Min swipes (found): {_winResult.minSwipes}");

				EditorGUILayout.LabelField($"Explored states: {_winResult.exploredStates}");
				EditorGUILayout.LabelField($"Unique states: {_winResult.uniqueStates}");

				if (_winResult.hitLimit)
				{
					EditorGUILayout.HelpBox(
						$"State limiti aşıldı (>{MaxStatesToExplore}). 'NOT WINNABLE' sonucu bu durumda kesin olmayabilir.",
						MessageType.Warning);
				}
			}

			EditorGUILayout.EndVertical();
		}

		private WinAnalysisResult CheckWinnablePaintAll(GridData grid, int maxStates)
		{
			grid.EnsureGridStorage();

			int xCount = grid.gridSize.x;
			int yCount = grid.gridSize.y;
			int total = grid.CellCount;

			// coord bazlı datadan runtime snapshot
			bool[] walls = new bool[total];

			var startBalls = new List<int>();
			var used = new HashSet<int>();

			for (int i = 0; i < grid.cells.Count; i++)
			{
				var cell = grid.cells[i];
				int idx = grid.CoordToIndex(cell.coord);

				if (idx < 0 || idx >= total) continue;

				walls[idx] = cell.isWall;

				if (!cell.isWall && cell.hasBall)
				{
					if (used.Add(idx))
						startBalls.Add(idx);
				}
			}

			startBalls.Sort();

			ulong[] goalMask = CreateGoalMask(total, walls);

			ulong[] startPaint = CreateEmptyMask(total);
			for (int i = 0; i < startBalls.Count; i++)
				SetBit(startPaint, startBalls[i], true);

			if (IsGoalReached(startPaint, goalMask))
			{
				return new WinAnalysisResult
				{
					winnable = true,
					minSwipes = 0,
					exploredStates = 1,
					uniqueStates = 1,
					hitLimit = false
				};
			}

			var startState = new PaintState(startBalls, startPaint);

			var visited = new HashSet<string>(capacity: 2048);
			var q = new Queue<(PaintState state, int depth)>(capacity: 2048);

			string startKey = EncodeKey(startState.ballsSorted, startState.paintMask);
			visited.Add(startKey);
			q.Enqueue((startState, 0));

			int explored = 0;
			bool hitLimit = false;

			while (q.Count > 0)
			{
				if (visited.Count >= maxStates)
				{
					hitLimit = true;
					break;
				}

				var (cur, depth) = q.Dequeue();
				explored++;

				for (int d = 0; d < 4; d++)
				{
					var dir = (SwipeDir)d;

					PaintState next = ApplySwipeWithPaint(cur, xCount, yCount, walls, dir);

					bool sameBalls = AreSameBalls(cur.ballsSorted, next.ballsSorted);
					bool samePaint = AreSameMask(cur.paintMask, next.paintMask);
					if (sameBalls && samePaint)
						continue;

					if (IsGoalReached(next.paintMask, goalMask))
					{
						return new WinAnalysisResult
						{
							winnable = true,
							minSwipes = depth + 1,
							exploredStates = explored,
							uniqueStates = visited.Count + 1,
							hitLimit = hitLimit
						};
					}

					string key = EncodeKey(next.ballsSorted, next.paintMask);
					if (!visited.Add(key))
						continue;

					q.Enqueue((next, depth + 1));

					if (visited.Count >= maxStates)
					{
						hitLimit = true;
						break;
					}
				}

				if (hitLimit)
					break;
			}

			return new WinAnalysisResult
			{
				winnable = false,
				minSwipes = -1,
				exploredStates = explored,
				uniqueStates = visited.Count,
				hitLimit = hitLimit
			};
		}

		private static PaintState ApplySwipeWithPaint(PaintState cur, int xCount, int yCount, bool[] walls,
			SwipeDir dir)
		{
			int total = xCount * yCount;

			bool[] occ = new bool[total];
			for (int i = 0; i < cur.ballsSorted.Count; i++)
				occ[cur.ballsSorted[i]] = true;

			ulong[] paint = CloneMask(cur.paintMask);
			bool[] nextOcc = new bool[total];

			if (dir == SwipeDir.Left || dir == SwipeDir.Right)
			{
				for (int y = 0; y < yCount; y++)
				{
					int x = 0;
					while (x < xCount)
					{
						while (x < xCount && walls[y * xCount + x]) x++;
						if (x >= xCount) break;

						int segStart = x;
						while (x < xCount && !walls[y * xCount + x]) x++;
						int segEnd = x - 1;

						var ballXs = new List<int>(4);
						for (int sx = segStart; sx <= segEnd; sx++)
						{
							if (occ[y * xCount + sx])
								ballXs.Add(sx);
						}

						if (ballXs.Count == 0)
							continue;

						if (dir == SwipeDir.Left) ballXs.Sort();
						else ballXs.Sort((a, b) => b - a);

						for (int i = 0; i < ballXs.Count; i++)
						{
							int startX = ballXs[i];
							int targetX = (dir == SwipeDir.Left) ? (segStart + i) : (segEnd - i);

							int finalIdx = y * xCount + targetX;
							nextOcc[finalIdx] = true;

							int from = Mathf.Min(startX, targetX);
							int to = Mathf.Max(startX, targetX);
							for (int px = from; px <= to; px++)
							{
								int pIdx = y * xCount + px;
								if (!walls[pIdx]) SetBit(paint, pIdx, true);
							}
						}
					}
				}
			}
			else
			{
				for (int x = 0; x < xCount; x++)
				{
					int y = 0;
					while (y < yCount)
					{
						while (y < yCount && walls[y * xCount + x]) y++;
						if (y >= yCount) break;

						int segStart = y;
						while (y < yCount && !walls[y * xCount + x]) y++;
						int segEnd = y - 1;

						var ballYs = new List<int>(4);
						for (int sy = segStart; sy <= segEnd; sy++)
						{
							if (occ[sy * xCount + x])
								ballYs.Add(sy);
						}

						if (ballYs.Count == 0)
							continue;

						if (dir == SwipeDir.Down) ballYs.Sort();
						else ballYs.Sort((a, b) => b - a);

						for (int i = 0; i < ballYs.Count; i++)
						{
							int startY = ballYs[i];
							int targetY = (dir == SwipeDir.Down) ? (segStart + i) : (segEnd - i);

							int finalIdx = targetY * xCount + x;
							nextOcc[finalIdx] = true;

							int from = Mathf.Min(startY, targetY);
							int to = Mathf.Max(startY, targetY);
							for (int py = from; py <= to; py++)
							{
								int pIdx = py * xCount + x;
								if (!walls[pIdx]) SetBit(paint, pIdx, true);
							}
						}
					}
				}
			}

			var nextBalls = new List<int>(cur.ballsSorted.Count);
			for (int i = 0; i < nextOcc.Length; i++)
				if (nextOcc[i])
					nextBalls.Add(i);

			return new PaintState(nextBalls, paint);
		}

		// =========================
		// Bitset helpers
		// =========================

		private static ulong[] CreateEmptyMask(int bitCount)
		{
			int words = (bitCount + 63) / 64;
			return new ulong[words];
		}

		private static ulong[] CloneMask(ulong[] src)
		{
			var dst = new ulong[src.Length];
			Array.Copy(src, dst, src.Length);
			return dst;
		}

		private static void SetBit(ulong[] mask, int bitIndex, bool value)
		{
			int word = bitIndex >> 6;
			int shift = bitIndex & 63;
			ulong bit = 1UL << shift;

			if (value) mask[word] |= bit;
			else mask[word] &= ~bit;
		}

		private static bool IsGoalReached(ulong[] paint, ulong[] goal)
		{
			for (int i = 0; i < goal.Length; i++)
			{
				ulong g = goal[i];
				if ((paint[i] & g) != g)
					return false;
			}

			return true;
		}

		private static ulong[] CreateGoalMask(int totalCells, bool[] walls)
		{
			var goal = CreateEmptyMask(totalCells);
			for (int i = 0; i < totalCells; i++)
				if (!walls[i])
					SetBit(goal, i, true);
			return goal;
		}

		private static bool AreSameBalls(List<int> a, List<int> b)
		{
			if (a.Count != b.Count) return false;
			for (int i = 0; i < a.Count; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}

		private static bool AreSameMask(ulong[] a, ulong[] b)
		{
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}

		private static string EncodeKey(List<int> ballsSorted, ulong[] mask)
		{
			var sb = new StringBuilder(64);

			if (ballsSorted.Count > 0)
			{
				sb.Append(ballsSorted[0]);
				for (int i = 1; i < ballsSorted.Count; i++)
				{
					sb.Append(',');
					sb.Append(ballsSorted[i]);
				}
			}

			sb.Append('|');

			for (int i = 0; i < mask.Length; i++)
				sb.Append(mask[i].ToString("X16"));

			return sb.ToString();
		}

		// =========================
		// GRID DRAW + INPUT (COORDINATE BASED)
		// =========================

		private void DrawGrid(GridData grid)
		{
			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			int xCount = grid.gridSize.x;
			int yCount = grid.gridSize.y;

			for (int y = yCount - 1; y >= 0; y--)
			{
				EditorGUILayout.BeginHorizontal();

				for (int x = 0; x < xCount; x++)
				{
					var c = new Vector2Int(x, y);

					Rect cellRect = GUILayoutUtility.GetRect(CellSize + CellPadding, CellSize + CellPadding,
						GUILayout.Width(CellSize + CellPadding), GUILayout.Height(CellSize + CellPadding));

					DrawCell(grid, c, cellRect);
					HandleCellMouse(grid, c, cellRect);
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndScrollView();
			HandleGlobalMouseUp();
		}

		private void DrawCell(GridData grid, Vector2Int c, Rect rect)
		{
			if (!grid.TryGetCell(c, out var cell))
			{
				GUI.Box(rect, GUIContent.none);
				return;
			}

			Color old = GUI.color;

			GUI.color = cell.isWall ? new Color(0.2f, 0.2f, 0.2f) : Color.white;
			GUI.Box(rect, GUIContent.none);

			if (cell.hasBall)
			{
				GUI.color = new Color(0.15f, 0.65f, 1f);
				var inner = rect;
				inner.x += 4;
				inner.y += 4;
				inner.width -= 8;
				inner.height -= 8;
				EditorGUI.DrawRect(inner, GUI.color);
			}

			if (cell.hasCoin)
			{
				GUI.color = new Color(1f, 0.85f, 0.1f); /// yellow for coin 
				var coinRect = rect;
				coinRect.width = 6;
				coinRect.height = 6;
				coinRect.x = rect.x + rect.width - 8; // Upper Right 
				coinRect.y = rect.y + 2;

				EditorGUI.DrawRect(coinRect, GUI.color);
			}

			GUI.color = Color.black;
			Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.black);

			GUI.color = old;
		}

		private void HandleCellMouse(GridData grid, Vector2Int c, Rect rect)
		{
			Event e = Event.current;
			if (e == null) return;

			bool isOver = rect.Contains(e.mousePosition);
			if (!isOver) return;

			if (_mode == EditMode.WallPaint)
			{
				if (e.type == EventType.MouseDown && e.button == 0)
				{
					e.Use();
					BeginWallDrag(grid, c);
					PaintWallIfNeeded(grid, c);
				}
				else if (_isDragging && e.type == EventType.MouseDrag && e.button == 0)
				{
					e.Use();
					PaintWallIfNeeded(grid, c);
				}
			}
			else if (_mode == EditMode.BallPlace)
			{
				if (e.type == EventType.MouseDown && e.button == 0)
				{
					e.Use();
					Undo.RecordObject(_asset, "Toggle Ball");
					grid.ToggleBall(c);
					MarkDirty();
					_hasWinAnalysis = false;
					Repaint();
				}
			}
			else // CoinPlace
			{
				if (e.type == EventType.MouseDown && e.button == 0)
				{
					e.Use();
					Undo.RecordObject(_asset, "Toggle Coin");
					grid.ToggleCoin(c);
					MarkDirty();
					_hasWinAnalysis = false;
					Repaint();
				}
			}
		}

		private void BeginWallDrag(GridData grid, Vector2Int firstCell)
		{
			Undo.RecordObject(_asset, "Paint Walls");

			_isDragging = true;
			_lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);

			bool currentlyHasWall = grid.HasWall(firstCell);
			_dragPaintValue = !currentlyHasWall;
		}

		private void PaintWallIfNeeded(GridData grid, Vector2Int c)
		{
			if (!_isDragging) return;
			if (_lastPaintedCell == c) return;

			grid.SetWall(c, _dragPaintValue);

			_lastPaintedCell = c;

			MarkDirty();
			_hasWinAnalysis = false;
			Repaint();
		}

		private void HandleGlobalMouseUp()
		{
			Event e = Event.current;
			if (e == null) return;

			if (_isDragging && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
			{
				ResetDragState();
				Repaint();
			}
		}

		private void ResetDragState()
		{
			_isDragging = false;
			_lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
		}

		private void MarkDirty()
		{
			if (_asset == null) return;
			EditorUtility.SetDirty(_asset);
		}
	}
}
#endif