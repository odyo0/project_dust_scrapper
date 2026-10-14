using DustScrapper.Interaction;
using DustScrapper.Items;
using DustScrapper.Noise;
using UnityEngine;

namespace DustScrapper.Glow
{
    /// <summary>
    /// 원정 맵의 글로우 광맥. E를 오래 누르고 있으면 원석이 나와 가방에 들어간다. 캘 때마다 소리가 나서 적이 들을 수 있다.
    /// 몇 번 캐면 다 떨어져 더는 고를 수 없다(흐려짐). 나오는 개수·횟수·소리는 데이터로 바꾼다.
    /// </summary>
    public sealed class GlowVein : Interactable
    {
        [SerializeField] ItemData ore;
        [SerializeField, Min(1)] int minPerDig = 1;
        [SerializeField, Min(1)] int maxPerDig = 2;
        [Tooltip("캘 수 있는 횟수")]
        [SerializeField, Min(1)] int digs = 3;
        [Tooltip("캘 때 나는 소음 거리 (칸)")]
        [SerializeField, Min(0f)] float noiseRadius = 9f;
        [SerializeField] SpriteRenderer look;
        [SerializeField, Range(0f, 1f)] float depletedAlpha = 0.25f;

        int digsLeft;

        public override bool IsAvailable => digsLeft > 0;
        public override string ActionName => "캐기";
        public override string TargetName => ore != null ? $"{ore.DisplayName} 광맥" : "광맥";

        void Awake() => digsLeft = digs;

        public override string BlockedReason(PlayerInteractor interactor) =>
            interactor.Inventory != null && interactor.Inventory.HasRoomFor(ore) ? null : "가방이 가득 찼다";

        public override void Interact(PlayerInteractor interactor)
        {
            int amount = Random.Range(minPerDig, maxPerDig + 1);
            int left = interactor.Inventory.Add(new ItemStack(ore, amount));
            if (left > 0) Debug.Log($"{name}: 가방이 차서 원석 {left}개를 못 담았다.");

            NoiseBus.Emit(new NoiseEvent(transform.position, noiseRadius, interactor.gameObject));
            digsLeft--;
            if (digsLeft <= 0 && look != null)
            {
                Color color = look.color;
                color.a = depletedAlpha;
                look.color = color;
            }
        }
    }
}
