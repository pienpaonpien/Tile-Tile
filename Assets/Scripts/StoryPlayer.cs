using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 会話演出の共通部分（セリフの文字送り / 背景の切り替え / BGM・SE / フェード / 画面揺れ）。
/// シーンには直接置かず、OpeningScript のように継承して使います。
///
/// セリフの中に [jan] と書くと、その文字が表示されたタイミングで SE「jan」が鳴ります。
/// </summary>
public abstract class StoryPlayer : MonoBehaviour
{
    [System.Serializable]
    public class SoundEntry
    {
        public string key;      // スクリプトから呼ぶ名前（メモに書いてある音の名前）
        public AudioClip clip;  // 実際の音源をドラッグ
    }

    // ───────────── Inspector ─────────────
    [Header("UI")]
    [SerializeField] protected Image background;          // 背景・イラスト（story_1 / op_s1 など）
    [SerializeField] Image dialogueFrame;       // セリフ枠（serihu_*）。テキストはこの子にする
    [SerializeField] TMP_Text tmpText;          // セリフのテキスト（TextMeshPro）
    [SerializeField] Text uiText;               // セリフのテキスト（旧Text）※どちらか一方でOK
    [SerializeField] Image fadeImage;           // 画面全体を覆う黒いImage（任意）
    [SerializeField] GameObject nextIndicator;  // 「▼」など“次へ”の合図（任意）

    [Header("素材")]
    [SerializeField] Sprite[] sprites;          // 画像をまとめてドラッグ（ファイル名で呼び出します）
    [SerializeField]
    List<SoundEntry> sounds = new List<SoundEntry>
    {
        new SoundEntry { key = "op_moriA" },
        new SoundEntry { key = "op_moriB" },
        new SoundEntry { key = "syu_asiA" },
        new SoundEntry { key = "op_wareru" },
        new SoundEntry { key = "rakka" },
        new SoundEntry { key = "tyakuti" },
        new SoundEntry { key = "syu_asiB" },
        new SoundEntry { key = "hone_asi" },
        new SoundEntry { key = "jan" },
        new SoundEntry { key = "op_tikaC" },
        new SoundEntry { key = "stageA" },
    };

    [Header("設定")]
    [SerializeField] float secondsPerChar = 0.04f;   // 1文字ごとの間隔（秒）
    [SerializeField, Range(0f, 1f)] float bgmVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] float seVolume = 1f;

    // ───────────── 内部 ─────────────
    struct Cue { public int pos; public string key; }   // 「pos 文字目まで出たら key のSEを鳴らす」

    static readonly Regex CueRegex = new Regex(@"\[([^\]]+)\]");
    static readonly Regex RichRegex = new Regex(@"<[^>]*>");

    readonly Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();
    readonly Dictionary<string, AudioClip> clipMap = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, AudioSource> loopSource = new Dictionary<string, AudioSource>();
    readonly Dictionary<string, string> lastFrame = new Dictionary<string, string>();

    AudioSource seSource;
    string currentPlain = "";
    int consumedFrame = -1;

    protected virtual void Awake()
    {
        EnsureStoryCanvasScale();

        if (sprites != null)
        {
            foreach (var sprite in sprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                spriteMap[sprite.name] = sprite;
                if (sprite.name.EndsWith("_0", StringComparison.Ordinal))
                {
                    string assetName = sprite.name.Substring(
                        0,
                        sprite.name.Length - 2
                    );
                    spriteMap[assetName] = sprite;
                }
            }
        }

        if (sounds != null)
            foreach (var e in sounds)
                if (e != null && e.clip != null && !string.IsNullOrEmpty(e.key))
                    clipMap[e.key] = e.clip;

        seSource = gameObject.AddComponent<AudioSource>();
        seSource.playOnAwake = false;

        if (background == null || dialogueFrame == null || (tmpText == null && uiText == null))
            Debug.LogError("[StoryPlayer] Background / Dialogue Frame / テキスト（TMP か Text）を割り当ててください", this);

        if (uiText != null) uiText.supportRichText = true;
        if (background != null) background.enabled = false;
        if (dialogueFrame != null) dialogueFrame.gameObject.SetActive(false);
        if (nextIndicator != null) nextIndicator.SetActive(false);
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = false;
            SetFadeAlpha(0f);
        }
    }

    // ───────────── セリフ ─────────────

    /// <summary>
    /// セリフを左から右へ1文字ずつ表示し、「次へ」キーが押されるまで待つ。
    /// 文字送り中に押すと全文を一気に表示し、もう一度押すと次へ進む。
    /// </summary>
    /// <param name="speaker">話者名。frame を省略（null）したとき、この話者が前回使った枠を使う</param>
    /// <param name="frame">セリフ枠の画像名（serihu_s2 など）</param>
    /// <param name="text">本文。[jan] のように書くとその位置でSEが鳴る</param>
    protected IEnumerator Say(string speaker, string frame, string text)
    {
        if (speaker == null) speaker = "";
        if (string.IsNullOrEmpty(frame)) lastFrame.TryGetValue(speaker, out frame);
        else lastFrame[speaker] = frame;
        ShowWindow(frame);

        var cues = new List<Cue>();
        int total = SetText(ExtractCues(text ?? "", cues));
        int shown = 0, nextCue = 0;
        float timer = 0f;

        // 文頭に置かれた合図
        while (nextCue < cues.Count && cues[nextCue].pos <= 0) PlaySE(cues[nextCue++].key);

        while (shown < total)
        {
            if (ConsumeAdvance()) break;                    // 文字送り中に押された → 全文表示へ
            timer += Time.unscaledDeltaTime;
            while (timer >= secondsPerChar && shown < total)
            {
                timer -= secondsPerChar;
                shown++;
                ShowChars(shown);
                while (nextCue < cues.Count && cues[nextCue].pos <= shown) PlaySE(cues[nextCue++].key);
            }
            yield return null;
        }
        ShowChars(total);
        while (nextCue < cues.Count) PlaySE(cues[nextCue++].key);   // 飛ばした分の合図もここで鳴らす

        if (nextIndicator != null) nextIndicator.SetActive(true);
        yield return WaitForAdvance();
        if (nextIndicator != null) nextIndicator.SetActive(false);
    }

    /// <summary>「次へ」が押されるまで待つ。</summary>
    protected IEnumerator WaitForAdvance()
    {
        while (!ConsumeAdvance()) yield return null;
    }

    /// <summary>
    /// 「次へ」を押したか。使うキーを変えたいときはここを書き換える。
    /// 現在：Enter / テンキーEnter / Space / Z
    /// </summary>
    protected virtual bool AdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                           kb.spaceKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame))
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z))
            return true;
