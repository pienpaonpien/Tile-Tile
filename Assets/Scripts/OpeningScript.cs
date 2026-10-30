using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 脚本メモをそのままC#にしたもの。メモの記号との対応：
///   [SE]            → PlaySE("SE名")
///   {BGM}           → PlayBGM("BGM名")                （ループ再生）
///   {A＋B}          → PlayBGM("A"); PlayBGM("B");      （同時再生）
///   [SE- … -SE]     → PlaySELoop("SE名") … StopSELoop("SE名")
///   {BGM- … -BGM}   → PlayBGM("BGM名") … StopBGM("BGM名")
///   『画像』         → SetBG("画像名")（セリフ枠の画像は Say の第2引数）
///   『N秒かけて…』   → yield return Fade(1f, N)（真っ黒へ） / Fade(0f, N)（黒から戻る）
///   セリフ中の[SE]  → 本文に [SE名] と書く（その文字が出た瞬間に鳴る）
///
/// 流す場面は Part で選びます。
/// オープニングのシーンでは Opening、ステージ1・2のシーンでは Stage1Tutorial / Stage2Tutorial にして、
/// 同じ Story（プレハブ化がおすすめ）を置いてください。
/// </summary>
public class OpeningScript : StoryPlayer
{
    public enum Part { Opening, Stage1Tutorial, Stage2Tutorial, Ending }

    [Header("再生設定")]
    [SerializeField] Part part = Part.Opening;     // このシーンで流す場面
    [SerializeField] bool playOnStart = true;      // シーン開始と同時に再生する
    [SerializeField] string nextSceneName = "";    // 終わったあとに読み込むシーン名（空なら何もしない）

    [Header("演出用（任意）")]
    [SerializeField] GameObject protagonist;       // story_1 の上に出す歩く主人公（空でもOK）
    [SerializeField] Sprite protagonistSprite;      // タイトル導入で表示する主人公画像

    [Header("ステージ操作")]
    [SerializeField] StoryInputGate gameplayInputGate;
    [SerializeField] StoryGuideHighlighter guideHighlighter;

    [Header("イベント")]
    public UnityEvent onStarted;      // 会話の開始直前（プレイヤー操作を止める など）
    public UnityEvent onShowGuides;   // 「タイルに青赤黄の光が付く」タイミング（BrockGuide など）
    public UnityEvent onFinished;     // 会話の終了直後（ステージ開始 など）

    // 時間の調整はここ
    const float ForestWait = 3f;      // 『3秒後主人公表示』
    const float QuakeTime = 1.5f;     // 地面が揺れている時間
    const float WalkTime = 2f;        // 歩きながら場面が切り替わる時間

    public RectTransform TitleCanvasRoot
    {
        get { return background != null ? background.transform.parent as RectTransform : null; }
    }

    public void ShowTitleBackground()
    {
        SetBG("story_1");
    }

    public void StartTitleBGM()
    {
        PlayBGM("op_moriA");
    }
    void Start()
    {
        EnsureProtagonist();
        if (playOnStart) Play(part);
    }

