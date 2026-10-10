using System;
using System.Collections.Generic;
using DustScrapper.Actors;
using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 이 대상에게 걸린 디버프들. 피해 정보에 디버프가 실려 오면(DamageInfo.Effect) 걸고, 시간이 지나면 푼다.
    /// 걸린 디버프를 모아 "움직일 수 있나(속도 배율)", "공격할 수 있나"를 정한다.
    /// 이동은 몸(ActorMotor)의 속도 배율로 바로 걸고, 공격은 장비를 든 쪽(WeaponLoadout·SupportHand)이 CanAttack을 읽어 막는다.
    /// 플레이어와 적이 같이 쓴다. 죽으면 모두 푼다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class StatusEffects : MonoBehaviour
    {
        [SerializeField] ActorMotor motor;

        readonly List<Active> active = new();
        Health health;

        /// <summary>지금 공격·스킬을 쓸 수 있는가.</summary>
        public bool CanAttack { get; private set; } = true;

        /// <summary>지금 걸린 디버프 중 머리 위에 보일 것의 색. 없으면 투명.</summary>
        public Color IndicatorColor { get; private set; } = Color.clear;

        /// <summary>공격을 막는 디버프가 새로 걸렸을 때. 하던 공격을 끊으라는 신호다.</summary>
        public event Action AttacksStopped;

        void Awake()
        {
            health = GetComponent<Health>();
            if (motor == null) motor = GetComponent<ActorMotor>();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            ClearAll();
        }

        /// <summary>디버프를 건다. 이미 걸려 있으면 남은 시간이 더 긴 쪽으로 맞춘다.</summary>
        public void Apply(StatusEffectData effect)
        {
            if (effect == null || health.IsDead) return;

            int index = active.FindIndex(a => a.Effect == effect);
            if (index >= 0) active[index] = new Active(effect, Mathf.Max(active[index].Remaining, effect.Duration));
            else active.Add(new Active(effect, effect.Duration));

            bool couldAttack = CanAttack;
            Refresh();
            if (couldAttack && !CanAttack) AttacksStopped?.Invoke();
        }

        public bool Has(StatusEffectData effect) => active.Exists(a => a.Effect == effect);

        void Update()
        {
            if (active.Count == 0) return;

            bool changed = false;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                float remaining = active[i].Remaining - Time.deltaTime;
                if (remaining > 0f)
                {
                    active[i] = new Active(active[i].Effect, remaining);
                    continue;
                }
                active.RemoveAt(i);
                changed = true;
            }
            if (changed) Refresh();
        }

        void OnDamaged(DamageInfo info)
        {
            if (info.Effect != null) Apply(info.Effect);
        }

        void OnDied(DamageInfo info) => ClearAll();

        void ClearAll()
        {
            active.Clear();
            Refresh();
        }

        /// <summary>걸린 디버프를 모아 다시 계산한다. 속도 배율은 곱하고, 하나라도 공격을 막으면 막는다.</summary>
        void Refresh()
        {
            float speed = 1f;
            bool canAttack = true;
            Color indicator = Color.clear;
            foreach (Active a in active)
            {
                speed *= a.Effect.MoveSpeedMultiplier;
                if (a.Effect.StopsAttacks) canAttack = false;
                if (a.Effect.ShowsIndicator) indicator = a.Effect.IndicatorColor;
            }

            CanAttack = canAttack;
            IndicatorColor = indicator;
            if (motor != null)
            {
                if (speed < 1f) motor.SetSpeedModifier(this, speed, true);
                else motor.ClearSpeedModifier(this);
            }
        }

        readonly struct Active
        {
            public readonly StatusEffectData Effect;
            public readonly float Remaining;

            public Active(StatusEffectData effect, float remaining)
            {
                Effect = effect;
                Remaining = remaining;
            }
        }
    }
}
