using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--- ステージ番号 ---")]
    [SerializeField] private int stageNumber = 1;

    [Header("--- 手数設定 ---")]
    [SerializeField] private int countMax = 99;

    [Header("--- 手数表示 ---")]
    [SerializeField] private TMP_Text moveCountText;

    [Header("--- クリア時に表示するUI ---")]
    [SerializeField] private GameObject clearUI;

    [Header("--- ステージ背景のRenderer ---")]
    [SerializeField] private SpriteRenderer[] stageRenderers;

    [Header("--- クリア前の彩度代わりの色 ---")]
    [SerializeField]
    private Color unclearedColor =
        new Color(0.55f, 0.55f, 0.55f, 1f);

    private BrockGuide[] allGuides;
    private Color[] originalColors;

    private bool cleared;
    private int moveCount;

    public int MoveCount => moveCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        allGuides = FindObjectsByType<BrockGuide>(
            FindObjectsSortMode.None
        );

        SaveOriginalColors();
        SetStageUnclearedColor();

        moveCount = 0;
        UpdateMoveCountUI();

        if (clearUI != null)
        {
            clearUI.SetActive(false);
        }

        if (allGuides.Length == 0)
        {
            Debug.LogWarning(
                "シーン内にBrockGuideがありません。"
                + "自動クリアを防止します。"
            );
        }
    }

    private void Update()
    {
        CheckClearCondition();
    }

    public void AddMoveCount(int amount)
    {
        if (cleared)
        {
            return;
        }

        moveCount = Mathf.Clamp(
            moveCount + amount,
            0,
            countMax
        );

        Player player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            player.MoveCount = moveCount;
        }

        UpdateMoveCountUI();
    }

    public void ResetMoveCount()
    {
        moveCount = 0;

        Player player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            player.MoveCount = 0;
        }

        UpdateMoveCountUI();
    }

    private void UpdateMoveCountUI()
    {
        if (moveCountText != null)
        {
            moveCountText.text = moveCount.ToString("00");
        }
    }

    private void CheckClearCondition()
    {
        if (cleared)
        {
            return;
        }

        if (allGuides == null || allGuides.Length == 0)
        {
            return;
        }

        foreach (BrockGuide guide in allGuides)
        {
            if (guide == null || !guide.isFilled)
            {
                return;
            }
        }

        OnAllBrocksPlaced();
    }

    public void CheckClearConditionNow()
    {
        CheckClearCondition();
    }

    private void OnAllBrocksPlaced()
    {
        if (cleared)
        {
            return;
        }

        cleared = true;

        SaveStageMoveCount();
        RestoreStageColors();

        if (clearUI != null)
        {
            clearUI.SetActive(true);
        }

        Debug.Log(
            "ステージクリア！ ステージ"
            + stageNumber
            + "の手数: "
            + moveCount
        );
    }

    private void SaveStageMoveCount()
    {
        string key = "Stage" + stageNumber + "MoveCount";

        PlayerPrefs.SetInt(key, moveCount);
        PlayerPrefs.Save();
    }

    private void SaveOriginalColors()
    {
        if (stageRenderers == null)
        {
            return;
        }

        originalColors = new Color[stageRenderers.Length];

        for (int i = 0; i < stageRenderers.Length; i++)
        {
            if (stageRenderers[i] != null)
            {
                originalColors[i] = stageRenderers[i].color;
            }
        }
    }

    private void SetStageUnclearedColor()
    {
        if (stageRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in stageRenderers)
        {
            if (renderer != null)
            {
                renderer.color = unclearedColor;
            }
        }
    }

    private void RestoreStageColors()
    {
        if (stageRenderers == null ||
            originalColors == null)
        {
            return;
        }

        for (int i = 0; i < stageRenderers.Length; i++)
        {
            if (stageRenderers[i] != null)
            {
                stageRenderers[i].color = originalColors[i];
            }
        }
    }
}