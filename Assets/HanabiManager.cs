using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine.Assertions.Must;
using NUnit.Framework; // TextMeshProを使うために必要
using DG.Tweening;
using Unity.VisualScripting;
public class HanabiManager : MonoBehaviour
{
    public enum GameState
    {
        Title,
        Ignition,
        IgnitionSuccess,
        Keeping,
        GameOver
    }
    public GameState currentState = GameState.Title; // ゲーム開始時の状態
    public Vector2 pivotpoint = new Vector2(0,-1.5f); // 支点の位置（仮に画面下あたり）
    public float elapsedTime = 0f; // ゲーム開始からの経過時間

    [Header("着火ミニゲーム")]
    public float ignitionGaugeValue = 0f; // 現在のゲージの値（0~1）
    public float ignitionSpeed = 1f; // ゲージが動く速さ
    private bool ignitionGoingUp = true; // 今ゲージが上昇下降しているか
    public GameObject ignitionGaugeUI; // 着火ゲージのUI一式(親のGameObject)
    public RectTransform ignitionMarker; // 動く目印のRectTransform
    public float gaugeWidth = 400f;
    private float ignitionGraceTimer = 0f; // 着火直後の猶予タイマー
    public float ignitionGraceDuration = 1f; // 猶予時間(秒)

    [Header("角度設定")]
    public float idealAngle = 45f; // 理想の角度
    public float idealAngleRight = 45f;   // 右斜め45度
    public float idealAngleLeft = 135f;   // 左斜め45度(鏡写し)
    public float toleranceRange = 15f; // 許容範囲（±15°）

    [Header("揺れの設定")]
    public float noiseSpeed = 1f; // 揺れの速さ
    public float noiseStrength = 30f; //  揺れの強さ
    
    [Header("スコア設定")]
    public float score = 0f; 
    public float baseScorePerSecond = 10f; 
    public float remainingTime = 60f;　// 最大残り時間

    [Header("UI")]
    public SpriteRenderer fireballrenderer; // FireBallのSprite Rendererへの参照
    public SpriteRenderer fireballCoreRenderer;
    public ParticleSystem fireballParticles; // パーティクル
    public ParticleSystem fireballParticlesLv2; // パーティクル（子供）
    public ParticleSystem fireballParticlesLv3; // パーティクル（孫）
    public Transform handleTransform; // 線香花火の持ち手部分
    public GameObject titlePanel; // タイトル背景
    public GameObject endpanel; // gameover背景
    public GameObject KeepingBG;
    public GameObject lighter;
    public TextMeshProUGUI scoreText; // スコア表示用のTextMeshProUGUIへの参照
    public TextMeshProUGUI timeText; // 残り時間表示用のTextMeshProUGUIへの参照
    public TextMeshProUGUI gameoverText; // gameoverの参照
   
   [Header("風向きイベント")]
   public bool isRightZoneActive = true; // 45°がokか
   public bool isLeftZoneActive = true; // 135°がokか
   private float WindCheckTimer = 0f; 
   public float WindCheckInterval = 4f; // 何秒おきに抽選するか

   [Header("シフト(スペースキー)連打ゲージ")]
   public float shiftGauge = 50f; // 現在のゲージ値(0〜100)
   public float decreaseGauge = 20f; // 放置していると減るゲージ量
   public float increaseAmount = 8f; // shiftで増加するゲージ量
   public float shiftGoodRangeMin = 40f;
   public float shiftGoodRangeMax = 60f; //　40~60の範囲内ならok 
   public GameObject ShiftGaugeUI; // shiftgaugeのUI一式
   public UnityEngine.UI.Image ShiftGaugeFillimage; // gaugeの中身

   [Header("警告演出(左右)")]
   public GameObject warningLeftPanel;  // 左半分の警告(左が使えなくなる時)
   public GameObject warningRightPanel; // 右半分の警告(右が使えなくなる時)
   public float warningLeadTime = 1f; // イベント発生の何秒前に警告するか
   public float blinkSpeed = 5f; // 点滅スピード
   public float windEventStartDelay = 10f; // 最初の10秒はイベントなし
   private bool hasDecidedThisWindow = false; // 今回分の抽選をもう決めたか
   private bool pendingEventWillHappen = false; // 次のイベントは発生するか
   private bool pendingUseRightOnly = false;    // 発生する場合、右のみOKになるか
   
