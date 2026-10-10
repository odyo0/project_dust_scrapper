using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 디버프(맞은 대상에게 일정 시간 붙는 나쁜 상태) 하나의 수치. 비틀거림, 나중에 화상·둔화 등.
    /// 무엇을 막고 얼마나 느리게 하는지를 데이터로만 정해서, 디버프를 늘릴 때 코드가 아니라 이 파일을 새로 만든다.
    /// 디버프는 피해 정보(DamageInfo.Effect)에 실려 오고, 맞은 쪽의 StatusEffects가 받아서 건다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Combat/Status Effect", fileName = "StatusEffect")]
    public sealed class StatusEffectData : ScriptableObject
    {
        [SerializeField] string displayName;

        [Tooltip("걸려 있는 시간 (초). 이미 걸려 있는데 다시 걸리면 남은 시간이 더 긴 쪽으로 맞춘다(쌓이지 않는다).")]
        [SerializeField, Min(0.01f)] float duration = 0.6f;

        [Header("효과")]
        [Tooltip("걸려 있는 동안 움직이지 못한다(밀려나는 것은 그대로).")]
        [SerializeField] bool stopsMovement;
        [Tooltip("걸려 있는 동안 공격·스킬을 쓰지 못한다. 걸리는 순간 하던 공격(준비 동작 등)도 끊긴다.")]
        [SerializeField] bool stopsAttacks;
        [Tooltip("이동 속도 배율 (1 = 그대로). 움직이지 못하게 하면 이 값은 쓰지 않는다.")]
        [SerializeField, Range(0f, 1f)] float moveSpeedMultiplier = 1f;

        [Header("표시")]
        [Tooltip("걸려 있는 동안 머리 위 표시에 쓸 색. 투명(알파 0)이면 표시하지 않는다.")]
        [SerializeField] Color indicatorColor = new(1f, 0.85f, 0.2f, 1f);

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public float Duration => duration;
        public bool StopsMovement => stopsMovement;
        public bool StopsAttacks => stopsAttacks;
        public float MoveSpeedMultiplier => stopsMovement ? 0f : moveSpeedMultiplier;
        public Color IndicatorColor => indicatorColor;
        public bool ShowsIndicator => indicatorColor.a > 0f;
    }
}
