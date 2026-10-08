using System.Collections.Generic;
using DustScrapper.Items;

namespace DustScrapper.Raid
{
    /// <summary>
    /// 끝난 한 판의 결과: 탈출했는가, 걸린 시간, 그때 들고 있던 물건(가방 + 무기 슬롯)과 가치 합계.
    /// 탈출했으면 가져온 것, 죽었으면 잃은 것이다. 결과 화면이 읽고, 나중에 보관함(페이즈 6)이 가져온 것을 받는다.
    /// </summary>
    public sealed class RaidResult
    {
        public RaidResult(bool extracted, float duration, List<ItemStack> items)
        {
            Extracted = extracted;
            Duration = duration;
            Items = items;
            foreach (var stack in items) TotalValue += stack.Item.Value * stack.Count;
        }

        public bool Extracted { get; }

        /// <summary>판 시작부터 끝까지 (초).</summary>
        public float Duration { get; }

        public IReadOnlyList<ItemStack> Items { get; }
        public int TotalValue { get; }
    }
}
