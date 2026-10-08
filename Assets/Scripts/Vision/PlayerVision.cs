using DustScrapper.Actors;
using UnityEngine;

namespace DustScrapper.Vision
{
    /// <summary>
    /// 플레이어가 지금 무엇을 볼 수 있는지 판정한다. 시야 = 내 주변 원 + 조준 방향 부채꼴(VisionProfile),
    /// 벽(시야를 가리는 레이어) 뒤는 못 본다. 그리지는 않는다.
    /// 어둠 그리기(VisionShade), 안 보이는 적 숨기기(HideOutsideVision)가 모두 이 판정 하나를 따른다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVision : MonoBehaviour
    {
        [Tooltip("바라보는 방향. 시야 부채꼴의 기준이다.")]
        [SerializeField] BodyAim bodyAim;
        [SerializeField] VisionProfile profile;

        [Tooltip("시야를 가리는 레이어(벽)")]
        [SerializeField] LayerMask sightBlockers;

        /// <summary>지금 플레이어의 시야. 적은 프리팹이라 씬 물체를 연결할 수 없어 이것으로 찾는다.</summary>
        public static PlayerVision Active { get; private set; }

        public Vector2 Origin => transform.position;
        public Vector2 Facing => bodyAim.AimDirection;
        public VisionProfile Profile => profile;
        public LayerMask SightBlockers => sightBlockers;

        /// <summary>시야 넓히는 장비 등으로 시야 모양이 바뀔 때 부른다.</summary>
        public void SetProfile(VisionProfile newProfile)
        {
            profile = newProfile;
        }

        void Awake()
        {
            if (bodyAim == null) bodyAim = GetComponent<BodyAim>();
        }

        void OnEnable() => Active = this;

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        /// <summary>direction 쪽으로 (벽이 없다면) 보이는 거리.</summary>
        public float SightRadius(Vector2 direction)
        {
            if (profile == null) return 0f;
            return profile.RadiusAt(Vector2.Angle(Facing, direction));
        }

        /// <summary>그 지점이 지금 보이는가. 거리·방향과 벽을 모두 본다.</summary>
        public bool CanSee(Vector2 point)
        {
            Vector2 toPoint = point - Origin;
            float distance = toPoint.magnitude;
            if (distance > SightRadius(toPoint)) return false;
            return Physics2D.Linecast(Origin, point, sightBlockers).collider == null;
        }
    }
}
