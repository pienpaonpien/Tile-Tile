using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [Header("--- 各ステージの手数表示 ---")]
    [SerializeField] private TMP_Text stage1Text;
    [SerializeField] private TMP_Text stage2Text;
    [SerializeField] private TMP_Text stage3Text;
    [SerializeField] private TMP_Text stage4Text;

    [Header("--- 合計手数表示 ---")]
    [SerializeField] private TMP_Text totalText;

    [Header("--- リザルトメッセージ ---")]
    [SerializeField] private TMP_Text resultMessageText;
    [SerializeField, TextArea(2, 4)]
    private string resultMessage = "すべてのステージをクリア！";

    [Header("--- リザルト画面の素材 ---")]
    [SerializeField] private Sprite resultBackground;
    [SerializeField] private AudioClip resultBgm;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private string returnPrompt = "ENTER TO RETURN TO TITLE";

    private AudioSource resultAudioSource;
    private bool isReturning;

    private void Start()
    {
        EnsureAudioSource();
        CreateDefaultResultUI();

        int stage1 = PlayerPrefs.GetInt("Stage1MoveCount", 0);
        int stage2 = PlayerPrefs.GetInt("Stage2MoveCount", 0);
        int stage3 = PlayerPrefs.GetInt("Stage3MoveCount", 0);
        int stage4 = PlayerPrefs.GetInt("Stage4MoveCount", 0);

        int total = stage1 + stage2 + stage3 + stage4;

        if (stage1Text != null)
            stage1Text.text = stage1.ToString("00");

        if (stage2Text != null)
            stage2Text.text = stage2.ToString("00");

        if (stage3Text != null)
            stage3Text.text = stage3.ToString("00");

        if (stage4Text != null)
            stage4Text.text = stage4.ToString("00");

        if (totalText != null)
            totalText.text = total.ToString("000");

        if (resultMessageText != null)
            resultMessageText.text = resultMessage;

        if (resultBgm != null)
        {
            resultAudioSource.clip = resultBgm;
            resultAudioSource.loop = true;
            resultAudioSource.Play();
        }
    }

    private void Update()
    {
        if (!isReturning && EnterPressed())
        {
            ReturnToTitle();
        }
    }

    private void EnsureAudioSource()
    {
        resultAudioSource = GetComponent<AudioSource>();
        if (resultAudioSource == null)
            resultAudioSource = gameObject.AddComponent<AudioSource>();

        resultAudioSource.playOnAwake = false;
        resultAudioSource.spatialBlend = 0f;
    }

    private void CreateDefaultResultUI()
    {
        if (stage1Text != null || stage2Text != null || stage3Text != null ||
            stage4Text != null || totalText != null || resultMessageText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("ResultCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject backgroundObject = new GameObject(
            "ResultBackground",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        RectTransform backgroundRect = (RectTransform)backgroundObject.transform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.sprite = resultBackground;
        backgroundImage.color = resultBackground != null ? Color.white : new Color(0.02f, 0.06f, 0.1f, 1f);
        backgroundImage.raycastTarget = false;

        resultMessageText = CreateText(
            canvasObject.transform, "ResultMessage", resultMessage,
            42f, new Vector2(0f, 175f), new Vector2(1000f, 80f), TextAlignmentOptions.Center
        );

        stage1Text = CreateStageRow(canvasObject.transform, "Stage1", "STAGE 1", 72f);
        stage2Text = CreateStageRow(canvasObject.transform, "Stage2", "STAGE 2", 18f);
        stage3Text = CreateStageRow(canvasObject.transform, "Stage3", "STAGE 3", -36f);
        stage4Text = CreateStageRow(canvasObject.transform, "Stage4", "STAGE 4", -90f);

        CreateText(
            canvasObject.transform, "TotalLabel", "TOTAL",
            40f, new Vector2(-120f, -180f), new Vector2(360f, 64f), TextAlignmentOptions.Right
        );
        totalText = CreateText(
            canvasObject.transform, "TotalMoves", "000",
            48f, new Vector2(130f, -180f), new Vector2(200f, 72f), TextAlignmentOptions.Left
        );
        CreateText(
            canvasObject.transform, "ReturnPrompt", returnPrompt,
            30f, new Vector2(0f, -390f), new Vector2(900f, 64f), TextAlignmentOptions.Center
        );
    }

    private TMP_Text CreateStageRow(Transform parent, string name, string label, float y)
    {
        CreateText(
            parent, name + "Label", label,
            32f, new Vector2(-120f, y), new Vector2(360f, 52f), TextAlignmentOptions.Right
        );
        return CreateText(
            parent, name + "Moves", "00",
            34f, new Vector2(130f, y), new Vector2(200f, 56f), TextAlignmentOptions.Left
        );
    }

    private TMP_Text CreateText(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        Vector2 position,
        Vector2 size,
        TextAlignmentOptions alignment
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = (RectTransform)textObject.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private bool EnterPressed()
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            pressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        return pressed;
    }

    // 手数の記録を削除する
    public void DeleteAllResultData()
    {
        PlayerPrefs.DeleteKey("Stage1MoveCount");
        PlayerPrefs.DeleteKey("Stage2MoveCount");
        PlayerPrefs.DeleteKey("Stage3MoveCount");
        PlayerPrefs.DeleteKey("Stage4MoveCount");

        PlayerPrefs.Save();
    }

    // 最初から遊ぶ
    public void Retry()
    {
        DeleteAllResultData();
        SceneManager.LoadScene("Stage1");
    }

    // タイトル画面へ戻る
    public void ReturnToTitle()
    {
        if (isReturning)
            return;

        isReturning = true;
        StartCoroutine(ReturnToTitleAfterConfirm());
    }

    private IEnumerator ReturnToTitleAfterConfirm()
    {
        if (confirmClip != null && resultAudioSource != null)
        {
            resultAudioSource.PlayOneShot(confirmClip);
            yield return new WaitForSecondsRealtime(confirmClip.length);
        }

        if (resultAudioSource != null)
            resultAudioSource.Stop();

        SceneManager.LoadScene("Title");
    }
}
