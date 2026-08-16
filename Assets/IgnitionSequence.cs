using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;

public class IgnitionSequence : MonoBehaviour
{
   [Header("演出に必要な参照")]
   public Transform handleTransform; //　持ち手
   public GameObject lighter; // チャッカマン
   public GameObject ignitionGaugeUI; // 着火ゲージのUI一式(親のGameObject)
   public RectTransform ignitionMarker; // 動く目印のRectTransform

    public void StartSequence()
    {
        // --- 1. 一度着火ゲージを消す ---
        if (ignitionGaugeUI != null) ignitionGaugeUI.gameObject.SetActive(false);
        if (ignitionMarker != null) ignitionMarker.gameObject.SetActive(false);

        Sequence seq = DOTween.Sequence();
        if (handleTransform != null)
        {
            // seq.Append(handleTransform.DOMoveX()) 左から右へ（あとで座標決定）
        }
    }
}
