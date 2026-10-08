using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
    public enum Part { Opening, Stage1Tutorial, Stage2Tutorial }

    [Header("再生設定")]
    [SerializeField] Part part = Part.Opening;     // このシーンで流す場面
    [SerializeField] bool playOnStart = true;      // シーン開始と同時に再生する
    [SerializeField] string nextSceneName = "";    // 終わったあとに読み込むシーン名（空なら何もしない）

    [Header("演出用（任意）")]
    [SerializeField] GameObject protagonist;       // story_1 の上に出す歩く主人公（空でもOK）

    [Header("イベント")]
    public UnityEvent onStarted;      // 会話の開始直前（プレイヤー操作を止める など）
    public UnityEvent onShowGuides;   // 「タイルに青赤黄の光が付く」タイミング（BrockGuide など）
    public UnityEvent onFinished;     // 会話の終了直後（ステージ開始 など）

    // 時間の調整はここ
    const float ForestWait = 3f;      // 『3秒後主人公表示』
    const float QuakeTime = 1.5f;     // 地面が揺れている時間
    const float WalkTime = 2f;        // 歩きながら場面が切り替わる時間

    void Start()
    {
        if (playOnStart) Play(part);
    }

    /// <summary>外部（GameManager など）から呼ぶ用。例：opening.Play(OpeningScript.Part.Stage2Tutorial);</summary>
    public void Play() { Play(part); }
    public void Play(Part which) { StartCoroutine(Run(which)); }

    IEnumerator Run(Part which)
    {
        yield return null;   // 開始キー（タイトルで押したキー等）を同じフレームで拾わないよう1フレーム待つ
        onStarted?.Invoke();

        switch (which)
        {
            case Part.Opening: yield return Opening(); break;
            case Part.Stage1Tutorial: yield return Stage1Tutorial(); break;
            case Part.Stage2Tutorial: yield return Stage2Tutorial(); break;
        }

        HideAll();
        onFinished?.Invoke();   // (プレイ画面に移行 / ステージ開始)
        if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
    }

    // ════════════════════════ オープニング ════════════════════════
    IEnumerator Opening()
    {
        if (protagonist != null) protagonist.SetActive(false);

        // ───── 森の中 ─────
        SetBG("story_1");                                   // 『story_1.png』（タイトルでキーを押した後）
        PlayBGM("hidamarinomori");                          // {hidamarinomori＋
        PlayBGM("kazeniyurerukusaki1");                     //  kazeniyurerukusaki1}
        yield return Wait(ForestWait);                      // 3秒後主人公表示
        if (protagonist != null) protagonist.SetActive(true);

        PlaySELoop("tsutinouewoaruku");                     // [tsutinouewoaruku-
        yield return Say("主人公", "serihu_s2", "旅行にきたのに道に迷うとか最悪...");   // (森林の中を歩いている)
        StopSELoop("tsutinouewoaruku");                     // -tsutinouewoaruku]
        if (protagonist != null) protagonist.SetActive(false);

        // ───── 地面が割れて穴に落ちる ─────
        StopBGM("hidamarinomori");                          // {hidamarinomori＋kazeniyurerukusaki1}停止
        StopBGM("kazeniyurerukusaki1");
        HideWindow();
        PlaySE("iwagamapputatsuniwareru");                  // [iwagamapputatsuniwareru]
        yield return Shake(QuakeTime);                      // (なんか地面揺れて…)
        SetBG("story_2");                                   // 『story_2.png』
        yield return Say("主人公", "serihu_s4", "んぎゃああああ！！！");

        HideWindow();
        PlaySE("落下音");                                    // [落下音]
        yield return Fade(1f, 1.5f);                        // 『1.5秒かけて画面真っ黒に』
        PlaySE("着地音");                                    // [着地音]
        SetBG("story_3");                                   // 『story_3.png』（ついた）
        yield return Fade(0f, 1f);                          // 『1秒かけて黒い画面から』

        // ───── 地下に到着 ─────
        yield return Say("主人公", "serihu_s3", "こ、ここは…？");
        yield return Say("主人公", "serihu_s2", "まあいいや、どこかに出口はあるはず");

        HideWindow();
        PlaySELoop("asufarutonouewoaruku2");                // [asufarutonouewoaruku2-
        yield return Wait(WalkTime / 2);
        SetBG("op_s1");                                     // 『op_s1.png』
        yield return Wait(WalkTime / 2);
        StopSELoop("asufarutonouewoaruku2");                // -asufarutonouewoaruku2]

        // ───── 骸骨との出会い ─────
        yield return Say("骸骨", "serihu_h1", "おーいそこの人ー！");
        PlaySE("getadehashiru");                            // [getadehashiru]
        yield return Say("主人公", "serihu_s3", "え？");
        SetBG("op_s1_h1");                                  // 『op_s1_h1.png』
        yield return Say("骸骨", "serihu_h1", "はぁ、疲れた、骨が折れるぜ...");        // (走ってくる)
        yield return Say("主人公", "serihu_s2", "...なんですかあなた");
        SetBG("op_s_h");                                    // 『op_s_h.png』
        // 枠の指定がメモに無かったので null（＝骸骨が前回使った serihu_h1 のまま）
        yield return Say("骸骨", null, "私はここに埋葬された人間さ☆[jan]ここって地下だろ？お前が落ちた衝撃で起きたんだよ");

        SetBG("op_s__h1");                                  // 『op_s__h1.png』
        yield return Say("主人公", "serihu_s2", "そうですか、では失礼");                // (立ち去ろうとする)
        yield return Say("骸骨", "serihu_h2", "ちょ、まてよ！この床のタイルを直してほしいんだ！");
        yield return Say("主人公", "serihu_s2", "いや、そもそも私、出口探さないといけないので、さようなら");
        PlaySE("asufarutonouewoaruku2");                    // [asufarutonouewoaruku2]
        yield return Say("骸骨", "serihu_h1", "待てって！[asufarutonouewoaruku2]ふーーーん、なるほどぉ？外に出たいのか。じゃあ、タイルを直してくれたら外に出るための方法を教えてやるってことで！");

        SetBG("op_sa_h");                                   // 『op_sa_h.png』
        yield return Say("主人公", "serihu_s2", "...そうするしかないみたいですね");
        yield return Say("骸骨", "serihu_h1", "がんばれよ☆");
        // (プレイ画面に移行) → Run() の最後の onFinished / nextSceneName
    }

    // ════════════════════════ ステージ1の開始時 ════════════════════════
    IEnumerator Stage1Tutorial()
    {
        PlayBGM("suzurankeikoku");                          // {suzurankeikoku-
        yield return Say("骸骨", "serihu_h1", "今から素人にも分かるように目印をつけてやる！！");
        onShowGuides?.Invoke();                             // (タイルに青赤黄の光が付く)
        yield return Say("主人公", "serihu_s1", "これは？");
        yield return Say("骸骨", "serihu_h1", "青が押すだけ、赤が引くだけで動かすんだ！");
        yield return Say("主人公", "serihu_s1", "はーい");
        StopBGM("suzurankeikoku");                          // -suzurankeikoku}

        // (ステージ開始) {doukutsuA}
        // ※ステージBGMを GameManager 側で鳴らしている場合は、この1行を消してください
        PlayBGM("doukutsuA");
    }

    // ════════════════════════ ステージ2の開始時 ════════════════════════
    IEnumerator Stage2Tutorial()
    {
        PlayBGM("suzurankeikoku");                          // {suzurankeikoku-
        yield return Say("主人公", "serihu_s2", "黄色のタイルがある…");   // (黄色の印ついてるタイルがある)
        yield return Say("骸骨", "serihu_h1", "これは押し引きどっちもできるやつだ！");
        yield return Say("主人公", "serihu_s1", "なるほど");
        StopBGM("suzurankeikoku");                          // -suzurankeikoku}
    }
}
