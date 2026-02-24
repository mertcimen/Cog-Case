using BaseSystems.Scripts.LevelSystem;
using BaseSystems.Scripts.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BaseSystems.Scripts.Utilities.Editor
{
	/// <summary>
	/// For quickly testing specific GridLevelAsset levels in editor.
	/// New system: LevelsSO holds GridLevelAsset list, LevelManager spawns a single Level prefab at runtime.
	/// </summary>
	[InitializeOnLoad]
	public static class TestGridLevelAsset
	{
		private static string[] dropdown;
		private static int framesToWaitUntilPlayMode;

		static TestGridLevelAsset()
		{
			EditorApplication.playModeStateChanged += ModeChanged;
		}

		[InitializeOnLoadMethod]
		private static void ShowStartSceneButton()
		{
			if (Application.isPlaying) return;

			ToolbarExtender.ToolbarExtender.RightToolbarGUI.Add(() =>
			{
				if (Application.isPlaying) return;

				var lm = LevelManager.Instance;
				if (!lm) return;

				var so = lm.LevelsSO;
				if (so == null || so.Levels == null || so.Levels.Count == 0) return;

				int levelCount = so.Levels.Count;

				// Build dropdown labels
				dropdown = new string[levelCount + 1];
				dropdown[0] = "Play Level";
				for (int i = 0; i < levelCount; i++)
					dropdown[i + 1] = $"{i + 1} - Level";

				EditorGUILayout.BeginHorizontal();

				// While playing: show a small button to ping the currently loaded GridLevelAsset
				if (EditorApplication.isPlaying && lm.LevelNo > 0)
				{
					int selectedIndex = lm.LevelNo - 1;
					if (selectedIndex >= 0 && selectedIndex < levelCount)
					{
						var levelAsset = so.Levels[selectedIndex];
						if (levelAsset != null)
						{
							Texture icon = EditorGUIUtility.IconContent("ScriptableObject Icon").image;
							if (GUILayout.Button(new GUIContent(icon, "Ping GridLevelAsset"), GUILayout.Width(25),
								    GUILayout.Height(18)))
							{
								EditorGUIUtility.PingObject(levelAsset);
								Selection.activeObject = levelAsset;
							}
						}
					}
				}

				GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;

				EditorGUI.BeginChangeCheck();
				int value = EditorGUILayout.Popup(0, dropdown, "Dropdown", GUILayout.Width(95));
				if (EditorGUI.EndChangeCheck())
				{
					if (value > 0)
					{
						// LevelNo is 1-based
						lm.LevelNo = value;

						EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.GameView"))
							.ShowNotification(new GUIContent("Testing Level " + value));

						EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

						framesToWaitUntilPlayMode = 0;
						EditorApplication.update -= EnterPlayMode;
						EditorApplication.update += EnterPlayMode;
					}
				}

				GUI.enabled = true;
				EditorGUILayout.EndHorizontal();
			});
		}

		private static void EnterPlayMode()
		{
			if (framesToWaitUntilPlayMode-- <= 0)
			{
				EditorApplication.update -= EnterPlayMode;

				EditorPrefs.SetBool("TestingLevel", true);

				// Optional: disable any Level objects already present in the scene before entering playmode
				// (old behavior kept for safety / legacy scenes)
				SetActiveLevels(false);

				EditorApplication.EnterPlaymode();
			}
		}

		private static void ModeChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredEditMode)
			{
				if (EditorPrefs.GetBool("TestingLevel") == true)
				{
					EditorPrefs.SetBool("TestingLevel", false);

					// Restore level objects if they existed in the scene
					SetActiveLevels(true);
				}
			}
		}

		private static void SetActiveLevels(bool isActive)
		{
			var levels = Object.FindObjectsByType<Level>(FindObjectsSortMode.None);
			foreach (var level in levels)
				level.gameObject.SetActive(isActive);
		}
	}
}