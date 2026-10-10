using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>마법 스킬이 붙은 장비 프리팹에 붙어서 시전 시작·탄 발사를 소리로 바꾼다. 마법 스크립트는 소리를 모른다.</summary>
    [RequireComponent(typeof(SpellCast))]
    [DisallowMultipleComponent]
    public sealed class SpellAudio : MonoBehaviour
    {
        [Tooltip("시전을 시작할 때 (비워 두면 없음)")]
        [SerializeField] SoundCue castStart;
        [Tooltip("탄이 나갈 때")]
        [SerializeField] SoundCue release;

        SpellCast spell;

        void Awake() => spell = GetComponent<SpellCast>();

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
