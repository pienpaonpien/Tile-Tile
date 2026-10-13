using System.Collections;
using TMPro;
using UnityEngine;

public class StoryDialogue : MonoBehaviour
{
    [Header("セリフ表示")]
    [SerializeField] private TMP_Text dialogueText;

    [Header("セリフ表示用パネル")]
    [SerializeField] private GameObject dialoguePanel;

    [Header("セリフ一覧")]
    [SerializeField] private string[] dialogueLines;

    [Header("1秒間に表示する文字数")]
    [SerializeField, Min(1f)]
    private float charactersPerSecond = 30f;

    private int currentLineIndex = -1;

    private Coroutine typingCoroutine;

    private bool isTyping = false;


    private void Start()
    {
        // テキストが設定されていない場合は処理を停止
        if (dialogueText == null)
        {
            Debug.LogError(
                "DialogueTextが設定されていません。",
                this
            );

            enabled = false;
            return;
        }

        // セリフが存在しない場合は処理を停止
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning(
                "セリフが設定されていません。",
                this
            );

            enabled = false;
            return;
        }

        // 最初のセリフを表示
        ShowNextLine();
    }


    private void Update()
    {
        // EnterキーまたはテンキーのEnterキー
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isTyping)
            {
                // 文字を表示している途中なら、一瞬で全文を表示
                CompleteCurrentLine();
            }
            else
            {
                // 全文が表示されているなら、次のセリフへ
                ShowNextLine();
            }
        }
    }


    private void ShowNextLine()
    {
        currentLineIndex++;

        // すべてのセリフを表示し終えた場合
        if (currentLineIndex >= dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        // 前の文字表示処理が残っている場合は停止
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // 次のセリフを1文字ずつ表示
        typingCoroutine = StartCoroutine(
            TypeLine(dialogueLines[currentLineIndex])
        );
    }


    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = line ?? "";

        // 最初は文字をすべて隠す
        dialogueText.maxVisibleCharacters = 0;

        // テキストの文字数を取得
        dialogueText.ForceMeshUpdate();

        int totalCharacters = dialogueText.textInfo.characterCount;

        isTyping = true;

        // 1文字ずつ表示する
        for (int i = 0; i < totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;

            yield return new WaitForSeconds(
                1f / Mathf.Max(charactersPerSecond, 1f)
            );
        }

        // 全文表示が完了
        dialogueText.maxVisibleCharacters = totalCharacters;

        isTyping = false;
        typingCoroutine = null;
    }


    private void CompleteCurrentLine()
    {
        // 文字表示のコルーチンを停止
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // 残りの文字をすべて表示
        dialogueText.maxVisibleCharacters =
            dialogueText.textInfo.characterCount;

        isTyping = false;
    }


    private void EndDialogue()
    {
        // パネルが設定されていれば非表示にする
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        else
        {
            dialogueText.text = "";
            enabled = false;
        }

        Debug.Log("ストーリー終了");
    }
}