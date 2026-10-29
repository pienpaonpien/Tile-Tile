using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Stage4EndingTransition : MonoBehaviour
{
    [SerializeField] private string endingSceneName = "Ending";
    [SerializeField, Min(0f)] private float fallbackClearDelay = 2f;
    [SerializeField, Min(0f)] private float transitionFadeDuration = 0.5f;

    private bool transitionStarted;

    private void Update()
    {
        if (transitionStarted)
        {
            return;
        }

        BrockGuide[] guides = FindObjectsByType<BrockGuide>(FindObjectsSortMode.None);
        if (guides.Length == 0)
        {
            return;
        }

        foreach (BrockGuide guide in guides)
        {
            if (guide == null || !guide.isFilled)
            {
                return;
            }
        }

        transitionStarted = true;
        StageClearVisual clearVisual = FindFirstObjectByType<StageClearVisual>();
        StartCoroutine(LoadEnding(clearVisual));
    }

    private IEnumerator LoadEnding(StageClearVisual clearVisual)
    {
        float waitSeconds = clearVisual != null
            ? clearVisual.DisplayDuration
            : fallbackClearDelay;
        if (waitSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(waitSeconds);
        }

        if (!Application.CanStreamedLevelBeLoaded(endingSceneName))
        {
            Debug.LogError("Cannot load ending scene '" + endingSceneName + "'. Add it to the active Build Profile scene list.", this);
            yield break;
        }

        StageTransitionFade.LoadScene(endingSceneName, transitionFadeDuration);
    }
}
