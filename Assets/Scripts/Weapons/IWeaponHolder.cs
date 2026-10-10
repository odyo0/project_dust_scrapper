using DustScrapper.Items;
using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 장비를 손에 든 쪽(왼손 WeaponLoadout, 오른손 SupportHand)이 장비에게 알려 주는 것들의 창구.
    /// 스킬은 이 창구로 조준 방향·자기 몸(자기 탄에 안 맞게, 소음의 주인)·탄 공급원·MP를 얻는다.
    /// 그래서 같은 장비를 어느 손에 들든 스킬 코드는 똑같다.
    /// </summary>
    public interface IWeaponHolder
    {
        /// <summary>장비를 든 캐릭터. 자기 탄·자기 휘두르기에 안 맞게 하고, 소음의 주인으로 쓴다.</summary>
        Transform transform { get; }

        GameObject gameObject { get; }

        Vector2 AimDirection { get; }

        /// <summary>총이 재장전할 때 탄을 꺼낼 곳. 없으면 null.</summary>
        Inventory AmmoSource { get; }

        /// <summary>마법이 쓸 MP. 없으면 null(마법을 쓰지 못한다).</summary>
        Mana Mana { get; }

        /// <summary>디버프(비틀거림 등) 때문에 지금 공격·스킬을 쓰지 못하는가.</summary>
        bool AttacksSuppressed { get; }

        /// <summary>
        /// 이 장비를 손에서 놓는다(칼 던지기 등). 놓은 장비를 물건 묶음으로 돌려주고, 손은 빈손이 된다.
        /// 이 손에서 놓을 수 없는 장비면 false.
        /// </summary>
        bool TryRelease(Weapon weapon, out ItemStack stack);
    }
}
