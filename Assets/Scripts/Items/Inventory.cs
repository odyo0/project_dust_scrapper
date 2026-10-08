using System;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 칸으로 나뉜 물건 보관 공간. 플레이어 가방과 상자가 같은 스크립트를 쓴다(상자는 칸이 있는 가방일 뿐이다).
    /// 한 칸에 물건 한 묶음이 들어가고, 겹칠 수 있는 물건(탄약 등)은 한 칸에 최대 개수까지 겹친다.
    /// 무엇이 바뀌었는지는 Changed로만 알린다. 가방 화면, 총의 탄 세기 등은 이 칸들을 읽기만 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Inventory : MonoBehaviour
    {
        [Tooltip("칸 수")]
        [SerializeField, Min(1)] int capacity = 12;

        [Tooltip("처음부터 들어 있는 물건 (상자 내용물 등)")]
        [SerializeField] ItemStack[] startingItems;

        ItemStack[] slots;

        public int Capacity => Slots.Length;

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
