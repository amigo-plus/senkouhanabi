using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;
using System.Security.Cryptography.X509Certificates;

public class IgnitionSequence : MonoBehaviour
{
   [Header("演出に必要な参照")]
   public Transform handleTransform; //　持ち手
   public GameObject lighter; // ライター
   public GameObject ignitionGaugeUI; // 着火ゲージのUI一式(親のGameObject)
   public RectTransform ignitionMarker; // 動く目印のRectTransform

   [Header("配置ポジション")]
   public Vector3 handleStartPos;
   public Vector3 handleEndPos;
   public float handleAngle = -45;
   public Vector3 lighterStartPos;
   public Vector3 lighterEndPos;

   [Header("演出タイミング")]
   public float handlemovetime = 1f; // 持ち手がinするまでかかる時間
   public float lightermovetime = 1f; // ↑のlighter版 
   public float delayBeforeGauge = 0.5f; // ゲージ登場までの時間

    public void StartSequence()
    {
        // --- 1. 一度着火ゲージを消す ---
        if (ignitionGaugeUI != null) ignitionGaugeUI.gameObject.SetActive(false);
        if (ignitionMarker != null) ignitionMarker.gameObject.SetActive(false);

        // --- 2. 持ち手とライターを開始位置に ---
        if (handleTransform != null)
        {
            handleTransform.position = handleStartPos;
            handleTransform.rotation = Quaternion.Euler(0f, 0f, handleAngle);
        }
        if (lighter != null) lighter.transform.position = lighterStartPos;

        // --- 3. DOTween構築・画面にオブジェクトin --- 
        Sequence seq = DOTween.Sequence();
        if (handleTransform != null)
        {  
            seq.Append(handleTransform.DOMove(handleEndPos, handlemovetime).SetEase(Ease.OutCubic)); // 右から左へ（あとで座標決定）
        }

        if (lighter != null)
        {
            seq.Join(lighter.transform.DOMove(lighterEndPos, lightermovetime).SetEase(Ease.OutCubic)); // 下から上に
        }

        // --- 4. ちょっと待機 ---
        seq.AppendInterval(delayBeforeGauge);

        // --- 5. ゲージ登場 --- 
        seq.OnComplete(() =>
        {
            if (ignitionGaugeUI != null) ignitionGaugeUI.gameObject.SetActive(true);
            if (ignitionGaugeUI != null) ignitionMarker.gameObject.SetActive(true); 
        });



    }
}
