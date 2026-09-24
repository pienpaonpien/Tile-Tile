using UnityEngine;

public class Brock : MonoBehaviour
{
    public static Brock Instance { get; private set; }

    [Header("--- 押し引き設定 ---")]
    public bool canPush = true;
    public bool canPull = true;

    [Header("--- ブロック識別番号 ---")]
    public int brockNum;

    [Header("--- ブロックのCollider ---")]
    public BoxCollider2D BrockCollider2D;

    [Header("--- 移動判定 ---")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float checkDistance = 0.1f;

    public bool IsHeld { get; set; }
    public bool IsPlaced { get; set; }

    public Vector3 InitialPosition { get; private set; }
    public Vector3 GrabStartPosition { get; private set; }

    private Transform initialParent;
    private Rigidbody2D rb;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (BrockCollider2D == null)
        {
            BrockCollider2D = GetComponentInChildren<BoxCollider2D>();
        }

        rb = GetComponent<Rigidbody2D>();

        InitialPosition = transform.position;
        GrabStartPosition = transform.position;
        initialParent = transform.parent;
    }

    public void BeginGrab()
    {
        if (IsPlaced)
        {
            return;
        }

        IsHeld = true;
        GrabStartPosition = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public float GetMovedDistance()
    {
        return Vector2.Distance(GrabStartPosition, transform.position);
    }

    public void ReturnToGrabPosition()
    {
        transform.SetParent(null);
        transform.position = GrabStartPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ResetBrock()
    {
        IsHeld = false;
        IsPlaced = false;

        transform.SetParent(initialParent);
        transform.position = InitialPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (BrockCollider2D != null)
        {
            BrockCollider2D.enabled = true;
        }
    }

    public void SetPlaced(Vector3 placedPosition)
    {
        IsHeld = false;
        IsPlaced = true;

        transform.SetParent(null);
        transform.position = placedPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    public bool CanMove(Vector2 direction)
    {
        if (BrockCollider2D == null)
        {
            return true;
        }

        RaycastHit2D hit = Physics2D.BoxCast(
            BrockCollider2D.bounds.center,
            BrockCollider2D.bounds.size * 0.9f,
            0f,
            direction.normalized,
            checkDistance,
            obstacleLayer
        );

        return hit.collider == null;
    }
}