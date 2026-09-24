using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("--- 現在掴んでいるブロック ---")]
    public Brock heldBrock;

    [Header("--- プレイヤーの移動速度 ---")]
    public float PlayerMoveSpeed = 2f;

    [Header("--- ゲーム時間 ---")]
    public static float GameTimer;

    [Header("--- 手数カウント ---")]
    public int MoveCount;

    [Header("--- 現在の方向 ---")]
    public int currentDir;

    [Header("--- ブロックを掴んでいるか ---")]
    [SerializeField]
    private bool _holdBrock;

    [Header("--- 手の当たり判定 ---")]
    [SerializeField]
    private GameObject handCollider;

    [Header("--- 手の位置（Playerからの距離） ---")]
    [SerializeField]
    private float handDistance = 0.5f;

    [Header("--- 左向きのときにFlip Xをオンにする ---")]
    [Tooltip("元の絵が右向きならオン、左向きならオフ。逆に反転する場合はチェックを切り替える")]
    [SerializeField]
    private bool flipXWhenFacingLeft = true;

    private SpriteRenderer spriteRenderer;

    public float inputH;
    public float inputV;

    private Animator anim;
    private Rigidbody2D rb;

    // 1=左、2=右、3=上、4=下
    private int lastDirection = 1;

    // 0=なし、1=左右、2=上下
    private int lockedAxis = 0;

    /// <summary>
    /// 現在向いている方向（ワールド座標）。
    /// Transformのスケール反転の影響を受けない。
    /// </summary>
    public Vector2 FacingVector
    {
        get
        {
            switch (lastDirection)
            {
                case 1:
                    return Vector2.left;

                case 2:
                    return Vector2.right;

                case 3:
                    return Vector2.up;

                case 4:
                    return Vector2.down;

                default:
                    return Vector2.left;
            }
        }
    }

    /// <summary>
    /// Playerの中心から手までのワールド座標でのずれ。
    /// スケールの大きさは反映し、符号（左右反転）は無視する。
    /// </summary>
    public Vector2 HandWorldOffset
    {
        get
        {
            Vector3 scale = transform.lossyScale;
            Vector2 dir = FacingVector;

            return new Vector2(
                dir.x * handDistance * Mathf.Abs(scale.x),
                dir.y * handDistance * Mathf.Abs(scale.y)
            );
        }
    }

    public bool HoldBrock
    {
        get
        {
            return _holdBrock;
        }

        set
        {
            bool wasHolding = _holdBrock;
            _holdBrock = value;

            if (anim != null)
            {
                anim.SetBool("HoldBrock", value);
            }

            if (!wasHolding && value)
            {
                if (lastDirection == 1 || lastDirection == 2)
                {
                    lockedAxis = 1;
                }
                else
                {
                    lockedAxis = 2;
                }
            }
            else if (wasHolding && !value)
            {
                lockedAxis = 0;
            }
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "【エラー】PlayerにSpriteRendererがありません。"
            );
        }

        if (anim == null)
        {
            Debug.LogError(
                "【エラー】PlayerにAnimatorがありません。"
            );
        }

        if (rb == null)
        {
            Debug.LogError(
                "【エラー】PlayerにRigidbody2Dがありません。"
            );
        }

        if (handCollider == null)
        {
            Transform handTransform =
                transform.Find("HandCollider");

            if (handTransform != null)
            {
                handCollider = handTransform.gameObject;
            }
        }

        if (handCollider == null)
        {
            Debug.LogError(
                "【エラー】Playerの子にHandColliderがありません。"
            );
        }
    }

    private void Update()
    {
        /*
         * UnityEngine.Inputと明記することで、
         * UnityEngine.Windows.Inputとの競合を防ぎます。
         */
        inputH =
            UnityEngine.Input.GetAxisRaw("Horizontal");

        inputV =
            UnityEngine.Input.GetAxisRaw("Vertical");

        // 斜め移動を防止する
        if (inputH != 0f)
        {
            inputV = 0f;
        }

        if (HoldBrock)
        {
            ApplyAxisRestriction();
            ApplyPushPullRestriction();
        }

        UpdateDirection();
        UpdateAnimation();
        UpdateHandPosition();
    }

    private void ApplyAxisRestriction()
    {
        if (lockedAxis == 1)
        {
            inputV = 0f;
        }
        else if (lockedAxis == 2)
        {
            inputH = 0f;
        }
    }

    private void ApplyPushPullRestriction()
    {
        if (heldBrock == null)
        {
            return;
        }

        bool tryingToPush = false;
        bool tryingToPull = false;

        switch (lockedAxis)
        {
            case 1:
                if (lastDirection == 1)
                {
                    tryingToPush = inputH < 0f;
                    tryingToPull = inputH > 0f;
                }
                else if (lastDirection == 2)
                {
                    tryingToPush = inputH > 0f;
                    tryingToPull = inputH < 0f;
                }

                break;

            case 2:
                if (lastDirection == 3)
                {
                    tryingToPush = inputV > 0f;
                    tryingToPull = inputV < 0f;
                }
                else if (lastDirection == 4)
                {
                    tryingToPush = inputV < 0f;
                    tryingToPull = inputV > 0f;
                }

                break;
        }

        if (tryingToPush && !heldBrock.canPush)
        {
            StopMovementInput();
        }

        if (tryingToPull && !heldBrock.canPull)
        {
            StopMovementInput();
        }
    }

    private void StopMovementInput()
    {
        inputH = 0f;
        inputV = 0f;
    }

    private void UpdateDirection()
    {
        currentDir = 0;

        if (inputH != 0f || inputV != 0f)
        {
            UpdateMovingDirection();
        }
        else
        {
            UpdateIdleDirection();
        }
    }

    private void UpdateMovingDirection()
    {
        if (inputH != 0f)
        {
            if (inputH > 0f &&
                HoldBrock &&
                lastDirection == 1)
            {
                currentDir = 17;
            }
            else if (inputH < 0f &&
                     HoldBrock &&
                     lastDirection == 2)
            {
                currentDir = 18;
            }
            else if (inputH < 0f)
            {
                currentDir = HoldBrock ? 6 : 1;

                if (!HoldBrock)
                {
                    lastDirection = 1;
                }
            }
            else if (inputH > 0f)
            {
                currentDir = HoldBrock ? 5 : 2;

                if (!HoldBrock)
                {
                    lastDirection = 2;
                }
            }
        }
        else if (inputV != 0f)
        {
            if (inputV > 0f &&
                HoldBrock &&
                lastDirection == 4)
            {
                currentDir = 20;
            }
            else if (inputV < 0f &&
                     HoldBrock &&
                     lastDirection == 3)
            {
                currentDir = 19;
            }
            else if (inputV > 0f)
            {
                currentDir = HoldBrock ? 7 : 3;

                if (!HoldBrock)
                {
                    lastDirection = 3;
                }
            }
            else if (inputV < 0f)
            {
                currentDir = HoldBrock ? 8 : 4;

                if (!HoldBrock)
                {
                    lastDirection = 4;
                }
            }
        }
    }

    private void UpdateIdleDirection()
    {
        if (HoldBrock)
        {
            switch (lastDirection)
            {
                case 1:
                    currentDir = 10;
                    break;

                case 2:
                    currentDir = 9;
                    break;

                case 3:
                    currentDir = 11;
                    break;

                case 4:
                    currentDir = 12;
                    break;

                default:
                    currentDir = 10;
                    break;
            }
        }
        else
        {
            switch (lastDirection)
            {
                case 1:
                    currentDir = 13;
                    break;

                case 2:
                    currentDir = 14;
                    break;

                case 3:
                    currentDir = 15;
                    break;

                case 4:
                    currentDir = 16;
                    break;

                default:
                    currentDir = 13;
                    break;
            }
        }
    }

    private void UpdateAnimation()
    {
        if (anim == null)
        {
            return;
        }

        anim.SetInteger("Direction", currentDir);
    }

    private void UpdateHandPosition()
    {
        if (handCollider == null)
        {
            return;
        }

        /*
         * localPositionで設定すると、Playerのスケールが
         * マイナス（左右反転）のとき手が反対側に出てしまうため、
         * ワールド座標で位置を決める。
         */
        Vector3 handPosition =
            transform.position + (Vector3)HandWorldOffset;

        handCollider.transform.position = handPosition;
    }

    /*
     * Animatorがスケールを書き換えた後に実行されるLateUpdateで、
     * スケールのXを常にプラスに固定し、
     * 左右の反転はSpriteRendererのFlip Xで行う。
     */
    private void LateUpdate()
    {
        Vector3 scale = transform.localScale;

        if (scale.x < 0f)
        {
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        if (spriteRenderer == null)
        {
            return;
        }

        // 1=左、2=右のときだけ切り替え、上下のときは反転しない
        bool facingLeft = lastDirection == 1;
        bool facingRight = lastDirection == 2;

        if (facingLeft || facingRight)
        {
            spriteRenderer.flipX =
                (facingLeft == flipXWhenFacingLeft);
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 movement =
            new Vector2(inputH, inputV);

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        rb.linearVelocity =
            movement * PlayerMoveSpeed;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}