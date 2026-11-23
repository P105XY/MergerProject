using Mech.Function;
using UnityEngine;

public class PlayTestDisplay : MonoBehaviour
{
    private const float SlotSize = 82f;
    private const float SlotGap = 7f;
    private const float ScreenMargin = 16f;
    private static readonly Color emptyBorder = new Color32(54, 69, 86, 255);
    private static readonly Color slotBackground = new Color32(16, 24, 36, 240);
    private static readonly Color specialBorder = new Color32(183, 142, 255, 255);

    [SerializeField] private PlayerBase player;
    [SerializeField] private TestEnemy[] enemies;
    [SerializeField] private SpriteRenderer[] shapes;
    [SerializeField] private SpriteRenderer attackIndicator;
    private Sprite generatedSprite;
    private float attackVisibleUntil;
    private JumpPathDisplay jumpPath;
    private GUIStyle fragmentLabel;
    private GUIStyle specialLabel;
    private GUIStyle usesLabel;

    // 테스트 장면의 임시 도형과 공격 표시를 준비한다.
    private void Awake()
    {
        Texture2D texture = Texture2D.whiteTexture;
        generatedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);

        foreach (SpriteRenderer shape in shapes) {
            if (shape != null) {
                shape.sprite = generatedSprite;
            }
        }

