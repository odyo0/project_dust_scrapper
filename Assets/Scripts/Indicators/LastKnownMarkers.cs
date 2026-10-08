using System.Collections.Generic;
using DustScrapper.Enemies;
using UnityEngine;

namespace DustScrapper.Indicators
{
    /// <summary>
    /// 적 팀이 플레이어를 마지막으로 본 위치에 반투명한 플레이어 모습을 띄운다(팀마다 하나).
    /// 팀이 경계 중이고 지금은 아무도 플레이어를 못 보고 있을 때만 보이고, 팀이 진정하면 사라진다.
    /// 플레이어는 이걸 보고 "적이 나를 어디쯤으로 알고 있는지"를 읽어 숨을 곳을 정한다.
    /// 팀의 정보(EnemyTeam)를 읽기만 하고 바꾸지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LastKnownMarkers : MonoBehaviour
    {
        [Tooltip("반투명 플레이어 모습. 그림의 색 투명도는 프리팹에 정한 값이 최대 진하기다.")]
        [SerializeField] GameObject ghostPrefab;

        [Tooltip("나타나고 사라지는 빠르기 (1초에 바뀌는 진하기, 0~1)")]
        [SerializeField, Min(0.1f)] float fadeSpeed = 4f;

        readonly Dictionary<EnemyTeam, Ghost> ghosts = new();
        readonly List<EnemyTeam> gone = new();

        void LateUpdate()
        {
            foreach (EnemyTeam team in EnemyTeam.All)
            {
                if (!ghosts.TryGetValue(team, out Ghost ghost))
                {
                    ghost = new Ghost(Instantiate(ghostPrefab, transform));
                    ghosts.Add(team, ghost);
                }

                bool show = team.IsAlerted && team.HasSighting && !team.IsSeeingTarget;
                if (show) ghost.Root.transform.position = team.LastKnownPosition;
                ghost.FadeTowards(show ? 1f : 0f, fadeSpeed * Time.deltaTime);
            }

            // 사라진 팀(씬에서 지워진 경우)의 모습은 지운다.
            gone.Clear();
            foreach (EnemyTeam team in ghosts.Keys)
            {
                if (team == null || !team.isActiveAndEnabled) gone.Add(team);
            }
            foreach (EnemyTeam team in gone)
            {
                Destroy(ghosts[team].Root);
                ghosts.Remove(team);
            }
        }

        /// <summary>모습 하나. 그림들의 원래 투명도를 기억해 두고 0~1 진하기를 곱한다.</summary>
        sealed class Ghost
        {
            readonly SpriteRenderer[] renderers;
            readonly float[] baseAlpha;
            float strength;

            public GameObject Root { get; }

            public Ghost(GameObject root)
            {
                Root = root;
                renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
                baseAlpha = new float[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) baseAlpha[i] = renderers[i].color.a;
                Apply();
            }

            public void FadeTowards(float target, float step)
            {
                float next = Mathf.MoveTowards(strength, target, step);
                if (Mathf.Approximately(next, strength) && Root.activeSelf == strength > 0f) return;
                strength = next;
                Apply();
            }

            void Apply()
            {
                Root.SetActive(strength > 0f);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Color color = renderers[i].color;
                    color.a = baseAlpha[i] * strength;
                    renderers[i].color = color;
                }
            }
        }
    }
}
