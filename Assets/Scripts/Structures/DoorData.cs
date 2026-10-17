using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Structures
{
    /// <summary>
    /// 문 종류 하나의 수치(나무 문, 튼튼한 문 …): 여닫는 빠르기, 열린 각도, 발차기를 버티는 힘, 부딪힘 피해.
    /// "문에 따라 세게 열리거나 문짝이 날아간다"(기획)는 kickResistance로 갈린다.
    /// 단위: 각도는 도, 빠르기는 도/초, 거리는 칸.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Door Data", fileName = "DoorData")]
    public sealed class DoorData : ScriptableObject
    {
        [Header("여닫기")]
        [Tooltip("F로 열고 닫을 때 문짝이 도는 빠르기")]
        [SerializeField, Min(1f)] float swingSpeed = 420f;
        [Tooltip("열린 각도. 닫힌 자리에서 이만큼 돌아 그 자리에 고정된다(기획: 각 방향 90도).")]
        [SerializeField, Range(10f, 170f)] float openAngle = 90f;
        [Tooltip("문짝이 이 각도보다 많이 열려 있으면 '열린 문'으로 친다(구역 소리 새기)")]
        [SerializeField, Range(1f, 90f)] float openThresholdAngle = 15f;

        [Header("발차기")]
        [Tooltip("이 세기 이상으로 차이면 돌쩌귀가 떨어져 문짝이 날아간다. 아니면 아주 빠르게 열린다.")]
        [SerializeField, Min(0f)] float kickResistance = 8f;
        [Tooltip("날아가지 않을 때 차인 문짝이 열리는 빠르기")]
        [SerializeField, Min(1f)] float kickSwingSpeed = 1400f;
        [Tooltip("날아갈 때 차기 세기 1당 문짝이 날아가는 빠르기 (칸/초)")]
        [SerializeField, Min(0f)] float flySpeedPerForce = 1.2f;
        [Tooltip("발차기 소음 (칸)")]
        [SerializeField, Min(0f)] float kickNoiseRadius = 10f;

        [Header("부딪힘 (차인 문짝·날아가는 문짝)")]
        [SerializeField, Min(0f)] float hitDamage = 10f;
        [SerializeField, Min(0f)] float hitKnockback = 10f;
        [Tooltip("맞은 것에 거는 디버프 (쓰러짐)")]
        [SerializeField] StatusEffectData hitEffect;

        [Header("소리")]
        [Tooltip("F로 열고 닫을 때 소음 (칸)")]
        [SerializeField, Min(0f)] float noiseRadius = 3f;

        public float SwingSpeed => swingSpeed;
        public float OpenAngle => openAngle;
        public float OpenThresholdAngle => openThresholdAngle;
        public float KickResistance => kickResistance;
        public float KickSwingSpeed => kickSwingSpeed;
        public float FlySpeedPerForce => flySpeedPerForce;
        public float KickNoiseRadius => kickNoiseRadius;
        public float HitDamage => hitDamage;
        public float HitKnockback => hitKnockback;
        public StatusEffectData HitEffect => hitEffect;
        public float NoiseRadius => noiseRadius;
    }
}
