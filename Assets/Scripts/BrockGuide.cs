using UnityEngine;

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

    private Brock currentBrock;
    private Hand playerHand;

    private void Start()
    {
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

        float distance = Vector2.Distance(
            brock.transform.position,
            transform.position
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
        currentBrock.SetPlaced(transform.position);

        isFilled = true;

        SetGuideBlocking(true);

        Debug.Log(
            $"ブロック({brock.brockNum})が"
            + $"ガイド({targetBrockNum})に配置されました"
        );

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckClearConditionNow();
        }
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