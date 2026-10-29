using System;
using System.IO;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class JapaneseTMPFontBootstrap : MonoBehaviour
{
    [SerializeField] private string fontFilePath = "%LOCALAPPDATA%/Microsoft/Windows/Fonts/KH-Dot-Akihabara-16.ttf";
    [SerializeField] private string fontFamily = "KH Dot Akihabara 16";
    [SerializeField] private string fontStyle = "Regular";
    [SerializeField, Min(1)] private int samplingPointSize = 90;

    private static TMP_FontAsset sharedFont;

    private void Awake()
    {
        ApplyFont();
    }

    private void OnEnable()
    {
        // Dialogue UI is initially inactive, so make sure it is initialized when shown as well.
        ApplyFont();
    }

    public void EnsureFont()
    {
        ApplyFont();
    }

    private void ApplyFont()
    {
        TMP_Text text = GetComponent<TMP_Text>();
        if (text == null)
        {
            return;
        }

        TMP_FontAsset font = LoadFont();
        if (font == null)
        {
            Debug.LogError(
                "KH Dot Akihabara 16 could not be loaded. Check the font file path or install the font.",
                this
            );
            return;
        }

        text.font = font;
        text.fontSharedMaterial = font.material;
        text.enabled = true;
        text.ForceMeshUpdate();
    }

    private TMP_FontAsset LoadFont()
    {
        if (sharedFont != null)
        {
            return sharedFont;
        }

        string resolvedFontPath = Environment.ExpandEnvironmentVariables(fontFilePath);
        if (!string.IsNullOrWhiteSpace(resolvedFontPath) && File.Exists(resolvedFontPath))
        {
            try
            {
                sharedFont = TMP_FontAsset.CreateFontAsset(
                    resolvedFontPath,
                    0,
                    samplingPointSize,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning("KH Dot Akihabara 16 could not be loaded from file: " + exception.Message, this);
            }
        }

        if (sharedFont == null && !string.IsNullOrWhiteSpace(fontFamily))
        {
            try
            {
                sharedFont = TMP_FontAsset.CreateFontAsset(fontFamily, fontStyle, samplingPointSize);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("KH Dot Akihabara 16 could not be loaded by family name: " + exception.Message, this);
            }
        }

        return sharedFont;
    }
}
