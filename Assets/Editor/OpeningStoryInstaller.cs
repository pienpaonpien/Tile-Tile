using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OpeningStoryInstaller
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string Stage1ScenePath = "Assets/Scenes/Stage1.unity";
    private const string Stage2ScenePath = "Assets/Scenes/Stage2.unity";

    private struct SoundMapping
    {
        public string key;
        public string assetPath;

        public SoundMapping(string key, string assetPath)
        {
            this.key = key;
            this.assetPath = assetPath;
        }
    }

    [MenuItem("Tools/Story/Install opening and tutorial scenes")]
    private static void InstallAllScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        InstallScene(TitleScenePath, OpeningScript.Part.Opening, "Stage1", false);
        InstallScene(Stage1ScenePath, OpeningScript.Part.Stage1Tutorial, string.Empty, true);
        InstallScene(Stage2ScenePath, OpeningScript.Part.Stage2Tutorial, string.Empty, true);

        Scene titleScene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(titleScene);
        Selection.activeGameObject = GameObject.Find("StoryManager");

        Debug.Log("Title, Stage1, and Stage2 story sequences are installed.");
    }

    private static void InstallScene(string path, OpeningScript.Part part, string nextScene, bool playOnStart)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        Canvas canvas = GetOrCreateStoryCanvas();
        Image background = GetOrCreateImage("StoryBackground", canvas.transform);
        Image dialogueFrame = GetOrCreateImage("StoryDialogueFrame", canvas.transform);
        TextMeshProUGUI dialogueText = GetOrCreateText("StoryDialogueText", dialogueFrame.transform);
        TextMeshProUGUI nextIndicator = GetOrCreateText("StoryNextIndicator", dialogueFrame.transform);
        Image fadeImage = GetOrCreateImage("StoryFade", canvas.transform);

        ConfigureLayout(background, dialogueFrame, dialogueText, nextIndicator, fadeImage);

        GameObject managerObject = GameObject.Find("StoryManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("StoryManager");
        }

        OpeningScript opening = managerObject.GetComponent<OpeningScript>();
        if (opening == null)
        {
            opening = Undo.AddComponent<OpeningScript>(managerObject);
        }

        StoryInputGate inputGate = managerObject.GetComponent<StoryInputGate>();
        StoryGuideHighlighter guideHighlighter = managerObject.GetComponent<StoryGuideHighlighter>();
        if (part != OpeningScript.Part.Opening)
        {
            if (inputGate == null)
            {
                inputGate = Undo.AddComponent<StoryInputGate>(managerObject);
            }

            if (guideHighlighter == null)
            {
                guideHighlighter = Undo.AddComponent<StoryGuideHighlighter>(managerObject);
            }
        }

        ConfigureOpeningScript(opening, part, nextScene, playOnStart, background, dialogueFrame,
            dialogueText, nextIndicator.gameObject, fadeImage, inputGate, guideHighlighter);

        if (part == OpeningScript.Part.Opening)
        {
            GameObject titleScreen = GameObject.Find("TitleScreen");
            Title title = titleScreen != null ? titleScreen.GetComponent<Title>() : null;
            if (title == null)
            {
                Debug.LogError("TitleScreen に Title コンポーネントが見つかりません。", managerObject);
                return;
            }

            SerializedObject serializedTitle = new SerializedObject(title);
            serializedTitle.Update();
            serializedTitle.FindProperty("openingStory").objectReferenceValue = opening;
            serializedTitle.FindProperty("titleScreenRenderer").objectReferenceValue =
                titleScreen.GetComponent<SpriteRenderer>();
            serializedTitle.FindProperty("nextSceneName").stringValue = "Stage1";
            serializedTitle.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(title);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Canvas GetOrCreateStoryCanvas()
    {
        GameObject canvasObject = GameObject.Find("StoryCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("StoryCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null) canvas = Undo.AddComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            Undo.AddComponent<GraphicRaycaster>(canvasObject);
        }

        return canvas;
    }

    private static Image GetOrCreateImage(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        GameObject imageObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
        {
            imageObject.transform.SetParent(parent, false);
        }

        Image image = imageObject.GetComponent<Image>();
        if (image == null) image = Undo.AddComponent<Image>(imageObject);
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    private static TextMeshProUGUI GetOrCreateText(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));

        if (existing == null)
        {
            textObject.transform.SetParent(parent, false);
        }

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null) text = Undo.AddComponent<TextMeshProUGUI>(textObject);
        if (textObject.GetComponent<JapaneseTMPFontBootstrap>() == null)
        {
            Undo.AddComponent<JapaneseTMPFontBootstrap>(textObject);
        }

        return text;
    }

    private static void ConfigureLayout(Image background, Image frame, TextMeshProUGUI text,
        TextMeshProUGUI indicator, Image fade)
    {
        Stretch(background.rectTransform);
        background.enabled = false;

        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = new Vector2(0f, 0f);
        frameRect.anchorMax = new Vector2(1f, 0f);
        frameRect.pivot = new Vector2(0.5f, 0f);
        frameRect.sizeDelta = new Vector2(0f, 300f);
        frameRect.anchoredPosition = Vector2.zero;
        frame.gameObject.SetActive(false);

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0.17f, 0.08f);
        textRect.anchorMax = new Vector2(0.98f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.text = string.Empty;
        text.color = Color.white;
        text.fontSize = 44f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 26f;
        text.fontSizeMax = 44f;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;

        RectTransform indicatorRect = indicator.rectTransform;
        indicatorRect.anchorMin = new Vector2(1f, 0f);
        indicatorRect.anchorMax = new Vector2(1f, 0f);
        indicatorRect.pivot = new Vector2(1f, 0f);
        indicatorRect.sizeDelta = new Vector2(230f, 38f);
        indicatorRect.anchoredPosition = new Vector2(-24f, 9f);
        indicator.text = "▼ Enter";
        indicator.fontSize = 25f;
        indicator.color = Color.white;
        indicator.alignment = TextAlignmentOptions.Right;
        indicator.enableAutoSizing = true;
        indicator.fontSizeMin = 18f;
        indicator.fontSizeMax = 25f;
        indicator.raycastTarget = false;
        indicator.gameObject.SetActive(false);

        Stretch(fade.rectTransform);
        fade.color = new Color(0f, 0f, 0f, 0f);
        fade.raycastTarget = false;
        fade.transform.SetAsLastSibling();
    }

    private static void ConfigureOpeningScript(OpeningScript opening, OpeningScript.Part part,
        string nextScene, bool playOnStart, Image background, Image dialogueFrame,
        TMP_Text dialogueText, GameObject nextIndicator, Image fadeImage,
        StoryInputGate inputGate, StoryGuideHighlighter guideHighlighter)
    {
        string[] spritePaths = Directory.GetFiles("Assets/image/op", "*.png", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Sprite[] sprites = spritePaths
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
            .Where(sprite => sprite != null)
            .OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        SoundMapping[] soundMappings =
        {
            new SoundMapping("hidamarinomori", "Assets/BGM&SE/op_moriA.mp3"),
            new SoundMapping("kazeniyurerukusaki1", null),
            new SoundMapping("tsutinouewoaruku", "Assets/BGM&SE/syu_asiA.mp3"),
            new SoundMapping("iwagamapputatsuniwareru", "Assets/BGM&SE/op_wareru.mp3"),
            new SoundMapping("落下音", "Assets/BGM&SE/rakka.mp3"),
            new SoundMapping("着地音", "Assets/BGM&SE/tyakuti.mp3"),
            new SoundMapping("asufarutonouewoaruku2", "Assets/BGM&SE/syu_asiB.mp3"),
            new SoundMapping("getadehashiru", "Assets/BGM&SE/hone_asi.mp3"),
            new SoundMapping("jan", "Assets/BGM&SE/jan.mp3"),
            new SoundMapping("suzurankeikoku", null),
            new SoundMapping("doukutsuA", "Assets/BGM&SE/stageA.mp3")
        };

        SerializedObject serialized = new SerializedObject(opening);
        serialized.Update();
        SetObject(serialized, "background", background);
        SetObject(serialized, "dialogueFrame", dialogueFrame);
        SetObject(serialized, "tmpText", dialogueText);
        SetObject(serialized, "uiText", null);
        SetObject(serialized, "fadeImage", fadeImage);
        SetObject(serialized, "nextIndicator", nextIndicator);
        SetObject(serialized, "gameplayInputGate", inputGate);
        SetObject(serialized, "guideHighlighter", guideHighlighter);

        SerializedProperty spriteArray = serialized.FindProperty("sprites");
        spriteArray.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            spriteArray.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        SerializedProperty sounds = serialized.FindProperty("sounds");
        sounds.arraySize = soundMappings.Length;
        for (int i = 0; i < soundMappings.Length; i++)
        {
            SerializedProperty entry = sounds.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("key").stringValue = soundMappings[i].key;
            entry.FindPropertyRelative("clip").objectReferenceValue =
                string.IsNullOrEmpty(soundMappings[i].assetPath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<AudioClip>(soundMappings[i].assetPath);
        }

        SerializedProperty partProperty = serialized.FindProperty("part");
        partProperty.enumValueIndex = (int)part;
        serialized.FindProperty("playOnStart").boolValue = playOnStart;
        serialized.FindProperty("nextSceneName").stringValue = nextScene;
        serialized.FindProperty("protagonist").objectReferenceValue = null;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(opening);
        EditorUtility.SetDirty(background);
        EditorUtility.SetDirty(dialogueFrame);
        EditorUtility.SetDirty(dialogueText);
        EditorUtility.SetDirty(fadeImage);

        if (sprites.Length == 0)
        {
            Debug.LogError("Assets/image/op に Sprite が見つかりません。", opening);
        }

        if (sprites.Length < spritePaths.Length)
        {
            Debug.LogWarning("一部のオープニング画像が Sprite として読み込めませんでした。", opening);
        }
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}


