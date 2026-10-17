using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Structures
{
    /// <summary>
    /// 문 종류 하나의 수치(나무 문, 튼튼한 문 …). 문짝의 무게·여닫는 힘·걸쇠·발차기를 버티는 힘·부딪힘 피해를 담는다.
    /// "문에 따라 세게 열리거나 문짝이 날아간다"(기획)는 이 파일의 kickResistance로 갈린다.
    /// 단위: 각도는 도, 빠르기는 도/초, 거리는 칸.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Door Data", fileName = "DoorData")]
    public sealed class DoorData : ScriptableObject
    {
        [Header("문짝")]
        [Tooltip("문짝 한 짝의 무게. 무거울수록 몸에 덜 밀리고, 같은 힘으로 덜 빨리 돈다.")]
        [SerializeField, Min(0.01f)] float leafMass = 2f;
        [Tooltip("돌던 문짝이 멈추는 빠르기. 클수록 빨리 멈춘다(흔들림이 짧다).")]
        [SerializeField, Min(0f)] float swingDamping = 4f;
        [Tooltip("닫힌 자리에서 양쪽으로 돌 수 있는 최대 각도")]
        [SerializeField, Range(10f, 179f)] float maxAngle = 100f;

        [Header("F로 밀기")]
        [Tooltip("F로 열 때 문짝을 미는 빠르기")]
        [SerializeField, Min(0f)] float openSpeed = 360f;
        [Tooltip("F로 열 때 이 각도까지 민다. 그 뒤는 남은 힘으로 조금 더 돌다 멈춘다.")]
        [SerializeField, Range(10f, 179f)] float openTargetAngle = 90f;
        [Tooltip("F로 닫을 때 문짝을 당기는 빠르기")]
        [SerializeField, Min(0f)] float closeSpeed = 300f;
        [Tooltip("밀거나 당기는 힘의 한계. 앞에 몸이 있으면 이 힘을 못 이기는 만큼 멈춘다.")]
        [SerializeField, Min(0f)] float pushTorque = 60f;
        [Tooltip("F로 밀고 당기는 것을 그만두는 시간 (초). 막혀서 못 가도 이 시간이 지나면 손을 뗀다.")]
        [SerializeField, Min(0.05f)] float pushTime = 0.8f;

        [Header("걸쇠")]
        [Tooltip("닫힌 자리에서 이 각도 안으로 들어오고 느려지면 걸쇠가 걸려 고정된다(몸으로 밀어도 안 열린다).")]
        [SerializeField, Range(0.5f, 20f)] float latchAngle = 4f;
        [Tooltip("이보다 느리게 돌 때만 걸쇠가 걸린다")]
        [SerializeField, Min(0f)] float latchSpeed = 90f;
        [Tooltip("문짝이 이 각도보다 많이 열려 있으면 '열린 문'으로 친다(구역 소리 새기)")]
        [SerializeField, Range(1f, 90f)] float openThresholdAngle = 15f;

        [Header("발차기")]
        [Tooltip("이 세기 이상으로 차이면 돌쩌귀가 떨어져 문짝이 날아간다. 아니면 세게 열린다.")]
        [SerializeField, Min(0f)] float kickResistance = 8f;
        [Tooltip("날아가지 않을 때 차인 문짝이 도는 빠르기")]
        [SerializeField, Min(0f)] float kickSpinSpeed = 900f;
        [Tooltip("날아갈 때 차기 세기 1당 문짝이 날아가는 빠르기 (칸/초)")]
        [SerializeField, Min(0f)] float flySpeedPerForce = 1.2f;
        [Tooltip("발차기 소음 (칸)")]
        [SerializeField, Min(0f)] float kickNoiseRadius = 10f;

        [Header("부딪힘")]
        [Tooltip("문짝 끝이 이 빠르기(칸/초)보다 빠르게 몸에 부딪히면 맞는다")]
        [SerializeField, Min(0f)] float hitSpeed = 6f;
        [SerializeField, Min(0f)] float hitDamage = 10f;
        [SerializeField, Min(0f)] float hitKnockback = 10f;
        [Tooltip("맞은 것에 거는 디버프 (쓰러짐)")]
        [SerializeField] StatusEffectData hitEffect;

        [Header("소리")]
        [Tooltip("F로 열고 닫을 때 소음 (칸)")]
        [SerializeField, Min(0f)] float noiseRadius = 3f;

        public float LeafMass => leafMass;
        public float SwingDamping => swingDamping;
        public float MaxAngle => maxAngle;
        public float OpenSpeed => openSpeed;
        public float OpenTargetAngle => Mathf.Min(openTargetAngle, maxAngle);
        public float CloseSpeed => closeSpeed;
        public float PushTorque => pushTorque;
        public float PushTime => pushTime;
        public float LatchAngle => latchAngle;
        public float LatchSpeed => latchSpeed;
        public float OpenThresholdAngle => openThresholdAngle;
        public float KickResistance => kickResistance;
        public float KickSpinSpeed => kickSpinSpeed;
        public float FlySpeedPerForce => flySpeedPerForce;
        public float KickNoiseRadius => kickNoiseRadius;
        public float HitSpeed => hitSpeed;
        public float HitDamage => hitDamage;
        public float HitKnockback => hitKnockback;
        public StatusEffectData HitEffect => hitEffect;
        public float NoiseRadius => noiseRadius;
    }
}
