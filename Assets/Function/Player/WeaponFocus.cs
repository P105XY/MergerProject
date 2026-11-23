using System.Collections.Generic;
using UnityEngine;

namespace Mech.Function
{
    sealed class WeaponFocus
    {
        public const int Capacity = 3;
        public const int HitsPerFragment = 3;
        public const int DirectionCount = 3;
        private const double FocusDuration = 5d;
        private const double Cooldown = 3d;
        private const float FocusSpeed = 0.15f;
        private static WeaponFocus owner;
        private readonly List<CollectibleType> directions = new(DirectionCount);
        private double endsAt;
        private double readyAt;
        private float previousSpeed;
        private float previousFixedStep;

        public bool IsActive => owner == this;
        public int Gauge { get; private set; }
        public int HitProgress { get; private set; }
        public IReadOnlyList<CollectibleType> Directions => directions;

        // 현재 무기 게이지와 재사용 제한으로 집중 진입 가능 여부를 알린다.
        public bool CanBegin(double now)
        {
            return owner == null && Gauge == Capacity && now >= readyAt && Time.timeScale > 0f;
        }

        // 누적 명중 세 번을 파편 게이지 한 칸으로 바꾼다.
        public void RegisterHit()
        {
            HitProgress++;

            if (HitProgress == HitsPerFragment) {
                HitProgress = 0;
                TryCharge();
            }
        }

        // 빈 게이지 한 칸을 충전하고 가득 찼으면 수집을 거부한다.
        public bool TryCharge()
        {
            if (Gauge >= Capacity) {
                return false;
            }

            Gauge++;
            return true;
        }

        // 게이지와 쿨타임을 확인하고 집중 시간 동안 게임과 물리 시간을 늦춘다.
        public bool TryBegin(double now)
        {
            if (!CanBegin(now)) {
                return false;
            }

            owner = this;
            endsAt = now + FocusDuration;
            previousSpeed = Time.timeScale;
            previousFixedStep = Time.fixedDeltaTime;
            Time.timeScale = previousSpeed * FocusSpeed;
            Time.fixedDeltaTime = previousFixedStep * FocusSpeed;
            return true;
        }

        // 새 방향을 기록해 마지막 세 입력을 조합에 사용한다.
        public void AddDirection(CollectibleType direction)
        {
            if (directions.Count == DirectionCount) {
                directions.RemoveAt(0);
            }

            directions.Add(direction);
        }

        // 실제 시간으로 집중 만료를 검사하고 다시 조합할 수 있는 시간을 예약한다.
        public void Tick(double now)
        {
            if (IsActive && now >= endsAt) {
                readyAt = now + Cooldown;
                Cancel();
            }
        }

        // 버튼을 놓으면 완성된 조합만 무기로 확정하고 게이지를 차감한다.
        public bool TryComplete(double now, out FragmentCombination result)
        {
            result = default;
            Tick(now);

            if (!IsActive) {
                return false;
            }

            bool complete = directions.Count == DirectionCount;

            if (complete) {
                result = FragmentCombination.FromDirections(directions);
                Gauge -= Capacity;
                readyAt = now + Cooldown;
            }

            Cancel();
            return complete;
        }

        // 집중을 끝내고 조합 입력 및 집중 전의 게임 시간을 복원한다.
        public void Cancel()
        {
            if (IsActive) {
                Time.timeScale = previousSpeed;
                Time.fixedDeltaTime = previousFixedStep;
                owner = null;
            }

            directions.Clear();
        }

        // 저장된 게이지와 명중 진행도를 복원하고 일시적인 집중 상태를 초기화한다.
        public void Restore(int gauge, int hitProgress)
        {
            Cancel();
            Gauge = gauge;
            HitProgress = hitProgress;
            readyAt = 0d;
        }
    }
}
