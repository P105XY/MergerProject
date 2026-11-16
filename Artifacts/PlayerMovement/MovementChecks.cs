using System;
using System.Reflection;
using Mech.Function;
using UnityEngine;
using UnityEngine.InputSystem;

public static class MovementChecks
{
    private static int passed;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    // 입력과 기대값 및 실제값을 출력하고 허용 오차 내 일치를 확인한다.
    private static void Check(string input, double expected, double actual, double tolerance = 0.0001)
    {
        Console.WriteLine($"입력: {input}, 기대: {expected:F5}, 실제: {actual:F5}");

        if (double.IsNaN(actual) || Math.Abs(expected - actual) > tolerance) {
            throw new Exception("Assertion failed: " + input);
        }

        passed++;
    }

    // 검증 대상 플레이어의 비공개 수치를 지정한다.
    private static void Set(PlayerBase player, string name, object value)
    {
        typeof(PlayerBase).GetField(name, PrivateInstance).SetValue(player, value);
    }

    // 실제 플레이어의 Unity 콜백 메서드를 직접 호출한다.
    private static void Invoke(PlayerBase player, string name)
    {
        typeof(PlayerBase).GetMethod(name, PrivateInstance).Invoke(player, null);
    }

    // 수치 비교용 기존 기준 설정으로 테스트 플레이어를 만든다.
    private static PlayerBase Create(float deltaTime = 0.02f, float gravityScale = 0f)
    {
        Time.fixedDeltaTime = deltaTime;
        Keyboard.current = new Keyboard();
        PlayerBase player = new PlayerBase();
        Set(player, "movementAcceleration", 40f);
        Set(player, "movementDeceleration", 50f);
        Set(player, "jumpForce", 8f);
        Set(player, "gravityScale", gravityScale);
        Invoke(player, "Awake");
        return player;
    }

    // 실제 입력 처리와 물리 갱신을 한 번 진행한다.
    private static void Step(PlayerBase player, int direction = 0, bool pressed = false, bool held = false)
    {
        Keyboard keyboard = Keyboard.current;
        keyboard.leftArrowKey.isPressed = direction < 0;
        keyboard.rightArrowKey.isPressed = direction > 0;
        keyboard.aKey.wasPressedThisFrame = pressed;
        keyboard.aKey.isPressed = held;
        Invoke(player, "Update");
        Invoke(player, "FixedUpdate");
    }

    // 단순한 하향 중력 적분으로 버튼 유지에 따른 점프 최고점을 비교한다.
    private static float Peak(bool hold, float heightLimit)
    {
        PlayerBase player = Create(gravityScale: 1f);
        Set(player, "maximumJumpHeight", heightLimit);
        Step(player, pressed: true, held: hold);
        player.TestBody.TestGrounded = false;
        float peak = 0f;

        for (int tick = 0; tick < 300; tick++) {
            Vector2 velocity = player.TestBody.linearVelocity;
            velocity.y -= 9.81f * player.TestBody.gravityScale * Time.fixedDeltaTime;
            player.TestBody.linearVelocity = velocity;
            player.TestBody.position += velocity * Time.fixedDeltaTime;
            peak = Math.Max(peak, player.TestBody.position.y);

            if (player.TestBody.position.y < 0f) {
                break;
            }

            Step(player, held: hold);
        }

        return peak;
    }