   [Header("演出")]
   public GameOverSequence gameOverSequence;
   private Vector3 coreOriginalScale;
   public IgnitionSequence ignitionSequence; 
   public AudioManager audioManager;
   private int currentHanabiPhase = 0; // 1:第一段階, 2:第二段階, 3:第三段階

    float GetWobble() // 揺れの設定
    {
        // Time.timeを使うことで、時間経過に応じた滑らかなランダム値が得られる
        float noise = Mathf.PerlinNoise(Time.time * noiseSpeed, 0f );
        // PerlinNoiseは0〜1の範囲で返ってくるので、-1〜1の範囲に変換してから強さをかける
        return (noise - 0.5f) * 2f * noiseStrength;
    }

    float GetWindEventChance(float t) // イベント発生確率
    {
        if (t < 40f)
        {
            return 0.5f; // 50%
        }
        else
        {
            return 0.8f; // ラストスパートは確率UP
        }
    }

    float GetMultiplier(float t) // 時間経過によってスコアの倍率を変化させる
    {
        if (t < 15f)
        {
            return 10f;
        }
        else if (t < 40f)
        {
            return 15f;
        } 
        else
        {
            return 25f;
        }
    }

    void Start()
    {
        Debug.Log("線香花火ゲーム、スタート！");
        coreOriginalScale = fireballCoreRenderer.transform.localScale;
        UpdateUIVisibility();
    }

    void UpdateHandleVisual(float angle)　// 花火の持ち手表示
    {
        if (handleTransform == null) return;
        float handleLength = 50f;
        float radians = angle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(radians),Mathf.Sin(radians))*(handleLength / 2f); // Vector2(cos, sin)で「その角度を向いた、長さ1の矢印」ができます。これにhandleLength / 2fを掛けることで、「棒の半分の長さ分、その角度の方向に進んだ位置」が計算できます

