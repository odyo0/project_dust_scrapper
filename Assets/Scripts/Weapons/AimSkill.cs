using DustScrapper.Actors;
using DustScrapper.Vision;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 조준 스킬(소총의 Space). 누르고 있는 동안
    /// - 같은 장비의 총 쏘기(GunFire) 퍼짐이 줄어 집탄율이 올라가고,
    /// - 바라보는 쪽 시야 부채꼴이 늘어나고(화면도 그만큼 그쪽으로 밀린다),
    /// - 천천히 걷는다(달리기 키는 무시).
    /// 떼거나, 집어넣거나, 디버프로 끊기면 모두 되돌린다.
    /// 이 장비 하나만 쓰는 값이라 데이터 파일 대신 프리팹의 이 부품에 둔다. 여러 장비가 나눠 쓰게 되면 데이터로 뺀다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AimSkill : Skill
    {
        [Tooltip("조준하는 동안 총 퍼짐에 곱하는 값 (작을수록 정확)")]
        [SerializeField, Range(0f, 1f)] float spreadMultiplier = 0.1f;
        [Tooltip("조준하는 동안 이동 속도에 곱하는 값")]
        [SerializeField, Range(0f, 1f)] float moveSpeedMultiplier = 0.5f;
        [Tooltip("조준하는 동안 바라보는 쪽 시야 부채꼴에 더하는 거리 (칸)")]
        [SerializeField, Min(0f)] float visionRangeBonus = 6f;

        GunFire gun;
        ActorMotor motor;
        PlayerVision vision;

        public bool IsAiming { get; private set; }

        protected override void OnBind()
        {
            gun = Weapon.FindSkill<GunFire>();
            motor = Holder.transform.GetComponent<ActorMotor>();
            // 적이 조준 스킬을 쓰면 플레이어 시야가 없으므로 시야는 건드리지 않는다.
            vision = Holder.transform.GetComponent<PlayerVision>();
        }

        protected override void OnCancel() => SetAiming(false);

        void OnDisable() => SetAiming(false);

        protected override void Tick(bool pressed, bool held, float deltaTime) => SetAiming(held);

        void SetAiming(bool aiming)
        {
            if (aiming == IsAiming) return;
            IsAiming = aiming;

            if (gun != null) gun.SpreadMultiplier = aiming ? spreadMultiplier : 1f;
            if (motor != null)
            {
                if (aiming) motor.SetSpeedModifier(this, moveSpeedMultiplier, true);
                else motor.ClearSpeedModifier(this);
            }
            if (vision != null) vision.AimRangeBonus = aiming ? visionRangeBonus : 0f;
        }
    }
}