    // 가속·감속과 점프 입력의 주요 경계 동작을 검증한다.
    public static void Main()
    {
        // 입력: dt 0.02에서 우측 1틱 → 기대: 속도 0.8, 즉시 최대 속도로 이동하지 않음.
        PlayerBase player = Create();
        Step(player, 1);
        Check("우측 첫 가속", 0.8, player.TestBody.linearVelocity.x);

        // 입력: 우측 지속 → 기대: 최대 속도 5로 제한.
        for (int tick = 0; tick < 20; tick++) {
            Step(player, 1);
        }

        Check("최대 속도 제한", 5, player.TestBody.linearVelocity.x);
        // 입력: 이동키 해제 1틱 및 총 5틱 → 기대: 4, 이후 0.
        Step(player);
        Check("감속 첫 틱", 4, player.TestBody.linearVelocity.x);

        for (int tick = 0; tick < 4; tick++) {
            Step(player);
        }

        Check("감속 정지", 0, player.TestBody.linearVelocity.x);
        // 입력: 반대 방향 → 기대: 좌측으로 점진적 가속.
        Step(player, -1);
        Check("좌측 가속", -0.8, player.TestBody.linearVelocity.x);
        // 입력: 가속도와 감속도 0 → 기대: 기존 속도 보존.
        Set(player, "movementAcceleration", 0f);
        Step(player, 1);
        Check("가속도 0", -0.8, player.TestBody.linearVelocity.x);
        Set(player, "movementDeceleration", 0f);
        Step(player);
        Check("감속도 0", -0.8, player.TestBody.linearVelocity.x);
        // 입력: 같은 0.1초를 서로 다른 dt로 진행 → 기대: 속도 4로 일치.
        player = Create(0.01f);

        for (int tick = 0; tick < 10; tick++) {
            Step(player, 1);
        }

        Check("dt 0.01 가속 0.1초", 4, player.TestBody.linearVelocity.x);
        player = Create();

        for (int tick = 0; tick < 5; tick++) {
            Step(player, 1);
        }

        Check("dt 0.02 가속 0.1초", 4, player.TestBody.linearVelocity.x);
        // 입력: 지상 점프 후 공중에서 다시 누름 → 기대: 점프 속도 8, 두 번째 입력으로 재점프하지 않음.
        player = Create();
        Step(player, pressed: true, held: true);
        Check("지상 점프", 8, player.TestBody.linearVelocity.y);
        player.TestBody.TestGrounded = false;
        player.TestBody.linearVelocity = new Vector2(0, 6f);
        Step(player, pressed: true, held: true);
        Check("공중 재점프 거부", 6, player.TestBody.linearVelocity.y);
        // 입력: 중력 배율 0, 상승 속도 6에서 해제 → 기대: 6 × -0.75 = -4.5.
        Step(player);
        Check("중력 배율 0에서 해제 시 하강 전환", -4.5, player.TestBody.linearVelocity.y);
        // 입력: 상승 속도 6, 중력 배율 9.8, 질량 2에서 해제 → 기대: 동일한 -4.5, 점프당 한 번 적용.
        player = Create(gravityScale: 9.8f);
        player.TestBody.mass = 2f;
        Step(player, pressed: true, held: true);
        player.TestBody.TestGrounded = false;
        player.TestBody.linearVelocity = new Vector2(0f, 6f);
        Check("상승 중 기본 중력 배율", 9.8, player.TestBody.gravityScale);
        Check("예측에 사용할 하강 중력 배율", 17.15, player.JumpFallGravityScale);
        Step(player);
        Check("해제 시 현재 수직 속도에 -0.75 적용", -4.5, player.TestBody.linearVelocity.y);
        Check("해제로 하강 전환 시 중력 배율", 17.15, player.TestBody.gravityScale);
        player.TestBody.linearVelocity = new Vector2(0f, 2f);
        Step(player);
        Check("같은 점프에서 해제 배율 중복 적용 없음", 2, player.TestBody.linearVelocity.y);
        Check("착지 전까지 하강 중력 배율 유지", 17.15, player.TestBody.gravityScale);
        // 입력: 착지와 같은 갱신에 다시 점프한 뒤 S를 유지해 최고점 도달 → 기대: 상승 9.8, 최고점 17.15.
        player.TestBody.linearVelocity = new Vector2(0f, -1f);
        player.TestBody.TestGrounded = true;
        Step(player, pressed: true, held: true);
        Check("착지 후 재점프의 기본 중력 배율", 9.8, player.TestBody.gravityScale);
        player.TestBody.TestGrounded = false;
        player.TestBody.linearVelocity = Vector2.zero;
        Step(player, held: true);
        Check("유지 점프 최고점의 Y 속도", 0, player.TestBody.linearVelocity.y);
        Check("유지 점프 최고점의 중력 배율", 17.15, player.TestBody.gravityScale);
        // 입력: 하강 중 착지 → 기대: 기본 중력 배율 복원 및 다시 점프 허용.
        player.TestBody.linearVelocity = new Vector2(0f, -1f);
        player.TestBody.TestGrounded = true;
        Step(player);
        Check("착지 시 기본 중력 배율 복원", 9.8, player.TestBody.gravityScale);
        Step(player, pressed: true, held: true);
        Check("착지 후 재점프", 8, player.TestBody.linearVelocity.y);
        // 입력: 코요테 범위 0.08초와 만료 0.14초 → 기대: 각각 허용·거부.
        player = Create();
        Step(player);
        player.TestBody.TestGrounded = false;

        for (int tick = 0; tick < 3; tick++) {
            Step(player);
        }

        Step(player, pressed: true, held: true);
        Check("코요테 범위 내 점프", 8, player.TestBody.linearVelocity.y);
        player = Create();
        Step(player);
        player.TestBody.TestGrounded = false;

        for (int tick = 0; tick < 6; tick++) {
            Step(player);
        }

        Step(player, pressed: true, held: true);
        Check("코요테 만료 거부", 0, player.TestBody.linearVelocity.y);
        // 입력: coyoteTime 0 지상과 초기 공중 → 기대: 지상만 점프 가능.
        player = Create();
        Set(player, "coyoteTime", 0f);
        Step(player, pressed: true, held: true);
        Check("코요테 0 지상 점프", 8, player.TestBody.linearVelocity.y);
        player = Create();
        player.TestBody.TestGrounded = false;
        Step(player, pressed: true, held: true);
        Check("초기 공중 점프 거부", 0, player.TestBody.linearVelocity.y);
        // 입력: 벽의 법선 y=0 → 기대: 지면으로 판정하지 않음.
        player = Create();
        player.TestBody.TestGroundNormalY = 0f;
        Step(player, pressed: true, held: true);
        Check("벽 접촉 점프 거부", 0, player.TestBody.linearVelocity.y);
        // 입력: 물리 갱신 전에 S를 눌렀다 놓음 → 기대: 8 × -0.75 = -6.
        player = Create(gravityScale: 9.8f);
        Keyboard.current.aKey.wasPressedThisFrame = true;
        Keyboard.current.aKey.isPressed = true;
        Invoke(player, "Update");
        Keyboard.current.aKey.wasPressedThisFrame = false;
        Keyboard.current.aKey.isPressed = false;
        Invoke(player, "Update");
        Invoke(player, "FixedUpdate");
        Check("빠른 탭 점프", -6, player.TestBody.linearVelocity.y);
        // 입력: 최대 높이 1과 기본 높이 3에서 중력 적분 → 기대: 상한 준수와 짧은 탭이 더 낮음.
        float cappedPeak = Peak(true, 1f);
        Check("최대 높이 1 상한", 1, cappedPeak <= 1.0001f ? 1 : 0);
        Check("최대 높이 1 근접", 1, cappedPeak >= 0.98f ? 1 : 0);
        float heldPeak = Peak(true, 3f);
        float tappedPeak = Peak(false, 3f);
        Check("기본 최대 높이 3 상한", 1, heldPeak <= 3.0001f ? 1 : 0);
        Check($"유지 최고점 {heldPeak:F3} / 탭 최고점 {tappedPeak:F3}", 1, heldPeak > tappedPeak ? 1 : 0);
        // 입력: 하강 속도 -3에서 S 해제 → 기대: 현재 하강 속도 유지.
        player = Create(gravityScale: 9.8f);
        Step(player, pressed: true, held: true);
        player.TestBody.TestGrounded = false;
        player.TestBody.linearVelocity = new Vector2(0f, -3f);
        Step(player, held: true);
        Step(player);
        Check("하강 속도 보존", -3, player.TestBody.linearVelocity.y);
        Check("하강 중 해제해도 중력 배율을 한 번만 적용", 17.15, player.TestBody.gravityScale);
        // 입력: 하강 중 비활성화 → 기대: 기본 중력 배율 복원.
        Invoke(player, "OnDisable");
        Check("비활성화 시 기본 중력 배율 복원", 9.8, player.TestBody.gravityScale);
        // 입력: 비활성화 뒤 입력 장치 없음 → 기대: 대기 점프와 코요테 권한 초기화.
        player = Create();
        Step(player);
        Set(player, "jumpRequested", true);
        Invoke(player, "OnDisable");
        player.TestBody.TestGrounded = false;
        Keyboard.current = null;
        Invoke(player, "Update");
        Invoke(player, "FixedUpdate");
        Check("비활성화 대기 점프 제거", 0, player.TestBody.linearVelocity.y);
        Console.WriteLine($"PASS {passed}/{passed}; Unity API 대역 검증이며 Unity 물리 실행 결과가 아님.");
    }
}
