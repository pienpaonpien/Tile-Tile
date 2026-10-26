using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BrockGuide : MonoBehaviour
{
    [Header("--- 受け付けるブロック番号 ---")]
    public int targetBrockNum;

    [Header("--- 吸着判定の許容距離 ---")]
    public float snapDistance = 0.3f;

    [Header("--- 状態確認用 ---")]
    public bool isFilled;

    [Header("--- 完成後に通過不可にするCollider ---")]
    [SerializeField] private Collider2D guideCollider;

    [Header("Stage transition fade duration")]
    [SerializeField, Min(0f)] private float transitionFadeDuration = 0.5f;

    private Brock currentBrock;
    private Hand playerHand;
    private bool stageTransitionStarted;

    private void Start()
    {
        // Reset guide state saved during editing so each run starts fresh.
        isFilled = false;
        currentBrock = null;
        stageTransitionStarted = false;
        playerHand = FindFirstObjectByType<Hand>();

        if (guideCollider == null)
        {
            guideCollider = GetComponent<Collider2D>();
        }

        SetGuideBlocking(false);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isFilled)
        {
            return;
        }

        Brock brock = other.GetComponentInParent<Brock>();

        if (brock == null)
        {
            return;
        }

        if (brock.brockNum != targetBrockNum)
        {
            return;
        }

        if (brock.IsPlaced)
        {
            return;
        }

        Vector2 blockCenter = brock.BrockCollider2D != null
     ? (Vector2)brock.BrockCollider2D.bounds.center
     : (Vector2)brock.transform.position;

        Vector2 guideCenter = guideCollider != null
            ? (Vector2)guideCollider.bounds.center
            : (Vector2)transform.position;

        float distance = Vector2.Distance(
            blockCenter,
            guideCenter
        );

        if (distance > snapDistance)
        {
            return;
        }

        if (brock.IsHeld && playerHand != null)
        {
            playerHand.ForceReleaseForPlacement();
        }

        SnapBrock(brock);
    }

    private void SnapBrock(Brock brock)
    {
        if (brock == null || isFilled)
        {
            return;
        }

        currentBrock = brock;
        Vector3 targetPosition = guideCollider != null
     ? guideCollider.bounds.center
     : transform.position;

        if (currentBrock.BrockCollider2D != null)
        {
            Vector3 colliderOffset =
                currentBrock.BrockCollider2D.bounds.center
                - currentBrock.transform.position;

            targetPosition -= colliderOffset;
        }

        currentBrock.SetPlaced(targetPosition);

        isFilled = true;

        StageClearVisual stageAudio =
            FindFirstObjectByType<StageClearVisual>();
        if (stageAudio != null)
        {
            stageAudio.PlayTilePlacementSound();
        }

        SetGuideBlocking(true);

        TryAdvanceStage();

        Debug.Log(
            $"ブロック({brock.brockNum})が"
            + $"ガイド({targetBrockNum})に配置されました"
        );

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckClearConditionNow();
        }
    }

    private void TryAdvanceStage()
    {
        if (stageTransitionStarted)
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

        string currentSceneName = SceneManager.GetActiveScene().name;
        string nextSceneName = GetNextStageName(currentSceneName);
        if (nextSceneName == null)
        {
            return;
        }

        stageTransitionStarted = true;

        StageClearVisual clearVisual =
            FindFirstObjectByType<StageClearVisual>();
        if (clearVisual != null)
        {
            clearVisual.ShowClearImage();
        }

        if (nextSceneName.Length == 0)
        {
            Debug.Log("All four stages are complete.", this);
            return;
        }

        StartCoroutine(LoadNextStage(nextSceneName, clearVisual));
    }

    private string GetNextStageName(string currentSceneName)
    {
        switch (currentSceneName)
        {
            case "Stage1": return "Stage2";
            case "Stage2": return "Stage3";
            case "Stage3": return "Stage4";
            case "Stage4": return "Result";
            default: return null;
        }
    }

    private IEnumerator LoadNextStage(string sceneName, StageClearVisual clearVisual)
    {
        float clearImageWait = clearVisual != null
            ? clearVisual.DisplayDuration
            : 1.5f;
        yield return new WaitForSeconds(clearImageWait);

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "Cannot load next stage '" + sceneName
                + "'. Add it to the active Build Profile scene list.",
                this
            );
            yield break;
        }

        StageTransitionFade.LoadScene(sceneName, transitionFadeDuration);
    }

    private void SetGuideBlocking(bool blocking)
    {
        if (guideCollider == null)
        {
            return;
        }

        /*
         * 空いている間:
         * Triggerとして吸着判定に使用
         *
         * 完成後:
         * 通常Colliderにして通過不可にする
         */
        guideCollider.isTrigger = !blocking;
    }

    public void ResetGuide()
    {
        isFilled = false;
        currentBrock = null;

        SetGuideBlocking(false);
    }
}
