using System;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 돈(계좌)을 늘리고 줄인다. 돈은 저장 내용(ProfileData.money)에 있고, 바꾸는 길은 여기 하나다.
    /// 상인에게 팔고 사는 것, 나중에 기지 시설을 짓는 것이 모두 이곳에서 돈을 꺼내고 넣는다.
    /// 저장은 하지 않고 Changed로 알리기만 한다. 기지(BaseProfileLink)가 듣고 물건 칸과 같은 때에 한 번에 저장해서,
    /// 물건은 팔렸는데 돈은 저장되지 않는 식으로 어긋나지 않게 한다.
    /// </summary>
    public static class Wallet
    {
        public static int Money => Profile.Current.money;

        /// <summary>돈이 바뀌었을 때.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay() => Changed = null;

        public static bool CanAfford(int amount) => amount >= 0 && Money >= amount;

        public static void Earn(int amount)
        {
            if (amount <= 0) return;
            Profile.Current.money += amount;
            Changed?.Invoke();
        }

        /// <summary>돈이 충분하면 꺼내고 true. 모자라면 그대로 두고 false.</summary>
        public static bool TrySpend(int amount)
        {
            if (!CanAfford(amount)) return false;
            if (amount == 0) return true;
            Profile.Current.money -= amount;
            Changed?.Invoke();
            return true;
        }
    }
}