    void EnsureProtagonist()
    {
        if (part != Part.Opening || protagonist != null || protagonistSprite == null || background == null)
        {
            return;
        }

        Transform storyCanvas = background.transform.parent;
        if (storyCanvas == null)
        {
            Debug.LogWarning("[OpeningScript] StoryBackground の親 Canvas が設定されていません。", this);
            return;
        }

        GameObject character = new GameObject(
            "StoryProtagonist",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        character.transform.SetParent(storyCanvas, false);

        RectTransform rectTransform = (RectTransform)character.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 24f);
        rectTransform.sizeDelta = new Vector2(340f, 340f);
        rectTransform.localScale = Vector3.one;

        Image image = character.GetComponent<Image>();
        image.sprite = protagonistSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        character.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1);
        character.SetActive(false);
        protagonist = character;
    }

    /// <summary>外部（GameManager など）から呼ぶ用。例：opening.Play(OpeningScript.Part.Stage2Tutorial);</summary>
    public void Play() { Play(part); }
    public void Play(Part which)
    {
        if (which != Part.Opening)
        {
            gameplayInputGate?.DisableGameplayInput();
            guideHighlighter?.HideAll();
        }

        StartCoroutine(Run(which));
    }

    IEnumerator Run(Part which)
    {
        yield return null;   // 開始キー（タイトルで押したキー等）を同じフレームで拾わないよう1フレーム待つ
        onStarted?.Invoke();

        switch (which)
        {
            case Part.Opening: yield return Opening(); break;
            case Part.Stage1Tutorial: yield return Stage1Tutorial(); break;
            case Part.Stage2Tutorial: yield return Stage2Tutorial(); break;
            case Part.Ending: yield return Ending(); break;
        }

        HideAll();
        onFinished?.Invoke();   // (プレイ画面に移行 / ステージ開始)
        if (which != Part.Opening) gameplayInputGate?.EnableGameplayInput();
        SceneManager.LoadScene("Title");
    }

    // ════════════════════════ オープニング ════════════════════════
    IEnumerator Opening()
    {
        if (protagonist != null) protagonist.SetActive(false);

        // ───── 森の中 ─────
        SetBG("story_1");                                   // 『story_1.png』（タイトルでキーを押した後）
        PlayBGM("op_moriB");                     //  op_moriB}
        yield return Wait(ForestWait);                      // 3秒後主人公表示
        if (protagonist != null) protagonist.SetActive(true);

        PlaySELoop("syu_asiA");                     // [syu_asiA-
        yield return Say("主人公", "serihu_s2", "旅行にきたのに道に迷うとか最悪...");   // (森林の中を歩いている)
        StopSELoop("syu_asiA");                     // -syu_asiA]
        if (protagonist != null) protagonist.SetActive(false);

        // ───── 地面が割れて穴に落ちる ─────
        StopBGM("op_moriA");                          // {op_moriA＋op_moriB}停止
        StopBGM("op_moriB");
        HideWindow();
        PlaySE("op_wareru");                  // [op_wareru]
        yield return Shake(QuakeTime);                      // (なんか地面揺れて…)
        SetBG("story_2");                                   // 『story_2.png』
        yield return Say("主人公", "serihu_s4", "んぎゃああああ！！！");

        HideWindow();
        PlaySE("rakka");                                    // [rakka]
        yield return Fade(1f, 1.5f);                        // 『1.5秒かけて画面真っ黒に』
        PlaySE("tyakuti");                                    // [tyakuti]
        SetBG("story_3");                                   // 『story_3.png』（ついた）
        yield return Fade(0f, 1f);                          // 『1秒かけて黒い画面から』

        // ───── 地下に到着 ─────
        yield return Say("主人公", "serihu_s3", "こ、ここは…？");
        yield return Say("主人公", "serihu_s2", "まあいいや、どこかに出口はあるはず");

        HideWindow();
        PlaySELoop("syu_asiB");                // [syu_asiB-
        yield return Wait(WalkTime / 2);
        SetBG("op_s1");                                     // 『op_s1.png』
        yield return Wait(WalkTime / 2);
        StopSELoop("syu_asiB");                // -syu_asiB]

        // ───── 骸骨との出会い ─────
        yield return Say("骸骨", "serihu_h1", "おーいそこの人ー！");
        PlaySE("hone_asi");                            // [hone_asi.mp3]
        yield return Say("主人公", "serihu_s3", "え？");
        SetBG("op_s1_h1");                                  // 『op_s1_h1.png』
        yield return Say("骸骨", "serihu_h1", "はぁ、疲れた、骨が折れるぜ...");        // (走ってくる)
        yield return Say("主人公", "serihu_s2", "...なんですかあなた");
        SetBG("op_s_h");                                    // 『op_s_h.png』
        // 枠の指定がメモに無かったので null（＝骸骨が前回使った serihu_h1 のまま）
        yield return Say("骸骨", null, "私はここに埋葬された人間さ☆[jan.mp3]ここって");
        yield return Say("骸骨", null, "地下だろ？お前が落ちた衝撃で起きたんだよ");

        SetBG("op_s__h1");                                  // 『op_s__h1.png』
        yield return Say("主人公", "serihu_s2", "そうですか、では失礼");                // (立ち去ろうとする)
        yield return Say("骸骨", "serihu_h2", "ちょ、まてよ！この床のタイルを直してほしいんだ！");
        yield return Say("主人公", "serihu_s2", "いや、そもそも私、出口探さないといけないので、さようなら");
        PlaySE("syu_asiB");                    // [syu_asiB]
        yield return Say("骸骨", "serihu_h1", "待てって！[hone_asi.mp3]ふーーーん、なるほどぉ？");
        yield return Say("骸骨", "serihu_h1", "外に出たいのか。");
        yield return Say("骸骨", "serihu_h1", "じゃあ、タイルを直してくれたら外に出るための方法を教えてやるってことで！");

        SetBG("op_sa_h");                                   // 『op_sa_h.png』
        yield return Say("主人公", "serihu_s2", "...そうするしかないみたいですね");
        yield return Say("骸骨", "serihu_h1", "がんばれよ☆");
        // (プレイ画面に移行) → Run() の最後の onFinished / nextSceneName
    }

    // ════════════════════════ ステージ1の開始時 ════════════════════════
    IEnumerator Stage1Tutorial()
    {
        yield return Say("骸骨", "serihu_h1", "今から素人にも分かるように目印をつけてやる！！");
        guideHighlighter?.ShowAll();
        onShowGuides?.Invoke();                             // (タイルに青赤黄の光が付く)
        yield return Say("主人公", "serihu_s1", "これは？");
        yield return Say("骸骨", "serihu_h1", "青が押すだけ、赤が引くだけで動かすんだ！");
        yield return Say("主人公", "serihu_s1", "はーい");
        // (ステージ開始) {stageA}
        // ※ステージBGMを GameManager 側で鳴らしている場合は、この1行を消してください
        PlayBGM("stageA");
    }

    // ════════════════════════ ステージ2の開始時 ════════════════════════
    IEnumerator Stage2Tutorial()
    {
        guideHighlighter?.ShowAll();
        yield return Say("主人公", "serihu_s2", "黄色のタイルがある…");   // (黄色の印ついてるタイルがある)
        yield return Say("骸骨", "serihu_h1", "これは押し引きどっちもできるやつだ！");
        yield return Say("主人公", "serihu_s1", "なるほど");
    }

    // ════════════════════════ ステージ4クリア後 ════════════════════════
    IEnumerator Ending()
    {
        PlayBGM("op_tikaC");
        SetBG("ed_s1_h1");

        yield return Say("主人公", "serihu_s1", "……これで全部、直りましたよ");
        yield return Say("骸骨", "serihu_h1", "おおー！！ 本当に全部直したのか！！");
        yield return Say("主人公", "serihu_s2", "約束しましたからね");
        yield return Say("骸骨", "serihu_h1", "いやぁ、助かったぜ！これでこの場所も元通りだ！");
        yield return Say("主人公", "serihu_s2", "それで、出口はどこなんです？");
        yield return Say("骸骨", "serihu_h1", "ああ、そうだったな");
        yield return Say("骸骨", "serihu_h1", "……えーっと");
        yield return Say("主人公", "serihu_s2", "……");

        SetBG("ed_s2_h3");
        yield return Say("骸骨", "serihu_h2", "……出口、どこだったっけ？");
        yield return Say("主人公", "serihu_s4", "は？");

        SetBG("ed_s2_h2");
        yield return Say("骸骨", "serihu_h1", "いや、ほら……私、ここに埋葬されてから結構経ってるからさ☆[hone_kachi]");
        yield return Say("主人公", "serihu_s4", "いやいやいや、そこ忘れちゃダメでしょ！");
        yield return Say("骸骨", "serihu_h2", "まあまあ、落ち着けって！");
        yield return Say("主人公", "serihu_s3", "こっちはずっと出口探してたんですけど！？");
        yield return Say("骸骨", "serihu_h1", "ちゃんと覚えてるって！ほら、こっちだ！");
        yield return Say("主人公", "serihu_s2", "本当でしょうね……");

        HideWindow();
        PlaySE("syu_asiB");
        PlaySE("hone_asi");
        SetFadeColor(Color.black);
        yield return Fade(1f, 1f);
        StopBGM("op_tikaC");

        SetBG("story6");
        yield return Fade(0f, 1f);
        PlayBGM("ed_A");

        yield return Say("骸骨", "serihu_h1", "ここをまっすぐ行けば、地上への道がある！");
        yield return Say("主人公", "serihu_s2", "……本当にありましたね");
        yield return Say("骸骨", "serihu_h1", "だろ？");
        yield return Say("主人公", "serihu_s2", "まあ、タイルを直した甲斐はあったみたいですね");
        yield return Say("骸骨", "serihu_h1", "ありがとな！お前が来てくれて助かったぜ！");
        yield return Say("主人公", "serihu_s1", "こちらこそ、出口を教えてもらえて助かりました");
        yield return Say("骸骨", "serihu_h1", "じゃあな！また落ちてくることがあったらよろしくな☆");
        yield return Say("主人公", "serihu_s3", "二度と落ちてきたくないです");
        yield return Say("骸骨", "serihu_h1", "ははは！そりゃそうか！");

        HideWindow();
        PlaySE("syu_asiB");
        SetFadeColor(Color.white);
        yield return Fade(1f, 1.5f);
        StopBGM("ed_A");
    }
}


