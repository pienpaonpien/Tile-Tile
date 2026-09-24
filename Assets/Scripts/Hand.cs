using UnityEngine;
using UnityEngine.InputSystem;

public class Hand : MonoBehaviour
{
    [Header("--- Player ---")]
    public Player playerScript;

    [Header("--- 掴み判定の補助設定 ---")]
    [Tooltip("プレイヤー本体から向いている方向へ、どこまでブロックを探すか")]
    [SerializeField] private float grabReach = 0.35f;

    [Tooltip("手のColliderの範囲を、掴む瞬間だけ少し広げる量")]
    [SerializeField] private float handPadding = 0.1f;

    private Brock brockInRange;
    private Brock grabbedBrock;

    private Collider2D playerBodyCollider;
    private Collider2D handCollider;

    private readonly Collider2D[] overlapResults = new Collider2D[16];
    private readonly RaycastHit2D[] castResults = new RaycastHit2D[16];

    private void Start()
    {
        if (playerScript == null)
        {
            playerScript = GetComponentInParent<Player>();
        }

        if (playerScript == null)
        {
            Debug.LogError(
                "【エラー】HandColliderの親にPlayerがありません。"
            );

            enabled = false;
            return;
        }

        handCollider = GetComponent<Collider2D>();

        playerBodyCollider =
            playerScript.GetComponent<Collider2D>();

        if (playerBodyCollider == null)
        {
            Debug.LogWarning(
                "【警告】PlayerにCollider2Dがありません。"
            );
        }
    }

