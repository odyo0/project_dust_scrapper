using DustScrapper.Actors;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Player
{
    /// <summary>
    /// 플레이어 입력(의도)을 몸에 전달한다: 이동 의도는 ActorMotor로, 조준점은 BodyAim으로,
    /// 발사·재장전·슬롯 바꾸기는 WeaponLoadout으로.
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
        [Tooltip("비워 두면 Main Camera를 쓴다.")]
        [SerializeField] Camera viewCamera;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<ActorMotor>();
            if (bodyAim == null) bodyAim = GetComponent<BodyAim>();
            if (loadout == null) loadout = GetComponent<WeaponLoadout>();
            if (viewCamera == null) viewCamera = Camera.main;
        }

        void OnDisable()
        {
            // 꺼진 뒤에도 마지막 명령이 남아 혼자 걷거나 쏘지 않게 비운다.
            motor.MoveIntent = Vector2.zero;
            if (loadout != null) loadout.SetTrigger(false);
        }

        void Update()
        {
            motor.MoveIntent = input.Move;

            if (viewCamera != null)
            {
                Vector2 aimWorld = viewCamera.ScreenToWorldPoint(input.AimScreenPosition);
                bodyAim.AimAt(aimWorld);
            }

            if (loadout == null) return;
            if (input.SlotPressed >= 0) loadout.Equip(input.SlotPressed);
            if (input.ReloadPressed) loadout.Reload();
            loadout.SetTrigger(input.FireHeld);
        }
    }
}
