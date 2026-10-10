using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 근접 무기의 수치. 한 번 휘두르기는 준비 → 휘두름(이때만 맞음) → 회복 세 단계로 나뉜다.
    /// 단검은 짧고 빠르게, 큰 검은 넓고 느리게처럼 무기 차이를 이 수치만으로 낸다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Weapons/Melee", fileName = "Melee")]
    public sealed class MeleeData : WeaponData
    {
        [Header("범위")]
        [Tooltip("캐릭터 중심에서 닿는 거리 (칸)")]
        [SerializeField, Min(0.1f)] float reach = 1.1f;
        [Tooltip("휘두르는 부채꼴의 전체 각도 (도)")]
        [SerializeField, Range(1f, 360f)] float arcAngle = 110f;

        [Header("시간 (초)")]
        [Tooltip("누른 뒤 휘두르기 시작할 때까지")]
        [SerializeField, Min(0f)] float windupTime = 0.04f;
        [Tooltip("실제로 맞는 시간")]
        [SerializeField, Min(0.01f)] float activeTime = 0.08f;
        [Tooltip("휘두른 뒤 다음 공격까지")]
        [SerializeField, Min(0f)] float recoveryTime = 0.18f;

        [Header("맞혔을 때")]
        [Tooltip("맞은 대상에게 거는 디버프 (비워 두면 없음). 예: 샷건 발차기의 비틀거림")]
        [SerializeField] StatusEffectData hitEffect;

        public float Reach => reach;
        public float ArcAngle => arcAngle;
        public float WindupTime => windupTime;
        public float ActiveTime => activeTime;
        public float RecoveryTime => recoveryTime;
        public StatusEffectData HitEffect => hitEffect;
    }
}
