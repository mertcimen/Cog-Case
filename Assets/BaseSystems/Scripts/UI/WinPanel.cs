using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.Datas;
using BaseSystems.CurrencySystem.Scripts;
using BaseSystems.Scripts.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaseSystems.Scripts.UI
{
	public class WinPanel : PanelUI
	{
		[SerializeField] private Button btnContinue;
		[SerializeField] private TMP_Text txtMoneyAmount;
		
		[SerializeField] private List<string> levelWinStrings;
		[SerializeField] private TextMeshProUGUI txtWinText;

		[SerializeField] private GameObject winFirstStage;
		[SerializeField] private GameObject winSecondStage;
		[SerializeField] private GameObject baseBackground;

		private long rewardMoney;
		
		private bool isWobbling = true;
		[SerializeField] private NextFeatureController nextFeatureController;


		private void Awake()
		{
			btnContinue.onClick.AddListener(Win);
		}

		private void Win()
		{
			CurrencyManager.Money.AddCurrency(rewardMoney,
				txtMoneyAmount.rectTransform.position, false);
			
			btnContinue.interactable = false;

			DOVirtual.DelayedCall(2f, () =>
			{
				ResetWinSecondStage();
				LevelManager.Instance.LoadNextLevel();
				Close();
			});
		}

		public override void Open()
		{
			rewardMoney = GameSettingsSO.Instance.GetGoldReward();
			SetMoneyAmount();
			base.Open();
			SetWinFirstStage();
		}
		
		private void SetMoneyAmount()
		{
			txtMoneyAmount.SetText("+" + rewardMoney.ToString());
		}

		private void WinUITasks()
		{
			int randomInt = Random.Range(0, levelWinStrings.Count);
			
			txtWinText.text = levelWinStrings[randomInt];
			isWobbling = true;
			StartCoroutine(WobbleEffectCoroutine());
		}
		
		IEnumerator WobbleEffectCoroutine()
		{
			while (isWobbling)
			{
				txtWinText.ForceMeshUpdate();
				TMP_TextInfo textInfo = txtWinText.textInfo;

				for (int i = 0; i < textInfo.characterCount; i++)
				{
					if (!textInfo.characterInfo[i].isVisible) continue;

					int vertexIndex = textInfo.characterInfo[i].vertexIndex;
					Vector3[] vertices = textInfo.meshInfo[textInfo.characterInfo[i].materialReferenceIndex].vertices;

					float wobbleOffset = Mathf.Sin((Time.time * 4) + (i * 0.3f)) * 10;

					vertices[vertexIndex + 0].y += wobbleOffset;
					vertices[vertexIndex + 1].y += wobbleOffset;
					vertices[vertexIndex + 2].y += wobbleOffset;
					vertices[vertexIndex + 3].y += wobbleOffset;
				}

				for (int i = 0; i < textInfo.meshInfo.Length; i++)
				{
					textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
					txtWinText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
				}
				
				yield return new WaitForSeconds(0.05f); // Efekti güncelleme süresi
			}
		}

		private void SetWinFirstStage()
		{
			baseBackground.SetActive(true);
			winFirstStage.SetActive(true);
			WinUITasks();
			DOVirtual.DelayedCall(GameSettingsSO.Instance.WinButtonShowDelay, () =>
			{
				SetWinSecondStage();
			});
			// SetWinSecondStage();
		}

		private void SetWinSecondStage()
		{
			btnContinue.interactable = true;
			nextFeatureController.InitializeFeatureUI();
			winSecondStage.SetActive(true);
			winSecondStage.GetComponent<CanvasGroup>().DOFade(1f, 0.5f).SetDelay(GameSettingsSO.Instance.WinPanelShowDelay).onComplete = () =>
			{
				nextFeatureController.PlayProgressAnimation();
			};
		}

		private void ResetWinSecondStage()
		{
			isWobbling = false;
			baseBackground.SetActive(false);
			
			winSecondStage.SetActive(false);
			winSecondStage.GetComponent<CanvasGroup>().alpha = 0;
			winSecondStage.GetComponent<CanvasGroup>().DOKill();
			winFirstStage.SetActive(false);
			winFirstStage.GetComponent<CanvasGroup>().alpha = 1f;
			winFirstStage.GetComponent<CanvasGroup>().DOKill();
		}
	}
}