using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>
    /// 소리 하나의 재생 방법: 어떤 소리 파일 중에서 고를지, 얼마나 크게, 음높이를 얼마나 흔들지, 얼마나 멀리까지 들리는지.
    /// 같은 총소리도 파일 여러 개 + 음높이 흔들기로 매번 조금씩 다르게 들린다.
    /// 나중에 진짜 소리로 바꿀 때는 이 데이터의 파일만 갈아 끼우면 되고 코드는 그대로다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Sound Cue", fileName = "SoundCue")]
    public sealed class SoundCue : ScriptableObject
    {
        [Tooltip("재생할 때마다 이 중 하나를 무작위로 고른다")]
        [SerializeField] AudioClip[] clips;

        [SerializeField, Range(0f, 1f)] float volume = 0.7f;

        [Tooltip("음높이를 이 범위에서 무작위로 고른다 (1 = 원래 높이)")]
        [SerializeField] Vector2 pitchRange = new(0.95f, 1.05f);

        [Tooltip("이 거리(칸)까지 들린다. 멀어질수록 작아진다. 적이 듣는 소음 거리와는 따로다.")]
        [SerializeField, Min(1f)] float maxDistance = 20f;

        public float Volume => volume;
        public float MaxDistance => maxDistance;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float PickPitch() => Random.Range(pitchRange.x, pitchRange.y);

        /// <summary>이 자리에서 이 소리를 낸다.</summary>
        public void PlayAt(Vector2 position) => SoundPlayer.Play(this, position);
    }
}
