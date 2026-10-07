using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 두 점 사이가 벽 등으로 막혀 있는지 검사한다. 근접 공격이 벽 너머를 베지 않게 하거나,
    /// AI가 대상을 볼 수 있는지 판단할 때 쓴다. 지정한 두 대상(보통 자기 자신과 상대)의 충돌체는 무시한다.
    /// </summary>
    public static class LineOfSight
    {
        static readonly List<RaycastHit2D> Hits = new();
        static readonly ContactFilter2D SolidOnly = new() { useTriggers = false };

        public static bool IsClear(Vector2 from, Vector2 to, Transform ignoreA, Transform ignoreB)
        {
            int count = Physics2D.Linecast(from, to, SolidOnly, Hits);
            for (int i = 0; i < count; i++)
            {
                Transform hit = Hits[i].collider.transform;
                if (ignoreA != null && hit.IsChildOf(ignoreA)) continue;
                if (ignoreB != null && hit.IsChildOf(ignoreB)) continue;
                return false;
            }
            return true;
        }
    }
}
