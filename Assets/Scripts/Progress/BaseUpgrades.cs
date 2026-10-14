using System;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 지은 기지 시설·업그레이드의 단계를 읽고 올린다. 저장 내용(ProfileData.upgrades)을 바꾸는 길은 여기 하나다.
    /// 돈을 내고 올리면 Changed로 알린다. 저장은 Wallet처럼 기지(BaseProfileLink)가 듣고 같은 때에 한 번에 한다.
    /// </summary>
    public static class BaseUpgrades
    {
        /// <summary>어떤 업그레이드의 단계가 올랐을 때.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay() => Changed = null;

        public static int LevelOf(BaseUpgradeData upgrade)
        {
            if (upgrade == null) return 0;
            foreach (var entry in Profile.Current.upgrades)
            {
                if (entry.id == upgrade.SaveId) return entry.level;
            }
            return 0;
        }

        /// <summary>다음 단계를 산다. 다 지었거나 돈이 모자라면 이유를, 샀으면 null.</summary>
        public static string TryUpgrade(BaseUpgradeData upgrade)
        {
            int level = LevelOf(upgrade);
            int price = upgrade.PriceAt(level);
            if (price < 0) return "이미 다 지었다";
            if (!Wallet.TrySpend(price)) return "돈이 모자란다";

            var list = Profile.Current.upgrades;
            int index = list.FindIndex(e => e.id == upgrade.SaveId);
            var next = new UpgradeLevel { id = upgrade.SaveId, level = level + 1 };
            if (index >= 0) list[index] = next;
            else list.Add(next);
            Changed?.Invoke();
            return null;
        }
    }
}
