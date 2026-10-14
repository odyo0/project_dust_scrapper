using System;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 업그레이드를 지었을 때만 기지에 나타나는 것들(충전대, 정제 시설, 벽에 걸린 보급품 …)을 켜고 끈다.
    /// 꺼진 물체는 스스로 다시 켜질 수 없어서, 늘 켜져 있는 이곳에서 목록으로 관리한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeUnlocks : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public BaseUpgradeData upgrade;
            [Min(1)] public int minLevel;
            public GameObject target;
        }

        [SerializeField] Entry[] entries;

        void OnEnable()
        {
            BaseUpgrades.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => BaseUpgrades.Changed -= Refresh;

        void Refresh()
        {
            foreach (var entry in entries)
            {
                if (entry.target != null) entry.target.SetActive(BaseUpgrades.LevelOf(entry.upgrade) >= entry.minLevel);
            }
        }
    }
}
