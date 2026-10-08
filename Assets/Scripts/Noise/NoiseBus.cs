using System;
using UnityEngine;

namespace DustScrapper.Noise
{
    /// <summary>
    /// 소리를 퍼뜨리는 통로. 소리를 내는 쪽은 Emit만 부르고 누가 듣는지 모른다.
    /// 듣는 쪽(적의 감각)은 Emitted를 구독해서 자기에게 들리는 거리인지만 판단한다.
    /// 그래서 소음 발생기나 마법 소리가 생겨도 듣는 쪽 코드는 그대로다.
    /// </summary>
    public static class NoiseBus
    {
        /// <summary>어디선가 소리가 났을 때.</summary>
        public static event Action<NoiseEvent> Emitted;

        public static void Emit(NoiseEvent noise)
        {
            if (noise.Radius <= 0f) return;
            Emitted?.Invoke(noise);
#if UNITY_EDITOR
            DrawInSceneView(noise);
#endif
        }

        // 도메인 다시 불러오기를 끈 설정에서도 지난 실행의 구독이 남지 않게 시작할 때 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSubscribers() => Emitted = null;

#if UNITY_EDITOR
        /// <summary>디버그용. Scene 창에 소리가 퍼진 범위를 잠깐 원으로 그린다(플레이 화면에는 안 보인다).</summary>
        static void DrawInSceneView(NoiseEvent noise)
        {
            const int Segments = 32;
            Color color = noise.IsAlarm ? new Color(1f, 0.3f, 0.2f, 0.8f) : new Color(1f, 0.9f, 0.3f, 0.5f);
            Vector2 previous = noise.Position + Vector2.right * noise.Radius;
            for (int i = 1; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                Vector2 next = noise.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * noise.Radius;
                Debug.DrawLine(previous, next, color, 0.4f);
                previous = next;
            }
        }
#endif
    }
}
