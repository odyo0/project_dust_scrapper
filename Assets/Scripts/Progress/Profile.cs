using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 지금 플레이 중인 저장 내용(ProfileData)과 그것을 바꾸는 규칙. 씬이 바뀌어도 남는다.
    /// 기지와 원정은 서로를 모르고 이곳을 통해서만 물건을 주고받는다: 기지가 출발(BeginRaid)을, 원정이 끝(EndRaid)을 알린다.
    /// 규칙: 출발하면 "원정 중"이 켜진 채 저장된다. 탈출하면 가져온 것을 몸에 남기고, 쓰러지면 몸에 지닌 것을 모두 잃는다.
    /// 원정 중에 게임을 끄면 다음에 켤 때 쓰러진 것으로 처리한다(익스트랙션 관례, 끄고 다시 켜서 살아남는 것을 막는다).
    /// </summary>
    public static class Profile
    {
        static ProfileData current;

        /// <summary>지금 저장 내용. 처음 쓸 때 파일에서 읽고, 파일이 없으면 새로 시작한다.</summary>
        public static ProfileData Current => current ??= SaveFile.Read() ?? new ProfileData();

        /// <summary>
        /// 이번 실행에서 기지를 거쳐 원정에 나왔는가. 에디터에서 원정 맵을 바로 켰을 때는 false라서,
        /// 원정 맵이 저장 내용을 건드리지 않고 프리팹 기본 장비로 시험할 수 있다.
        /// </summary>
        public static bool DepartedThisRun { get; private set; }

        /// <summary>기지에서 쓰러졌다(글로우병 등). 원정에서 쓰러진 것과 같이 몸에 지닌 것을 잃고 글로우병은 0이 된다.</summary>
        public static void DieAtBase()
        {
            Current.carried = LoadoutData.Empty;
            Current.glowSickness = 0f;
            Save();
        }

        // 에디터에서 플레이를 다시 시작할 때(도메인을 다시 불러오지 않는 설정이어도) 전 플레이의 상태가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay()
        {
            current = null;
            DepartedThisRun = false;
        }

        public static void Save() => SaveFile.Write(Current);

        /// <summary>
        /// 원정 중에 게임이 꺼졌던 저장이면 쓰러진 것으로 처리한다. 기지가 열릴 때 부른다.
        /// 처리했으면 true(몸에 지닌 것을 잃었다).
        /// </summary>
        public static bool ResolveAbandonedRaid()
        {
            if (!Current.inRaid || DepartedThisRun) return false;
            Current.carried = LoadoutData.Empty;
            Current.glowSickness = 0f;
            Current.inRaid = false;
            Save();
            return true;
        }

        /// <summary>몸에 지닌 것을 들고 원정에 나간다.</summary>
        public static void BeginRaid(LoadoutData carried)
        {
            Current.carried = carried;
            Current.inRaid = true;
            DepartedThisRun = true;
            Save();
        }

        /// <summary>원정이 끝났다. 탈출했으면 가져온 것과 글로우병을 그대로 남기고, 쓰러졌으면 모두 잃고 글로우병은 0으로 돌아온다.</summary>
        public static void EndRaid(bool extracted, LoadoutData broughtBack, float glowSickness)
        {
            Current.carried = extracted ? broughtBack : LoadoutData.Empty;
            Current.glowSickness = extracted ? glowSickness : 0f;
            Current.inRaid = false;
            DepartedThisRun = false;
            Save();
        }
    }
}
