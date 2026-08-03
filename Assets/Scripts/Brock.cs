using UnityEngine;

public class Brock : MonoBehaviour
{
    public static Brock Instance { get; private set; }
    private Player playerScript;

    [Header("--- ‰Ÿ‚µˆø‚«İ’è ---")]
    public bool canPush = true;
    public bool canPull = true;

    [Header("--- ƒuƒƒbƒN¯•Ê”Ô† ---")]
    public int brockNum;

    public BoxCollider2D BrockCollider2D;

    public bool IsHeld { get; set; } = false;
    public bool IsPlaced { get; set; } = false;

    void Start()
    {
        playerScript = FindFirstObjectByType<Player>();
        if (playerScript != null)
        {
            playerScript.HoldBrock = false;
        }
    }
    [Header("--- ˆÚ“®”»’è ---")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float checkDistance = 1.0f;

    public bool CanMove(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            BrockCollider2D.bounds.center,
            BrockCollider2D.bounds.size * 0.9f,
            0f,
            direction,
            checkDistance,
            obstacleLayer);

        return hit.collider == null;
    }
}