    private void Update()
    {
        if (playerScript == null)
        {
            return;
        }

        bool grabButtonDown =
            UnityEngine.Input.GetKeyDown(KeyCode.Space);

        bool grabButtonUp =
            UnityEngine.Input.GetKeyUp(KeyCode.Space);

        if (Gamepad.current != null)
        {
            grabButtonDown |=
                Gamepad.current.bButton.wasPressedThisFrame;

            grabButtonUp |=
                Gamepad.current.bButton.wasReleasedThisFrame;
        }

        if (grabButtonDown)
        {
            Grab();
        }

        if (grabButtonUp)
        {
            Release();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        DetectBrock(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (grabbedBrock == null)
        {
            DetectBrock(collision);
        }
    }

    private void DetectBrock(Collider2D collision)
    {
        Brock detectedBrock =
            collision.GetComponentInParent<Brock>();

        if (detectedBrock == null)
        {
            return;
        }

        if (detectedBrock.IsPlaced)
        {
            return;
        }

        if (grabbedBrock != null &&
            grabbedBrock != detectedBrock)
        {
            return;
        }

        brockInRange = detectedBrock;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Brock exitedBrock =
            collision.GetComponentInParent<Brock>();

        if (exitedBrock == null)
        {
            return;
        }

        if (exitedBrock == brockInRange &&
            exitedBrock != grabbedBrock)
        {
            brockInRange = null;
        }
    }

    private void Grab()
    {
        if (playerScript == null)
        {
            return;
        }

        if (grabbedBrock != null ||
            playerScript.HoldBrock)
        {
            return;
        }

        /*
         * 掴む瞬間に、Playerが向いている方向の前方を直接検索する。
         * 手のTransformの位置には頼らないので、
         * Playerのスケール反転（左右反転）の影響を受けない。
         * 見つからなかった時だけ、トリガーで検出した結果を使う。
         */
        Brock target = FindBrockInFront();

        if (target == null &&
            brockInRange != null &&
            !brockInRange.IsPlaced)
        {
            target = brockInRange;
        }

        if (target == null)
        {
            brockInRange = null;

            Debug.Log(
                "手の範囲内にブロックがありません。(向き: "
                + playerScript.FacingVector
                + ")"
            );

            return;
        }

        brockInRange = target;
        grabbedBrock = target;

        grabbedBrock.IsHeld = true;

        playerScript.heldBrock = grabbedBrock;
        playerScript.HoldBrock = true;

        grabbedBrock.transform.SetParent(
            playerScript.transform,
            true
        );

        SetIgnoreCollision(
            grabbedBrock,
            true
        );

        Debug.Log(
            "ブロックを掴みました: "
            + grabbedBrock.name
        );
    }

    // 手があるべき位置と検索範囲を求める
    private void GetSearchBox(
        out Vector2 handCenter,
        out Vector2 boxCenter,
        out Vector2 boxSize
    )
    {
        handCenter =
            (Vector2)playerScript.transform.position
            + playerScript.HandWorldOffset;

        boxCenter = handCenter;
        boxSize = Vector2.one * 0.4f;

        if (handCollider != null)
        {
            Bounds bounds = handCollider.bounds;

            // Colliderの中心が手のTransformからどれだけずれているか
            boxCenter =
                handCenter
                + (Vector2)(bounds.center - transform.position);

            boxSize = bounds.size;
        }

        boxSize += Vector2.one * handPadding;
    }

    private Brock FindBrockInFront()
    {
        // Transformの移動を物理演算側に反映させてから検索する
        Physics2D.SyncTransforms();

        Brock best = null;
        float bestDistance = float.MaxValue;

        Vector2 handCenter;
        Vector2 boxCenter;
        Vector2 boxSize;

        GetSearchBox(
            out handCenter,
            out boxCenter,
            out boxSize
        );

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();

        // ① 手があるべき位置の周辺にあるブロックを探す
        int overlapCount = Physics2D.OverlapBox(
            boxCenter,
            boxSize,
            0f,
            filter,
            overlapResults
        );

        for (int i = 0; i < overlapCount; i++)
        {
            EvaluateCandidate(
                overlapResults[i],
                handCenter,
                ref best,
                ref bestDistance
            );
        }

        // ② 見つからなければ、プレイヤー本体から向いている方向へ探す
        if (best == null && playerBodyCollider != null)
        {
            int castCount = playerBodyCollider.Cast(
                playerScript.FacingVector,
                filter,
                castResults,
                grabReach
            );

            for (int i = 0; i < castCount; i++)
            {
                EvaluateCandidate(
                    castResults[i].collider,
                    handCenter,
                    ref best,
                    ref bestDistance
                );
            }
        }

        return best;
    }

    private void EvaluateCandidate(
        Collider2D candidate,
        Vector2 origin,
        ref Brock best,
        ref float bestDistance
    )
    {
        if (candidate == null)
        {
            return;
        }

        Brock brock =
            candidate.GetComponentInParent<Brock>();

        if (brock == null || brock.IsPlaced)
        {
            return;
        }

        float distance = Vector2.Distance(
            origin,
            candidate.ClosestPoint(origin)
        );

        if (distance < bestDistance)
        {
            best = brock;
            bestDistance = distance;
        }
    }

    public void Release()
    {
        if (playerScript == null)
        {
            return;
        }

        if (grabbedBrock == null)
        {
            playerScript.heldBrock = null;
            playerScript.HoldBrock = false;
            return;
        }

        Brock releaseBrock = grabbedBrock;

        SetIgnoreCollision(
            releaseBrock,
            false
        );

        releaseBrock.transform.SetParent(
            null,
            true
        );

        releaseBrock.IsHeld = false;

        grabbedBrock = null;
        brockInRange = null;

        playerScript.heldBrock = null;
        playerScript.HoldBrock = false;

        Debug.Log(
            "ブロックを離しました: "
            + releaseBrock.name
        );
    }

    public void ForceReleaseForPlacement()
    {
        if (playerScript == null)
        {
            return;
        }

        if (grabbedBrock == null)
        {
            playerScript.heldBrock = null;
            playerScript.HoldBrock = false;
            return;
        }

        Brock releaseBrock = grabbedBrock;

        SetIgnoreCollision(
            releaseBrock,
            false
        );

        releaseBrock.transform.SetParent(
            null,
            true
        );

        releaseBrock.IsHeld = false;

        grabbedBrock = null;
        brockInRange = null;

        playerScript.heldBrock = null;
        playerScript.HoldBrock = false;

        Debug.Log(
            "配置位置に入ったため自動的に離しました。"
        );
    }

    private void SetIgnoreCollision(
        Brock brock,
        bool ignore
    )
    {
        if (playerBodyCollider == null ||
            brock == null)
        {
            return;
        }

        Collider2D brockCollider =
            brock.BrockCollider2D;

        if (brockCollider == null)
        {
            return;
        }

        Physics2D.IgnoreCollision(
            playerBodyCollider,
            brockCollider,
            ignore
        );
    }

    // 再生中にHandColliderを選択すると、掴み判定の検索範囲が黄色の枠で表示される
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || playerScript == null)
        {
            return;
        }

        Vector2 handCenter;
        Vector2 boxCenter;
        Vector2 boxSize;

        GetSearchBox(
            out handCenter,
            out boxCenter,
            out boxSize
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(
            boxCenter,
            new Vector3(boxSize.x, boxSize.y, 0.01f)
        );
    }
}