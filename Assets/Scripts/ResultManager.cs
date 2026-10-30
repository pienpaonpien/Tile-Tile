using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultManager : MonoBehaviour
{
    private const float StageSummaryFontSize = 56f;
    private const int MoveCountFontSize = 44;
    private const float PromptFadeSeconds = 0.32f;
    private const float PromptHoldSeconds = 0.18f;
    private const float PromptDimAlpha = 0.18f;

    [Header("--- 各ステージの手数表示 ---")]
    [SerializeField] private TMP_Text stage1Text;
    [SerializeField] private TMP_Text stage2Text;
    [SerializeField] private TMP_Text stage3Text;
    [SerializeField] private TMP_Text stage4Text;

    [Header("--- 合計手数表示 ---")]
    //[SerializeField] private TMP_Text totalText;

    [Header("--- リザルトメッセージ ---")]
    [SerializeField] private TMP_Text resultMessageText;
    [SerializeField, TextArea(2, 4)]
    private string resultMessage = "手数";

    [Header("--- リザルト画面の素材 ---")]
    [SerializeField] private Sprite resultBackground;
    [SerializeField] private AudioClip resultBgm;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private string returnPrompt = "-ENTER TO RETURN  TO TITLE-";

    private AudioSource resultAudioSource;
    private CanvasGroup returnPromptCanvasGroup;
    private Coroutine returnPromptBlinkCoroutine;
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
            SetStageSummary(stage1Text, 1, stage1);

        if (stage2Text != null)
            SetStageSummary(stage2Text, 2, stage2);

        if (stage3Text != null)
            SetStageSummary(stage3Text, 3, stage3);

        if (stage4Text != null)
            SetStageSummary(stage4Text, 4, stage4);

        if (returnPromptCanvasGroup == null)
        {
            GameObject promptObject = GameObject.Find("ReturnPrompt");
            if (promptObject != null)
            {
                returnPromptCanvasGroup = promptObject.GetComponent<CanvasGroup>();
                if (returnPromptCanvasGroup == null)
                    returnPromptCanvasGroup = promptObject.AddComponent<CanvasGroup>();
            }
        }

        if (returnPromptCanvasGroup != null)
        {
            returnPromptCanvasGroup.alpha = 1f;
            returnPromptCanvasGroup.interactable = false;
            returnPromptCanvasGroup.blocksRaycasts = false;
            returnPromptBlinkCoroutine = StartCoroutine(BlinkReturnPrompt());
        }

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
            stage4Text != null || resultMessageText != null)
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

        GameObject letterboxObject = new GameObject(
            "ResultLetterbox",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        letterboxObject.transform.SetParent(canvasObject.transform, false);
        RectTransform letterboxRect = (RectTransform)letterboxObject.transform;
        letterboxRect.anchorMin = Vector2.zero;
        letterboxRect.anchorMax = Vector2.one;
        letterboxRect.offsetMin = Vector2.zero;
        letterboxRect.offsetMax = Vector2.zero;
        Image letterboxImage = letterboxObject.GetComponent<Image>();
        letterboxImage.color = new Color(12f / 255f, 26f / 255f, 37f / 255f, 1f);
        letterboxImage.raycastTarget = false;

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
        backgroundImage.color = Color.white;
        backgroundImage.preserveAspect = true;
        backgroundImage.raycastTarget = false;

        stage4Text = CreateText(
            canvasObject.transform, "Stage4Summary", FormatStageSummary(4, 0),
            StageSummaryFontSize, new Vector2(-698f, 405f), new Vector2(430f, 140f), TextAlignmentOptions.Left
        );
        stage3Text = CreateText(
            canvasObject.transform, "Stage3Summary", FormatStageSummary(3, 0),
            StageSummaryFontSize, new Vector2(252f, 405f), new Vector2(430f, 140f), TextAlignmentOptions.Left
        );
        stage1Text = CreateText(
            canvasObject.transform, "Stage1Summary", FormatStageSummary(1, 0),
            StageSummaryFontSize, new Vector2(-698f, -110f), new Vector2(430f, 140f), TextAlignmentOptions.Left
        );
        stage2Text = CreateText(
            canvasObject.transform, "Stage2Summary", FormatStageSummary(2, 0),
            StageSummaryFontSize, new Vector2(-145f, -110f), new Vector2(430f, 140f), TextAlignmentOptions.Left
        );

        TMP_Text returnPromptText = CreateText(
            canvasObject.transform, "ReturnPrompt", returnPrompt,
            36f, new Vector2(0f, -350f), new Vector2(1000f, 64f), TextAlignmentOptions.Center
        );
        RectTransform returnPromptRect = (RectTransform)returnPromptText.transform;
        returnPromptRect.anchorMin = new Vector2(0f, 0.5f);
        returnPromptRect.anchorMax = new Vector2(1f, 0.5f);
        returnPromptRect.pivot = new Vector2(0.5f, 0.5f);
        returnPromptRect.sizeDelta = new Vector2(0f, 64f);
        returnPromptRect.anchoredPosition = new Vector2(0f, -350f);

        returnPromptCanvasGroup = returnPromptText.gameObject.AddComponent<CanvasGroup>();
        returnPromptCanvasGroup.alpha = 1f;
        returnPromptCanvasGroup.interactable = false;
        returnPromptCanvasGroup.blocksRaycasts = false;
    }

    private static string FormatStageSummary(int stage, int moves)
    {
        return $"ステージ{stage}\n<size={MoveCountFontSize}>手数: {moves}</size>";
    }

    private static void SetStageSummary(TMP_Text text, int stage, int moves)
    {
        text.enableAutoSizing = false;
        text.fontSize = StageSummaryFontSize;
        text.text = FormatStageSummary(stage, moves);
    }

    private IEnumerator BlinkReturnPrompt()
    {
        while (!isReturning && returnPromptCanvasGroup != null)
        {
            yield return FadeReturnPrompt(PromptDimAlpha);
            yield return new WaitForSecondsRealtime(PromptHoldSeconds);
            yield return FadeReturnPrompt(1f);
            yield return new WaitForSecondsRealtime(PromptHoldSeconds);
        }
    }

    private IEnumerator FadeReturnPrompt(float targetAlpha)
    {
        if (returnPromptCanvasGroup == null)
            yield break;

        float startAlpha = returnPromptCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < PromptFadeSeconds && returnPromptCanvasGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / PromptFadeSeconds);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            returnPromptCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothProgress);
            yield return null;
        }

        if (returnPromptCanvasGroup != null)
            returnPromptCanvasGroup.alpha = targetAlpha;
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
        rectTransform.pivot = alignment == TextAlignmentOptions.Left
            ? new Vector2(0f, 0.5f)
            : new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        textObject.AddComponent<JapaneseTMPFontBootstrap>();
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
        if (returnPromptBlinkCoroutine != null)
        {
            StopCoroutine(returnPromptBlinkCoroutine);
            returnPromptBlinkCoroutine = null;
        }

        if (returnPromptCanvasGroup != null)
            returnPromptCanvasGroup.alpha = 1f;

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
