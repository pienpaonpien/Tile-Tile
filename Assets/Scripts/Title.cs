using UnityEngine;
using UnityEngine.SceneManagement;

public class Title : MonoBehaviour
{
    public string nextSceneName;

    [SerializeField] private OpeningScript openingStory;
    [SerializeField] private SpriteRenderer titleScreenRenderer;

    private bool hasStarted;

    private void Awake()
    {
        if (titleScreenRenderer == null)
        {
            titleScreenRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (hasStarted)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Space) &&
            !Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            return;
        }

        if (openingStory != null)
        {
            hasStarted = true;

            if (titleScreenRenderer != null)
            {
                titleScreenRenderer.enabled = false;
            }

            openingStory.Play(OpeningScript.Part.Opening);
            return;
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            hasStarted = true;
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("遷移先の nextSceneName がインスペクターで設定されていません！");
        }
    }
}
