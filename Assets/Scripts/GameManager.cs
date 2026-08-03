using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("--- シーン内の全ガイドを自動収集 ---")]
    private BrockGuide[] allGuides;

    void Start()
    {
        allGuides = FindObjectsByType<BrockGuide>(FindObjectsSortMode.None);
    }

    void Update()
    {
        CheckClearCondition();
    }

    private void CheckClearCondition()
    {
        foreach (var guide in allGuides)
        {
            if (!guide.isFilled)
            {
                return; // 1つでも未配置のガイドがあればまだクリアではない
            }
        }

        OnAllBrocksPlaced();
    }

    private bool cleared = false;

    private void OnAllBrocksPlaced()
    {
        if (cleared) return; // 二重発火防止
        cleared = true;

        Debug.Log("ステージクリア！全てのブロックが正しい位置に配置されました");
        // ここにクリア演出、シーン遷移、UIの表示などを追加してください
    }
}