#endif
        return false;
    }

    // 同じフレームに1回のキー入力を2か所で拾ってしまわないようにする
    bool ConsumeAdvance()
    {
        if (Time.frameCount == consumedFrame) return false;
        if (!AdvancePressed()) return false;
        consumedFrame = Time.frameCount;
        return true;
    }

    void ShowWindow(string frame)
    {
        if (dialogueFrame == null) return;
        if (!string.IsNullOrEmpty(frame))
        {
            Sprite sp;
            if (spriteMap.TryGetValue(frame, out sp)) dialogueFrame.sprite = sp;
            else Debug.LogWarning("[StoryPlayer] セリフ枠の画像が見つかりません: " + frame, this);
        }
        dialogueFrame.gameObject.SetActive(true);

        if (tmpText != null)
        {
            tmpText.gameObject.SetActive(true);
            tmpText.enabled = true;
            tmpText.transform.SetAsLastSibling();

            JapaneseTMPFontBootstrap fontBootstrap = tmpText.GetComponent<JapaneseTMPFontBootstrap>();
            if (fontBootstrap != null)
            {
                fontBootstrap.EnsureFont();
            }
            else if (tmpText.font == null)
            {
                Debug.LogError("[StoryPlayer] 字幕のTMP_FontAssetが未設定で、フォント初期化コンポーネントもありません。", tmpText);
            }
        }
        else if (uiText != null)
        {
            uiText.gameObject.SetActive(true);
            uiText.enabled = true;
            uiText.transform.SetAsLastSibling();
        }
    }

    void EnsureStoryCanvasScale()
    {
        if (background == null)
        {
            return;
        }

        Canvas storyCanvas = background.GetComponentInParent<Canvas>();
        if (storyCanvas == null)
        {
            return;
        }

        Transform canvasTransform = storyCanvas.transform;
        Vector3 scale = canvasTransform.localScale;
        if (Mathf.Abs(scale.x) < 0.0001f || Mathf.Abs(scale.y) < 0.0001f || Mathf.Abs(scale.z) < 0.0001f)
        {
            canvasTransform.localScale = Vector3.one;
            Debug.LogWarning("[StoryPlayer] StoryCanvas のスケールが 0 だったため、(1, 1, 1) に修正しました。", storyCanvas);
        }
    }

    /// <summary>セリフ枠（とテキスト）を隠す。</summary>
    protected void HideWindow()
    {
        if (dialogueFrame != null) dialogueFrame.gameObject.SetActive(false);
    }

    // テキストを入れ替える（まだ1文字も見せない）。表示される全文字数を返す
    int SetText(string plain)
    {
        currentPlain = plain;
        if (tmpText != null)
        {
            tmpText.maxVisibleCharacters = 99999;
            tmpText.text = plain;
            tmpText.ForceMeshUpdate();
            int count = tmpText.textInfo.characterCount;
            tmpText.maxVisibleCharacters = 0;
            // TMP can report zero before its dynamic font atlas has populated Japanese glyphs.
            // Keep the typewriter running so the text becomes visible as soon as the font is ready.
            return count > 0 || string.IsNullOrEmpty(plain) ? count : plain.Length;
        }
        ShowChars(0);
        return plain.Length;
    }

    // 先頭から n 文字だけ見せる
    void ShowChars(int n)
    {
        if (tmpText != null)
        {
            tmpText.maxVisibleCharacters = n;
            return;
        }
        n = Mathf.Clamp(n, 0, currentPlain.Length);
        // 残りは透明にして、文字が増えても折り返し位置が変わらないようにする
        uiText.text = currentPlain.Substring(0, n) + "<color=#00000000>" + currentPlain.Substring(n) + "</color>";
    }

    // "…☆[jan]ここって…" → 本文から [jan] を取り除き、「何文字目で鳴らすか」を cues に記録する
    static string ExtractCues(string raw, List<Cue> cues)
    {
        var sb = new StringBuilder();
        int last = 0;
        foreach (Match m in CueRegex.Matches(raw))
        {
            sb.Append(raw, last, m.Index - last);
            int visible = RichRegex.Replace(sb.ToString(), "").Length;   // <color> などのタグは数えない
            string key = m.Groups[1].Value.Trim();
            if (key.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - 4);
            cues.Add(new Cue { pos = visible, key = key });
            last = m.Index + m.Length;
        }
        sb.Append(raw, last, raw.Length - last);
        return sb.ToString();
    }

    // ───────────── 背景 ─────────────

    /// <summary>背景・イラストを切り替える（画像のファイル名、拡張子なし）。</summary>
    protected void SetBG(string spriteName)
    {
        if (background == null) return;
        Sprite sp;
        if (!spriteMap.TryGetValue(spriteName, out sp))
        {
            Debug.LogWarning("[StoryPlayer] 画像が見つかりません: " + spriteName, this);
            return;
        }
        background.sprite = sp;
        background.enabled = true;
    }

    protected void HideBG()
    {
        if (background != null) background.enabled = false;
    }

    /// <summary>背景とセリフ枠をまとめて隠す。</summary>
    protected void HideAll()
    {
        HideBG();
        HideWindow();
    }

    // ───────────── 音 ─────────────

    /// <summary>SEを1回鳴らす。</summary>
    protected void PlaySE(string key)
    {
        var clip = FindClip(key);
        if (clip != null) seSource.PlayOneShot(clip, seVolume);
    }

    /// <summary>BGMを再生（既定ではループ）。複数のBGMを同時に鳴らせる。</summary>
    protected void PlayBGM(string key, bool loop = true) { PlayLoop(key, bgmVolume, loop); }
    protected void StopBGM(string key) { StopLoop(key); }

    /// <summary>[SE- … -SE] の開始／終了（歩く音などのループSE）。</summary>
    protected void PlaySELoop(string key) { PlayLoop(key, seVolume, true); }
    protected void StopSELoop(string key) { StopLoop(key); }

    protected void StopAllSounds()
    {
        foreach (var src in loopSource.Values)
            if (src != null) src.Stop();
        if (seSource != null) seSource.Stop();
    }

    AudioClip FindClip(string key)
    {
        AudioClip clip;
        if (!clipMap.TryGetValue(key, out clip))
            Debug.LogWarning("[StoryPlayer] 音が割り当てられていません: " + key + "（Sounds の Key と Clip を確認）", this);
        return clip;
    }

    void PlayLoop(string key, float volume, bool loop)
    {
        var clip = FindClip(key);
        if (clip == null) return;

        AudioSource src;
        if (!loopSource.TryGetValue(key, out src) || src == null)
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            loopSource[key] = src;
        }
        src.clip = clip;
        src.loop = loop;
        src.volume = volume;
        src.Play();
    }

    void StopLoop(string key)
    {
        AudioSource src;
        if (loopSource.TryGetValue(key, out src) && src != null) src.Stop();
    }

    // ───────────── 画面効果・待ち ─────────────

    /// <summary>黒フェード。to=1 で真っ黒に、to=0 で元の画面に戻る。</summary>
    protected IEnumerator Fade(float to, float seconds)
    {
        if (fadeImage == null) yield break;
        float from = fadeImage.color.a;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            SetFadeAlpha(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    void SetFadeAlpha(float a)
    {
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
        fadeImage.enabled = a > 0f;   // 透明のときは描画しない
    }

    /// <summary>背景を揺らす（地震など）。だんだん弱くなる。</summary>
    protected IEnumerator Shake(float seconds, float power = 12f)
    {
        if (background == null) yield break;
        RectTransform rt = background.rectTransform;
        Vector2 origin = rt.anchoredPosition;
        Vector3 scale = rt.localScale;
        rt.localScale = scale * 1.04f;   // 揺れたときに画面の縁が見えないよう少し拡大
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float damp = 1f - t / seconds;
            rt.anchoredPosition = origin + UnityEngine.Random.insideUnitCircle * power * damp;
            yield return null;
        }
        rt.anchoredPosition = origin;
        rt.localScale = scale;
    }

    /// <summary>指定秒数待つ（Time.timeScale の影響を受けない）。</summary>
    protected static WaitForSecondsRealtime Wait(float seconds)
    {
        return new WaitForSecondsRealtime(seconds);
    }
}
