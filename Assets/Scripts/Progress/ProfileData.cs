using System;
using System.Collections.Generic;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 원정 밖에서 남는 모든 것(저장 파일 한 개의 내용): 돈, 보관함, 몸에 지닌 것, 원정 중인지.
    /// 영구히 남는 것이 생기면 여기에 칸을 늘린다.
    /// </summary>
    [Serializable]
    public sealed class ProfileData
    {
        /// <summary>저장 형식이 바뀌면 올린다. 옛 파일을 고쳐 읽을 때 쓴다. (2: 지닌 칸을 이름표 목록으로, 2026-10-17)</summary>
        public int version = 2;

        /// <summary>돈(계좌). 원정에 들고 가지 않아서 쓰러져도 잃지 않는다. 바꿀 때는 Wallet을 거친다.</summary>
        public int money;

        /// <summary>지은 기지 시설·업그레이드와 그 단계. 바꿀 때는 BaseUpgrades를 거친다.</summary>
        public List<UpgradeLevel> upgrades = new();

        /// <summary>글로우병(%). 기지로 돌아와도 남고, 죽으면 0이 된다(기획).</summary>
        public float glowSickness;

        public SavedStack[] storage = Array.Empty<SavedStack>();
        public LoadoutData carried = new();

        /// <summary>
        /// 원정에 나가 있는가. 출발할 때 켜고, 판이 끝나면 끈다.
        /// 켜진 채로 게임을 다시 켰다면 원정 중에 끈 것이라 사망으로 처리한다.
        /// </summary>
        public bool inRaid;
    }

    /// <summary>업그레이드 하나의 단계(0이면 안 지음). 저장용.</summary>
    [Serializable]
    public struct UpgradeLevel
    {
        public string id;
        public int level;
    }
}
