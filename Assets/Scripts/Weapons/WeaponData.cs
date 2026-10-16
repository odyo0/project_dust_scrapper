using DustScrapper.Items;
using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>어느 칸에 끼울 수 있는가. 무기 칸 = 무기 손(좌클릭·E), 보조 칸 = 보조 손(우클릭·Q).</summary>
    public enum HandSlotRule
    {
        /// <summary>무기 칸에만 (총, 칼, 마법서, 오른손 글러브 등)</summary>
        WeaponSlot,
        /// <summary>보조 칸에만 (왼손 글러브 등)</summary>
        SupportSlot,
        /// <summary>어느 칸이든 (스태프 등)</summary>
        Either,
    }

    /// <summary>무기를 쥐는 방식. 나중에 상체·팔 그림 레이어를 고를 때 쓴다(그림 수 = 쥐는 방식 × 동작).</summary>
    public enum GripType
    {
        OneHanded,
        TwoHanded,
    }

    /// <summary>
    /// 손에 드는 모든 장비가 공통으로 갖는 수치. 총은 GunData, 근접 무기는 MeleeData가 이것을 이어받아 자기 수치를 더한다.
    /// 수치가 모두 스킬 쪽에 있는 장비(빈 보조 손, 마법 글러브·스태프 등)는 이것을 그대로 쓴다.
    /// 장비도 주워서 들고 다니는 물건이라 ItemData를 이어받는다(이름·가치는 거기에 있다). 장비는 겹쳐 담지 않는다.
    /// 장비를 늘릴 때는 코드가 아니라 이 데이터 파일과 프리팹(스킬 부품 조합)을 새로 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Weapons/Hand Item", fileName = "HandItem")]
    public class WeaponData : ItemData
    {
        [Tooltip("손에 들 때 만들 장비 프리팹(Weapon). 프리팹의 스킬 부품이 이 데이터 종류와 맞아야 한다(총 쏘기 = GunData 등).")]
        [SerializeField] Weapon prefab;

        [Tooltip("두손 무기를 무기 칸에 들면, '두손 무기와 같이 못 씀' 장비를 낀 보조 칸은 빈손으로 친다.")]
        [SerializeField] GripType grip = GripType.OneHanded;

        [Header("칸·속성")]
        [Tooltip("어느 칸에 끼울 수 있는가")]
        [SerializeField] HandSlotRule slotRule = HandSlotRule.WeaponSlot;
        [Tooltip("보조 칸에 있을 때 무기 칸에 두손 무기를 들면 쓰지 못하고 보조 손을 빈손으로 친다 (스태프)")]
        [SerializeField] bool conflictsWithTwoHanded;
        [Tooltip("이 장비의 속성. 비워 두면 무속성. 반대 손 장비의 마법 이펙트를 바꾼다.")]
        [SerializeField] ElementData element;

        [Tooltip("한 번 맞힐 때 피해 (총 쏘기·근접 공격 스킬이 쓴다)")]
        [SerializeField, Min(0f)] float damage = 10f;

        [Tooltip("맞은 대상을 밀어내는 힘")]
        [SerializeField, Min(0f)] float knockback = 1f;

        [Tooltip("꺼낸 뒤 쓸 수 있게 되기까지 걸리는 시간 (초)")]
        [SerializeField, Min(0f)] float drawTime = 0.25f;

        [Tooltip("쓸 때(쏘거나 휘두를 때) 나는 소리가 적에게 들리는 거리 (칸). 0이면 소리가 나지 않는다. 총 쏘기·근접 공격 스킬이 쓴다.")]
        [SerializeField, Min(0f)] float noiseRadius;

        public override int MaxStack => 1;
        public Weapon Prefab => prefab;
        public GripType Grip => grip;
        public bool FitsWeaponSlot => slotRule != HandSlotRule.SupportSlot;
        public bool FitsSupportSlot => slotRule != HandSlotRule.WeaponSlot;
        public bool ConflictsWithTwoHanded => conflictsWithTwoHanded;
        public ElementData Element => element;
        public float Damage => damage;
        public float Knockback => knockback;
        public float DrawTime => drawTime;
        public float NoiseRadius => noiseRadius;
    }
}
