using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [Header("--- 各ステージの手数表示 ---")]
    [SerializeField] private TMP_Text stage1Text;
    [SerializeField] private TMP_Text stage2Text;
    [SerializeField] private TMP_Text stage3Text;
    [SerializeField] private TMP_Text stage4Text;

    [Header("--- 合計手数表示 ---")]
    [SerializeField] private TMP_Text totalText;

    [Header("--- リザルトメッセージ ---")]
    [SerializeField] private TMP_Text resultMessageText;
    [SerializeField, TextArea(2, 4)]
    private string resultMessage = "すべてのステージをクリア！";

    private void Start()
    {
        int stage1 = PlayerPrefs.GetInt("Stage1MoveCount", 0);
        int stage2 = PlayerPrefs.GetInt("Stage2MoveCount", 0);
        int stage3 = PlayerPrefs.GetInt("Stage3MoveCount", 0);
        int stage4 = PlayerPrefs.GetInt("Stage4MoveCount", 0);

        int total = stage1 + stage2 + stage3 + stage4;

        if (stage1Text != null)
            stage1Text.text = stage1.ToString("00");

        if (stage2Text != null)
            stage2Text.text = stage2.ToString("00");

        if (stage3Text != null)
            stage3Text.text = stage3.ToString("00");

        if (stage4Text != null)
            stage4Text.text = stage4.ToString("00");

        if (totalText != null)
            totalText.text = total.ToString("000");

        if (resultMessageText != null)
            resultMessageText.text = resultMessage;
    }

    // 手数の記録を削除する
    public void DeleteAllResultData()
    {
        PlayerPrefs.DeleteKey("Stage1MoveCount");
        PlayerPrefs.DeleteKey("Stage2MoveCount");
        PlayerPrefs.DeleteKey("Stage3MoveCount");
        PlayerPrefs.DeleteKey("Stage4MoveCount");

        PlayerPrefs.Save();
    }

    // 最初から遊ぶ
    public void Retry()
    {
        DeleteAllResultData();
        SceneManager.LoadScene("Stage1");
    }

    // タイトル画面へ戻る
    public void ReturnToTitle()
    {
        SceneManager.LoadScene("Title");
    }
}
