using BaseSystems.Scripts.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaseSystems.Scripts.UI
{
    public class LosePanel : PanelUI
    {
        [SerializeField] private Button btnRetry;
        [SerializeField] private Transform loseTextImage;
        [SerializeField] private TextMeshProUGUI loseText;
        private void Awake()
        {
            btnRetry.onClick.AddListener(Retry);
        }

        private void Retry()
        {
            ResetUITasks();
            LevelManager.Instance.RetryLevel();
            Close();
        }

        public void SetLosePanelText(string text)
        {
            loseText.text = text;
        }
        
        public override void Open()
        {
            base.Open();
            LoseUITasks();
        }
		
        private void LoseUITasks()
        {
            btnRetry.gameObject.transform.localScale = Vector3.zero;
            loseTextImage.gameObject.transform.localScale = Vector3.zero;

            btnRetry.gameObject.transform.DOScale(1f, 0.75f).SetEase(Ease.OutBack);
            loseTextImage.gameObject.transform.DOScale(1f, 0.75f).SetEase(Ease.OutBack);
        }
		
        private void ResetUITasks()
        {
            btnRetry.gameObject.transform.DOKill();
            loseTextImage.gameObject.transform.DOKill();
        }
    }
}