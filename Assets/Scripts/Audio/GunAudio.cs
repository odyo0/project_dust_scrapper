using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>총 프리팹에 붙어서 총의 사건(쏘기, 재장전 시작)을 소리로 바꾼다. 총 스크립트는 소리를 모른다.</summary>
    [RequireComponent(typeof(GunFire))]
    [DisallowMultipleComponent]
    public sealed class GunAudio : MonoBehaviour
    {
        [SerializeField] SoundCue fire;
        [SerializeField] SoundCue reload;

        GunFire gun;

        void Awake() => gun = GetComponent<GunFire>();

        void OnEnable()
        {
            gun.Fired += OnFired;
            gun.ReloadStarted += OnReloadStarted;
        }

        void OnDisable()
        {
            gun.Fired -= OnFired;
            gun.ReloadStarted -= OnReloadStarted;
        }

        void OnFired(Vector2 direction)
        {
            if (fire != null) fire.PlayAt(transform.position);
        }

        void OnReloadStarted()
        {
            if (reload != null) reload.PlayAt(transform.position);
        }
    }
}
