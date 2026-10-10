using System;
using DustScrapper.Actors;
using DustScrapper.Items;
using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 무기 손(왼손잡이 기준 왼손)의 무기 슬롯. 슬롯마다 무기를 만들어 손에 붙여 두고, 하나만 꺼내 둔다.
    /// 든 무기의 기본 스킬은 좌클릭, 특수 스킬은 Space다(플레이어는 SkillRouter를 거쳐 받는다).
    /// 아무 슬롯도 들지 않은 상태가 "빈손"이다. 빈손일 때 쓸 장비(주먹: 주먹질만, Space는 없음)를 따로 하나 두면 그것을 꺼내 든다.
    /// 빈손 장비는 슬롯에 들어가지 않아서 가방 화면에 보이지 않고 떨어뜨릴 수도 없다.
    /// 판 도중에 주운 무기를 넣거나(PutWeapon) 뺄 수 있고, 가방 화면에서는 가방과 같은 칸 창구(IItemSlots)로 옮겨진다.
    /// 두뇌(플레이어 입력이나 AI)는 이곳에 기본·특수 스킬 키·재장전·슬롯 바꾸기만 요청하고, 손에 든 무기가 무엇인지는 몰라도 된다.
    /// 주운 물건을 담는 가방(Inventory)과는 따로이고, 총이 재장전할 탄만 가방에서 꺼낸다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponLoadout : MonoBehaviour, IItemSlots, IWeaponHolder
    {
        [Tooltip("무기를 붙일 손 위치. 상체 아래에 두어 조준을 따라 돌게 한다. 손잡이는 이 위치와 보조 손 위치를 맞바꿔 정한다.")]
        [SerializeField] Transform hand;
        [SerializeField] BodyAim aim;

        [Tooltip("시작할 때 슬롯마다 들 무기. 칸 수가 곧 슬롯 수다(숫자키는 1~3까지). 빈 칸은 비워 둔다. 시작 무기는 탄창이 가득 찬 채로 나온다.")]
        [SerializeField] WeaponData[] slots = new WeaponData[3];

        [Tooltip("빈손일 때 드는 장비(좌클릭 주먹질, Space 없음). 슬롯에 들어가지 않는다. 비워 두면 빈손일 때 아무것도 들지 않는다(공격 못 함).")]
        [SerializeField] WeaponData unarmed;

        [Tooltip("총이 재장전할 때 탄을 꺼낼 곳(가방). 비워 두면 탄창에 든 탄만 쓴다.")]
        [SerializeField] Inventory ammoSource;

        [Tooltip("마법 장비(마법서 등)가 쓸 MP. 비워 두면 같은 물체에서 찾고, 없으면 마법을 못 쓴다.")]
        [SerializeField] Mana mana;

        Weapon[] weapons;
        Weapon unarmedWeapon;
        bool primaryHeld;
        bool specialHeld;

        public int SlotCount => slots.Length;

        /// <summary>손에 든 슬롯 번호. 빈손이면 -1.</summary>
        public int CurrentIndex { get; private set; } = -1;

        /// <summary>지금 꺼내 든 무기. 빈손이면 빈손 장비(없으면 null).</summary>
        public Weapon Current { get; private set; }
        public Vector2 AimDirection => aim.AimDirection;
        public Inventory AmmoSource => ammoSource;
        public Mana Mana => mana;

        /// <summary>손에 든 무기가 바뀌었을 때. 값은 새 무기(빈손이면 빈손 장비, 그것도 없으면 null).</summary>
        public event Action<Weapon> CurrentChanged;

        /// <summary>슬롯에 무기가 들어오거나 빠졌을 때.</summary>
        public event Action Changed;

        public Weapon GetWeapon(int index) => index >= 0 && index < weapons.Length ? weapons[index] : null;

        void Awake()
        {
            if (aim == null) aim = GetComponent<BodyAim>();
            if (mana == null) mana = GetComponent<Mana>();

            weapons = new Weapon[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) weapons[i] = Weapon.Spawn(slots[i], hand, this);
            }
            if (unarmed != null) unarmedWeapon = Weapon.Spawn(unarmed, hand, this);
            EquipFirstAvailable();
        }

        void OnEnable()
        {
            // 죽었다 살아나는 등 꺼졌다 켜졌을 때 들고 있던 무기를 다시 꺼낸다.
            if (Current != null && !Current.gameObject.activeSelf) Current.Draw(false, false);
        }

        void OnDisable()
        {
            primaryHeld = false;
            specialHeld = false;
            if (Current == null) return;
            Current.Holster();
        }

        /// <summary>
        /// 해당 슬롯을 든다. 빈 슬롯이면 들고 있던 무기를 집어넣고 빈손이 된다.
        /// 이미 든 슬롯이거나 슬롯 번호가 범위 밖이면 무시한다.
        /// </summary>
        public void Equip(int index)
        {
            if (!isActiveAndEnabled && Current != null) return;
            if (index < 0 || index >= weapons.Length) return;
            if (weapons[index] == null) index = -1;
            if (index == CurrentIndex) return;
            Hold(index);
        }

        /// <summary>슬롯(-1이면 빈손)으로 바꿔 든다. 하던 동작은 집어넣으면서 취소된다.</summary>
        void Hold(int index)
        {
            if (Current != null) Current.Holster();
            CurrentIndex = index;
            Current = index >= 0 ? weapons[index] : unarmedWeapon;
            if (Current != null) Current.Draw(primaryHeld, specialHeld);
            CurrentChanged?.Invoke(Current);
        }

        /// <summary>기본 스킬 키(플레이어는 좌클릭)를 누르고 있는가.</summary>
        public void SetPrimary(bool held)
        {
            primaryHeld = held;
            if (Current != null) Current.SetPrimary(held);
        }

        /// <summary>특수 스킬 키(플레이어는 Space)를 누르고 있는가.</summary>
        public void SetSpecial(bool held)
        {
            specialHeld = held;
            if (Current != null) Current.SetSpecial(held);
        }

        public void Reload()
        {
            if (Current != null) Current.Reload();
        }

        /// <summary>
        /// 무기를 슬롯에 넣고 바로 꺼내 든다. 빈 슬롯이 있으면 거기에 넣는다.
        /// 슬롯이 다 찼으면 손에 든 무기와 바꾸고, 빠진 무기를 돌려준다(바닥에 떨어뜨리는 건 부른 쪽 몫이다).
        /// </summary>
        public ItemStack PutWeapon(ItemStack stack)
        {
            if (!(stack.Item is WeaponData data))
            {
                Debug.LogError($"{name}: 무기가 아닌 물건은 슬롯에 넣을 수 없다.", this);
                return stack;
            }

            int index = Array.IndexOf(weapons, null);
            ItemStack replaced = ItemStack.Empty;
            if (index < 0)
            {
                index = CurrentIndex;
                replaced = RemoveWeapon(index);
            }
            if (index < 0) return stack;

            if (!Install(index, data, stack.LoadedAmmo)) return stack;
            Equip(index);
            Changed?.Invoke();
            return replaced;
        }

        /// <summary>그 슬롯의 무기를 물건 묶음으로 본다(총은 지금 탄창 탄 수를 함께). 빈 슬롯이면 빈 묶음.</summary>
        public ItemStack GetSlot(int index)
        {
            Weapon weapon = GetWeapon(index);
            if (weapon == null) return ItemStack.Empty;
            return new ItemStack(weapon.Data, 1, LoadedAmmoOf(weapon));
        }

        /// <summary>무기 슬롯에는 무기만 놓인다.</summary>
        public bool CanPlace(int index, ItemStack stack) => stack.Item is WeaponData;

        /// <summary>슬롯의 무기를 빼서 물건 묶음으로 돌려준다. 손에 든 무기였으면 남은 무기 중 첫 번째를(없으면 빈손을) 든다.</summary>
        public ItemStack TakeSlot(int index)
        {
            ItemStack stack = RemoveWeapon(index);
            if (stack.IsEmpty) return stack;
            // 일부러 빈손으로 있던 중이면 그대로 둔다. 손에 든 무기가 빠졌을 때만 새로 든다.
            if (Current == null) EquipFirstAvailable();
            Changed?.Invoke();
            return stack;
        }

        /// <summary>빈 슬롯에 무기를 넣는다. 빈손이었으면 바로 든다.</summary>
        public void PlaceInEmptySlot(int index, ItemStack stack)
        {
            if (!(stack.Item is WeaponData data) || GetWeapon(index) != null) return;
            if (!Install(index, data, stack.LoadedAmmo)) return;
            if (CurrentIndex < 0) Equip(index);
            Changed?.Invoke();
        }

        bool Install(int index, WeaponData data, int loadedAmmo)
        {
            Weapon weapon = Weapon.Spawn(data, hand, this);
            if (weapon == null) return false;
            GunFire gun = weapon.FindSkill<GunFire>();
            if (gun != null) gun.SetMagazine(loadedAmmo);
            weapons[index] = weapon;
            return true;
        }

        /// <summary>슬롯을 비운다. 손에 든 무기였으면 빈손 장비도 꺼내지 않은 상태가 된다(다음에 무엇을 들지는 부른 쪽이 정한다).</summary>
        ItemStack RemoveWeapon(int index)
        {
            Weapon weapon = GetWeapon(index);
            if (weapon == null) return ItemStack.Empty;

            var stack = new ItemStack(weapon.Data, 1, LoadedAmmoOf(weapon));

            if (index == CurrentIndex)
            {
                weapon.Holster();
                CurrentIndex = -1;
                Current = null;
            }
            weapons[index] = null;
            Destroy(weapon.gameObject);
            return stack;
        }

        /// <summary>첫 번째로 무기가 있는 슬롯을 든다. 하나도 없으면 빈손.</summary>
        void EquipFirstAvailable()
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null) continue;
                Hold(i);
                return;
            }
            Hold(-1);
        }

        /// <summary>총이면 지금 탄창 탄 수, 아니면 0. 무기를 물건 묶음으로 바꿀 때 탄창을 함께 넘긴다.</summary>
        static int LoadedAmmoOf(Weapon weapon)
        {
            GunFire gun = weapon.FindSkill<GunFire>();
            return gun != null ? gun.AmmoInMagazine : 0;
        }
    }
}
