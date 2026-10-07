using System;
using DustScrapper.Actors;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 지금 바로 꺼내 쓸 수 있는 무기 슬롯. 시작할 때 슬롯마다 무기를 만들어 손에 붙여 두고, 하나만 꺼내 둔다.
    /// 두뇌(플레이어 입력이나 AI)는 이곳에 방아쇠·재장전·슬롯 바꾸기만 요청하고, 손에 든 무기가 무엇인지는 몰라도 된다.
    /// 주운 물건을 담는 가방(페이즈 4)과는 따로다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponLoadout : MonoBehaviour
    {
        [Tooltip("무기를 붙일 손 위치. 상체 아래에 두어 조준을 따라 돌게 한다.")]
        [SerializeField] Transform hand;
        [SerializeField] BodyAim aim;

        [Tooltip("슬롯마다 들 무기. 칸 수가 곧 슬롯 수다(숫자키는 1~4까지). 빈 칸은 비워 둔다.")]
        [SerializeField] WeaponData[] slots = new WeaponData[3];

        Weapon[] weapons;
        bool triggerHeld;

        public int SlotCount => slots.Length;
        public int CurrentIndex { get; private set; } = -1;
        public Weapon Current => CurrentIndex >= 0 ? weapons[CurrentIndex] : null;
        public Vector2 AimDirection => aim.AimDirection;

        /// <summary>손에 든 무기가 바뀌었을 때. 값은 새 무기.</summary>
        public event Action<Weapon> CurrentChanged;

        public Weapon GetWeapon(int index) => index >= 0 && index < weapons.Length ? weapons[index] : null;

        void Awake()
        {
            if (aim == null) aim = GetComponent<BodyAim>();

            weapons = new Weapon[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                WeaponData data = slots[i];
                if (data == null) continue;
                if (data.Prefab == null)
                {
                    Debug.LogError($"{name}: {data.name}에 무기 프리팹이 없다.", data);
                    continue;
                }

                Weapon weapon = Instantiate(data.Prefab, hand);
                weapon.name = data.name;
                weapon.Initialize(data, this);
                weapon.gameObject.SetActive(false);
                weapons[i] = weapon;
            }

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null) continue;
                Equip(i);
                break;
            }
        }

        void OnEnable()
        {
            // 죽었다 살아나는 등 꺼졌다 켜졌을 때 들고 있던 무기를 다시 꺼낸다.
            if (Current != null && !Current.gameObject.activeSelf) Current.Draw(false);
        }

        void OnDisable()
        {
            triggerHeld = false;
            if (Current == null) return;
            Current.SetTrigger(false);
            Current.Holster();
        }

        /// <summary>해당 슬롯의 무기로 바꿔 든다. 비었거나 이미 든 슬롯이면 무시한다.</summary>
        public void Equip(int index)
        {
            if (!isActiveAndEnabled && CurrentIndex >= 0) return;
            if (GetWeapon(index) == null || index == CurrentIndex) return;

            if (Current != null) Current.Holster();
            CurrentIndex = index;
            Current.Draw(triggerHeld);
            CurrentChanged?.Invoke(Current);
        }

        public void SetTrigger(bool held)
        {
            triggerHeld = held;
            if (Current != null) Current.SetTrigger(held);
        }

        public void Reload()
        {
            if (Current != null) Current.Reload();
        }
    }
}
