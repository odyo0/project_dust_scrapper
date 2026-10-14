using DustScrapper.Interaction;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 기지의 정제 시설. 글로우 원석 몇 개를 정제된 글로우 하나로 바꾼다.
    /// 원석은 가방에서 먼저, 모자라면 보관함에서 꺼내고, 결과물은 가방에 먼저 넣고 넘치면 보관함에 넣는다.
    /// 지금은 바로 바뀐다. 정제에 시간이 걸리게 하려면(기획 빈칸) 여기서 "맡겨 두고 시간이 지나면 받기"로 바꾼다.
    /// </summary>
    public sealed class RefineStation : Station
    {
        [SerializeField] string displayName = "정제 시설";

        [Tooltip("넣는 것 (글로우 원석)")]
        [SerializeField] ItemData input;

        [Tooltip("나오는 것 (정제된 글로우)")]
        [SerializeField] ItemData output;

        [Tooltip("정제된 글로우 하나를 만드는 데 드는 원석 개수")]
        [SerializeField, Min(1)] int inputPerOutput = 2;

        public ItemData Input => input;
        public ItemData Output => output;
        public int InputPerOutput => inputPerOutput;

        public override string ActionName => "정제";
        public override string TargetName => displayName;

        public override string BlockedReason(PlayerInteractor interactor) =>
            input == null || output == null ? "정제할 것이 정해지지 않았다" : null;

        /// <summary>가방 + 보관함에 있는 원석 개수.</summary>
        public int InputCount(Inventory bag) =>
            (bag != null ? bag.CountOf(input) : 0) + (Storage != null ? Storage.CountOf(input) : 0);

        /// <summary>지금 원석으로 몇 개까지 만들 수 있나.</summary>
        public int MaxBatches(Inventory bag) => InputCount(bag) / inputPerOutput;

        /// <summary>정제된 글로우를 batches개 만든다. 원석이나 넣을 자리가 모자라면 하지 않는다. 못 하면 이유를, 했으면 null.</summary>
        public string TryRefine(int batches, Inventory bag)
        {
            if (batches <= 0) return "정제할 개수가 없다";
            int need = batches * inputPerOutput;
            if (InputCount(bag) < need) return $"{input.DisplayName}이(가) 모자란다";

            // 원석을 빼면 그 자리가 비므로, 빼기 전 자리만 보고 판단하면 실제보다 적게 잡힌다(안전한 쪽).
            int space = (bag != null ? bag.SpaceFor(output) : 0) + (Storage != null ? Storage.SpaceFor(output) : 0);
            if (space < batches) return "넣을 자리가 없다";

            int taken = bag != null ? bag.Remove(input, need) : 0;
            if (taken < need && Storage != null) Storage.Remove(input, need - taken);

            int left = bag != null ? bag.Add(new ItemStack(output, batches)) : batches;
            if (left > 0 && Storage != null) Storage.Add(new ItemStack(output, left));
            return null;
        }
    }
}