        handleTransform.position = (Vector2)pivotpoint + offset; // 棒の中心を、支点から見て「棒の半分の長さだけ角度方向にずらした位置」に置くことで、結果的に棒の先端(手前側)がちょうど支点に来るようになります
        handleTransform.rotation = Quaternion.Euler( 0f , 0f , angle - 90f);
    }

    void UpdateWindEvent(float t) // 風向きイベント
    {
        if (t < windEventStartDelay)
        {
            return;
        }

        WindCheckTimer += Time.deltaTime;
        bool inWarningWindow = WindCheckTimer >= (WindCheckInterval - warningLeadTime);
        // --- 警告ウィンドウに入った瞬間、先に結果を決めておく ---
        if (inWarningWindow && !hasDecidedThisWindow)
        {
            hasDecidedThisWindow = true;
            float chance = GetWindEventChance(t);
            pendingEventWillHappen = Random.value < chance;
            pendingUseRightOnly = Random.value > 0.5f;

            if (pendingEventWillHappen && audioManager != null)
            {
                audioManager.PlaySE(audioManager.Warning); // 警告音
            }
        }
        UpdateWarningDisplay(inWarningWindow);
    
        if (WindCheckTimer >= WindCheckInterval)
        {
            WindCheckTimer = 0f; // タイマーリセット
            hasDecidedThisWindow = false;

            if (pendingEventWillHappen)
            {
                isRightZoneActive = pendingUseRightOnly;
                isLeftZoneActive = !pendingUseRightOnly;
                Debug.Log(pendingUseRightOnly ? "風向き変更！右側のみOK" : "風向き変更！左側のみOK");
                if (audioManager != null) audioManager.PlaySE(audioManager.WindSE); // 風の音
            }
            else
            {
                isRightZoneActive = true;
                isLeftZoneActive = true;
            }
        }
    }

    void UpdateWarningDisplay(bool inWarningWindow)
    {
        bool warnLeft = inWarningWindow && pendingEventWillHappen && pendingUseRightOnly;   // 左が使えなくなる予告
        bool warnRight = inWarningWindow && pendingEventWillHappen && !pendingUseRightOnly; // 右が使えなくなる予告
        if (warningLeftPanel != null)
        {
            warningLeftPanel.SetActive(warnLeft);
            if (warnLeft) UpdateBlink(warningLeftPanel);
        }
        if (warningRightPanel != null)
        {
            warningRightPanel.SetActive(warnRight);
            if (warnRight) UpdateBlink(warningRightPanel);
        }
    }

    void UpdateBlink(GameObject panel)  //　点滅設定
    {
        UnityEngine.UI.Image img = panel.GetComponent<UnityEngine.UI.Image>();
        if (img == null) return;

        float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        Color c = img.color;
        c.a = alpha * 0.4f;
        img.color = c;
    }

    void UpdateFireballColor(float t) // 時間経過に応じてFireBallの色を変化させる
    {
        if (fireballrenderer == null ) return; // 参照が設定されてなければ何もしない(エラー防止)

        Color currentColor; // 現在の色を保存
        if (t < 15f)
        {
            currentColor = new Color(1f, 0.60f, 0.2f);
            if (currentHanabiPhase != 1 && audioManager != null)
            {
                currentHanabiPhase = 1;
                audioManager.PlayHanabiSE(audioManager.hanabiSE_1);
            }
        }
        else if (t < 40f)
        {
            currentColor = new Color(1f, 0.45f, 0.1f);
            if (currentHanabiPhase != 2 && audioManager != null) 
            {
                currentHanabiPhase = 2;
                audioManager.PlayHanabiSE(audioManager.hanabiSE_2);
            }
        }
        else
        {
            float hue = Mathf.Repeat(t * 0.8f, 1f); // 0~1をループする値
            currentColor = Color.HSVToRGB(hue, 1f, 1f);　// HSV(色相・彩度・明度)で設定、hueで色相変化させ虹色に
            if (currentHanabiPhase != 3 && audioManager != null)
            {
                currentHanabiPhase = 3;
                audioManager.PlayHanabiSE(audioManager.hanabiSE_3);
            }
        }
        
        fireballrenderer.color = currentColor;

        if (fireballCoreRenderer != null)
        {
            fireballCoreRenderer.color = Color.white;
        }
        UpdateParticleSettings(currentColor, t);
    }

    void UpdateParticleSettings(Color color, float t)　// 火花パーティクル設定
    {
        if (fireballParticles == null) return;

        var main = fireballParticles.main; // MainModuleという設定のカタマリを取得
        main.startColor = color; // 色を反映

        var emission = fireballParticles.emission;
        if (t < 15f)
        {
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f); 
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            emission.rateOverTime = 15f;
        }
        else if (t < 40f)
        {
            main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            emission.rateOverTime = 25f;
        }
        else
        {
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f,1f);
            emission.rateOverTime = 100f;
        }

        if (fireballParticlesLv2 != null)
        {
            var mainLv2 = fireballParticlesLv2.main;
            mainLv2.startColor = color;

            var emissionLv2 = fireballParticlesLv2.emission;
            if (t < 40f)
            {
                mainLv2.startSpeed = new ParticleSystem.MinMaxCurve(3f,5f);
                mainLv2.startSize = new ParticleSystem.MinMaxCurve(0.05f,0.1f);
            }
            else
            {
                mainLv2.startSpeed = new ParticleSystem.MinMaxCurve(5f,7f);
                mainLv2.startSize = new ParticleSystem.MinMaxCurve(0.1f,0.3f);
            }
        } 

        if (fireballParticlesLv3 != null)
        {
            if (t >= 15f)
            {
                if (!fireballParticlesLv3.isPlaying) // まだ再生していなければ
                {
                    fireballParticlesLv3.Play(); // 明示的に再生開始
                }
            }
            else
            {
                fireballParticlesLv3.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 発生を止めて、既存の粒も消す
            }
            
            var mainLv3 = fireballParticlesLv3.main;
            mainLv3.startColor = color;

            var emissionLv3 = fireballParticlesLv3.emission;
            if (t < 40f)
            {
                mainLv3.startSpeed = new ParticleSystem.MinMaxCurve(3f,5f);
                mainLv3.startSize = new ParticleSystem.MinMaxCurve(0.01f,0.02f);
            }
            else
            {
                mainLv3.startSpeed = new ParticleSystem.MinMaxCurve(5f,7f);
                mainLv3.startSize = new ParticleSystem.MinMaxCurve(0.03f,0.05f);
            }
        } 
    }

    void UpdateUI() // 点数表示更新
    {
        if (scoreText != null)
        {
            scoreText.text = "スコア:" + Mathf.FloorToInt(score); // FloorToIntは「小数点以下を切り捨てて整数にする」関数
        }
        if (timeText != null)
        {
            timeText.text = "残り時間:" + Mathf.CeilToInt(remainingTime); // こちらは「切り上げ」
        }
    }

    void UpdateUIVisibility()　// UIの表示非表示
    {
        bool isTitle = currentState == GameState.Title;
        bool isIgnition = currentState == GameState.Ignition;
        bool isIgnitionSuccess = currentState == GameState.IgnitionSuccess;
        bool isKeeping = currentState == GameState.Keeping;
        bool isGameOver = currentState == GameState.GameOver;

        if (titlePanel != null) titlePanel.gameObject.SetActive(isTitle);
        if (KeepingBG != null) KeepingBG.gameObject.SetActive(isIgnition || isIgnitionSuccess || isKeeping || isGameOver);
        if (ShiftGaugeUI != null) ShiftGaugeUI.gameObject.SetActive(isKeeping);
        if (fireballrenderer != null) fireballrenderer.gameObject.SetActive(isKeeping || isGameOver);
        if (scoreText != null) scoreText.gameObject.SetActive(isKeeping);
        if (timeText != null) timeText.gameObject.SetActive(isKeeping);
        if (gameoverText != null) gameoverText.gameObject.SetActive(isGameOver);
        if (endpanel != null) endpanel.gameObject.SetActive(isGameOver);

        if (!isKeeping) // Keeping状態でなければ、強制的に警告パネルをオフにする
        {
            if (warningLeftPanel != null) warningLeftPanel.SetActive(false);
            if (warningRightPanel != null) warningRightPanel.SetActive(false);
        }
        if (ignitionGaugeUI != null) ignitionGaugeUI.gameObject.SetActive(false);
    }

    void Update()
    {
        switch (currentState)
        {
            case GameState.Title:
            UpdateTitle();
            break;
            case GameState.Ignition:
            UpdateIgnition();
            break;
            case GameState.IgnitionSuccess:
            break;
            case GameState.Keeping:
            UpdateKepping();
            break;
            case GameState.GameOver:
            UpdateGameOver();
            break;
        }
    }
    void UpdateTitle()
    {
         // ボタンクリックで状態が変わるので、ここは基本何もしなくてOK
    }

    public void OnStartButtonClicked() // ボタンクリックでスタート
    {
        currentState = GameState.Ignition;
        UpdateUIVisibility();
        if (audioManager != null)
        {
            audioManager.StopHanabiSE();
            audioManager.PlayBGM(audioManager.bgmbeforeIgnition, fadeDuration: 1.0f); // 虫の音
        }
        ignitionSequence.StartSequence();
    }
    public void OnHowToPlayButtonClicked()
    {
        Debug.Log("遊び方ボタンが押された");
        // 中身は後で実装
    }

    void UpdateIgnition() // 点火部分
    {
          // --- ゲージを0〜1の間で往復させる ---
        if (ignitionGoingUp)　// ゲージの上昇下降
       {
            ignitionGaugeValue += ignitionSpeed * Time.deltaTime;
            if (ignitionGaugeValue >= 1f)
            {
                ignitionGaugeValue = 1f;
                ignitionGoingUp = false; //上限まで来たら反転
            }            
        }
        else
        {
           ignitionGaugeValue -= ignitionSpeed * Time.deltaTime;
           if (ignitionGaugeValue <= 0f)
            {
                ignitionGaugeValue = 0f;
                ignitionGoingUp = true; // 下限で反転
            }
        }
        // Debug.Log("ゲージ: " + ignitionGaugeValue);

        if (ignitionMarker != null)
        {
            float xPos = (ignitionGaugeValue - 0.5f)* gaugeWidth;  // 0〜1を「-半分〜+半分」の位置に変換
            ignitionMarker.anchoredPosition = new Vector2(xPos, 0f); //anchoredPositionはRectTransformで位置を指定する時に使うプロパティ
        }

        // --- クリック判定 ---
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) // 左クリックorスペースキーが押された瞬間
        {
            if (audioManager != null) audioManager.PlaySE(audioManager.lighterSE);
            JudgeIgnition(ignitionGaugeValue);
        }
    }
        
    void JudgeIgnition(float value) // 点火タイミング評価
    {
        float diff = Mathf.Abs(value - 0.5f);
        if (diff < 0.1f)
        {
            Debug.Log("Perfect!");
            toleranceRange = 30f;
            score += 1000f;
        } 
        else if (diff < 0.25f)
        {
            Debug.Log("Good");
            toleranceRange = 20f;
            remainingTime -= 5f;
            score += 500f;
        }
        else
        {
            Debug.Log("Bad");
            toleranceRange = 10f;
            remainingTime -= 10f;
        }

        currentState = GameState.IgnitionSuccess;
        UpdateUIVisibility();

        if (ignitionSequence != null)
        {
            ignitionSequence.PlayIgnitionSuccess(OnignitionSuccessComplete);
        }
    }

    void OnignitionSuccessComplete() //　演出終了後Keepingに移行
    {
        currentState = GameState.Keeping;
        UpdateUIVisibility();
        if (audioManager != null)
        {
            audioManager.PlayBGM(audioManager.bgmKeeping, fadeDuration:1.5f);
        }
    }

    void UpdateShiftGauge()　// shiftgaugeの設定
    {
        shiftGauge -= decreaseGauge * Time.deltaTime;
        if(Input.GetKeyDown(KeyCode.Space))
        {
            shiftGauge += increaseAmount;
        }

        shiftGauge = Mathf.Clamp(shiftGauge, 0f, 100f);

        if (ShiftGaugeFillimage != null)
        {
            ShiftGaugeFillimage.fillAmount = shiftGauge / 100f;
        }

        // --- goodゾーンにいる時だけ色を変える ---
        bool isGood = IsShiftGaugeGood();
        ShiftGaugeFillimage.color = isGood ? new Color(0.16f, 0.80f, 0.60f, 0.76f) : Color.gray; // 三項演算子でサクッと切り替え
    }
    bool IsShiftGaugeGood() // shiftゲージ判定
    {
        return shiftGauge >= shiftGoodRangeMin && shiftGauge <= shiftGoodRangeMax;
    }

        
    void UpdateKepping() // ゲーム本編
    {
        // --- 角度の計算 ---
        // マウスの画面上の位置を取得（ピクセル座標）
        Vector3 mouseScreenPos = Input.mousePosition;
        // ピクセル座標を「ワールド座標」に変換
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        // 支点からマウスまでの方向ベクトルを計算
        Vector2 direction = (Vector2)mouseWorldPos - pivotpoint;
        // 方向ベクトルから角度を計算（度数法、0〜360）
        float angle = Mathf.Atan2(direction.y,direction.x)* Mathf.Rad2Deg;　// Mathf.Rad2Degでラジアンを弧度法に
        
        // --- 揺れを混ぜた目標角度 ---
        float wobble = GetWobble();
        float currentIdealAngleRight = idealAngleRight + wobble; // 常に微妙にズレる目標角度
        float currentIdealAngleLeft = idealAngleLeft + wobble;

        // --- イベント管理 --- 
        UpdateWindEvent(elapsedTime);
        UpdateShiftGauge();

        float diffRight = isRightZoneActive ? Mathf.Abs(Mathf.DeltaAngle(angle, currentIdealAngleRight)) : 999f;  // 適正角度との差　Mathf.Absは「絶対値」を取る関数
        float diffLeft = isLeftZoneActive ? Mathf.Abs(Mathf.DeltaAngle(angle, currentIdealAngleLeft)) : 999f;

        float diff = Mathf.Min(diffRight, diffLeft); // どちらか近い方(小さい方)を採用
        bool isgoodangle = diff <= toleranceRange;　// 差が許容範囲以内かどうかで判定
        bool isgoodshift = IsShiftGaugeGood();
        bool isTotallyGood = isgoodangle && isgoodshift; //　両方そろって初めてgood

        // --- 即終了判定(真下エリア) ---
        if (ignitionGraceTimer > 0f)
        {
            ignitionGraceTimer -= Time.deltaTime;
        }
        if (ignitionGraceTimer <= 0f)
        {
            float dangerZoneCenter = -90f; // 真下の角度
            float dangerZoneRange = 30f;   // 真下からこの範囲に入ったら即アウト

            float distFromDanger = Mathf.Abs(Mathf.DeltaAngle(angle, dangerZoneCenter));
            if (distFromDanger <= dangerZoneRange)
            {
                Debug.Log("危険角度！ 即ゲームオーバー");
                currentState = GameState.GameOver;
                UpdateUIVisibility();
                gameOverSequence.StartSequence(score);
                return;
            }   
        }
        
        UpdateHandleVisual(angle);
        
        // --- 時間管理 ---
        elapsedTime += Time.deltaTime; // Time.deltaTimeは「前のフレームからどれだけ時間が経過したか」を秒単位で返す値
        if (remainingTime <= 0 )
        {
            currentState = GameState.GameOver;
            UpdateUIVisibility();
            gameOverSequence.StartSequence(score);
            return;
        }
        if (isTotallyGood)
        {
            remainingTime -= Time.deltaTime;
        }
        else
        {
            remainingTime -= Time.deltaTime * 4f; // 角度が悪いと時間が早く減る
        }
        
        // --- スコア加算 ---
        float multiplier = GetMultiplier(elapsedTime);
        score += baseScorePerSecond * multiplier * Time.deltaTime;

        // --- 見た目の更新 ---
        UpdateFireballColor(elapsedTime);
        UpdateUI();

        Debug.Log("角度差: " + diff + "/スコア: " + score + "/経過時間" + elapsedTime);
    }
    
    void UpdateGameOver()
    {
   
    }     

    public void OnBackToTitleClicked() // ボタンクリックでタイトル画面へ
    {
        score = 0f;
        remainingTime = 60f;
        elapsedTime = 0f;
        ignitionGaugeValue = 0f;
        ignitionGoingUp = true;
        toleranceRange = 20f;
        fireballCoreRenderer.transform.localScale = coreOriginalScale;
        currentHanabiPhase = 0;
        if (audioManager != null)
        {
            audioManager.StopBGM();
            audioManager.StopSE();
            audioManager.StopHanabiSE();
        }
        currentState = GameState.Title;
        UpdateUIVisibility();
        ignitionSequence.StartSequence();
    }
    public void OnRestartButtonClicked() // ボタンクリックでもう一度
    {
        score = 0f;
        remainingTime = 60f;
        elapsedTime = 0f;
        ignitionGaugeValue = 0f;
        ignitionGoingUp = true;
        toleranceRange = 20f;
        fireballCoreRenderer.transform.localScale = coreOriginalScale;
        currentHanabiPhase = 0;
        if (audioManager != null)
        {
            audioManager.StopSE();
            audioManager.StopHanabiSE();
            audioManager.PlayBGM(audioManager.bgmbeforeIgnition, true, 1.0f);
        }
        currentState = GameState.Ignition;
        UpdateUIVisibility();
        ignitionSequence.StartSequence();
    }
    
}