        player.AttackRequested += ShowAttack;
        InitializeJumpPath();
        attackIndicator.enabled = false;
    }

    // 공격 표시 시간을 확인하고 처치된 적은 충돌을 끈 뒤 플래시가 끝나면 숨긴다.
    private void Update()
    {
        attackIndicator.enabled = Time.time < attackVisibleUntil;

        foreach (TestEnemy enemy in enemies) {
            if (enemy != null && enemy.IsDefeated && enemy.gameObject.activeSelf) {
                if (!enemy.IsWhiteFlashing) {
                    enemy.gameObject.SetActive(false);
                } else if (enemy.TryGetComponent(out Collider2D hitbox)) {
                    hitbox.enabled = false;
                }
            }
        }
    }

    // 실제 공격 판정과 같은 중심·회전·크기로 짧게 표시한다.
    private void ShowAttack(PlayerBase source)
    {
        Transform indicator = attackIndicator.transform;
        indicator.SetPositionAndRotation(source.LastAttackCenter, Quaternion.Euler(0f, 0f, source.LastAttackAngle));
        float depthScale = indicator.localScale.z;
        indicator.localScale = new Vector3(1f, 1f, depthScale);
        Vector3 worldScale = indicator.lossyScale;
        Vector2 size = source.AttackSize;
        float width = worldScale.x != 0f ? size.x / Mathf.Abs(worldScale.x) : 0f;
        float height = worldScale.y != 0f ? size.y / Mathf.Abs(worldScale.y) : 0f;
        indicator.localScale = new Vector3(width, height, depthScale);
        attackVisibleUntil = Time.time + 0.15f;
    }

    // 물리 처리가 끝난 뒤 실제 점프 경로와 남은 예상 경로를 갱신한다.
    private void LateUpdate()
    {
        InitializeJumpPath();
        jumpPath.UpdatePath();
    }

    // 초기화 순서와 관계없이 점프 표시를 만들고 시작 이벤트를 한 번 연결한다.
    private void InitializeJumpPath()
    {
        if (jumpPath == null) {
            jumpPath = new JumpPathDisplay(player);
            player.JumpStarted += jumpPath.Begin;
        }
    }

    // 화면 왼쪽 아래에 파편 게이지 또는 방향 입력과 무기 상태를 표시한다.
    private void OnGUI()
    {
        jumpPath?.Draw();

        if (Event.current.type != EventType.Repaint || player == null) {
            return;
        }

        Rect first = GetSlotBounds(0, Screen.width, Screen.height);

        if (first.width < 16f) {
            return;
        }

        if (fragmentLabel == null) {
            fragmentLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            specialLabel = new GUIStyle(fragmentLabel) { wordWrap = true };
            usesLabel = new GUIStyle(fragmentLabel) { fontStyle = FontStyle.Normal };
            usesLabel.normal.textColor = specialBorder;
        }

        fragmentLabel.fontSize = Mathf.FloorToInt(first.width * 0.33f);
        specialLabel.fontSize = Mathf.FloorToInt(first.width * 0.16f);
        usesLabel.fontSize = Mathf.FloorToInt(first.width * 0.14f);
        for (int index = 0; index < PlayerBase.FragmentSlotCount; index++) {
            DrawCombinationSlot(index);
        }

        Rect special = GetSlotBounds(PlayerBase.FragmentSlotCount, Screen.width, Screen.height);
        DrawSlot(special, player.IsSpecialAvailable || player.IsFocusingWeapon || player.CanFocusWeapon ? specialBorder : emptyBorder);

        if (player.IsSpecialAvailable) {
            string name = player.GetActiveSpecialName();
            GUI.Label(new Rect(special.x + 3f, special.y + 2f, special.width - 6f, special.height * 0.72f), name, specialLabel);
            GUI.Label(new Rect(special.x, special.y + special.height * 0.72f, special.width, special.height * 0.24f),
                $"D  ×{player.SpecialUsesRemaining}", usesLabel);
        } else if (player.IsFocusingWeapon) {
            GUI.Label(special, "Focus", specialLabel);
        } else if (player.WeaponGauge == PlayerBase.WeaponGaugeCapacity) {
            GUI.Label(special, player.CanFocusWeapon ? "Hold D" : "Wait", specialLabel);
        }
    }

    // 평상시에는 게이지를, 집중 중에는 입력 순서에 따른 방향을 그린다.
    private void DrawCombinationSlot(int index)
    {
        Rect bounds = GetSlotBounds(index, Screen.width, Screen.height);
        Color chargeColor = new Color32(115, 244, 213, 255);

        if (player.IsFocusingWeapon && index < player.CombinationInputs.Count) {
            string arrow = player.CombinationInputs[index] switch {
                CollectibleType.A => "←",
                CollectibleType.B => "↓",
                _ => "→"
            };
            DrawSlot(bounds, specialBorder);
            fragmentLabel.normal.textColor = specialBorder;
            GUI.Label(bounds, arrow, fragmentLabel);
        } else if (!player.IsFocusingWeapon && index < player.WeaponGauge) {
            DrawSlot(bounds, chargeColor);
            fragmentLabel.normal.textColor = chargeColor;
            GUI.Label(bounds, "◆", fragmentLabel);
        } else {
            DrawSlot(bounds, emptyBorder);

            if (!player.IsFocusingWeapon && index == player.WeaponGauge && player.HitsTowardFragment > 0) {
                GUI.Label(bounds, $"{player.HitsTowardFragment}/{WeaponFocus.HitsPerFragment}", usesLabel);
            }
        }
    }

    // 네 칸이 화면 안에 들어오도록 같은 크기의 정사각형 위치를 계산한다.
    private static Rect GetSlotBounds(int index, int screenWidth, int screenHeight)
    {
        int slots = PlayerBase.FragmentSlotCount + 1;
        float availableWidth = screenWidth - 2f * ScreenMargin - (slots - 1) * SlotGap;
        float size = Mathf.Min(SlotSize, Mathf.Floor(availableWidth / slots), screenHeight - 2f * ScreenMargin);
        return new Rect(ScreenMargin + index * (size + SlotGap), screenHeight - ScreenMargin - size, size, size);
    }

    // 슬롯에 테두리와 배경을 그리고 GUI 색을 복원한다.
    private static void DrawSlot(Rect bounds, Color border)
    {
        Color original = GUI.color;
        GUI.color = border;
        GUI.DrawTexture(bounds, Texture2D.whiteTexture);
        GUI.color = slotBackground;
        GUI.DrawTexture(new Rect(bounds.x + 1f, bounds.y + 1f, bounds.width - 2f, bounds.height - 2f), Texture2D.whiteTexture);
        GUI.color = original;
    }

    // 테스트 화면에서 생성한 스프라이트와 이벤트 구독을 정리한다.
    private void OnDestroy()
    {
        if (player != null) {
            player.AttackRequested -= ShowAttack;

            if (jumpPath != null) {
                player.JumpStarted -= jumpPath.Begin;
            }
        }

        if (generatedSprite != null) {
            Destroy(generatedSprite);
        }
    }
}
