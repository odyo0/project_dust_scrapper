using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 손에 드는 모든 무기의 공통 틀. 주인(WeaponLoadout)은 방아쇠를 당기고 있는지만 알려 주고,
    /// 쏠지 휘두를지는 무기 종류(Gun, MeleeWeapon)가 정한다.
    /// 그래서 플레이어 두뇌나 적 AI는 손에 든 게 총인지 칼인지 몰라도 된다.
    /// </summary>
    public abstract class Weapon : MonoBehaviour
    {
        float drawTimer;
        bool triggerHeld;
        bool triggerWasHeld;
        bool pressedWhileDrawing;

        public WeaponData Data { get; private set; }

        /// <summary>이 무기를 든 쪽. 조준 방향과 "자기 자신"(자기 탄에 안 맞게)을 여기서 얻는다.</summary>
        protected WeaponLoadout Owner { get; private set; }

        /// <summary>꺼내는 중이 끝나 쓸 수 있는 상태인가.</summary>
        public bool IsDrawn => drawTimer <= 0f;

        /// <summary>WeaponLoadout이 무기를 만든 직후 한 번 부른다.</summary>
        public void Initialize(WeaponData data, WeaponLoadout owner)
        {
            Data = data;
            Owner = owner;
            OnInitialize();
        }

        /// <summary>꺼낸다. 이미 방아쇠를 누르고 있었다면 한 번 뗐다 눌러야 단발이 나간다.</summary>
        public void Draw(bool triggerAlreadyHeld)
        {
            gameObject.SetActive(true);
            drawTimer = Data.DrawTime;
            triggerHeld = triggerAlreadyHeld;
            triggerWasHeld = triggerAlreadyHeld;
            pressedWhileDrawing = false;
            OnDrawn();
        }

        /// <summary>집어넣는다. 하던 동작(재장전, 휘두르기)은 취소된다.</summary>
        public void Holster()
        {
            triggerHeld = false;
            triggerWasHeld = false;
            pressedWhileDrawing = false;
            OnHolstered();
            gameObject.SetActive(false);
        }

        public void SetTrigger(bool held) => triggerHeld = held;

        /// <summary>재장전. 재장전이 없는 무기는 아무것도 하지 않는다.</summary>
        public virtual void Reload() { }

        void Update()
        {
            bool pressed = triggerHeld && !triggerWasHeld;
            triggerWasHeld = triggerHeld;

            if (drawTimer > 0f)
            {
                // 꺼내는 중에 누른 클릭은 버리지 않고, 다 꺼낸 첫 프레임에 넘겨준다.
                drawTimer -= Time.deltaTime;
                pressedWhileDrawing |= pressed;
                return;
            }

            pressed |= pressedWhileDrawing;
            pressedWhileDrawing = false;
            Tick(pressed, triggerHeld, Time.deltaTime);
        }

        /// <summary>매 프레임 무기 동작. 꺼내는 중에는 불리지 않는다.</summary>
        protected abstract void Tick(bool triggerPressed, bool triggerHeld, float deltaTime);

        protected virtual void OnInitialize() { }
        protected virtual void OnDrawn() { }
        protected virtual void OnHolstered() { }
    }
}
