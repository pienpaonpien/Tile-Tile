using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class JapaneseTMPFontBootstrap : MonoBehaviour
{
    [SerializeField] private string fontFamily = "Noto Sans JP";
    [SerializeField] private string fontStyle = "Regular";
    [SerializeField, Min(1)] private int samplingPointSize = 90;

    private void Awake()
    {
        TMP_Text text = GetComponent<TMP_Text>();
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
            fontFamily,
            fontStyle,
            samplingPointSize
        );

        if (font == null)
        {
            Debug.LogWarning(
                "Noto Sans JP is not installed. Assign a Japanese TMP font asset to DialogueText.",
                this
            );
            return;
        }

        text.font = font;
    }
}
