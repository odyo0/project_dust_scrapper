using System;
using DustScrapper.Actors;
using DustScrapper.Navigation;
using DustScrapper.Noise;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Enemies
{
    public enum EnemyState
    {
        /// <summary>제자리에서 정해진 쪽을 보고 있다.</summary>
        Idle,
        /// <summary>소리가 난 곳을 확인하러 간다.</summary>
        Investigate,
        /// <summary>대상을 쫓고, 닿으면 공격한다.</summary>
        Chase,
        /// <summary>대상을 놓친 곳에서 잠시 두리번거린다.</summary>
        Search,
        /// <summary>처음 자리로 돌아간다.</summary>
        Return,
    }

    /// <summary>
    /// 근접으로 몰려오는 적(떼거리)의 두뇌. 감각(EnemySenses)이 알려 준 것을 보고 상태를 바꾸고,
    /// 몸 부품에 명령한다: 이동은 PathFollower가 알려 준 방향으로 ActorMotor에, 바라보기는 BodyAim에,
    /// 공격은 WeaponLoadout에. 플레이어 두뇌와 같은 자리에 끼우는 AI다.
    /// 대상을 처음 발견하면 외침(소리)을 내서 주변 적을 부른다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] ActorMotor motor;
        [SerializeField] BodyAim bodyAim;
        [SerializeField] WeaponLoadout loadout;
        [SerializeField] EnemySenses senses;
        [SerializeField] PathFollower pathFollower;

        [Header("움직임 (이동 세기 0~1, 최고 속도에 곱한다)")]
        [Tooltip("소리를 확인하러 갈 때")]
        [SerializeField, Range(0f, 1f)] float walkIntent = 0.45f;
        [Tooltip("쫓을 때, 동료의 외침을 듣고 달려갈 때")]
        [SerializeField, Range(0f, 1f)] float runIntent = 1f;
        [Tooltip("공격하는 동안. 낮을수록 휘두를 때 멈칫해서 피할 틈이 생긴다.")]
        [SerializeField, Range(0f, 1f)] float attackMoveIntent = 0.15f;
        [Tooltip("몸을 돌리는 빠르기 (도/초)")]
        [SerializeField, Min(0f)] float turnSpeed = 360f;
        [Tooltip("처음 자리에서 바라보는 방향")]
        [SerializeField] Vector2 idleLookDirection = Vector2.down;

        [Header("공격")]
        [Tooltip("대상이 이 거리(칸) 안에 보이면 공격한다")]
        [SerializeField, Min(0f)] float attackRange = 1.1f;

        [Header("수색")]
        [Tooltip("놓친 곳에서 두리번거리는 시간 (초)")]
        [SerializeField, Min(0f)] float searchDuration = 3f;
        [Tooltip("두리번거릴 때 좌우로 고개를 돌리는 폭 (도)")]
        [SerializeField, Range(0f, 180f)] float searchSweepAngle = 70f;

        [Header("동료 부르기")]
        [Tooltip("대상을 발견했을 때 외침이 들리는 거리 (칸)")]
        [SerializeField, Min(0f)] float alarmRadius = 9f;
        [Tooltip("한 번 외친 뒤 다시 외칠 수 있을 때까지 (초)")]
        [SerializeField, Min(0f)] float alarmCooldown = 5f;

        Vector2 home;
        Vector2 facing;
        Vector2 searchBaseFacing;
        Vector2 pointOfInterest;
        bool investigateUrgently;
        float stateTimer;
        float alarmTimer;

        public EnemyState State { get; private set; }

        /// <summary>대상을 발견해 동료를 부르는 외침을 냈을 때. 외침 소리가 듣는다.</summary>
        public event Action Alarmed;

        void Awake()
        {
            if (motor == null) motor = GetComponent<ActorMotor>();
            if (bodyAim == null) bodyAim = GetComponent<BodyAim>();
            if (loadout == null) loadout = GetComponent<WeaponLoadout>();
            if (senses == null) senses = GetComponent<EnemySenses>();
            if (pathFollower == null) pathFollower = GetComponent<PathFollower>();

            home = transform.position;
            if (idleLookDirection.sqrMagnitude < 0.0001f) idleLookDirection = Vector2.down;
        }

        void OnEnable()
        {
            senses.NoiseHeard += OnNoiseHeard;
            // 처음 시작할 때, 죽었다 되살아날 때 모두 처음 자리를 보고 서 있는 상태에서 시작한다.
            facing = idleLookDirection.normalized;
            alarmTimer = 0f;
            EnterState(EnemyState.Idle);
        }

        void OnDisable()
        {
            senses.NoiseHeard -= OnNoiseHeard;
            motor.MoveIntent = Vector2.zero;
            loadout.SetTrigger(false);
            pathFollower.Stop();
            senses.IsTracking = false;
        }

        void Update()
        {
            alarmTimer -= Time.deltaTime;
            stateTimer += Time.deltaTime;

            if (senses.CanSeeTarget && State != EnemyState.Chase) StartChase();

            switch (State)
            {
                case EnemyState.Idle: UpdateIdle(); break;
                case EnemyState.Investigate: UpdateInvestigate(); break;
                case EnemyState.Chase: UpdateChase(); break;
                case EnemyState.Search: UpdateSearch(); break;
                case EnemyState.Return: UpdateReturn(); break;
            }
        }

        void UpdateIdle()
        {
            Halt();
            TurnTowards(idleLookDirection);
        }

        void UpdateInvestigate()
        {
            MoveTo(pointOfInterest, investigateUrgently ? runIntent : walkIntent);
            if (pathFollower.HasArrived || pathFollower.IsUnreachable) EnterState(EnemyState.Search);
        }

        void UpdateChase()
        {
            bool canSee = senses.CanSeeTarget;
            Vector2 goal = canSee ? (Vector2)senses.Target.position : senses.LastSeenPosition;
            Vector2 toTarget = goal - (Vector2)transform.position;

            Weapon weapon = loadout.Current;
            bool busy = weapon != null && weapon.IsBusy;
            bool inRange = canSee && toTarget.sqrMagnitude <= attackRange * attackRange;
            // 근접 무기는 "새로 누를 때" 휘두르므로, 한 번 휘두르는 동안은 방아쇠를 뗐다가 끝나면 다시 누른다.
            loadout.SetTrigger(inRange && !busy);

            pathFollower.SetDestination(goal);
            // 닿을 거리에서는 밀어붙이지 않고 멈칫한다. 플레이어를 벽까지 밀고 가지 않게.
            motor.MoveIntent = pathFollower.DesiredDirection * (busy || inRange ? attackMoveIntent : runIntent);

            // 휘두르기 시작하면 방향을 고정한다. 준비 동작을 보고 옆으로 피할 수 있게.
            if (!busy) TurnTowards(canSee ? toTarget : pathFollower.DesiredDirection);

            if (!canSee && (pathFollower.HasArrived || pathFollower.IsUnreachable)) EnterState(EnemyState.Search);
        }

        void UpdateSearch()
        {
            Halt();
            float sweep = Mathf.Sin(stateTimer * 2.5f) * searchSweepAngle;
            TurnTowards(Quaternion.Euler(0f, 0f, sweep) * searchBaseFacing);
            if (stateTimer >= searchDuration) EnterState(EnemyState.Return);
        }

        void UpdateReturn()
        {
            MoveTo(home, walkIntent);
            if (pathFollower.HasArrived || pathFollower.IsUnreachable) EnterState(EnemyState.Idle);
        }

        void StartChase()
        {
            EnterState(EnemyState.Chase);
            if (alarmTimer > 0f) return;
            alarmTimer = alarmCooldown;
            NoiseBus.Emit(new NoiseEvent(transform.position, alarmRadius, gameObject, isAlarm: true));
            Alarmed?.Invoke();
        }

        void OnNoiseHeard(NoiseEvent noise)
        {
            if (State == EnemyState.Chase) return;
            pointOfInterest = noise.Position;
            // 이미 달려가는 중이면 뒤이은 작은 소리 때문에 걷기로 바뀌지 않게 한다.
            investigateUrgently = noise.IsAlarm || (State == EnemyState.Investigate && investigateUrgently);
            if (State != EnemyState.Investigate) EnterState(EnemyState.Investigate);
            else pathFollower.SetDestination(pointOfInterest);
        }

        void EnterState(EnemyState next)
        {
            State = next;
            stateTimer = 0f;
            senses.IsTracking = next == EnemyState.Chase;
            if (next != EnemyState.Chase) loadout.SetTrigger(false);

            switch (next)
            {
                case EnemyState.Idle:
                    pathFollower.Stop();
                    break;
                case EnemyState.Investigate:
                    pathFollower.SetDestination(pointOfInterest);
                    break;
                case EnemyState.Search:
                    pathFollower.Stop();
                    searchBaseFacing = facing;
                    break;
                case EnemyState.Return:
                    pathFollower.SetDestination(home);
                    break;
            }
        }

        void MoveTo(Vector2 point, float intent)
        {
            pathFollower.SetDestination(point);
            Vector2 direction = pathFollower.DesiredDirection;
            motor.MoveIntent = direction * intent;
            TurnTowards(direction);
        }

        void Halt()
        {
            motor.MoveIntent = Vector2.zero;
        }

        /// <summary>바라보는 방향을 정해진 빠르기로 돌린다. 감각의 시야 부채꼴도 이 방향을 따른다.</summary>
        void TurnTowards(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                float current = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
                float wanted = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float next = Mathf.MoveTowardsAngle(current, wanted, turnSpeed * Time.deltaTime) * Mathf.Deg2Rad;
                facing = new Vector2(Mathf.Cos(next), Mathf.Sin(next));
            }
            bodyAim.AimAt((Vector2)transform.position + facing);
        }
    }
}
