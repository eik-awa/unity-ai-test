using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// スマホ向けバーチャルジョイスティック。
/// Canvas 上のUI要素として配置し、タッチ（またはマウス）で操作する。
///
/// 使い方:
///   Direction  → 正規化された入力方向 (-1〜1)
///   IsHeld     → 現在押されているか
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // ────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform knob;

    [Range(0f, 0.3f)]
    [SerializeField] private float deadZone = 0.1f; // この値以下の入力は無視

    // ────────────────────────────────────────────────
    //  公開プロパティ
    // ────────────────────────────────────────────────
    public Vector2 Direction { get; private set; }
    public bool    IsHeld    { get; private set; }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private float _radius;

    private void Start()
    {
        // background の半径（ローカル座標系）
        _radius = background != null ? background.rect.width * 0.5f : 80f;
    }

    // ────────────────────────────────────────────────
    //  タッチ / ポインターイベント
    // ────────────────────────────────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        IsHeld = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null) return;

        // スクリーン座標 → background のローカル座標に変換
        Vector2 localPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out localPos))
            return;

        // 半径内にクランプ
        localPos = Vector2.ClampMagnitude(localPos, _radius);
        if (knob != null) knob.localPosition = localPos;

        // 方向を計算（-1〜1 に正規化）
        Vector2 raw = localPos / _radius;
        Direction = raw.magnitude < deadZone ? Vector2.zero : raw;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Direction = Vector2.zero;
        IsHeld    = false;
        if (knob != null) knob.localPosition = Vector2.zero;
    }
}
