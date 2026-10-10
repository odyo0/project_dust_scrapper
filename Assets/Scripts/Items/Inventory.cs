using System;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 칸으로 나뉜 물건 보관 공간. 플레이어 가방과 상자가 같은 스크립트를 쓴다(상자는 칸이 있는 가방일 뿐이다).
    /// 한 칸에 물건 한 묶음이 들어가고, 겹칠 수 있는 물건(탄약 등)은 한 칸에 최대 개수까지 겹친다.
    /// 무엇이 바뀌었는지는 Changed로만 알린다. 가방 화면, 총의 탄 세기 등은 이 칸들을 읽기만 한다.
    /// 칸 사이 옮기기는 IItemSlots 창구로 한다(ItemTransfer).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Inventory : MonoBehaviour, IItemSlots
    {
        [Tooltip("기본 칸 수. 가방(배낭) 장비가 SetExtraSlots로 칸을 더 늘릴 수 있다.")]
        [SerializeField, Min(1)] int capacity = 12;

        [Tooltip("처음부터 들어 있는 물건 (상자 내용물 등)")]
        [SerializeField] ItemStack[] startingItems;

        ItemStack[] slots;

        public int Capacity => Slots.Length;
        public int SlotCount => Slots.Length;

        /// <summary>장비 없이 처음부터 있는 칸 수.</summary>
        public int BaseCapacity => capacity;

        public bool IsEmpty
        {
            get
            {
                foreach (var stack in Slots)
                {
                    if (!stack.IsEmpty) return false;
                }
                return true;
            }
        }

        /// <summary>칸 내용이 바뀔 때마다.</summary>
        public event Action Changed;

        // 다른 스크립트가 Awake에서 먼저 읽어도 비어 있지 않게, 처음 쓸 때 칸을 만든다.
        ItemStack[] Slots
        {
            get
            {
                if (slots == null) Build();
                return slots;
            }
        }

        void Awake()
        {
            if (slots == null) Build();
        }

        void Build()
        {
            slots = new ItemStack[capacity];
            if (startingItems == null) return;
            foreach (var stack in startingItems)
            {
                if (stack.IsEmpty) continue;
                int left = AddWithoutNotify(stack);
                if (left > 0) Debug.LogWarning($"{name}: 처음 물건 {stack.Item.name} {left}개가 칸이 모자라 들어가지 못했다.", this);
            }
        }

        public ItemStack GetSlot(int index) => Slots[index];

        /// <summary>start번 칸부터 끝까지 모두 비었는가. 가방을 벗어 칸이 줄어들 때 사라질 칸이 비었는지 볼 때 쓴다.</summary>
        public bool IsEmptyFrom(int start)
        {
            for (int i = Mathf.Max(0, start); i < Slots.Length; i++)
            {
                if (!Slots[i].IsEmpty) return false;
            }
            return true;
        }

        /// <summary>
        /// 기본 칸 수에 더할 칸 수를 정한다(가방 장비). 칸이 줄어들 때 사라질 칸에 물건이 있으면 줄이지 않는다
        /// (부른 쪽이 먼저 비었는지 확인해야 한다. 물건이 사라지지 않게 하는 마지막 안전장치다).
        /// </summary>
        public void SetExtraSlots(int extra)
        {
            int count = capacity + Mathf.Max(0, extra);
            if (count == Slots.Length) return;
            if (count < Slots.Length && !IsEmptyFrom(count))
            {
                Debug.LogError($"{name}: 줄어들 칸에 물건이 있어 가방 칸을 줄이지 않았다.", this);
                return;
            }
            Array.Resize(ref slots, count);
            Changed?.Invoke();
        }

        /// <summary>가방·상자 칸에는 어떤 물건이든 놓인다.</summary>
        public bool CanPlace(int index, ItemStack stack) => true;

        public ItemStack TakeSlot(int index)
        {
            ItemStack stack = Slots[index];
            if (stack.IsEmpty) return ItemStack.Empty;
            Slots[index] = ItemStack.Empty;
            Changed?.Invoke();
            return stack;
        }

        public void PlaceInEmptySlot(int index, ItemStack stack)
        {
            if (stack.IsEmpty) return;
            if (!Slots[index].IsEmpty)
            {
                Debug.LogError($"{name}: {index}번 칸이 차 있어 놓지 못했다.", this);
                return;
            }
            Slots[index] = stack;
            Changed?.Invoke();
        }

        /// <summary>이 물건이 모두 몇 개 있는가.</summary>
        public int CountOf(ItemData item)
        {
            if (item == null) return 0;
            int total = 0;
            foreach (var stack in Slots)
            {
                if (stack.Item == item) total += stack.Count;
            }
            return total;
        }

        /// <summary>이 물건을 하나라도 더 넣을 자리가 있는가.</summary>
        public bool HasRoomFor(ItemData item)
        {
            if (item == null) return false;
            foreach (var stack in Slots)
            {
                if (stack.IsEmpty) return true;
                if (stack.Item == item && stack.Count < item.MaxStack) return true;
            }
            return false;
        }

        /// <summary>넣을 수 있는 만큼 넣고, 자리가 없어 남은 개수를 돌려준다.</summary>
        public int Add(ItemStack stack)
        {
            int left = AddWithoutNotify(stack);
            if (left < stack.Count) Changed?.Invoke();
            return left;
        }

        /// <summary>이 물건을 최대 amount개 꺼내고, 실제로 꺼낸 개수를 돌려준다.</summary>
        public int Remove(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return 0;

            int removed = 0;
            // 뒤 칸부터 꺼내서, 앞쪽의 꽉 찬 묶음보다 나중에 들어온 덜 찬 묶음이 먼저 줄어들게 한다.
            for (int i = Slots.Length - 1; i >= 0 && removed < amount; i--)
            {
                if (Slots[i].Item != item) continue;
                int take = Mathf.Min(amount - removed, Slots[i].Count);
                Slots[i] = Slots[i].WithCount(Slots[i].Count - take);
                removed += take;
            }

            if (removed > 0) Changed?.Invoke();
            return removed;
        }

        int AddWithoutNotify(ItemStack stack)
        {
            if (stack.IsEmpty) return 0;

            ItemData item = stack.Item;
            int left = stack.Count;

            // 겹칠 수 있는 물건은 이미 있는 묶음부터 채운다.
            if (item.MaxStack > 1)
            {
                for (int i = 0; i < Slots.Length && left > 0; i++)
                {
                    if (Slots[i].Item != item) continue;
                    int put = Mathf.Min(left, item.MaxStack - Slots[i].Count);
                    if (put <= 0) continue;
                    Slots[i] = Slots[i].WithCount(Slots[i].Count + put);
                    left -= put;
                }
            }

            for (int i = 0; i < Slots.Length && left > 0; i++)
            {
                if (!Slots[i].IsEmpty) continue;
                int put = Mathf.Min(left, item.MaxStack);
                Slots[i] = new ItemStack(item, put, stack.LoadedAmmo);
                left -= put;
            }

            return left;
        }
    }
}
