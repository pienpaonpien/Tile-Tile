using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageReset : MonoBehaviour
{
    [Header("--- リセットに必要な長押し時間 ---")]
    [SerializeField] private float holdTime = 1.5f;

    [Header("--- 暗転用Image ---")]
    [SerializeField] private Image fadeImage;

    [Header("--- 暗転時間 ---")]
    [SerializeField] private float fadeDuration = 0.5f;

    private float currentHoldTime;
    private bool isResetting;

    private void Start()
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;

            fadeImage.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isResetting)
        {
            return;
        }

        if (IsResetButtonPressed())
        {
            currentHoldTime += Time.deltaTime;

            if (currentHoldTime >= holdTime)
            {
                StartCoroutine(ResetStageCoroutine());
            }
        }
        else
        {
            currentHoldTime = 0f;
        }
    }

    private bool IsResetButtonPressed()
    {
        bool keyboardPressed =
            Input.GetKey(KeyCode.R);

        bool gamepadPressed =
            Gamepad.current != null &&
            Gamepad.current.yButton.isPressed;

        return keyboardPressed || gamepadPressed;
    }

    private IEnumerator ResetStageCoroutine()
    {
        isResetting = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetMoveCount();
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);

            Color color = fadeImage.color;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;

                color.a = Mathf.Clamp01(
                    elapsedTime / fadeDuration
                );

                fadeImage.color = color;

                yield return null;
            }
        }

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.name
        );
    }

    public float GetResetProgress()
    {
        if (holdTime <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(
            currentHoldTime / holdTime
        );
    }
}