using System;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Magic
{
    public enum SpellPhase
    {
        Ready,
        Casting,
        Recovery,
    }

    /// <summary>
    /// 탄을 쏘는 마법 스킬. 누르면 MP를 쓰고 시전(짧은 준비) → 탄 발사 → 다음 시전까지 기다리기 순서로 돈다.
    /// 누르고 있으면 기다리는 시간이 끝날 때마다 계속 나간다. MP가 모자라면 나가지 않고 MP 쪽이 SpendFailed를 알린다.
    /// 시전하는 동안은 IsCasting이 켜져서 반대 손 공격이 막힌다(SkillRouter). 탄은 총과 같은 Projectile을 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpellCast : Skill
    {
        /// <summary>기다리는 중에 누른 클릭을 이 시간(초) 동안 기억했다가 바로 다음 시전으로 잇는다.</summary>
        const float PressBufferTime = 0.15f;

        [SerializeField] SpellData spell;

        float phaseTimer;
        float pressBuffer;

        public SpellData Spell => spell;
        public SpellPhase Phase { get; private set; }
        public override bool IsBusy => Phase != SpellPhase.Ready;
        public override bool IsCasting => Phase == SpellPhase.Casting;

        /// <summary>시전을 시작했을 때(MP를 쓴 직후).</summary>
        public event Action CastStarted;

        /// <summary>탄이 나갔을 때. 값은 쏜 방향.</summary>
        public event Action<Vector2> Released;

        protected override void OnBind()
        {
            if (spell == null || spell.Projectile == null)
            {
                Debug.LogError($"{name}: 마법 데이터나 탄 프리팹이 비어 있다.", this);
                enabled = false;
            }
        }

        protected override void OnCancel()
        {
            Phase = SpellPhase.Ready;
            phaseTimer = 0f;
            pressBuffer = 0f;
        }

        protected override void Tick(bool pressed, bool held, float deltaTime)
        {
            pressBuffer = pressed ? PressBufferTime : pressBuffer - deltaTime;

            if (Phase != SpellPhase.Ready)
            {
                phaseTimer -= deltaTime;
                if (phaseTimer > 0f) return;

                if (Phase == SpellPhase.Casting) Release();
                else Phase = SpellPhase.Ready;
                if (Phase != SpellPhase.Ready) return;
            }

            if (held || pressBuffer > 0f) TryStartCast();
        }

        /// <summary>
        /// MP를 쓰고 시전을 시작한다. MP가 모자라면 시작하지 않는다.
        /// 모자람 알림(SpendFailed)은 새로 누른 클릭(기억해 둔 클릭 포함)이 실패할 때만 보낸다.
        /// 누르고만 있는 동안 매 프레임 깜빡이지 않게 하고, MP가 다시 차면 누르고 있던 그대로 나가게 한다.
        /// </summary>
        void TryStartCast()
        {
            Mana mana = Holder.Mana;
            if (mana == null) return;

            if (mana.Current < spell.ManaCost)
            {
                if (pressBuffer > 0f) mana.TrySpend(spell.ManaCost);
                pressBuffer = 0f;
                return;
            }

            mana.TrySpend(spell.ManaCost);
            pressBuffer = 0f;
            Phase = SpellPhase.Casting;
            phaseTimer = spell.CastTime;
            CastStarted?.Invoke();
            if (phaseTimer <= 0f) Release();
        }

        void Release()
        {
            Vector2 aim = Holder.AimDirection;
            Vector2 start = Holder.transform.position;
            // 총과 같이 캐릭터 중심에서 출발해 손까지 한 번에 이동한다(손이 벽 너머로 삐져나와도 벽을 뚫지 않게).
            float handDistance = Vector2.Distance(start, transform.position);

            Projectile projectile = Instantiate(spell.Projectile, start, Quaternion.identity);
            projectile.Launch(start, aim, handDistance, spell.ProjectileSpeed, spell.Range,
                spell.Damage, spell.Knockback, Holder.gameObject);

            EmitNoise(spell.NoiseRadius);
            Phase = SpellPhase.Recovery;
            phaseTimer = spell.Cooldown;
            Released?.Invoke(aim);
            if (phaseTimer <= 0f) Phase = SpellPhase.Ready;
        }
    }
}
