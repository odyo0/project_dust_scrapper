using UnityEngine;

namespace DustScrapper.Noise
{
    /// <summary>
    /// 소리 한 번의 정보. 소리를 낸 쪽(총, 발걸음, 적의 외침)이 만들어 NoiseBus로 퍼뜨린다.
    /// 듣는 쪽은 이 값만 보고 반응하므로, 무엇이 낸 소리인지 몰라도 된다.
    /// </summary>
    public readonly struct NoiseEvent
    {
        /// <summary>소리가 난 곳(월드 좌표).</summary>
        public readonly Vector2 Position;

        /// <summary>소리가 들리는 거리(칸).</summary>
        public readonly float Radius;

        /// <summary>소리를 낸 쪽. 자기 소리는 듣지 않게 할 때 쓴다. 없으면 null.</summary>
        public readonly GameObject Source;

        /// <summary>적이 동료를 부르는 외침인가. 들은 적은 걷지 않고 달려온다.</summary>
        public readonly bool IsAlarm;

        public NoiseEvent(Vector2 position, float radius, GameObject source, bool isAlarm = false)
        {
            Position = position;
            Radius = radius;
            Source = source;
            IsAlarm = isAlarm;
        }
    }
}
