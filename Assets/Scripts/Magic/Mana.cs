using System;
using UnityEngine;

namespace DustScrapper.Magic
{
    /// <summary>
    /// MP. 마법을 쓰면 줄고, 쓴 뒤 잠깐 쉬었다가 저절로 다시 찬다.
    /// 체력(Health)처럼 따로 떼어 둔 부품이라, 마법을 쓰는 쪽(스킬)은 "이만큼 쓸 수 있나"만 묻는다.
    /// 화면 표시(MP 막대)는 이 값을 읽고, 모자라서 못 쓴 순간은 SpendFailed로 알려 막대가 깜빡이게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Mana : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxMana = 100f;

        [Tooltip("1초에 다시 차는 양")]
        [SerializeField, Min(0f)] float regenPerSecond = 12f;

        [Tooltip("MP를 쓴 뒤 다시 차기 시작할 때까지 쉬는 시간 (초). 0이면 쓰는 중에도 늘 같은 속도로 찬다.")]
        [SerializeField, Min(0f)] float regenDelay = 0.5f;

        float delayTimer;
        float maxBonus;
        float regenBonus;

        /// <summary>최대 MP = 기본값 + 장비 보너스.</summary>
        public float Max => maxMana + maxBonus;

        /// <summary>1초에 다시 차는 양 = 기본값 + 장비 보너스.</summary>
        public float RegenPerSecond => regenPerSecond + regenBonus;
        public float Current { get; private set; }

        /// <summary>MP가 모자라 마법을 쓰지 못했을 때.</summary>
        public event Action SpendFailed;

        void Awake()
        {
            Current = maxMana;
        }

        /// <summary>그만큼 쓸 수 있으면 쓰고 true. 모자라면 쓰지 않고 SpendFailed를 알린 뒤 false.</summary>
        public bool TrySpend(float amount)
        {
            if (Current < amount)
            {
                SpendFailed?.Invoke();
                return false;
            }

            Current -= amount;
            delayTimer = regenDelay;
            return true;
        }

        public void RestoreFull()
        {
            Current = Max;
            delayTimer = 0f;
        }

        /// <summary>
        /// 장비 보너스를 정한다(몸 장비가 바뀔 때 BodyEquipment가 부른다). 더하기 값이다.
        /// 최대치가 올라도 지금 MP는 그대로다(장비를 바꿔 끼우는 것으로 MP를 채우지 못하게). 내려가면 넘치는 만큼 잘린다.
        /// </summary>
        public void SetBonuses(float maxManaBonus, float regenPerSecondBonus)
        {
            maxBonus = maxManaBonus;
            regenBonus = regenPerSecondBonus;
            Current = Mathf.Min(Current, Max);
        }

        void Update()
        {
            if (delayTimer > 0f)
            {
                delayTimer -= Time.deltaTime;
                return;
            }
            Current = Mathf.Min(Max, Current + RegenPerSecond * Time.deltaTime);
        }
    }
}
