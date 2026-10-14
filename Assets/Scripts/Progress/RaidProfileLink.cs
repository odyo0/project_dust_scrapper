using DustScrapper.Raid;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 원정 씬과 저장 내용(Profile)을 잇는다. 기지에서 나왔으면 몸에 지닌 것을 저장대로 채우고,
    /// 판이 끝나면(RaidSession.Ended) 가져온 것 또는 잃은 것을 저장에 알린다.
    /// 에디터에서 원정 맵을 바로 켰으면(기지를 거치지 않음) 아무것도 하지 않아서, 프리팹 기본 장비로 시험하고 저장도 건드리지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaidProfileLink : MonoBehaviour
    {
        [SerializeField] RaidSession session;

        PlayerLoadout player;
        bool linked;

        void Start()
        {
            if (!Profile.DepartedThisRun) return;

            player = PlayerLoadout.Of(GameObject.FindWithTag("Player"));
            if (player == null)
            {
                Debug.LogError($"{name}: Player 태그가 붙은 플레이어가 없어 들고 온 것을 채울 수 없다.", this);
                return;
            }
            player.Apply(Profile.Current.carried);
            session.Ended += OnEnded;
            linked = true;
        }

        void OnDestroy()
        {
            if (linked) session.Ended -= OnEnded;
        }

        void OnEnded(RaidResult result) => Profile.EndRaid(result.Extracted, result.Extracted ? player.Capture() : LoadoutData.Empty);
    }
}
