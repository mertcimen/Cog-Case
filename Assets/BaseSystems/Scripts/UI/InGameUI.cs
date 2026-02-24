using _Main.Scripts.GamePlay;
using BaseSystems.Scripts.Managers;
using DG.Tweening;
using Lofelt.NiceVibrations;
using TMPro;
using TriInspector;
using UnityEngine;
using UnityEngine.UI;

namespace BaseSystems.Scripts.UI
{
	public class InGameUI : MonoBehaviour
	{
		[Title("Level")]
		[SerializeField] private TMP_Text txtLevelNo;
		[Title("Goal")]
		[SerializeField] private RectTransform goalRectTransform;
		[Title("Move Count")]
		[SerializeField] private TMP_Text txtMoveCount;

		[Title("Buttons")]
		[SerializeField] private bool askBeforeRestart;
		[SerializeField] private Button btnRestart;
		[SerializeField] private Button btnSettings;
		public TimerCounter timerCounter;
		[SerializeField] private Image progressBarImg;
		[SerializeField] private TextMeshProUGUI progressText;

		private void Awake()
		{
			btnRestart.onClick.AddListener(Restart);
			btnSettings.onClick.AddListener(OpenSettings);

			LevelManager.OnLevelLoad += OnLevelLoaded;

			SetLevelNo(LevelManager.Instance.LevelNo);
		}

		private void OnDestroy()
		{
			LevelManager.OnLevelLoad -= OnLevelLoaded;
		}

		private void OnLevelLoaded()
		{
			SetLevelNo(LevelManager.Instance.LevelNo);
		}

		Tween fillTween;

		public void SetProgress(float normalized)
		{
			if (progressBarImg == null) return;

			if (fillTween != null)
			{
				fillTween.Kill();
			}

			fillTween = progressBarImg.DOFillAmount(Mathf.Clamp01(normalized), 0.3f).OnComplete((() =>
			{
				fillTween.Kill();
				fillTween = null;
			}));
		}

		public void SetProgress(int painted, int required)
		{
			float progress = (required <= 0) ? 0f : (float)painted / required;
			progressText.SetText($"{painted}/{required}");
			SetProgress(progress);
		}

		public void SetLevelNo(int levelNo)
		{
			if (txtLevelNo)
				txtLevelNo.SetText("LEVEL " + levelNo.ToString());
		}

		public void SetMoveCount(int moveCount)
		{
			if (txtMoveCount)
				txtMoveCount.SetText(moveCount.ToString());
		}

		private void Restart()
		{
			HapticManager.Instance.PlayHaptic(HapticPatterns.PresetType.MediumImpact);
			if (askBeforeRestart)
			{
				MessageBox.Scripts.MessageBox.Instance.Show("Are you sure you want to restart?", "Restart",
					MessageBox.Scripts.MessageBox.MessageBoxButtons.YesNo,
					MessageBox.Scripts.MessageBox.MessageBoxType.Question, LevelManager.Instance.RestartLevel);
			}
			else
			{
				LevelManager.Instance.RestartLevel();
			}
		}

		private void OpenSettings()
		{
			UIManager.Instance.ShowSettingsPanel();
		}

		public void Show()
		{
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}
	}
}