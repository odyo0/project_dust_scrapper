using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Enemies
{
    /// <summary>
    /// 몇 마리씩 묶인 적의 팀. 경계 중인 팀원들이 "플레이어를 마지막으로 본 위치"를 여기에 모아 같이 쓴다.
    /// 한 팀원이 플레이어를 보면 위치를 알리고, 놓친 팀원들은 그 위치로 간다. 다른 팀은 이 정보를 모른다.
    /// 씬에서 적들을 이 컴포넌트가 붙은 물체 아래에 넣으면 한 팀이 된다. 팀에 묶이지 않은 적은 자기 혼자 한 팀이 된다.
    /// 경계 중인 팀원이 하나도 없으면 위치 정보는 지워진다(팀 전체가 진정됨).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyTeam : MonoBehaviour
    {
        /// <summary>팀원이 지금 플레이어를 보고 있다고 칠 시간 (초). 이보다 오래 못 봤으면 "놓쳤다".</summary>
        const float SeeingGrace = 0.15f;

        static readonly List<EnemyTeam> all = new();

        readonly HashSet<EnemyBrain> alertedMembers = new();

        /// <summary>씬에 있는 모든 팀. 마지막 위치 표시가 팀마다 하나씩 그릴 때 쓴다.</summary>
        public static IReadOnlyList<EnemyTeam> All => all;

        /// <summary>플레이어를 본 위치 정보가 있는가.</summary>
        public bool HasSighting { get; private set; }

        /// <summary>팀원 누군가가 플레이어를 마지막으로 본 위치.</summary>
        public Vector2 LastKnownPosition { get; private set; }

        /// <summary>그 위치를 본 시각 (Time.time). 팀원이 "새 정보인가"를 판단할 때 쓴다.</summary>
        public float LastSightingTime { get; private set; } = float.NegativeInfinity;

        /// <summary>경계 중인 팀원이 있는가.</summary>
        public bool IsAlerted => alertedMembers.Count > 0;

        /// <summary>지금 팀원 누군가가 플레이어를 보고 있는가.</summary>
        public bool IsSeeingTarget => HasSighting && Time.time - LastSightingTime <= SeeingGrace;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        /// <summary>팀원이 플레이어를 보고 있을 때 매 프레임 부른다.</summary>
        public void ReportSighting(Vector2 position)
        {
            HasSighting = true;
            LastKnownPosition = position;
            LastSightingTime = Time.time;
        }

        /// <summary>팀원이 경계 상태에 들어가거나 벗어날 때 부른다.</summary>
        public void SetAlerted(EnemyBrain member, bool alerted)
        {
            if (alerted) alertedMembers.Add(member);
            else alertedMembers.Remove(member);

            if (alertedMembers.Count == 0) HasSighting = false;
        }

        public bool Contains(GameObject other) => other != null && other.GetComponentInParent<EnemyTeam>() == this;

        // 도메인 다시 불러오기를 끈 설정에서도 지난 실행의 팀 목록이 남지 않게 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() => all.Clear();
    }
}
