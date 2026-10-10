using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>
    /// 마법 스킬이 붙은 장비 프리팹에 붙어서 시전 시작·탄 발사를 소리로 바꾼다. 마법 스크립트는 소리를 모른다.
    /// 한 장비에 마법이 둘(기본·특수)이면 마법마다 하나씩 붙이고 spell에 그 마법을 넣는다.
    /// </summary>
    [RequireComponent(typeof(SpellCast))]
    public sealed class SpellAudio : MonoBehaviour
    {
        [Tooltip("소리를 낼 마법. 비워 두면 같은 물체의 첫 마법.")]
        [SerializeField] SpellCast spell;

        [Tooltip("시전을 시작할 때 (비워 두면 없음)")]
        [SerializeField] SoundCue castStart;
        [Tooltip("탄이 나갈 때")]
        [SerializeField] SoundCue release;

        void Awake()
        {
            if (spell == null) spell = GetComponent<SpellCast>();
        }

        void OnEnable()
        {
            spell.CastStarted += OnCastStarted;
            spell.Released += OnReleased;
        }

        void OnDisable()
        {
            spell.CastStarted -= OnCastStarted;
            spell.Released -= OnReleased;
        }

        void OnCastStarted()
        {
            if (castStart != null) castStart.PlayAt(transform.position);
        }

        void OnReleased(Vector2 direction)
        {
            if (release != null) release.PlayAt(transform.position);
        }
    }
}
