using DustScrapper.Items;
using DustScrapper.Player;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 플레이어가 몸에 지닌 칸 전부(PlayerGear.SaveOrder)를 저장 형식(LoadoutData)으로 읽고 쓴다.
    /// 기지(출발할 때 들고 갈 것)와 원정(가져온 것)이 저장할 때 모두 이 한 곳을 거친다.
    /// 어떤 칸이 있는지는 PlayerGear가 정하므로, 칸이 늘어도 여기는 그대로다.
    /// </summary>
    public sealed class PlayerLoadout
    {
        readonly PlayerGear gear;

        PlayerLoadout(PlayerGear gear) => this.gear = gear;

        /// <summary>플레이어 물체에서 만든다. 지닌 칸 묶음(PlayerGear)이 없으면 null.</summary>
        public static PlayerLoadout Of(GameObject player)
        {
            PlayerGear gear = player != null ? player.GetComponent<PlayerGear>() : null;
            if (player != null && gear == null) Debug.LogError($"{player.name}: PlayerGear가 없어 지닌 것을 저장할 수 없다.", player);
            return gear != null ? new PlayerLoadout(gear) : null;
        }

        /// <summary>칸이 하나라도 바뀌었을 때(저장할 때를 알려고 쓴다).</summary>
        public event System.Action Changed
        {
            add { foreach (var saved in gear.SaveOrder) saved.Slots.Changed += value; }
            remove { foreach (var saved in gear.SaveOrder) saved.Slots.Changed -= value; }
        }

        public LoadoutData Capture()
        {
            var data = new LoadoutData();
            foreach (var saved in gear.SaveOrder) data.Set(saved.Key, Save(saved.Slots));
            return data;
        }

        /// <summary>
        /// 지금 지닌 것을 모두 치우고 저장된 대로 채운다(프리팹에 넣어 둔 시작 무기도 치운다).
        /// SaveOrder 순서로 채우고 거꾸로 치운다(배낭이 가방 칸을 늘리므로 몸 장비를 가방보다 먼저 끼고, 치울 때는 가방부터 비운다).
        /// </summary>
        public void Apply(LoadoutData data)
        {
            data ??= LoadoutData.Empty;
            var order = gear.SaveOrder;
            for (int i = order.Count - 1; i >= 0; i--) Clear(order[i].Slots);
            foreach (var saved in order) Load(saved.Slots, data.Get(saved.Key));
        }

        static SavedStack[] Save(IItemSlots slots)
        {
            var saved = new SavedStack[slots.SlotCount];
            for (int i = 0; i < saved.Length; i++) saved[i] = SavedStack.From(slots.GetSlot(i));
            return saved;
        }

        static void Clear(IItemSlots slots)
        {
            for (int i = slots.SlotCount - 1; i >= 0; i--) slots.TakeSlot(i);
        }

        /// <summary>
        /// 저장된 칸 번호 그대로 놓는다. 그 칸에 놓을 수 없으면(칸이 줄었거나 규칙이 바뀜) 가방에 넣어 물건이 사라지지 않게 한다.
        /// </summary>
        void Load(IItemSlots slots, SavedStack[] saved)
        {
            for (int i = 0; i < saved.Length; i++)
            {
                ItemStack stack = saved[i].ToStack();
                if (stack.IsEmpty) continue;

                if (i < slots.SlotCount && slots.GetSlot(i).IsEmpty && slots.CanPlace(i, stack))
                {
                    slots.PlaceInEmptySlot(i, stack);
                    continue;
                }
                int left = gear.Bag != null ? gear.Bag.Add(stack) : stack.Count;
                if (left > 0) Debug.LogWarning($"저장된 {stack.Item.name} {left}개를 놓을 칸이 없어 버렸다.");
            }
        }
    }
}
