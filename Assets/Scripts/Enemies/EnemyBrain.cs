using System;
using DustScrapper.Actors;
using DustScrapper.Combat;
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
        /// <summary>대상(또는 팀이 마지막으로 본 위치)을 쫓고, 닿으면 공격한다.</summary>
        Chase,
        /// <summary>놓친 곳이나 확인한 곳에서 잠시 두리번거린다.</summary>
        Search,
        /// <summary>기점 주변의 아무 곳으로 걸어가 잠시 서기를 되풀이한다.</summary>
        Roam,
        /// <summary>기점으로 돌아간다.</summary>
        Return,
        /// <summary>위협에게서 멀어지는 쪽으로 달아난다.</summary>
        Flee,
    }

    /// <summary>먼저 덤비는가. 같은 두뇌라도 이 설정에 따라 구역 안의 적과 구역 밖의 몹이 갈린다.</summary>
    public enum Temperament
    {
        /// <summary>보이면 쫓아와 공격하고, 소리가 나면 확인하러 온다. 구역 안의 적.</summary>
        Aggressive,
        /// <summary>먼저 덤비지 않는다. 맞으면 그때부터 쫓아와 공격하고, 놓치면 다시 얌전해진다.</summary>
        Retaliates,
        /// <summary>먼저 덤비지 않는다. 맞거나 대상이 가까이 오면 달아난다.</summary>
        Skittish,
    }

    /// <summary>아무 일도 없을 때 무엇을 하는가.</summary>
    public enum CalmBehavior
    {
        /// <summary>제자리에서 정해진 쪽을 보고 서 있다.</summary>
        StandStill,
        /// <summary>기점 주변을 돌아다닌다.</summary>
        Wander,
    }

    /// <summary>대상을 놓친 뒤 무엇을 하는가. 같은 두뇌라도 이 설정에 따라 적의 유형이 갈린다.</summary>
    public enum LostTargetBehavior
    {
        /// <summary>잠깐 두리번거린 뒤 처음 자리로 돌아간다.</summary>
        ReturnHome,
        /// <summary>놓친 곳을 새 기점으로 삼아 그 주변을 계속 돌아다닌다.</summary>
        SettleAndRoam,
        /// <summary>놓친 곳 주변을 정해진 시간 동안 돌아다니다가 처음 자리로 돌아간다.</summary>
        RoamThenReturn,
    }

    /// <summary>
    /// 근접으로 몰려오는 적(떼거리)의 두뇌. 감각(EnemySenses)이 알려 준 것과 팀(EnemyTeam)이 공유한 위치를 보고 상태를 바꾸고,
    /// 몸 부품에 명령한다: 이동은 PathFollower가 알려 준 방향으로 ActorMotor에, 바라보기는 BodyAim에,
    /// 공격은 WeaponLoadout에. 플레이어 두뇌와 같은 자리에 끼우는 AI다.
    /// 대상을 처음 발견하면 외침(소음)을 낸다. 같은 팀이 들으면 팀이 본 위치로 달려오고, 다른 팀은 보통 소리처럼 걸어서 확인하러 온다
    /// (다른 팀까지 달려오면 팀이 줄줄이 불려 와 맵 전체가 몰려온다).
    /// 성격(Temperament)이 공격적이 아니면 보거나 들어도 덤비지 않는다: 맞으면 반격하거나(Retaliates), 달아난다(Skittish).
    /// 구역 밖의 덜 위협적인 몹을 따로 두뇌를 짜지 않고 같은 두뇌의 설정으로 만든다.
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
        [SerializeField] Health health;

        [Header("성격 (적 유형)")]
        [Tooltip("먼저 덤비는가. 구역 안의 적은 Aggressive, 구역 밖의 몹은 Retaliates나 Skittish.")]
        [SerializeField] Temperament temperament = Temperament.Aggressive;
        [Tooltip("아무 일도 없을 때 서 있는가, 기점 주변을 돌아다니는가")]
        [SerializeField] CalmBehavior calmBehavior = CalmBehavior.StandStill;

        [Header("움직임 (이동 세기 0~1, 최고 속도에 곱한다)")]
        [Tooltip("소리를 확인하러 갈 때(다른 팀의 외침 포함), 돌아다닐 때, 돌아갈 때")]
        [SerializeField, Range(0f, 1f)] float walkIntent = 0.45f;
        [Tooltip("쫓을 때, 같은 팀의 외침을 듣고 달려갈 때")]
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

        [Header("놓친 뒤 행동 (적 유형)")]
        [SerializeField] LostTargetBehavior lostTargetBehavior = LostTargetBehavior.ReturnHome;
        [Tooltip("돌아다닐 때 기점에서 벗어나는 최대 거리 (칸)")]
        [SerializeField, Min(0.5f)] float roamRadius = 4f;
        [Tooltip("RoamThenReturn: 놓친 곳 주변을 돌아다니는 시간 (초)")]
        [SerializeField, Min(0f)] float roamDuration = 15f;
        [Tooltip("돌아다니다 한 곳에 멈춰 서 있는 시간 (초, 최소~최대)")]
        [SerializeField] Vector2 roamPause = new(1f, 2.5f);

        [Header("달아나기 (Skittish)")]
        [Tooltip("대상이 보이고 이 거리(칸) 안이면 달아난다")]
        [SerializeField, Min(0f)] float fleeTriggerRadius = 3f;
        [Tooltip("한 번 달아날 때 멀어지려는 거리 (칸)")]
        [SerializeField, Min(1f)] float fleeDistance = 8f;
        [Tooltip("달아나다 이 시간(초)이 지나면 길이 막혔어도 멈춘다")]
        [SerializeField, Min(0.5f)] float fleeTimeout = 4f;

        [Header("동료 부르기")]
        [Tooltip("끄면 발견해도 외치지 않는다. 조용히 다가오는 적. 이미 경계 중인 팀원과는 여전히 위치를 공유하지만, 쉬고 있는 팀원을 깨우지는 않는다.")]
        [SerializeField] bool shoutsWhenSpotting = true;
        [Tooltip("대상을 발견했을 때 외침이 들리는 거리 (칸)")]
        [SerializeField, Min(0f)] float alarmRadius = 9f;
        [Tooltip("한 번 외친 뒤 다시 외칠 수 있을 때까지 (초)")]
        [SerializeField, Min(0f)] float alarmCooldown = 5f;

        EnemyTeam team;
        Vector2 home;
        Vector2 anchor;
        bool settled;
        Vector2 facing;
        Vector2 searchBaseFacing;
        Vector2 pointOfInterest;
        bool lostTarget;
        bool roamIsTimed;
        bool roamWalking;
        float roamPauseTimer;
        float handledSightingTime;
        float stateTimer;
        float alarmTimer;
        bool provoked;

        public EnemyState State { get; private set; }

        public Temperament Temperament => temperament;

        /// <summary>지금 대상에게 덤비는가: 원래 공격적이거나, 얌전한 몹이 맞아서 화가 난 동안.</summary>
        public bool IsHostile => temperament == Temperament.Aggressive || provoked;

        /// <summary>경계 중인가: 쫓거나, 놓쳐서 수색하거나, 놓친 곳 주변을 한동안 돌아다니는 중.</summary>
        public bool IsAlerted => State == EnemyState.Chase ||
                                 (State == EnemyState.Search && lostTarget) ||
                                 (State == EnemyState.Roam && roamIsTimed);

        /// <summary>대상을 발견해 동료를 부르는 외침을 냈을 때. 외침 소리가 듣는다.</summary>
        public event Action Alarmed;

        void Awake()
        {
            if (motor == null) motor = GetComponent<ActorMotor>();
            if (bodyAim == null) bodyAim = GetComponent<BodyAim>();
            if (loadout == null) loadout = GetComponent<WeaponLoadout>();
            if (senses == null) senses = GetComponent<EnemySenses>();
            if (pathFollower == null) pathFollower = GetComponent<PathFollower>();
            if (health == null) health = GetComponent<Health>();

            // 팀 물체 아래에 없으면 혼자 한 팀이 된다.
            team = GetComponentInParent<EnemyTeam>();
            if (team == null) team = gameObject.AddComponent<EnemyTeam>();

            home = transform.position;
            if (idleLookDirection.sqrMagnitude < 0.0001f) idleLookDirection = Vector2.down;
        }

        void OnEnable()
        {
            senses.NoiseHeard += OnNoiseHeard;
            if (health != null) health.Damaged += OnDamaged;
            // 처음 시작할 때, 죽었다 되살아날 때 모두 처음 자리에서 얌전한 상태로 시작한다.
            anchor = home;
            settled = false;
            provoked = false;
            facing = idleLookDirection.normalized;
            alarmTimer = 0f;
            EnterCalm();
        }

        void OnDisable()
        {
            senses.NoiseHeard -= OnNoiseHeard;
            if (health != null) health.Damaged -= OnDamaged;
            motor.MoveIntent = Vector2.zero;
            loadout.SetPrimary(false);
            pathFollower.Stop();
            senses.IsTracking = false;
            team.SetAlerted(this, false);
        }

        void Update()
        {
            alarmTimer -= Time.deltaTime;
            stateTimer += Time.deltaTime;

            if (senses.CanSeeTarget && IsHostile)
            {
                team.ReportSighting(senses.Target.position);
                if (State != EnemyState.Chase) StartChase();
            }
            else if (senses.CanSeeTarget)
            {
                // 얌전한 몹은 보기만 한다. 겁 많은 몹은 너무 가까이 오면 달아난다.
                if (temperament == Temperament.Skittish && State != EnemyState.Flee && IsWithin(senses.Target.position, fleeTriggerRadius))
                    StartFlee(senses.Target.position);
            }
            else if (IsHostile && IsAlerted && State != EnemyState.Chase && HasNewTeamSighting())
            {
                // 놓치고 수색하던 중에 같은 팀이 새로 발견했다 → 그곳으로 간다.
                EnterState(EnemyState.Chase);
            }

            switch (State)
            {
                case EnemyState.Idle: UpdateIdle(); break;
                case EnemyState.Investigate: UpdateInvestigate(); break;
                case EnemyState.Chase: UpdateChase(); break;
                case EnemyState.Search: UpdateSearch(); break;
                case EnemyState.Roam: UpdateRoam(); break;
                case EnemyState.Return: UpdateReturn(); break;
                case EnemyState.Flee: UpdateFlee(); break;
            }
        }

        void UpdateIdle()
        {
            Halt();
            TurnTowards(idleLookDirection);
        }

        void UpdateInvestigate()
        {
            MoveTo(pointOfInterest, walkIntent);
            if (pathFollower.HasArrived || pathFollower.IsUnreachable) BeginSearch(false);
        }

        void UpdateChase()
        {
            bool canSee = senses.CanSeeTarget;
            Vector2 goal = canSee ? (Vector2)senses.Target.position : team.LastKnownPosition;
            Vector2 toTarget = goal - (Vector2)transform.position;

            Weapon weapon = loadout.Current;
            bool busy = weapon != null && weapon.IsBusy;
            bool inRange = canSee && toTarget.sqrMagnitude <= attackRange * attackRange;
            // 근접 무기는 "새로 누를 때" 휘두르므로, 한 번 휘두르는 동안은 방아쇠를 뗐다가 끝나면 다시 누른다.
            loadout.SetPrimary(inRange && !busy);

            pathFollower.SetDestination(goal);
            // 닿을 거리에서는 밀어붙이지 않고 멈칫한다. 플레이어를 벽까지 밀고 가지 않게.
            motor.MoveIntent = pathFollower.DesiredDirection * (busy || inRange ? attackMoveIntent : runIntent);

            // 휘두르기 시작하면 방향을 고정한다. 준비 동작을 보고 옆으로 피할 수 있게.
            if (!busy) TurnTowards(canSee ? toTarget : pathFollower.DesiredDirection);

            bool teamLostTrack = !team.HasSighting;
            if (!canSee && (pathFollower.HasArrived || pathFollower.IsUnreachable || teamLostTrack)) BeginSearch(true);
        }

        void UpdateSearch()
        {
            Halt();
            float sweep = Mathf.Sin(stateTimer * 2.5f) * searchSweepAngle;
            TurnTowards(Quaternion.Euler(0f, 0f, sweep) * searchBaseFacing);
            if (stateTimer < searchDuration) return;

            if (!lostTarget)
            {
                // 소리만 확인했을 때는 유형과 상관없이 기점으로 돌아간다.
                EnterState(EnemyState.Return);
                return;
            }

            switch (lostTargetBehavior)
            {
                case LostTargetBehavior.SettleAndRoam:
                    anchor = transform.position;
                    settled = true;
                    StartRoam(false);
                    break;
                case LostTargetBehavior.RoamThenReturn:
                    anchor = transform.position;
                    StartRoam(true);
                    break;
                default:
                    EnterState(EnemyState.Return);
                    break;
            }
        }

        void UpdateRoam()
        {
            if (roamIsTimed && stateTimer >= roamDuration)
            {
                anchor = home;
                EnterState(EnemyState.Return);
                return;
            }

            if (roamWalking)
            {
                Vector2 direction = pathFollower.DesiredDirection;
                motor.MoveIntent = direction * walkIntent;
                TurnTowards(direction);
                if (!pathFollower.HasArrived && !pathFollower.IsUnreachable) return;

                roamWalking = false;
                pathFollower.Stop();
                roamPauseTimer = UnityEngine.Random.Range(roamPause.x, Mathf.Max(roamPause.x, roamPause.y));
                searchBaseFacing = facing;
                return;
            }

            // 멈춰 서서 두리번거리다가 다음 곳으로 간다.
            Halt();
            roamPauseTimer -= Time.deltaTime;
            float sweep = Mathf.Sin(stateTimer * 1.8f) * searchSweepAngle * 0.6f;
            TurnTowards(Quaternion.Euler(0f, 0f, sweep) * searchBaseFacing);
            if (roamPauseTimer > 0f) return;

            NavGrid grid = NavGrid.Active;
            Vector2 next = anchor + UnityEngine.Random.insideUnitCircle * roamRadius;
            if (grid != null && !grid.TryGetRandomWalkablePoint(anchor, roamRadius, out next)) return;
            pathFollower.SetDestination(next);
            roamWalking = true;
        }

        void UpdateReturn()
        {
            MoveTo(anchor, walkIntent);
            if (!pathFollower.HasArrived && !pathFollower.IsUnreachable) return;

            // 놓친 곳에 눌러앉은 적은 그 기점에서 다시 돌아다니고, 나머지는 평소대로(서 있거나 돌아다니거나).
            if (settled) StartRoam(false);
            else EnterCalm();
        }

        void UpdateFlee()
        {
            Vector2 direction = pathFollower.DesiredDirection;
            motor.MoveIntent = direction * runIntent;
            TurnTowards(direction);
            if (!pathFollower.HasArrived && !pathFollower.IsUnreachable && stateTimer < fleeTimeout) return;

            // 달아난 곳에서 진정한다. 원래 자리로 돌아가면 위협 쪽으로 다시 걸어가게 되므로 여기를 새 기점으로 삼는다.
            anchor = transform.position;
            EnterCalm();
        }

        /// <summary>아무 일도 없는 평소 상태로: 기점에 서 있거나 기점 주변을 돌아다닌다.</summary>
        void EnterCalm()
        {
            if (calmBehavior == CalmBehavior.Wander) StartRoam(false);
            else EnterState(EnemyState.Idle);
        }

        void StartFlee(Vector2 threat)
        {
            Vector2 position = transform.position;
            Vector2 away = position - threat;
            if (away.sqrMagnitude < 0.0001f) away = UnityEngine.Random.insideUnitCircle;
            Vector2 goal = position + away.normalized * fleeDistance;

            NavGrid grid = NavGrid.Active;
            if (grid != null && grid.TryGetRandomWalkablePoint(goal, 2f, out Vector2 walkable)) goal = walkable;
            pointOfInterest = goal;
            EnterState(EnemyState.Flee);
        }

        bool IsWithin(Vector2 point, float radius) => ((Vector2)transform.position - point).sqrMagnitude <= radius * radius;

        void StartChase()
        {
            EnterState(EnemyState.Chase);
            if (!shoutsWhenSpotting || alarmTimer > 0f) return;
            alarmTimer = alarmCooldown;
            NoiseBus.Emit(new NoiseEvent(transform.position, alarmRadius, gameObject, isAlarm: true));
            Alarmed?.Invoke();
        }

        void BeginSearch(bool afterLosingTarget)
        {
            lostTarget = afterLosingTarget;
            handledSightingTime = team.LastSightingTime;
            EnterState(EnemyState.Search);
        }

        void StartRoam(bool timed)
        {
            roamIsTimed = timed;
            roamWalking = false;
            roamPauseTimer = 0f;
            EnterState(EnemyState.Roam);
        }

        bool HasNewTeamSighting() => team.HasSighting && team.LastSightingTime > handledSightingTime;

        void OnDamaged(DamageInfo info)
        {
            if (temperament == Temperament.Aggressive || health.IsDead) return;
            Vector2 attacker = info.Source != null ? (Vector2)info.Source.transform.position : info.Point - info.Direction * fleeDistance;

            if (temperament == Temperament.Skittish)
            {
                StartFlee(attacker);
                return;
            }

            // 맞은 쪽을 알고 화가 난다. 보이지 않는 곳에서 맞았어도 때린 곳으로 쫓아간다.
            provoked = true;
            team.ReportSighting(attacker);
            if (State != EnemyState.Chase) StartChase();
        }

        void OnNoiseHeard(NoiseEvent noise)
        {
            // 얌전한 몹은 소리를 확인하러 오지 않는다.
            if (!IsHostile || State == EnemyState.Chase || State == EnemyState.Flee) return;

            // 같은 팀의 외침: 팀이 본 위치를 받아 그곳으로 달려간다.
            if (noise.IsAlarm && team.Contains(noise.Source) && team.HasSighting)
            {
                EnterState(EnemyState.Chase);
                return;
            }

            // 그 밖의 소리(다른 팀의 외침 포함): 소리 난 곳을 걸어서 확인하러 간다. 경계 중이면 무시하지 않고 그쪽도 본다.
            pointOfInterest = noise.Position;
            if (State != EnemyState.Investigate) EnterState(EnemyState.Investigate);
            else pathFollower.SetDestination(pointOfInterest);
        }

        void EnterState(EnemyState next)
        {
            State = next;
            stateTimer = 0f;
            senses.IsTracking = next == EnemyState.Chase;
            if (next != EnemyState.Chase) loadout.SetPrimary(false);
            // "놓쳐서 수색 중"인지는 BeginSearch가 정한다. 경계와 상관없는 상태로 가면 지운다.
            if (next == EnemyState.Idle || next == EnemyState.Investigate || next == EnemyState.Return) lostTarget = false;
            if (next != EnemyState.Roam) roamIsTimed = false;
            // 얌전한 몹이 쫓다 놓치고 평소 상태로 돌아가면 화가 풀린다.
            if (next == EnemyState.Idle || next == EnemyState.Return || (next == EnemyState.Roam && !roamIsTimed)) provoked = false;

            switch (next)
            {
                case EnemyState.Idle:
                    pathFollower.Stop();
                    break;
                case EnemyState.Investigate:
                case EnemyState.Flee:
                    pathFollower.SetDestination(pointOfInterest);
                    break;
                case EnemyState.Search:
                case EnemyState.Roam:
                    pathFollower.Stop();
                    searchBaseFacing = facing;
                    break;
                case EnemyState.Return:
                    pathFollower.SetDestination(anchor);
                    break;
            }
            team.SetAlerted(this, IsAlerted);
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
