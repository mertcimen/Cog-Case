using System.Collections;
using _Main.Scripts.Datas;
using BaseSystems.Scripts.LevelSystem;
using Fiber.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace BaseSystems.Scripts.Managers
{
	public class StateManager : SingletonInit<StateManager>
	{
		public GameState CurrentState
		{
			get => gameState;
			set
			{
				gameState = value;
				OnStateChanged?.Invoke(gameState);
			}
		}

		[Header("Debug")]
		[SerializeField] private GameState gameState = GameState.None;

		
		private readonly WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1);
		 private Coroutine levelCompleteTimeCoroutine;
		

		public static event UnityAction<GameState> OnStateChanged;

		private void OnEnable()
		{
			LevelManager.OnLevelLoad += LevelLoading;
			LevelManager.OnLevelStart += StartLevel;
			LevelManager.OnLevelRestart += RestartLevel;
			LevelManager.OnLevelLose += LoseLevel;
			LevelManager.OnLevelWin += WinLevel;
			LevelManager.OnLevelWinWithMoveCount += WinLevelWithMoveCount;
		}

		private void OnDisable()
		{
			LevelManager.OnLevelLoad -= LevelLoading;
			LevelManager.OnLevelStart -= StartLevel;
			LevelManager.OnLevelRestart -= RestartLevel;
			LevelManager.OnLevelLose -= LoseLevel;
			LevelManager.OnLevelWin -= WinLevel;
			LevelManager.OnLevelWinWithMoveCount -= WinLevelWithMoveCount;
		}

		private void LevelLoading()
		{
			CurrentState = GameState.Loading;
		}

		private void StartLevel()
		{
			DebugLog("GAME START");


			CurrentState = GameState.OnStart;

			levelCompleteTimeCoroutine = StartCoroutine(LevelCompleteTime());
		}

		private void RestartLevel()
		{
			DebugLog("GAME RESTART");
			LoseLevel();
		}

		private void WinLevel()
		{
			DebugLog("GAME WIN");
			if(levelCompleteTimeCoroutine!=null)
				StopCoroutine(levelCompleteTimeCoroutine);

			CurrentState = GameState.OnWin;

		}

		private void WinLevelWithMoveCount(int moveCount)
		{
			DebugLog("GAME WIN");
			if(levelCompleteTimeCoroutine!=null)
				StopCoroutine(levelCompleteTimeCoroutine);
			
			CurrentState = GameState.OnWin;

		}

		private void LoseLevel()
		{
			DebugLog("GAME LOSE");

			CurrentState = GameState.OnLose;
			if(levelCompleteTimeCoroutine!=null)
				StopCoroutine(levelCompleteTimeCoroutine);
		}

		private void DebugLog(string message)
		{
#if UNITY_EDITOR
			if(GameSettingsSO.Instance.EnableEditorAnalyticsLogs)
				Debug.Log(message);
#else
				Debug.Log(message);
#endif
		}


		private IEnumerator LevelCompleteTime()
		{
			while (true) 
			{
				if (Application.isFocused)
				{
					
				}
				yield return wait; 
			}
		}
	}
}