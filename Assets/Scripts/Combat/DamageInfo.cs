using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 피해 한 번의 정보. 때린 쪽(탄, 칼 등)이 만들어 Health에 넘긴다.
    /// 맞은 쪽의 반응(번쩍임, 밀려남)은 이 값만 보고 정하므로, 무엇에 맞았는지 몰라도 된다.
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>깎을 체력.</summary>
        public readonly float Amount;

        /// <summary>맞은 지점(월드 좌표).</summary>
        public readonly Vector2 Point;

        /// <summary>피해가 들어온 방향. 길이 1.</summary>
        public readonly Vector2 Direction;

        /// <summary>밀어내는 힘(충격량). 0이면 밀지 않는다.</summary>
        public readonly float Knockback;

        /// <summary>피해를 준 쪽(무기를 든 캐릭터). 없으면 null.</summary>
        public readonly GameObject Source;

        /// <summary>함께 거는 디버프(비틀거림 등). 없으면 null. 맞은 쪽의 StatusEffects가 받는다.</summary>
        public readonly StatusEffectData Effect;

        public DamageInfo(float amount, Vector2 point, Vector2 direction, float knockback, GameObject source,
            StatusEffectData effect = null)
        {
            Amount = amount;
            Point = point;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
            Knockback = knockback;
            Source = source;
            Effect = effect;
        }
    }
}
