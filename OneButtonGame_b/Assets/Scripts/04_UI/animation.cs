using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections; // コルーチンのために必要

public class ButtonScaler : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    public float pulseSpeed = 1.0f; // 脈動の速さ
    public float pulseAmount = 0.1f; // 脈動の大きさ (1.0f + 0.1f = 1.1倍)

    private Coroutine pulseCoroutine = null;
    private bool isSelectedOrHovered = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    // --- 選択またはマウスが乗った ---
    private void StartPulsing()
    {
        isSelectedOrHovered = true;
        // 既にコルーチンが動いていなければ新しく開始
        if (pulseCoroutine == null)
        {
            pulseCoroutine = StartCoroutine(Pulse());
        }
    }

    // --- 選択解除またはマウスが離れた ---
    private void StopPulsing()
    {
        isSelectedOrHovered = false;
        // 既にコルーチンが動いていれば停止
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        // 必ず元のサイズに戻す
        transform.localScale = originalScale;
    }

    // 脈動させるコルーチン
    private IEnumerator Pulse()
    {
        while (isSelectedOrHovered)
        {
            // Mathf.Sin を使うと滑らかに -1.0 ～ 1.0 を往復する
            // (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f で 0.0 ～ 1.0 の範囲に変換
            float scaleFactor = 1.0f + ((Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1.0f) / 2.0f) * pulseAmount;

            // PingPong を使う場合
            // float scaleFactor = 1.0f + Mathf.PingPong(Time.time * pulseSpeed, pulseAmount);

            transform.localScale = originalScale * scaleFactor;

            yield return null; // 1フレーム待つ
        }
    }

    // --- イベントハンドラ ---
    public void OnSelect(BaseEventData eventData) { StartPulsing(); }
    public void OnDeselect(BaseEventData eventData) { StopPulsing(); }
    public void OnPointerEnter(PointerEventData eventData) { StartPulsing(); }
    public void OnPointerExit(PointerEventData eventData) { StopPulsing(); }
}