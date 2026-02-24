using _Main.Scripts.Data;
using _Main.Scripts.LevelEditor;
using BaseSystems.AudioSystem.Scripts;
using BaseSystems.Scripts.LevelSystem;
using BaseSystems.Scripts.Utilities;
using Fiber.Utilities;
using TriInspector;
using UnityEngine;
using UnityEngine.Events;

namespace BaseSystems.Scripts.Managers
{
	[DefaultExecutionOrder(-2)]
	public class LevelManager : Singleton<LevelManager>
	{
#if UNITY_EDITOR
		[SerializeField] private bool isActiveTestLevel;
		[ShowIf(nameof(isActiveTestLevel))]
		[SerializeField] private Level testLevelPrefab;
#endif

		[SerializeField] private Level levelPrefab;
		[SerializeField] private LevelsSO levelsSO;

		public LevelsSO LevelsSO => levelsSO;

		public int LevelNo
		{
			get => PlayerPrefs.GetInt(PlayerPrefsNames.LEVEL_NO, 1);
			set => PlayerPrefs.SetInt(PlayerPrefsNames.LEVEL_NO, value);
		}

		public Level CurrentLevel { get; private set; }

		private int currentLevelIndex;
		public int CurrentLevelIndex => currentLevelIndex;

		public static event UnityAction OnLevelLoad;
		public static event UnityAction OnLevelUnload;
		public static event UnityAction OnLevelStart;
		public static event UnityAction OnLevelRestart;

		public static event UnityAction OnLevelWin;
		public static event UnityAction<int> OnLevelWinWithMoveCount;
		public static event UnityAction OnLevelLose;

		private void Awake()
		{
			if (levelsSO == null || levelsSO.Count == 0)
				Debug.LogWarning($"{name}: LevelsSO is empty!", this);

			if (!isActiveTestLevel && levelPrefab == null)
				Debug.LogError($"{name}: Level Prefab missing!", this);
		}

		private void Start()
		{
			LoadCurrentLevel(true);
		}

		public void LoadCurrentLevel(bool isStart)
		{
			StateManager.Instance.CurrentState = GameState.OnStart;

			int totalLevels = levelsSO != null ? levelsSO.Count : 0;
			if (totalLevels <= 0)
			{
				Debug.LogError("LevelManager: LevelsSO empty!");
				return;
			}

			currentLevelIndex = Mathf.Clamp(LevelNo - 1, 0, totalLevels - 1);

			LoadLevel(currentLevelIndex);
		}

		private void LoadLevel(int index)
		{
			int totalLevels = levelsSO != null ? levelsSO.Count : 0;
			if (index < 0 || index >= totalLevels)
			{
				Debug.LogError($"Invalid level index: {index}");
				return;
			}

			GridLevelAsset asset = levelsSO.Get(index);
			if (asset == null)
			{
				Debug.LogError($"GridLevelAsset missing at index: {index}");
				return;
			}

#if UNITY_EDITOR
			Level prefabToSpawn = isActiveTestLevel ? testLevelPrefab : levelPrefab;
#else
			Level prefabToSpawn = levelPrefab;
#endif
			if (prefabToSpawn == null)
			{
				Debug.LogError("LevelManager: Level prefab is null!");
				return;
			}

			CurrentLevel = Instantiate(prefabToSpawn);
			CurrentLevel.Load(asset);

			OnLevelLoad?.Invoke();

			StartLevel();
		}

		public void StartLevel()
		{
			CurrentLevel.Play();
			OnLevelStart?.Invoke();
		}

		public void RetryLevel()
		{
			UnloadLevel();
			LoadLevel(currentLevelIndex);
		}

		public void RestartFromFirstLevel()
		{
			LevelNo = 1;
			OnLevelRestart?.Invoke();
			LoadCurrentLevel(false);
		}

		public void RestartLevel()
		{
			OnLevelRestart?.Invoke();
			RetryLevel();
		}

		public void LoadNextLevel()
		{
			UnloadLevel();

			LevelNo++;

			if (levelsSO != null && LevelNo > levelsSO.Count)
				LevelNo = 1;

			LoadCurrentLevel(false);
		}

		public void LoadBackLevel()
		{
			UnloadLevel();

			LevelNo--;

			if (levelsSO != null && LevelNo < 1)
				LevelNo = levelsSO.Count;

			LoadCurrentLevel(false);
		}

		private void UnloadLevel()
		{
			OnLevelUnload?.Invoke();

			if (CurrentLevel != null)
			{
				CurrentLevel.Unload();
				Destroy(CurrentLevel.gameObject);
				CurrentLevel = null;
			}
		}

		[Button]
		public void Win()
		{
			if (StateManager.Instance.CurrentState != GameState.OnStart) return;

			AudioManager.Instance.PlayAudio(AudioName.LevelWin);
			OnLevelWin?.Invoke();
		}

		public void Win(int moveCount)
		{
			if (StateManager.Instance.CurrentState != GameState.OnStart) return;

			AudioManager.Instance.PlayAudio(AudioName.LevelWin);
			OnLevelWinWithMoveCount?.Invoke(moveCount);
		}

		[Button]
		public void Lose(string loseText)
		{
			if (StateManager.Instance.CurrentState != GameState.OnStart) return;

			UIManager.Instance.SetLosePanelText(loseText);
			AudioManager.Instance.PlayAudio(AudioName.LevelLose);
			OnLevelLose?.Invoke();
		}
	}
}