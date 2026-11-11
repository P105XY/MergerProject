using System.Collections.Generic;
using Mech.Function;
using UnityEngine;

internal sealed class JumpPathDisplay
{
    private const float TrailDuration = 1.25f;
    private readonly PlayerBase player;
    private readonly Rigidbody2D body;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
    private readonly List<Vector2> traveled = new List<Vector2>(128);
    private readonly List<Vector2> predicted = new List<Vector2>(32);
    private bool tracking;
    private float hideAt;

    // 플레이 테스트의 플레이어와 물리 바디를 연결한다.
    internal JumpPathDisplay(PlayerBase player)
    {
        this.player = player;
        body = player.GetComponent<Rigidbody2D>();
    }

    // 점프가 실제로 시작되면 이륙 위치부터 새 경로를 기록한다.
    internal void Begin()
    {
        traveled.Clear();
        predicted.Clear();
        traveled.Add(body.position);
        tracking = true;
    }

    // 물리 갱신 뒤에 현재 위치를 추가하고 아직 공중이면 예상 경로를 계산한다.
    internal void UpdatePath()
    {
        if (!tracking || player.IsDead || !player.isActiveAndEnabled) {
            return;
        }

        if (traveled[traveled.Count - 1] != body.position && traveled.Count < 128) {
            traveled.Add(body.position);
        }

        if (player.IsJumping) {
            PredictPath();
        } else {
            predicted.Clear();
            tracking = false;
            hideAt = Time.unscaledTime + TrailDuration;
        }
    }

    // 현재 입력이 이어진다고 가정해 최대 1.8초 또는 첫 충돌까지의 궤적을 예측한다.
    private void PredictPath()
    {
        predicted.Clear();
        Vector2 position = body.position;
        Vector2 velocity = body.linearVelocity;
        Vector2 riseGravity = Physics2D.gravity * body.gravityScale;
        Vector2 fallGravity = Physics2D.gravity * player.JumpFallGravityScale;
        bool descending = player.IsJumpDescending;
        float deltaTime = Time.fixedDeltaTime;
        float damping = 1f + body.linearDamping * deltaTime;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(player.JumpGroundLayers.value & Physics2D.GetLayerCollisionMask(player.gameObject.layer));
        predicted.Add(position);

        for (int step = 1; step * deltaTime <= 1.8f; step++) {
            velocity = player.PredictJumpVelocity(position, velocity, deltaTime);
            descending |= velocity.y <= 0f;
            Vector2 gravity = descending ? fallGravity : riseGravity;
            velocity = (velocity + gravity * deltaTime) / damping;
            Vector2 displacement = velocity * deltaTime;
            float distance = displacement.magnitude;

            if (distance <= 0.0001f) {
                continue;
            }

            Vector2 direction = displacement / distance;
            hits.Clear();
            body.Cast(position, body.rotation, direction, filter, hits, distance);
            bool blocked = false;

            foreach (RaycastHit2D hit in hits) {
                if (Vector2.Dot(direction, hit.normal) < -0.01f) {
                    position += direction * hit.distance;
                    predicted.Add(position);
                    blocked = true;
                    break;
                }
            }

            if (blocked) {
                break;
            }

            position += displacement;

            if (step % 3 == 0) {
                predicted.Add(position);
            }
        }
    }

    // 게임 화면에 실제 이동은 민트색 실선으로, 예측은 노란 점으로 그린다.
    internal void Draw()
    {
        if (Event.current.type != EventType.Repaint || player.IsDead || !player.isActiveAndEnabled
            || traveled.Count == 0 || (!tracking && Time.unscaledTime >= hideAt)) {
            return;
        }

        Camera camera = Camera.main;

        if (camera == null) {
            return;
        }

        Color oldColor = GUI.color;
        GUI.color = new Color(0.3f, 0.94f, 0.77f);

        for (int index = 1; index < traveled.Count; index++) {
            DrawSegment(ToScreen(camera, traveled[index - 1]), ToScreen(camera, traveled[index]));
        }

        GUI.color = new Color(1f, 0.82f, 0.34f);

        for (int index = 1; index < predicted.Count; index++) {
            Vector2 screen = ToScreen(camera, predicted[index]);
            GUI.DrawTexture(new Rect(screen.x - 2f, screen.y - 2f, 4f, 4f), Texture2D.whiteTexture);
        }

        GUI.color = oldColor;
    }

    // 카메라 좌표를 게임 화면 GUI의 위쪽 원점 좌표로 변환한다.
    private static Vector2 ToScreen(Camera camera, Vector2 point)
    {
        Vector3 screen = camera.WorldToScreenPoint(point);
        return new Vector2(screen.x, Screen.height - screen.y);
    }

    // 게임 화면의 두 점을 두께 3픽셀의 선으로 잇는다.
    private static void DrawSegment(Vector2 start, Vector2 end)
    {
        Vector2 difference = end - start;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg, start);
        GUI.DrawTexture(new Rect(start.x, start.y - 1.5f, difference.magnitude + 1f, 3f), Texture2D.whiteTexture);
        GUI.matrix = oldMatrix;
    }
}
