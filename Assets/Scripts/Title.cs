using UnityEngine;
using UnityEngine.SceneManagement;  

public class Title : MonoBehaviour
{
    public string nextSceneName; // 次のシーンの名前を指定するための変数
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)|| Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("遷移先の nextSceneName がインスペクターで設定されていません！");
            }
        }
    }
}
