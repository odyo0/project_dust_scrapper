using DustScrapper.Actors;
using DustScrapper.Interaction;
using DustScrapper.Items;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Player
{
    /// <summary>
    /// 플레이어 입력(의도)을 몸에 전달한다: 이동 의도는 ActorMotor로, 조준점은 BodyAim으로,
    /// 스킬 키(좌클릭·E·우클릭·Q)와 재장전은 SkillRouter로(어느 손의 어느 스킬인지는 거기서 정한다), 슬롯 바꾸기는 WeaponLoadout으로, 줍기·뒤지기 키는 PlayerInteractor로, 소모품 단축 키(4·5·6)는 ItemUser로. 살금살금 키를 누르면 이동 세기를 줄인다(발소리가 안 나는 속도).
    /// 달리기 키를 누르면 몸에 달리라고 알린다. 살금살금 키와 같이 누르면 살금살금이 이긴다(조용히 하려던 것이 무시되어 들키지 않게).
    /// 적은 같은 몸 부품을 쓰고 이 자리에 자기 AI 두뇌를 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class PlayerBrain : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] ActorMotor motor;
        [SerializeField] BodyAim bodyAim;
        [SerializeField] WeaponLoadout loadout;
        [SerializeField] SkillRouter skills;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] ItemUser itemUser;
        [SerializeField] QuickSlots quickSlots;
        [Tooltip("비워 두면 Main Camera를 쓴다.")]
        [SerializeField] Camera viewCamera;

        [Tooltip("살금살금 걸을 때의 이동 세기 (최고 속도에 곱하는 0~1). 발소리가 나지 않는 속도보다 느리게 둔다.")]
        [SerializeField, Range(0f, 1f)] float sneakIntent = 0.4f;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<ActorMotor>();
            if (bodyAim == null) bodyAim = GetComponent<BodyAim>();
            if (loadout == null) loadout = GetComponent<WeaponLoadout>();
            if (skills == null) skills = GetComponent<SkillRouter>();
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (itemUser == null) itemUser = GetComponent<ItemUser>();
            if (quickSlots == null) quickSlots = GetComponent<QuickSlots>();
            if (viewCamera == null) viewCamera = Camera.main;
        }

        void OnDisable()
        {
            // 꺼진 뒤에도 마지막 명령이 남아 혼자 걷거나 쏘지 않게 비운다.
            motor.MoveIntent = Vector2.zero;
            motor.Sprinting = false;
            if (skills != null) skills.ReleaseAll();
            if (interactor != null) interactor.SetHeld(false);
        }

        void Update()
        {
            motor.MoveIntent = input.SneakHeld ? Vector2.ClampMagnitude(input.Move, sneakIntent) : input.Move;
            motor.Sprinting = input.SprintHeld && !input.SneakHeld;

            if (viewCamera != null)
            {
                Vector2 aimWorld = viewCamera.ScreenToWorldPoint(input.AimScreenPosition);
                bodyAim.AimAt(aimWorld);
            }

            if (interactor != null) interactor.SetHeld(input.InteractHeld);

            if (loadout != null && input.SlotPressed >= 0) loadout.Equip(input.SlotPressed);
            if (itemUser != null && quickSlots != null && input.QuickPressed >= 0) itemUser.Use(quickSlots, input.QuickPressed);

            if (skills == null) return;
            if (input.ReloadPressed) skills.Reload();
            skills.SetWeaponHand(input.FireHeld, input.SpecialHeld);
            skills.SetSupportHand(input.SupportPrimaryHeld, input.SupportSpecialHeld);
        }
    }
}
