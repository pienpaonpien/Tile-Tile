using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StageTransitionFade : MonoBehaviour
{
    private const int FadeSortingOrder = 32767;
    private static StageTransitionFade instance;

    private Image fadeImage;
    private bool isTransitioning;

    public static void LoadScene(string sceneName, float fadeDuration)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        StageTransitionFade transition = GetOrCreate();
        if (transition == null || transition.isTransitioning)
        {
            return;
        }

        transition.StartCoroutine(
            transition.LoadSceneRoutine(
                sceneName,
                Mathf.Max(0f, fadeDuration)
            )
        );
    }

    private static StageTransitionFade GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject transitionObject = new GameObject(
            "StageTransitionFade"
        );
        DontDestroyOnLoad(transitionObject);
        return transitionObject.AddComponent<StageTransitionFade>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeOverlay();
    }

    private void CreateFadeOverlay()
    {
        GameObject canvasObject = new GameObject("FadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = FadeSortingOrder;

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform =
            imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
    }

    private IEnumerator LoadSceneRoutine(
        string sceneName,
        float fadeDuration
    )
    {
        isTransitioning = true;

        yield return FadeTo(1f, fadeDuration);

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError(
                "Could not start loading scene '" + sceneName + "'."
            );

            yield return FadeTo(0f, fadeDuration);
            isTransitioning = false;
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return FadeTo(0f, fadeDuration);

        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        float startingAlpha = fadeImage.color.a;

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            SetAlpha(
                Mathf.Lerp(
                    startingAlpha,
                    targetAlpha,
                    Mathf.Clamp01(elapsedTime / duration)
                )
            );

            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}
