using System;
using UnityEngine;

namespace DustScrapper.Lantern
{
    /// <summary>
    /// 빛 랜턴의 효과: 초음파처럼 둘레 안의 적을 벽 너머·어둠 속까지 드러낸다. 소리는 내지 않는다(기획).
    /// 드러난 적은 정해진 시간 동안 움직이는 대로 계속 따라 보인다. 보여 주는 일은 표시 쪽(RevealedEnemyMarkers)이
    /// Revealed를 듣고 맡아서, 이 효과는 화면을 모른다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Lantern/Reveal Enemies", fileName = "Reveal")]
    public sealed class RevealEnemiesEffect : LanternEffect
    {
        [Tooltip("드러내는 둘레 (칸). AI 임시값")]
        [SerializeField, Min(0.5f)] float radius = 12f;
        [Tooltip("드러난 적이 보이는 시간 (초). AI 임시값")]
        [SerializeField, Min(0.1f)] float duration = 4f;

        /// <summary>드러냈을 때: 중심, 둘레(칸), 보이는 시간(초).</summary>
        public static event Action<Vector2, float, float> Revealed;

        public float Radius => radius;
        public float Duration => duration;

        public override bool Activate(GameObject user)
        {
            Revealed?.Invoke(user.transform.position, radius, duration);
            return true;
        }
    }
}
