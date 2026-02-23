using System.Collections.Generic;
using System.Linq;
using _Main.Scripts.Datas;
using BaseSystems.Scripts.Managers;
using BaseSystems.Scripts.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NextFeatureController : PanelUI
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private GameObject nextFeatureText;
    [SerializeField] private Image lockImage;
    [SerializeField] private Image effect;
    [SerializeField] private TextMeshProUGUI unlockedFeatureText;
    [SerializeField] private GameObject nextFeatureParentObject;

    [Header("Feature Data"), SerializeField, ReadOnly]
    private Dictionary<NextFeatureObstacleType, int> obstacleUnlockLevels = new(); 
    [SerializeField] private Dictionary<NextFeatureObstacleType, ObstacleTypeClass> obstacleTypeData = new();

    private float progressAnimationDuration;

    private NextFeatureObstacleType? currentObstacle;
    private int startLevel;
    private int endLevel;
    private float formerProgress;
    private float progress;
    
    public void InitializeFeatureUI()
    {
        effect.transform.localScale = Vector3.zero;
        
        obstacleUnlockLevels.Clear();
      

        progressAnimationDuration = GameSettingsSO.Instance.NextFeatureAnimationDuration;
        
        currentObstacle = GetCurrentObstacle();
        if (currentObstacle == null)
        {
            nextFeatureParentObject.SetActive(false);
            return;
        }

        nextFeatureParentObject.SetActive(true);

        var data = obstacleTypeData[currentObstacle.Value];
        unlockedFeatureText.text = data.FinalText;
        backgroundImage.sprite = data.BackgroundSprite;
        progressFillImage.sprite = data.FinalSprite;

        SetProgressValues();
        progressFillImage.fillAmount = formerProgress;
        percentageText.text = Mathf.RoundToInt(formerProgress * 100) + "%";

    }

    public void PlayProgressAnimation()
    {
        if (currentObstacle == null) return;

        progressFillImage.DOFillAmount(progress, progressAnimationDuration).OnUpdate(() =>
        {
            percentageText.text = Mathf.RoundToInt(progressFillImage.fillAmount * 100) + "%";
        }).OnComplete(() =>
        {
            if (progress >= 1f)
                PlayCompletedAnimation();
        });

    }

    private void SetProgressValues()
    {
        int currentLevel = LevelManager.Instance.LevelNo;

        currentObstacle = GetCurrentObstacle();
        if (currentObstacle == null) return;

        startLevel = GetPreviousUnlockLevel() ;
        endLevel = obstacleUnlockLevels[currentObstacle.Value];

        int totalLevelsForFeature = endLevel - startLevel;
        Debug.Log(endLevel+"    "+startLevel);
        
        float ratio = (float)(currentLevel - startLevel) / totalLevelsForFeature;
        float nextRatio = (float)(currentLevel - startLevel + 1) / totalLevelsForFeature;
        formerProgress = ratio;
        progress = nextRatio;

        formerProgress = Mathf.Clamp01(formerProgress);
        progress = Mathf.Clamp01(progress);
    }


    private NextFeatureObstacleType? GetCurrentObstacle()
    {
        int currentLevel = LevelManager.Instance.LevelNo + 1;

        foreach (var pair in obstacleUnlockLevels.OrderBy(x => x.Value))
        {
            if (currentLevel <= pair.Value) 
                return pair.Key;
        }

        return null;
    }

    private int GetPreviousUnlockLevel()
    {
        int prev = 1;
        int currentLevel = LevelManager.Instance.LevelNo;
        foreach (var pair in obstacleUnlockLevels)
        {
            if (pair.Value <=currentLevel && pair.Value > prev)
            {
                prev = pair.Value;
            }
        }
        return prev;
    }

    public void PlayCompletedAnimation()
    {
        percentageText.text = "UNLOCKED";

        PlayUnlockAnimation();
        effect.transform.DOScale(Vector3.one * 1.6f, .75f);
        effect.transform.DOLocalRotate(new Vector3(0,0,-50), .75f, RotateMode.LocalAxisAdd);
        progressFillImage.transform.DOScale(Vector3.one * 1.2f, .2f).OnComplete(() =>
        {
            progressFillImage.transform.DOScale(Vector3.one * 1.1f, .1f);
        });
    }

    private void PlayUnlockAnimation()
    {
        lockImage.transform.localScale = Vector3.one;
        lockImage.transform.DOScale(Vector3.one * 1.1f, 0.3f).OnComplete(() =>
        {
            lockImage.transform.DOScale(Vector3.zero, 0.3f);
        });
    }
}
[System.Serializable]
public enum NextFeatureObstacleType
{
    Obstacle1,
    Obstacle2,
}

[System.Serializable]
public class ObstacleTypeClass
{
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite finalSprite;
    [SerializeField] private string finalText;
    
    public Sprite BackgroundSprite => backgroundSprite;
    public Sprite FinalSprite => finalSprite;
    public string FinalText => finalText;
}
