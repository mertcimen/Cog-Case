using _Main.Scripts.Data;
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
		[SerializeField] private Level testLevel;
#endif

		private int realLevelNo;

		public int LevelNo
		{
			get => PlayerPrefs.GetInt(PlayerPrefsNames.LEVEL_NO, 1);
			set => PlayerPrefs.SetInt(PlayerPrefsNames.LEVEL_NO, value);
		}

		public int RealLevelNo => realLevelNo;

		[SerializeField] private LevelsSO levelsSO;
		public LevelsSO LevelsSO => levelsSO;

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
			if (levelsSO == null || levelsSO.Levels.Count == 0)
			{
				Debug.LogWarning($"{name}: LevelsSO is empty!", this);
			}
		}

		private void Start()
		{
#if UNITY_EDITOR
			var levels = FindObjectsByType<Level>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (var level in levels)
				level.gameObject.SetActive(false);
#endif
			LoadCurrentLevel(true);
		}

		// -----------------------------
		// LEVEL LOAD
		// -----------------------------
		public void LoadCurrentLevel(bool isStart)
		{
			StateManager.Instance.CurrentState = GameState.OnStart;

			int totalLevels = levelsSO.Levels.Count;
			realLevelNo = LevelNo;

			currentLevelIndex = Mathf.Clamp(LevelNo - 1, 0, totalLevels - 1);

			LoadLevel(currentLevelIndex);
		}

		private void LoadLevel(int index)
		{
			if (index < 0 || index >= levelsSO.Levels.Count)
			{
				Debug.LogError($"Invalid level index: {index}");
				return;
			}

			var levelData = levelsSO.Levels[index];
			var levelPrefab = levelData.Level;

#if UNITY_EDITOR
			CurrentLevel = Instantiate(isActiveTestLevel ? testLevel : levelPrefab).GetComponent<Level>();
#else
            CurrentLevel = Instantiate(levelPrefab).GetComponent<Level>();
#endif

			CurrentLevel.Load();

			OnLevelLoad?.Invoke();

			StartLevel();
		}

		// -----------------------------
		// PLAY
		// -----------------------------
		public void StartLevel()
		{
			CurrentLevel.Play();
			OnLevelStart?.Invoke();
		}

#if UNITY_EDITOR
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
				RetryLevel();
		}
#endif

		// -----------------------------
		// LEVEL FLOW
		// -----------------------------
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

			// Eğer son leveli geçtiysek başa dön
			if (LevelNo > levelsSO.Levels.Count)
				LevelNo = 1;

			LoadCurrentLevel(false);
		}

		public void LoadBackLevel()
		{
			UnloadLevel();
			LevelNo--;

			if (LevelNo < 1)
				LevelNo = levelsSO.Levels.Count;

			LoadCurrentLevel(false);
		}

		private void UnloadLevel()
		{
			OnLevelUnload?.Invoke();

			
			if (CurrentLevel != null)
				CurrentLevel.Unload();

			Destroy(CurrentLevel.gameObject);
		}


		// -----------------------------
		// RESULT
		// -----------------------------
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

#if UNITY_EDITOR
		[Button(ButtonSizes.Medium, "Add Level Assets To List")]
		private void AddLevelAssetsToList()
		{
			const string levelPath = "Assets/_Main/Prefabs/Levels/Lev-Des";
			var levels = EditorUtilities.LoadAllAssetsFromPath<Level>(levelPath);

			levelsSO.Levels.Clear();

			foreach (var level in levels)
			{
				if (level.name.ToLower().Contains("test")) continue;
				if (level.name.ToLower().Contains("_base")) continue;
				levelsSO.AddLevel(level);
			}
		}
#endif
	}
}
