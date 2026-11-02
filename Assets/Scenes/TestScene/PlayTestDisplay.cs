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
        attackIndicator.enabled = false;
    }

    // 공격 방향을 표시하고 처치된 적의 외형과 충돌체를 끈다.
    private void Update()
    {
        attackIndicator.enabled = Time.time < attackVisibleUntil;
        attackIndicator.transform.localPosition = new Vector3(player.FacingDirection * 1.1f, 0f, 0f);

        foreach (TestEnemy enemy in enemies) {
            if (enemy != null && enemy.IsDefeated && enemy.gameObject.activeSelf) {
                enemy.gameObject.SetActive(false);
            }
        }
    }

    // 공격이 요청되면 짧은 시간 동안 공격 범위를 표시한다.
    private void ShowAttack(PlayerBase source)
    {
        attackVisibleUntil = Time.time + 0.15f;
    }

    // 플레이어 입력과 조각 및 특수 기능 상태를 화면에 표시한다.
    private void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 390, 130), "PLAY TEST");
        GUI.Label(new Rect(30, 42, 365, 22), "Arrow keys: Move    S: Jump    A: Attack");
        GUI.Label(new Rect(30, 66, 365, 22), $"Fragments: A {player.TypeACount}   B {player.TypeBCount}   C {player.TypeCCount}");
        GUI.Label(new Rect(30, 90, 365, 22), $"D: Special request   Uses: {player.SpecialUsesRemaining}");
        GUI.Label(new Rect(30, 114, 365, 22), "Collect 3 fragments, then press D 3 times.");
    }

    // 테스트 화면에서 생성한 스프라이트와 이벤트 구독을 정리한다.
    private void OnDestroy()
    {
        if (player != null) {
            player.AttackRequested -= ShowAttack;
        }

        if (generatedSprite != null) {
            Destroy(generatedSprite);
        }
    }
}
