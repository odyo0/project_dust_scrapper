using System;
using System.Collections.Generic;
using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Weapons
{
    public enum MeleePhase
    {
        Idle,
        Windup,
        Active,
        Recovery,
    }

    /// <summary>
    /// 근접 공격 스킬(베기, 주먹질, 할퀴기, 펀치). 누르면 준비 → 휘두름 → 회복 순서로 한 번 휘두른다.
    /// 휘두름 단계 동안 캐릭터 앞쪽 부채꼴 안의 대상을 맞히고, 한 번 휘두를 때 같은 대상은 한 번만 맞는다.
    /// 벽 너머의 대상은 맞지 않는다. 휘두르는 모습은 MeleeSwingVisual이 단계를 읽어 그린다.
    /// 수치는 보통 장비 데이터(MeleeData)를 쓰고, 장비와 다른 수치가 필요하면(오른손 빈손의 펀치 등) stats에 따로 넣는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MeleeAttack : Skill
    {
        /// <summary>휘두르는 중에 누른 클릭을 이 시간(초) 동안 기억했다가 바로 다음 공격으로 잇는다.</summary>
        const float PressBufferTime = 0.2f;

        static readonly List<Collider2D> Overlaps = new();
        static readonly ContactFilter2D SolidOnly = new() { useTriggers = false };

        readonly HashSet<Health> hitThisSwing = new();

        [Tooltip("이 공격의 수치. 비워 두면 장비 데이터(MeleeData)를 쓴다.")]
        [SerializeField] MeleeData stats;

        MeleeData melee;
        float phaseTimer;
        float pressBuffer;

        public MeleeData MeleeData => melee;
        public MeleePhase Phase { get; private set; }
        public override bool IsBusy => Phase != MeleePhase.Idle;

        /// <summary>지금 단계가 얼마나 진행됐는지 (0~1).</summary>
        public float PhaseProgress
        {
            get
            {
                float length = PhaseLength(Phase);
                return length > 0f ? Mathf.Clamp01(1f - phaseTimer / length) : 1f;
            }
        }

        /// <summary>지금까지 휘두른 횟수. 연출이 좌우를 번갈아 휘두를 때 쓴다.</summary>
        public int SwingCount { get; private set; }

        /// <summary>대상을 맞혔을 때.</summary>
        public event Action<DamageInfo> Hit;

        /// <summary>단계가 바뀌었을 때(준비 시작, 휘두름 시작 등). 값은 새 단계.</summary>
        public event Action<MeleePhase> PhaseChanged;

        protected override void OnBind()
        {
            melee = stats != null ? stats : Weapon.Data as MeleeData;
            if (melee == null)
            {
                Debug.LogError($"{name}: 근접 공격 스킬은 stats를 넣거나 MeleeData 장비에 붙인다.", this);
                enabled = false;
            }
        }

        protected override void OnCancel()
        {
            Phase = MeleePhase.Idle;
            phaseTimer = 0f;
            pressBuffer = 0f;
        }

        protected override void Tick(bool pressed, bool held, float deltaTime)
        {
            pressBuffer = pressed ? PressBufferTime : pressBuffer - deltaTime;

            if (Phase == MeleePhase.Active) HitTargetsInArc();

            if (Phase != MeleePhase.Idle)
            {
                phaseTimer -= deltaTime;
                if (phaseTimer > 0f) return;
                AdvancePhase();
            }

            if (Phase == MeleePhase.Idle && pressBuffer > 0f) StartSwing();
        }

        void StartSwing()
        {
            pressBuffer = 0f;
            SwingCount++;
            hitThisSwing.Clear();
            EmitNoise(melee.NoiseRadius);
            EnterPhase(MeleePhase.Windup);
        }

        void AdvancePhase()
        {
            switch (Phase)
            {
                case MeleePhase.Windup: EnterPhase(MeleePhase.Active); break;
                case MeleePhase.Active: EnterPhase(MeleePhase.Recovery); break;
                default: EnterPhase(MeleePhase.Idle); break;
            }
        }

        void EnterPhase(MeleePhase phase)
        {
            Phase = phase;
            phaseTimer = PhaseLength(phase);
            PhaseChanged?.Invoke(phase);
            // 준비 시간이 0이면 누른 그 프레임에 바로 맞힌다.
            if (phase == MeleePhase.Active) HitTargetsInArc();
        }

        float PhaseLength(MeleePhase phase) => phase switch
        {
            MeleePhase.Windup => melee.WindupTime,
            MeleePhase.Active => melee.ActiveTime,
            MeleePhase.Recovery => melee.RecoveryTime,
            _ => 0f,
        };

        void HitTargetsInArc()
        {
            Transform ownerRoot = Holder.transform;
            Vector2 origin = ownerRoot.position;
            Vector2 aim = Holder.AimDirection;
            float halfArc = melee.ArcAngle * 0.5f;

            int count = Physics2D.OverlapCircle(origin, melee.Reach, SolidOnly, Overlaps);
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = Overlaps[i];
                if (collider.transform.IsChildOf(ownerRoot)) continue;

                Health health = collider.GetComponentInParent<Health>();
                if (health == null || health.IsDead || hitThisSwing.Contains(health)) continue;

                // 대상의 중심이 아니라 가장 가까운 지점으로 각도를 재서, 큰 대상의 가장자리도 벨 수 있게 한다.
                Vector2 point = collider.ClosestPoint(origin);
                Vector2 toPoint = point - origin;
                if (toPoint.sqrMagnitude > 0.0001f && Vector2.Angle(aim, toPoint) > halfArc) continue;
                if (!LineOfSight.IsClear(origin, point, ownerRoot, health.transform)) continue;

                hitThisSwing.Add(health);
                Vector2 direction = toPoint.sqrMagnitude > 0.0001f ? toPoint : aim;
                var info = new DamageInfo(melee.Damage, point, direction, melee.Knockback, Holder.gameObject);
                if (health.TakeDamage(info)) Hit?.Invoke(info);
            }
        }
    }
}
