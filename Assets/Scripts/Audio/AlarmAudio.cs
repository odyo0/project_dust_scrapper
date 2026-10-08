using DustScrapper.Enemies;
using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>적이 대상을 발견해 동료를 부를 때 외침 소리를 낸다. 시야 밖에서 들려도 "들켰다"를 알 수 있다.</summary>
    [RequireComponent(typeof(EnemyBrain))]
    [DisallowMultipleComponent]
    public sealed class AlarmAudio : MonoBehaviour
    {
        [SerializeField] SoundCue alarm;

        EnemyBrain brain;

        void Awake() => brain = GetComponent<EnemyBrain>();
        void OnEnable() => brain.Alarmed += OnAlarmed;
        void OnDisable() => brain.Alarmed -= OnAlarmed;

        void OnAlarmed()
        {
            if (alarm != null) alarm.PlayAt(transform.position);
        }
    }
}
