using System.Collections.Generic;
using DustScrapper.Equipment;
using DustScrapper.Items;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Player
{
    /// <summary>
    /// 플레이어가 몸에 지닌 칸 전부(무기 칸, 보조 칸, 몸 장비, 가방, 소모품 단축 칸)를 한곳에 모은 목록.
    /// 칸의 종류를 아는 곳은 여기 하나다. 줍기(WorldItem)·빠른 옮기기·판 결과·저장은 이 목록만 돌기 때문에,
    /// 새 칸(예: 랜턴 칸)은 칸 스크립트를 만들고 아래 두 목록에 넣기만 하면 된다(가방 화면에 칸 그림 하나는 따로 붙인다).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGear : MonoBehaviour
    {
        /// <summary>저장에 쓰는 칸 이름표. 바꾸면 옛 저장 파일의 그 칸을 못 읽는다.</summary>
        public static class Keys
        {
            public const string Weapons = "weapons";
            public const string Support = "support";
            public const string Body = "body";
            public const string Bag = "bag";
            public const string Quick = "quick";
        }

        /// <summary>저장할 칸 하나: 이름표와 칸.</summary>
        public readonly struct SlotEntry
        {
            public readonly string Key;
            public readonly IItemSlots Slots;

            public SlotEntry(string key, IItemSlots slots)
            {
                Key = key;
                Slots = slots;
            }
        }

        [SerializeField] Inventory bag;
        [SerializeField] WeaponLoadout weapons;
        [SerializeField] SupportHand support;
        [SerializeField] BodyEquipment body;
        [SerializeField] QuickSlots quick;

        readonly List<IEquipSlots> equipOrder = new();
        readonly List<SlotEntry> saveOrder = new();
        bool built;

        public Inventory Bag { get { Build(); return bag; } }
        public WeaponLoadout Weapons { get { Build(); return weapons; } }
        public SupportHand Support { get { Build(); return support; } }
        public BodyEquipment Body { get { Build(); return body; } }
        public QuickSlots Quick { get { Build(); return quick; } }

        /// <summary>
        /// 주운 장비를 끼울 빈 칸을 찾는 순서. 무기 칸 → 보조 칸 → 몸 장비.
        /// 스태프처럼 무기 칸과 보조 칸에 다 맞으면 무기 칸이 먼저다(기획).
        /// </summary>
        public IReadOnlyList<IEquipSlots> EquipOrder { get { Build(); return equipOrder; } }

        /// <summary>
        /// 저장·판 결과에 들어가는 칸 전부. 불러올 때 이 순서로 채우고, 치울 때는 거꾸로 비운다.
        /// 배낭(몸 장비)이 가방 칸을 늘리므로 몸 장비가 가방보다 먼저다.
        /// </summary>
        public IReadOnlyList<SlotEntry> SaveOrder { get { Build(); return saveOrder; } }

        void Awake() => Build();

        void Build()
        {
            if (built) return;
            built = true;
            if (bag == null) bag = GetComponent<Inventory>();
            if (weapons == null) weapons = GetComponent<WeaponLoadout>();
            if (support == null) support = GetComponent<SupportHand>();
            if (body == null) body = GetComponent<BodyEquipment>();
            if (quick == null) quick = GetComponent<QuickSlots>();

            AddEquip(weapons);
            AddEquip(support);
            AddEquip(body);

            AddSaved(Keys.Body, body);
            AddSaved(Keys.Bag, bag);
            AddSaved(Keys.Weapons, weapons);
            AddSaved(Keys.Support, support);
            AddSaved(Keys.Quick, quick);
        }

        void AddEquip(IEquipSlots slots)
        {
            if (slots != null) equipOrder.Add(slots);
        }

        void AddSaved(string key, IItemSlots slots)
        {
            if (slots != null) saveOrder.Add(new SlotEntry(key, slots));
        }

        /// <summary>이 물건을 지금 바로 낄 수 있는 첫 칸. 없으면 null(가방으로 간다).</summary>
        public IEquipSlots FindEquipSlot(ItemStack stack)
        {
            if (stack.IsEmpty) return null;
            foreach (var slots in EquipOrder)
            {
                if (slots.CanAutoEquip(stack)) return slots;
            }
            return null;
        }

        /// <summary>이 칸 묶음이 플레이어가 지닌 칸(가방 포함)인가.</summary>
        public bool Holds(IItemSlots slots)
        {
            if (slots == null) return false;
            foreach (var saved in SaveOrder)
            {
                if (saved.Slots == slots) return true;
            }
            return false;
        }

        /// <summary>지금 지닌 물건 전부(빈 칸 빼고). 판 결과가 쓴다.</summary>
        public List<ItemStack> CollectItems()
        {
            var items = new List<ItemStack>();
            foreach (var saved in SaveOrder)
            {
                for (int i = 0; i < saved.Slots.SlotCount; i++)
                {
                    ItemStack stack = saved.Slots.GetSlot(i);
                    if (!stack.IsEmpty) items.Add(stack);
                }
            }
            return items;
        }
    }
}
