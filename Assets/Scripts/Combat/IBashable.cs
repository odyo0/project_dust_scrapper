using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 걷어차기 같은 강한 근접 공격에 반응하는 것(문 등). 체력이 없어서 Health로는 맞힐 수 없는 것이 이 창구로 반응한다.
    /// 어떤 근접 공격이 걷어차기인지는 공격 쪽 데이터(MeleeData.Bashes)가 정하고, 맞은 쪽은 무엇에 차였는지 몰라도 된다.
    /// </summary>
    public interface IBashable
    {
        /// <summary>from에서 direction 쪽으로 걷어차였다. source는 걷어찬 캐릭터.</summary>
        void Bash(Vector2 from, Vector2 direction, GameObject source);
    }
}
