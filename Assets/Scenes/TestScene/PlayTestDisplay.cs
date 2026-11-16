using Mech.Function;
using UnityEngine;

public class PlayTestDisplay : MonoBehaviour
{
    [SerializeField] private PlayerBase player;
    [SerializeField] private TestEnemy[] enemies;
    [SerializeField] private SpriteRenderer[] shapes;
    [SerializeField] private SpriteRenderer attackIndicator;
    private Sprite generatedSprite;
    private float attackVisibleUntil;
    private JumpPathDisplay jumpPath;

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

    // 공격 방향을 표시하고 처치된 적은 충돌을 끈 뒤 플래시가 끝나면 숨긴다.
    private void Update()
    {
        attackIndicator.enabled = Time.time < attackVisibleUntil;
        attackIndicator.transform.localPosition = new Vector3(player.FacingDirection * 1.1f, 0f, 0f);

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

    // 공격이 요청되면 짧은 시간 동안 공격 범위를 표시한다.
    private void ShowAttack(PlayerBase source)
    {
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

    // 플레이어 체력, 조작법과 조각 및 특수 기능 상태를 화면에 표시한다.
    private void OnGUI()
    {
        jumpPath?.Draw();
        GUI.Box(new Rect(16, 16, 390, 130), "PLAY TEST");
        GUI.Label(new Rect(280, 15, 115, 22), $"HP {player.CurrentHealth:0.#} / {player.MaxHealth:0.#}");
        GUI.Label(new Rect(30, 42, 365, 22), "Arrows: Move    A: Jump + path    S: Attack");
        GUI.Label(new Rect(30, 66, 365, 22), $"Fragments: A {player.TypeACount}   B {player.TypeBCount}   C {player.TypeCCount}");
        GUI.Label(new Rect(30, 90, 365, 22), $"D: Special request   Uses: {player.SpecialUsesRemaining}");
        GUI.Label(new Rect(30, 114, 365, 22), "Collect 3 fragments, then press D 3 times.");
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
