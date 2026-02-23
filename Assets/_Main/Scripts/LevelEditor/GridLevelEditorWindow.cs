#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _Main.Scripts.LevelEditor
{
	public class GridLevelEditorWindow : EditorWindow
	{
		private const int CellSize = 22;
		private const int CellPadding = 2;

		private GridLevelAsset _asset;
		private EditMode _mode = EditMode.WallPaint;

		// Drag paint state
		private bool _isDragging;
		private bool _dragPaintValue;
		private Vector2Int _lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);

		private Vector2 _scroll;

		private enum EditMode
		{
			WallPaint,
			BallPlace
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
				Repaint();
			}

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Create New Asset", GUILayout.Height(22)))
			{
				CreateNewAsset();
			}

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
			string path = EditorUtility.SaveFilePanelInProject(
				"Create Grid Level Asset",
				"GridLevelAsset",
				"asset",
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
			Repaint();
		}

		private void DrawLevelMeta(GridLevelAsset asset)
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			int newTime = EditorGUILayout.IntField("Level Time", asset.levelTime);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(asset, "Edit Level Time");
				asset.levelTime = Mathf.Max(0, newTime);
				MarkDirty();
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

				bool sizeChanged = newSize != grid.gridSize;
				grid.gridSize = newSize;

				if (sizeChanged)
				{
					grid.EnsureGridStorage();
					grid.RemoveOutOfBoundsBalls();
				}

				MarkDirty();
				ResetDragState();
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawModeToolbar()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Edit Mode", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			_mode = (EditMode)GUILayout.Toolbar((int)_mode, new[] { "Wall Paint", "Ball Place" });
			if (EditorGUI.EndChangeCheck())
				ResetDragState();

			EditorGUILayout.HelpBox(
				_mode == EditMode.WallPaint
					? "Wall Paint: Sol tık basılı tutup sürükleyerek duvar ekle/sil. İlk tıklama ekleme mi silme mi olacağını belirler."
					: "Ball Place: Hücreye tıklayınca ball ekler. Hücrede ball varsa tıklayınca siler.",
				MessageType.Info);

			EditorGUILayout.EndVertical();
		}

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

					Rect cellRect = GUILayoutUtility.GetRect(
						CellSize + CellPadding,
						CellSize + CellPadding,
						GUILayout.Width(CellSize + CellPadding),
						GUILayout.Height(CellSize + CellPadding));

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
			bool hasWall = grid.HasWall(c);
			bool hasBall = grid.HasBall(c);

			Color old = GUI.color;

			GUI.color = hasWall ? new Color(0.2f, 0.2f, 0.2f) : Color.white;
			GUI.Box(rect, GUIContent.none);

			if (hasBall)
			{
				GUI.color = new Color(0.15f, 0.65f, 1f);
				var inner = rect;
				inner.x += 4;
				inner.y += 4;
				inner.width -= 8;
				inner.height -= 8;
				EditorGUI.DrawRect(inner, GUI.color);
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

			if (_mode == EditMode.WallPaint)
			{
				if (isOver && e.type == EventType.MouseDown && e.button == 0)
				{
					e.Use();
					BeginWallDrag(grid, c);
					PaintWallIfNeeded(grid, c);
				}
				else if (_isDragging && isOver && e.type == EventType.MouseDrag && e.button == 0)
				{
					e.Use();
					PaintWallIfNeeded(grid, c);
				}
			}
			else // BallPlace
			{
				if (isOver && e.type == EventType.MouseDown && e.button == 0)
				{
					e.Use();
					ToggleBall(grid, c);
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

			if (_dragPaintValue)
				grid.RemoveBall(c);

			_lastPaintedCell = c;

			MarkDirty();
			Repaint();
		}

		private void ToggleBall(GridData grid, Vector2Int c)
		{
			Undo.RecordObject(_asset, "Toggle Ball");

			if (grid.HasWall(c))
			{
				grid.RemoveBall(c);
				MarkDirty();
				return;
			}

			if (grid.HasBall(c)) grid.RemoveBall(c);
			else grid.AddBall(c);

			MarkDirty();
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
