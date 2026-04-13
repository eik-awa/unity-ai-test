using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// タコ（プレイヤー）の移動制御。
/// WASD / 矢印キーで上下左右に動く。
/// PixelPerfect な動きになるよう整数スナップ機能付き。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // ────────────────────────────────────────────────
    //  参照
    // ────────────────────────────────────────────────
    public static PlayerController Instance { get; private set; }

    [Header("コンポーネント")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator       animator; // 任意

    // ────────────────────────────────────────────────
    //  設定
    // ────────────────────────────────────────────────
    [Header("移動境界（カメラ範囲に合わせて設定）")]
    [SerializeField] private float boundX = 8.5f;
    [SerializeField] private float boundY = 4.5f;

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private Rigidbody2D    _rb;
    private PlayerStats    _stats;
    private Vector2        _moveInput;
    private bool           _canMove = true;
    private VirtualJoystick _moveJoystick; // モバイル用（OctoShooterSetup から設定）

    // ────────────────────────────────────────────────
    //  Unity ライフサイクル
    // ────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _rb    = GetComponent<Rigidbody2D>();
        _stats = GetComponent<PlayerHealth>()?.Stats
              ?? new PlayerStats(); // フォールバック
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged.AddListener(OnGameStateChanged);
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged.RemoveListener(OnGameStateChanged);
    }

    private void Update()
    {
        if (!_canMove) return;
        ReadInput();
        FlipSprite();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (!_canMove) { _rb.linearVelocity = Vector2.zero; return; }
        Move();
    }

    // ────────────────────────────────────────────────
    //  内部
    // ────────────────────────────────────────────────
    private void ReadInput()
    {
        // ── キーボード（PC） ──
        Vector2 keyInput = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed  ? 1f : 0f);
            keyInput = new Vector2(x, y);
        }

        // ── バーチャルジョイスティック（スマホ） ──
        Vector2 joyInput = _moveJoystick != null ? _moveJoystick.Direction : Vector2.zero;

        _moveInput = keyInput + joyInput;
        if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();
    }

    private void Move()
    {
        _rb.linearVelocity = _moveInput * _stats.moveSpeed;

        // 画面外に出ないようにクランプ
        Vector2 pos = _rb.position;
        pos.x = Mathf.Clamp(pos.x, -boundX, boundX);
        pos.y = Mathf.Clamp(pos.y, -boundY, boundY);
        _rb.position = pos;
    }

    private void FlipSprite()
    {
        if (spriteRenderer == null) return;
        if (_moveInput.x > 0.01f)       spriteRenderer.flipX = false;
        else if (_moveInput.x < -0.01f) spriteRenderer.flipX = true;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", _moveInput.magnitude);
        animator.SetFloat("MoveX", _moveInput.x);
        animator.SetFloat("MoveY", _moveInput.y);
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        _canMove = (state == GameManager.GameState.Playing);
    }

    // ────────────────────────────────────────────────
    //  公開 API（他スクリプトから使う）
    // ────────────────────────────────────────────────

    /// <summary>外部から Stats を差し替えるとき（PlayerHealth が持っているため通常は不要）</summary>
    public void SetStats(PlayerStats stats)
    {
        _stats = stats;
    }

    /// <summary>モバイル用移動ジョイスティックを接続する（OctoShooterSetup から呼ぶ）</summary>
    public void SetMoveJoystick(VirtualJoystick joystick)
    {
        _moveJoystick = joystick;
    }
}
