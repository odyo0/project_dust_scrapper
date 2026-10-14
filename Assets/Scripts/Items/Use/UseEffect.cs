using UnityEngine;

namespace DustScrapper.Items.Use
{
    /// <summary>
    /// 소모품을 쓸 때 일어나는 일 하나(MP 채우기, 글로우병 오르기, 연막 던지기 …).
    /// 소모품 데이터(ConsumableData)는 이것들을 목록으로 가져서, 새 소모품은 효과를 조합해 데이터로 만든다.
    /// 새 종류의 효과는 이것을 이어받아 Apply만 쓴다.
    /// </summary>
    public abstract class UseEffect : ScriptableObject
    {
        /// <summary>쓴 쪽(플레이어)에게 효과를 낸다. 낼 수 없으면 false(이 경우 소모품이 줄지 않는다).</summary>
        public abstract bool Apply(GameObject user);
    }
}
