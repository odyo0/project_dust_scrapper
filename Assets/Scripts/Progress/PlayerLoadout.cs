using System.Collections.Generic;
using DustScrapper.Equipment;
using DustScrapper.Items;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 플레이어가 몸에 지닌 칸들(무기 칸, 보조 칸, 몸 장비, 가방, 소모품 단축 칸)을 한 덩어리로 읽고 쓴다.
    /// 기지(출발할 때 들고 갈 것), 원정(가져온 것·잃은 것), 저장이 모두 이 한 곳을 거쳐서, 칸이 늘어도 고칠 곳이 하나다.
    /// </summary>
    public sealed class PlayerLoadout
    {
        readonly WeaponLoadout weapons;
        readonly SupportHand support;
        readonly BodyEquipment body;
        readonly Inventory bag;
        readonly QuickSlots quick;

        PlayerLoadout(GameObject player)
        {
            weapons = player.GetComponent<WeaponLoadout>();
            support = player.GetComponent<SupportHand>();
            body = player.GetComponent<BodyEquipment>();
            bag = player.GetComponent<Inventory>();
            quick = player.GetComponent<QuickSlots>();
        }

        public static PlayerLoadout Of(GameObject player) => player != null ? new PlayerLoadout(player) : null;

        /// <summary>칸이 하나라도 바뀌었을 때(저장할 때를 알려고 쓴다).</summary>
        public event System.Action Changed
        {
            add { foreach (var slots in AllSlots()) slots.Changed += value; }
            remove { foreach (var slots in AllSlots()) slots.Changed -= value; }
        }

        /// <summary>지금 지닌 물건 전부(빈 칸 빼고). 판 결과가 쓴다.</summary>
        public List<ItemStack> CollectItems()
        {
            var items = new List<ItemStack>();
            foreach (var slots in AllSlots())
            {
                for (int i = 0; i < slots.SlotCount; i++)
                {
                    ItemStack stack = slots.GetSlot(i);
                    if (!stack.IsEmpty) items.Add(stack);
                }
            }
            return items;
        }

        public LoadoutData Capture() => new()
        {
            weapons = Save(weapons),
            support = Save(support),
            body = Save(body),
            bag = Save(bag),
            quick = Save(quick),
        };

        /// <summary>
        /// 지금 지닌 것을 모두 치우고 저장된 대로 채운다(프리팹에 넣어 둔 시작 무기도 치운다).
        /// 가방(배낭)이 가방 칸을 늘리므로 몸 장비를 먼저 끼고 가방을 채우고, 치울 때는 거꾸로 가방부터 비운다.
        /// </summary>
        public void Apply(LoadoutData data)
        {
            data ??= LoadoutData.Empty;
            Clear(bag);
            Clear(body);
            Clear(weapons);
            Clear(support);
            Clear(quick);

            Load(body, data.body);
            Load(bag, data.bag);
            Load(weapons, data.weapons);
            Load(support, data.support);
            Load(quick, data.quick);
        }

        IEnumerable<IItemSlots> AllSlots()
        {
            if (weapons != null) yield return weapons;
            if (support != null) yield return support;
            if (body != null) yield return body;
            if (bag != null) yield return bag;
            if (quick != null) yield return quick;
        }

        static SavedStack[] Save(IItemSlots slots)
        {
            if (slots == null) return System.Array.Empty<SavedStack>();
            var saved = new SavedStack[slots.SlotCount];
            for (int i = 0; i < saved.Length; i++) saved[i] = SavedStack.From(slots.GetSlot(i));
            return saved;
        }

        static void Clear(IItemSlots slots)
        {
            if (slots == null) return;
            for (int i = slots.SlotCount - 1; i >= 0; i--) slots.TakeSlot(i);
        }

        /// <summary>
        /// 저장된 칸 번호 그대로 놓는다. 그 칸에 놓을 수 없으면(칸이 줄었거나 규칙이 바뀜) 가방에 넣어 물건이 사라지지 않게 한다.
        /// </summary>
        void Load(IItemSlots slots, SavedStack[] saved)
        {
            if (slots == null || saved == null) return;
            for (int i = 0; i < saved.Length; i++)
            {
                ItemStack stack = saved[i].ToStack();
                if (stack.IsEmpty) continue;

                if (i < slots.SlotCount && slots.GetSlot(i).IsEmpty && slots.CanPlace(i, stack))
                {
                    slots.PlaceInEmptySlot(i, stack);
                    continue;
                }
                int left = bag != null ? bag.Add(stack) : stack.Count;
                if (left > 0) Debug.LogWarning($"저장된 {stack.Item.name} {left}개를 놓을 칸이 없어 버렸다.");
            }
        }
    }
}
