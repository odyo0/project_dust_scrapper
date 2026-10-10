using DustScrapper.Noise;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 장비가 가진 스킬 하나(총 쏘기, 휘두르기, 마법 탄 등). 장비 프리팹에 부품으로 붙고, 장비(Weapon)가 기본 스킬·특수 스킬 칸에 끼운다.
    /// 장비는 "키를 누르고 있는가"만 넘기고, 언제 무엇을 할지는 스킬이 정한다.
    /// 키가 "새로 눌렸는가"는 여기서 한 번만 계산해서, 꺼내는 중에 누른 클릭은 다 꺼낸 첫 프레임에 넘겨준다.
    /// </summary>
    public abstract class Skill : MonoBehaviour
    {
        bool held;
        bool wasHeld;
        bool pressedWhileDrawing;

        /// <summary>이 스킬을 가진 장비.</summary>
        protected Weapon Weapon { get; private set; }

        /// <summary>장비를 든 쪽. 조준 방향·자기 몸·탄·MP를 여기서 얻는다.</summary>
        protected IWeaponHolder Holder => Weapon.Holder;

        /// <summary>지금 동작(재장전, 휘두르기, 시전 등)을 하느라 바쁜가.</summary>
        public virtual bool IsBusy => false;

        /// <summary>마법을 시전하는 중인가. 시전 중에는 반대 손이 공격하지 못한다(SkillRouter가 막는다).</summary>
        public virtual bool IsCasting => false;

        /// <summary>장비가 만들어질 때 한 번 부른다.</summary>
        public void Bind(Weapon weapon)
        {
            Weapon = weapon;
            OnBind();
        }

        public void SetHeld(bool value) => held = value;

        /// <summary>꺼낼 때. 이미 누르고 있던 키는 한 번 뗐다 눌러야 "새로 눌림"이 된다(단발이 저절로 나가지 않게).</summary>
        public void ResetInput(bool alreadyHeld)
        {
            held = alreadyHeld;
            wasHeld = alreadyHeld;
            pressedWhileDrawing = false;
        }

        /// <summary>집어넣거나 끊길 때. 하던 동작은 취소된다. 누르고 있던 키는 한 번 뗐다 눌러야 다시 나간다.</summary>
        public void Cancel()
        {
            ResetInput(held);
            OnCancel();
        }

        /// <summary>
        /// 장비가 매 프레임 부른다.
        /// drawn: 다 꺼냈는가(꺼내는 중이면 누른 클릭만 기억해 둔다).
        /// blocked: 반대 손이 시전 중이라 공격을 막았는가(누름은 버리고, 재장전 같은 진행 중인 일은 계속한다).
        /// </summary>
        public void Step(bool drawn, bool blocked, float deltaTime)
        {
            bool pressed = held && !wasHeld;
            wasHeld = held;

            if (!drawn)
            {
                pressedWhileDrawing |= pressed;
                return;
            }

            pressed |= pressedWhileDrawing;
            pressedWhileDrawing = false;
            if (blocked) Tick(false, false, deltaTime);
            else Tick(pressed, held, deltaTime);
        }

        /// <summary>재장전(R). 재장전이 없는 스킬은 아무것도 하지 않는다.</summary>
        public virtual void Reload() { }

        /// <summary>매 프레임 스킬 동작. 꺼내는 중에는 불리지 않는다.</summary>
        protected abstract void Tick(bool pressed, bool held, float deltaTime);

        /// <summary>스킬을 쓸 때 나는 소리를 퍼뜨린다(적이 듣는 소음).</summary>
        protected void EmitNoise(float radius)
        {
            NoiseBus.Emit(new NoiseEvent(Holder.transform.position, radius, Holder.gameObject));
        }

        protected virtual void OnBind() { }
        protected virtual void OnCancel() { }
    }
}
