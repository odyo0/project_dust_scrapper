using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>
    /// 효과음을 월드의 한 자리에서 재생한다. 소리를 듣는 귀(AudioListener)는 플레이어에 있어서,
    /// 멀수록 작게, 왼쪽에서 나면 왼쪽에서 들린다.
    /// 재생할 때마다 오디오 물체를 만들지 않도록, 처음 쓸 때 재생기 묶음을 만들어 돌려 쓴다.
    /// 기관단총처럼 초당 열 번 넘게 소리가 나도 쓰레기(메모리)가 쌓이지 않는다.
    /// </summary>
    public static class SoundPlayer
    {
        const int PoolSize = 32;
        /// <summary>이 거리(칸) 안에서는 줄지 않고 최대 크기로 들린다.</summary>
        const float FullVolumeDistance = 1.5f;

        static AudioSource[] pool;
        static int next;

        public static void Play(SoundCue cue, Vector2 position)
        {
            if (cue == null) return;
            AudioClip clip = cue.PickClip();
            if (clip == null) return;

            AudioSource source = TakeSource();
            source.transform.position = new Vector3(position.x, position.y, 0f);
            source.clip = clip;
            source.volume = cue.Volume;
            source.pitch = cue.PickPitch();
            source.maxDistance = cue.MaxDistance;
            source.Play();
        }

        /// <summary>쉬고 있는 재생기를 찾는다. 모두 재생 중이면 가장 오래전에 쓴 것을 끊고 쓴다.</summary>
        static AudioSource TakeSource()
        {
            if (pool == null || pool[0] == null) CreatePool();

            for (int i = 0; i < PoolSize; i++)
            {
                AudioSource candidate = pool[(next + i) % PoolSize];
                if (candidate.isPlaying) continue;
                next = (next + i + 1) % PoolSize;
                return candidate;
            }
            AudioSource oldest = pool[next];
            next = (next + 1) % PoolSize;
            return oldest;
        }

        static void CreatePool()
        {
            var root = new GameObject("Sound Player");
            Object.DontDestroyOnLoad(root);
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var child = new GameObject("Source " + i);
                child.transform.SetParent(root.transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                // 위에서 내려다보는 2D라 소리가 화면 위치에 따라 좌우로 갈리고 거리에 따라 줄게 한다.
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = FullVolumeDistance;
                source.dopplerLevel = 0f;
                pool[i] = source;
            }
            next = 0;
        }

        // 도메인 다시 불러오기를 끈 설정에서도 지난 실행의 재생기 묶음을 쓰지 않게 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPool() => pool = null;
    }
}
