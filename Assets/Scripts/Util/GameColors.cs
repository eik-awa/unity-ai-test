using UnityEngine;

/// <summary>
/// ゲーム全体で使うカラーパレット
/// テーマ: くすみブルー・紫・ピンク / 水族館・水彩ドット
/// </summary>
public static class GameColors
{
    // ── 水・海 ──────────────────────────────────────
    public static readonly Color DustyBlue    = HexColor("6B9BAB"); // くすみブルー（メイン）
    public static readonly Color SkyBlue      = HexColor("8DB4C7"); // 明るいくすみブルー
    public static readonly Color DeepTeal     = HexColor("4A7A8A"); // 深みのある青緑
    public static readonly Color AquaLight    = HexColor("B3D8E4"); // 薄い水色（背景用）

    // ── 紫・ピンク ──────────────────────────────────
    public static readonly Color MutedPurple  = HexColor("9B7EC8"); // 鮮やかすぎない紫
    public static readonly Color LightPurple  = HexColor("B89DD4"); // 明るい紫
    public static readonly Color DustyPink    = HexColor("D4789C"); // くすみピンク
    public static readonly Color LightPink    = HexColor("E8A0B4"); // 明るいピンク

    // ── 背景・影 ────────────────────────────────────
    public static readonly Color DeepNavy     = HexColor("2D3561"); // 深い紺（UI背景）
    public static readonly Color DarkBg       = HexColor("1A1A2E"); // 最暗色（ゲーム背景）
    public static readonly Color MidBg        = HexColor("25254A"); // 中間背景

    // ── テキスト・UI ────────────────────────────────
    public static readonly Color TextLight    = HexColor("F0ECFA"); // 明るいテキスト
    public static readonly Color TextDim      = HexColor("9B9BC0"); // 薄いテキスト
    public static readonly Color AccentYellow = HexColor("F5D76E"); // ポイント色（コイン・スコア）
    public static readonly Color DangerRed    = HexColor("E8637A"); // ダメージ・警告

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
