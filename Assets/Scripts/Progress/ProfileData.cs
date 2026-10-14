using System;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 원정 밖에서 남는 모든 것(저장 파일 한 개의 내용): 돈, 보관함, 몸에 지닌 것, 원정 중인지.
    /// 기지 시설·업그레이드처럼 영구히 남는 것이 생기면 여기에 칸을 늘린다.
    /// </summary>
    [Serializable]
    public sealed class ProfileData
    {
        /// <summary>저장 형식이 바뀌면 올린다. 옛 파일을 고쳐 읽을 때 쓴다.</summary>
        public int version = 1;

        /// <summary>돈(계좌). 원정에 들고 가지 않아서 쓰러져도 잃지 않는다. 바꿀 때는 Wallet을 거친다.</summary>
        public int money;

        public SavedStack[] storage = Array.Empty<SavedStack>();
        public LoadoutData carried = new();

        /// <summary>
        /// 원정에 나가 있는가. 출발할 때 켜고, 판이 끝나면 끈다.
        /// 켜진 채로 게임을 다시 켰다면 원정 중에 끈 것이라 사망으로 처리한다.
        /// </summary>
        public bool inRaid;
    }
}
