using UnityEngine;

namespace DustScrapper.Actors
{
    /// <summary>
    /// 이동의 무게감을 정하는 수치 묶음. 장비(무거운 갑옷, 큰 무기 등)에 따라 통째로 갈아 끼운다.
    /// 단위: 1 = 월드 1칸(32px 잠정).
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Movement Profile", fileName = "MovementProfile")]
    public sealed class MovementProfile : ScriptableObject
    {
        [Tooltip("최고 속도 (칸/초)")]
        [SerializeField, Min(0f)] float maxSpeed = 4.5f;

        [Tooltip("이동 키를 누를 때 속도가 붙는 빠르기 (칸/초²). 낮을수록 출발이 무겁다.")]
        [SerializeField, Min(0f)] float acceleration = 25f;

        [Tooltip("이동 키를 뗐을 때 멈추는 빠르기 (칸/초²). 낮을수록 미끄러진다.")]
        [SerializeField, Min(0f)] float deceleration = 35f;

        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
    }
}
