using DustScrapper.Raid;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 원정 씬과 저장 내용(Profile)을 잇는다. 기지에서 나왔으면 몸에 지닌 것을 저장대로 채우고,
    /// 판이 끝나면(RaidSession.Ended) 가져온 것 또는 잃은 것을 저장에 알린다.
    /// 에디터에서 원정 맵을 바로 켰으면(기지를 거치지 않음) 저장을 건드리지 않고 프리팹 기본 장비로 시험한다.
    /// 어느 쪽이든 판을 시작할 때 MP를 가득 채운다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaidProfileLink : MonoBehaviour
    {
        [SerializeField] RaidSession session;

        PlayerLoadout player;
        Glow.GlowSickness sickness;
        bool linked;

        void Start()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogError($"{name}: Player 태그가 붙은 플레이어가 없어 들고 온 것을 채울 수 없다.", this);
                return;
            }

            if (Profile.DepartedThisRun)
            {
                player = PlayerLoadout.Of(playerObject);
                player.Apply(Profile.Current.carried);
                if (playerObject.TryGetComponent(out sickness)) sickness.Load(Profile.Current.glowSickness);
                session.Ended += OnEnded;
                linked = true;
            }
            // 장비 보너스가 붙은 뒤에 MP를 가득 채운다(장비를 끼는 것만으로는 MP가 차지 않으므로).
            if (playerObject.TryGetComponent(out Magic.Mana mana)) mana.RestoreFull();
        }

        void OnDestroy()
        {
            if (linked) session.Ended -= OnEnded;
        }

        void OnEnded(RaidResult result) =>
            Profile.EndRaid(result.Extracted, result.Extracted ? player.Capture() : LoadoutData.Empty, sickness != null ? sickness.Value : 0f);
    }
}
