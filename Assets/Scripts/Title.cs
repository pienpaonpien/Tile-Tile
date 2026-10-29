using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Title : MonoBehaviour
{
    public string nextSceneName;

    [SerializeField] private OpeningScript openingStory;
    [SerializeField] private SpriteRenderer titleScreenRenderer;
    [SerializeField] private Texture2D[] titleFrames = new Texture2D[3];
    [SerializeField, Min(0.1f)] private float frameHoldSeconds = 0.7f;
    [SerializeField, Min(0.05f)] private float transitionSeconds = 0.35f;
    [SerializeField, Min(0f)] private float finalHoldSeconds = 0.8f;

    private bool hasStarted;
    private RawImage[] titleLayers;

    private void Awake()
    {
        if (titleScreenRenderer == null)
        {
            titleScreenRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        if (titleScreenRenderer != null)
        {
            titleScreenRenderer.enabled = false;
        }

        if (openingStory != null)
        {
            openingStory.ShowTitleBackground();
            CreateTitleLayers();
        }
    }

    private void Update()
    {
        if (hasStarted)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Return) &&
            !Input.GetKeyDown(KeyCode.KeypadEnter) &&
            !Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            return;
        }

        hasStarted = true;

        if (openingStory != null)
        {
            if (HasTitleFrames())
            {
                StartCoroutine(PlayTitleSequence());
            }
            else
            {
                Debug.LogWarning("[Title] TitleA / TitleB / TitleC を設定できないため、オープニングを開始します。", this);
                openingStory.Play(OpeningScript.Part.Opening);
            }

            return;
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("遷移先の nextSceneName がインスペクターで設定されていません！");
        }
    }

    private bool HasTitleFrames()
    {
        return titleLayers != null &&
               titleFrames != null && titleFrames.Length >= 3 &&
               titleFrames[0] != null && titleFrames[1] != null && titleFrames[2] != null;
    }

    private void CreateTitleLayers()
    {
        RectTransform canvasRoot = openingStory.TitleCanvasRoot;
        if (canvasRoot == null)
        {
            Debug.LogWarning("[Title] StoryCanvas が見つからないため、タイトル画像を作成できません。", this);
            return;
        }

        titleLayers = new RawImage[2];
        for (int i = 0; i < titleLayers.Length; i++)
        {
            GameObject layerObject = new GameObject(
                "TitleFrameOverlay" + (i + 1),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage)
            );
            layerObject.transform.SetParent(canvasRoot, false);

            RectTransform rectTransform = (RectTransform)layerObject.transform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            RawImage image = layerObject.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            layerObject.SetActive(false);
            titleLayers[i] = image;
        }
    }

    private IEnumerator PlayTitleSequence()
    {
        int activeLayer = 0;
        SetFrame(titleLayers[activeLayer], titleFrames[0]);
        SetAlpha(titleLayers[activeLayer], 1f);
        titleLayers[activeLayer].gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(frameHoldSeconds);

        for (int frame = 1; frame < 3; frame++)
        {
            int nextLayer = 1 - activeLayer;
            SetFrame(titleLayers[nextLayer], titleFrames[frame]);
            SetAlpha(titleLayers[nextLayer], 0f);
            titleLayers[nextLayer].gameObject.SetActive(true);

            yield return CrossFade(activeLayer, nextLayer);

            titleLayers[activeLayer].gameObject.SetActive(false);
            activeLayer = nextLayer;
            yield return new WaitForSecondsRealtime(frameHoldSeconds);
        }

        yield return new WaitForSecondsRealtime(finalHoldSeconds);
        titleLayers[0].gameObject.SetActive(false);
        titleLayers[1].gameObject.SetActive(false);
        openingStory.Play(OpeningScript.Part.Opening);
    }

    private void SetFrame(RawImage image, Texture2D texture)
    {
        image.texture = texture;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        image.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        image.rectTransform.SetAsLastSibling();
    }

    private IEnumerator CrossFade(int fromLayer, int toLayer)
    {
        float duration = Mathf.Max(0.05f, transitionSeconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - 2f * progress);
            SetAlpha(titleLayers[fromLayer], 1f - smoothProgress);
            SetAlpha(titleLayers[toLayer], smoothProgress);
            yield return null;
        }

        SetAlpha(titleLayers[fromLayer], 0f);
        SetAlpha(titleLayers[toLayer], 1f);
    }

    private void SetAlpha(RawImage image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
