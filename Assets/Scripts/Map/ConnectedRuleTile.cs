using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DustScrapper.Map
{
    /// <summary>
    /// 옆 칸을 보고 모양을 고르는 룰 타일인데, '같은 것'으로 칠 타일을 더 넣을 수 있다.
    /// 벽은 창문 칸과도 이어져야 해서(창은 벽 줄 안에 있다) 창 타일을 friends에 넣는다. 문 칸은 넣지 않는다 — 문 앞에서 벽 끝이 보여야 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Connected Rule Tile")]
    public sealed class ConnectedRuleTile : RuleTile
    {
        [Tooltip("이 타일과 이어진 것으로 볼 다른 타일(예: 벽에 대한 창문)")]
        public List<TileBase> friends = new();

        public override bool RuleMatch(int neighbor, TileBase other)
        {
            bool same = other == this || (other != null && friends.Contains(other));
            return neighbor switch
            {
                TilingRuleOutput.Neighbor.This => same,
                TilingRuleOutput.Neighbor.NotThis => !same,
                _ => base.RuleMatch(neighbor, other),
            };
        }
    }
}
