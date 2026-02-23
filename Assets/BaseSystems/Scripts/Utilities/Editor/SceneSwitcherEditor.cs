using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaseSystems.Scripts.Utilities.Editor
{
	/// <summary>
	/// For quickly switching a scene
	/// </summary>
	[InitializeOnLoad]
	public static class SceneSwitcherEditor
	{
		private static readonly Dictionary<int, string> dropdown = new Dictionary<int, string>();

		[InitializeOnLoadMethod]
		private static void ShowStartSceneButton()
		{
			ToolbarExtender.ToolbarExtender.LeftToolbarGUI.Add(() =>
			{
				GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;

				dropdown.Clear();

				dropdown[0] = "Scene Switcher";
				int index = 1;

				// Build’teki sahneler
				var sceneCount = SceneManager.sceneCountInBuildSettings;
				for (int i = 0; i < sceneCount; i++, index++)
					dropdown[index] = SceneUtility.GetScenePathByBuildIndex(i);

				// Build’e eklenmemiş özel sahne

				string extraScenePath = AssetDatabase.GetAssetPath(
					AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Main/Scenes/ArtScene.unity"));

				if (!string.IsNullOrEmpty(extraScenePath))
					dropdown[index++] = extraScenePath;

				EditorGUI.BeginChangeCheck();
				int value = EditorGUILayout.Popup(0,
					dropdown.Values.Select(System.IO.Path.GetFileNameWithoutExtension).ToArray(), "Dropdown",
					GUILayout.Width(150));

				if (EditorGUI.EndChangeCheck())
				{
					if (value > 0)
					{
						EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
						EditorSceneManager.OpenScene(dropdown[value], OpenSceneMode.Single);
					}
				}

				GUI.enabled = true;
			});
		}
	}
}