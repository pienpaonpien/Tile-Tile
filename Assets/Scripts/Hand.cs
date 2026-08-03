using UnityEngine;
using UnityEngine.InputSystem;

public class Hand : MonoBehaviour
{
    public Player playerScript;
    private Brock brockInRange;
    private Collider2D playerBodyCollider; // ★追加：プレイヤー本体の当たり判定

    void Start()
    {
        if (playerScript == null)
        {
            Debug.LogError("【エラー】親オブジェクトに Player スクリプトが見つかりません！");
        }
        else
        {
            // ★プレイヤー本体（親オブジェクト）のCollider2Dを取得
            playerBodyCollider = playerScript.GetComponent<BoxCollider2D>();
            if (playerBodyCollider == null)
            {
                Debug.LogWarning("【警告】Playerオブジェクトに Collider2D が見つかりません。押す動作の衝突回避ができません。");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("Collider_Brock"))
        {
            brockInRange = collision.GetComponentInParent<Brock>();
            Debug.Log("ブロックが範囲内: " + (brockInRange != null ? brockInRange.name : "見つからず"));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("Collider_Brock"))
        {
            Brock exited = collision.GetComponentInParent<Brock>();
            if (exited == brockInRange)
            {
                brockInRange = null;
            }
        }
    }

    void Update()
    {
        if (playerScript == null || brockInRange == null) return;

        if(Gamepad.current != null)
        {
            if (Gamepad.current.bButton.isPressed)
            {

                Grab();
            }
            else if (Gamepad.current.bButton.wasReleasedThisFrame)
            {

                Release();
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.Space))
            {
                //if (!playerScript.HoldBrock)
                //{
                Grab();
                //}
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {

                Release();
            }
        }

    }

    private void Grab()
    {
        playerScript.HoldBrock = true;
        playerScript.heldBrock = brockInRange;
        brockInRange.IsHeld = true; // ★追加：掴んだフラグON

        brockInRange.transform.SetParent(playerScript.transform);
        SetIgnoreCollision(brockInRange, true);

        Debug.Log("ブロックを掴みました: " + brockInRange.name);
    }

    private void Release()
    {
        playerScript.HoldBrock = false;

        if (brockInRange != null)
        {
            SetIgnoreCollision(brockInRange, false);
            brockInRange.transform.SetParent(null);
            brockInRange.IsHeld = false; // ★追加：掴んだフラグOFF
        }

        playerScript.heldBrock = null;
        Debug.Log("ブロックを離しました");
    }

    private void SetIgnoreCollision(Brock brock, bool ignore)
    {
        if (playerBodyCollider == null || brock == null) return;

        Collider2D brockCollider = brock.BrockCollider2D;
        if (brockCollider != null)
        {
            Physics2D.IgnoreCollision(playerBodyCollider, brockCollider, ignore);
        }
    }
}