using UnityEngine;

public class BrockGuide : MonoBehaviour
{
    [Header("--- このガイドが受け付けるブロック番号 ---")]
    public int targetBrockNum;

    [Header("--- 吸着判定の許容距離 ---")]
    public float snapDistance = 0.3f; // このガイドの中心からどれだけ離れていたら自動吸着するか

    [Header("--- 状態確認用（Inspectorで見えるだけ、いじらない） ---")]
    public bool isFilled = false; // 現在このガイドが正しいブロックで埋まっているか

    private Brock currentBrock; // 今このガイドに吸着しているブロック

    private void OnTriggerStay2D(Collider2D other)
    {
        // 触れた相手がブロックの実体コライダーかどうか
        Brock brock = other.GetComponentInParent<Brock>();
        if (brock == null) return;

        // ブロック番号が合わない、または既に掴まれている最中は吸着しない
        if (brock.brockNum != targetBrockNum) return;
        if (brock.IsHeld) return; // 掴んでいる間は吸着させない（操作性のため）
        if (brock.IsPlaced) return; // 既に配置済みなら何もしない

        float distance = Vector2.Distance(brock.transform.position, transform.position);

        if (distance <= snapDistance)
        {
            SnapBrock(brock);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Brock brock = other.GetComponentInParent<Brock>();
        if (brock == null) return;

        if (brock == currentBrock)
        {
            brock.IsPlaced = false;
            currentBrock = null;
            isFilled = false;
        }
    }

    private void SnapBrock(Brock brock)
    {
        brock.transform.position = transform.position;
        brock.IsPlaced = true;
        currentBrock = brock;
        isFilled = true;

        Debug.Log($"ブロック({brock.brockNum})がガイド({targetBrockNum})に配置されました");
    }
}