using DustScrapper.Actors;
using DustScrapper.Combat;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Enemies
{
    /// <summary>
    /// 시험용 고정 포탑의 두뇌. 대상이 사거리 안에 보이면 천천히 그쪽으로 돌고, 정해진 간격으로 끊어 쏜다.
    /// 플레이어와 같은 몸 부품(BodyAim, WeaponLoadout, 총)을 쓰고 두뇌만 다르다.
    /// 페이즈 2에서 "맞는 쪽"을 시험하려고 둔 것이고, 페이즈 3에서 진짜 적이 생기면 방에서 뺀다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class TurretBrain : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] BodyAim bodyAim;
        [SerializeField] WeaponLoadout loadout;

        [Tooltip("이 거리(칸) 안에 보일 때만 쏜다")]
        [SerializeField, Min(0f)] float range = 11f;
        [Tooltip("포신이 도는 빠르기 (도/초). 느릴수록 옆으로 달려 피하기 쉽다.")]
        [SerializeField, Min(0f)] float turnSpeed = 120f;
        [Tooltip("포신이 대상과 이 각도(도) 안으로 맞아야 쏜다")]
        [SerializeField, Range(0f, 180f)] float fireAngle = 12f;

        [Header("끊어 쏘기 (초)")]
        [SerializeField, Min(0f)] float burstDuration = 1f;
        [SerializeField, Min(0f)] float pauseDuration = 1.4f;

        Health targetHealth;
        Vector2 aimDirection = Vector2.left;
        float cycleTimer;

        void Awake()
        {
            if (bodyAim == null) bodyAim = GetComponent<BodyAim>();
            if (loadout == null) loadout = GetComponent<WeaponLoadout>();
            if (target != null) targetHealth = target.GetComponentInParent<Health>();
        }

        void OnDisable()
        {
            if (loadout != null) loadout.SetPrimary(false);
            cycleTimer = 0f;
        }

        void Update()
        {
            bool canSee = CanSeeTarget(out Vector2 toTarget);
            if (canSee)
            {
                float current = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                float wanted = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                float next = Mathf.MoveTowardsAngle(current, wanted, turnSpeed * Time.deltaTime) * Mathf.Deg2Rad;
                aimDirection = new Vector2(Mathf.Cos(next), Mathf.Sin(next));
            }
            bodyAim.AimAt((Vector2)transform.position + aimDirection);

            // 보이는 동안만 "쏘기 → 쉬기" 주기가 돈다. 처음 발견하면 바로 쏘지 않고 쉬기부터 시작한다.
            cycleTimer = canSee ? cycleTimer + Time.deltaTime : 0f;
            float cycle = burstDuration + pauseDuration;
            bool inBurst = cycle > 0f && cycleTimer % cycle >= pauseDuration;
            bool aligned = canSee && Vector2.Angle(aimDirection, toTarget) <= fireAngle;

            loadout.SetPrimary(inBurst && aligned);
        }

        bool CanSeeTarget(out Vector2 toTarget)
        {
            toTarget = Vector2.zero;
            if (target == null || (targetHealth != null && targetHealth.IsDead)) return false;

            toTarget = (Vector2)target.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude > range * range) return false;
            return LineOfSight.IsClear(transform.position, target.position, transform, target);
        }
    }
}
