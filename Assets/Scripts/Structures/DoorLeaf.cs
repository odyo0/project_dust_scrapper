using System.Collections.Generic;
using DustScrapper.Combat;
using DustScrapper.Navigation;
using UnityEngine;

namespace DustScrapper.Structures
{
    /// <summary>
    /// 물리로 움직이는 문짝 한 짝. 돌쩌귀(이 물체 위치)에 매달려(HingeJoint2D) 힘을 받은 만큼 돌고, 문짝 충돌체(자식, 벽 레이어)가
    /// 몸·총알·시야를 그 각도대로 막는다.
    /// 닫힌 자리에서 느려지면 걸쇠가 걸려 고정된다(몸으로 밀어도 안 열림, 기획). 열려 있는 동안에는 몸에 밀려 흔들린다.
    /// 문(Door)이 밀기(Push)·당기기(PullClosed)·차기(Kick)를 시킨다. 버티는 힘보다 세게 차이면 돌쩌귀가 떨어져 날아가고,
    /// 멈추면 그 자리에 낮은 엄폐물(Cover 레이어)로 남는다. 차인 뒤 잠깐과 날아가는 동안에는 부딪힌 몸이 맞아 쓰러진다.
    /// 닫힌 자리 = 처음 놓인 자리. 처음 놓인 자리에서 이 물체의 오른쪽(+x)이 문짝 방향이다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DoorLeaf : MonoBehaviour
    {
        enum Drive { None, Open, Close }

        /// <summary>차인 뒤 이 시간 동안은 부딪힌 몸이 맞는다 (초).</summary>
        const float KickHitWindow = 0.6f;
        /// <summary>밀거나 당긴 직후 이 시간 동안은 걸쇠가 안 걸린다 (초). 닫힌 자리에서 밀기 시작할 때 다시 걸리지 않게.</summary>
        const float LatchGrace = 0.25f;
        /// <summary>날아가던 문짝이 이보다 느려지면 멈춘 것으로 친다 (칸/초).</summary>
        const float SettleSpeed = 0.4f;

        static readonly List<Collider2D> Nearby = new();

        Rigidbody2D body;
        HingeJoint2D joint;
        Collider2D[] leafColliders;
        readonly List<Collider2D> ignoredWalls = new();
        readonly Dictionary<Health, float> lastHitTime = new();

        DoorData data;
        Door owner;
        GameObject kicker;
        Drive drive;
        float driveSign;
        float driveTimer;
        float latchGraceTimer;
        float dangerousUntil;
        float closedAngle;

        /// <summary>걸쇠가 걸려 닫힌 채 고정되어 있는가.</summary>
        public bool IsLatched { get; private set; }

        /// <summary>돌쩌귀가 떨어져 문에서 빠졌는가(날아가는 중이거나 바닥에 떨어짐).</summary>
        public bool IsDetached { get; private set; }

        /// <summary>닫힌 자리에서 돈 각도(도, 부호 있음). 떨어진 문짝은 0.</summary>
        public float Angle => IsDetached || joint == null ? 0f : Mathf.DeltaAngle(closedAngle, body.rotation);

        /// <summary>'열린 문'으로 칠 만큼 열렸는가. 떨어진 문짝은 늘 열림(문간이 뚫렸다).</summary>
        public bool IsOpen => IsDetached || Mathf.Abs(Angle) > data.OpenThresholdAngle;

        /// <summary>문이 처음 켜질 때 한 번 부른다. 물리 몸·돌쩌귀를 데이터대로 맞추고 닫힌 채 걸쇠를 건다.</summary>
        public void Setup(DoorData doorData, Door door, IReadOnlyList<DoorLeaf> siblings)
        {
            data = doorData;
            owner = door;
            body = GetComponent<Rigidbody2D>();
            leafColliders = GetComponentsInChildren<Collider2D>();

            body.gravityScale = 0f;
            body.mass = data.LeafMass;
            body.angularDamping = data.SwingDamping;
            body.linearDamping = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            closedAngle = body.rotation;
            joint = gameObject.AddComponent<HingeJoint2D>();
            joint.autoConfigureConnectedAnchor = true;
            joint.anchor = Vector2.zero;
            joint.connectedBody = null; // 세상에 고정된 돌쩌귀
            joint.useLimits = true;
            joint.limits = new JointAngleLimits2D { min = -data.MaxAngle, max = data.MaxAngle };
            joint.useMotor = false;

            IgnoreFrameAndSiblings(siblings);
            Latch();
        }

        /// <summary>from 반대쪽으로 문짝이 돌려면 몇 쪽(+1/-1)으로 돌아야 하는가(닫힌 자리 기준).</summary>
        public float SignAwayFrom(Vector2 from)
        {
            Vector2 along = Quaternion.Euler(0f, 0f, closedAngle) * Vector3.right;
            Vector2 swingSide = new(-along.y, along.x); // +로 돌 때 문짝이 가는 쪽
            Vector2 leafCenter = (Vector2)transform.position + along * 0.5f;
            return Vector2.Dot(swingSide, leafCenter - from) >= 0f ? 1f : -1f;
        }

        /// <summary>sign 쪽으로 밀어 연다(F, 다가오는 적). 걸쇠를 풀고 열린 각도까지 밀다가 손을 뗀다.</summary>
        public void Push(float sign)
        {
            if (IsDetached) return;
            Unlatch();
            StartDrive(Drive.Open, sign);
        }

        /// <summary>닫히는 쪽으로 당긴다(F). 닫힌 자리에 닿으면 걸쇠가 걸린다.</summary>
        public void PullClosed()
        {
            if (IsDetached || IsLatched) return;
            Unlatch();
            StartDrive(Drive.Close, -Mathf.Sign(Angle));
        }

        /// <summary>
        /// 걷어차인다. force가 버티는 힘(kickResistance) 이상이면 돌쩌귀가 떨어져 direction 쪽으로 날아가고, 아니면 sign 쪽으로 세게 돈다.
        /// 어느 쪽이든 잠깐 동안 부딪힌 몸이 맞는다.
        /// </summary>
        public void Kick(float sign, Vector2 direction, float force, GameObject source)
        {
            if (IsDetached) return;
            kicker = source;
            dangerousUntil = Time.time + KickHitWindow;
            drive = Drive.None;
            joint.useMotor = false;

            if (force >= data.KickResistance)
            {
                Detach(direction.normalized * force * data.FlySpeedPerForce, sign * data.KickSpinSpeed);
                return;
            }
            Unlatch();
            SpinAboutHinge(sign * data.KickSpinSpeed);
        }

        /// <summary>
        /// 돌쩌귀를 축으로 그 빠르기(도/초)로 돌게 한다. 회전 속도만 바꾸면 물리는 문짝 가운데를 축으로 돌리려 하고
        /// 돌쩌귀가 그걸 붙잡으며 속도가 크게 깎이므로(문짝 무게중심이 돌쩌귀에서 떨어져 있어서), 가운데의 이동 속도도 함께 맞춘다.
        /// </summary>
        void SpinAboutHinge(float degreesPerSecond)
        {
            Vector2 fromHinge = body.worldCenterOfMass - (Vector2)transform.position;
            body.angularVelocity = degreesPerSecond;
            body.linearVelocity = Vector2.Perpendicular(fromHinge) * (degreesPerSecond * Mathf.Deg2Rad);
        }

        void FixedUpdate()
        {
            if (IsDetached)
            {
                if (body.bodyType == RigidbodyType2D.Dynamic && body.linearVelocity.magnitude < SettleSpeed && Mathf.Abs(body.angularVelocity) < 30f)
                    Settle();
                return;
            }
            if (IsLatched) return;

            latchGraceTimer -= Time.fixedDeltaTime;
            if (drive != Drive.None) UpdateDrive();

            if (drive != Drive.Open && latchGraceTimer <= 0f
                && Mathf.Abs(Angle) <= data.LatchAngle && Mathf.Abs(body.angularVelocity) <= data.LatchSpeed)
                Latch();
        }

        void UpdateDrive()
        {
            driveTimer -= Time.fixedDeltaTime;
            float angle = Angle;
            // 닫을 때는 닫힌 자리를 지나쳤어도(한 물리 프레임에 걸쇠 범위보다 많이 돌 수 있다) 닫힌 것으로 친다.
            bool reached = drive == Drive.Open
                ? angle * driveSign >= data.OpenTargetAngle
                : Mathf.Abs(angle) <= data.LatchAngle || angle * driveSign >= 0f;
            if (!reached && driveTimer > 0f) return;

            bool closedNow = drive == Drive.Close && reached;
            drive = Drive.None;
            joint.useMotor = false;
            if (closedNow) Latch();
        }

        void StartDrive(Drive kind, float sign)
        {
            drive = kind;
            driveSign = sign == 0f ? 1f : sign;
            driveTimer = data.PushTime;
            latchGraceTimer = LatchGrace;
            float speed = kind == Drive.Open ? data.OpenSpeed : data.CloseSpeed;
            // 돌쩌귀 모터의 빠르기는 '세상 기준 이 문짝'이 아니라 반대 방향으로 잰다(문짝이 joint의 첫 몸이고 세상이 둘째라서). 그래서 부호를 뒤집는다.
            joint.motor = new JointMotor2D { motorSpeed = -driveSign * speed, maxMotorTorque = data.PushTorque };
            joint.useMotor = true;
        }

        void Latch()
        {
            IsLatched = true;
            drive = Drive.None;
            if (joint != null) joint.useMotor = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.rotation = closedAngle;
            transform.rotation = Quaternion.Euler(0f, 0f, closedAngle);
        }

        void Unlatch()
        {
            if (!IsLatched) return;
            IsLatched = false;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.WakeUp();
        }

        /// <summary>돌쩌귀가 떨어진다. 문에서 빠져(길찾기가 더는 문으로 보지 않음) 자유롭게 날아간다.</summary>
        void Detach(Vector2 velocity, float spin)
        {
            IsDetached = true;
            IsLatched = false;
            if (joint != null) Destroy(joint);
            joint = null;
            foreach (var wall in ignoredWalls)
            {
                if (wall == null) continue;
                foreach (var own in leafColliders) Physics2D.IgnoreCollision(own, wall, false);
            }
            ignoredWalls.Clear();

            transform.SetParent(owner.transform.parent, true);
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearDamping = 3f;
            body.angularDamping = 3f;
            body.linearVelocity = velocity;
            body.angularVelocity = spin;
            owner.NotifyLeafDetached(this);
        }

        /// <summary>날아간 문짝이 멈췄다. 그 자리에 낮은 엄폐물로 남는다(몸·총알은 막고 시야는 안 막음).</summary>
        void Settle()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Static;
            int cover = LayerMask.NameToLayer("Cover");
            Bounds area = default;
            for (int i = 0; i < leafColliders.Length; i++)
            {
                if (cover >= 0) leafColliders[i].gameObject.layer = cover;
                if (i == 0) area = leafColliders[i].bounds;
                else area.Encapsulate(leafColliders[i].bounds);
            }
            if (NavGrid.Active != null)
            {
                area.Expand(2f);
                NavGrid.Active.RebuildArea(area);
            }
            enabled = false;
        }

        void OnCollisionEnter2D(Collision2D collision) => TryHit(collision);

        void OnCollisionStay2D(Collision2D collision) => TryHit(collision);

        /// <summary>차인 뒤 잠깐, 또는 날아가는 동안 빠르게 부딪힌 몸이 맞아 쓰러진다.</summary>
        void TryHit(Collision2D collision)
        {
            bool dangerous = Time.time <= dangerousUntil || (IsDetached && body.bodyType == RigidbodyType2D.Dynamic);
            if (!dangerous) return;

            Health health = collision.collider.GetComponentInParent<Health>();
            if (health == null || health.IsDead) return;
            if (kicker != null && health.transform.IsChildOf(kicker.transform)) return;
            if (lastHitTime.TryGetValue(health, out float last) && Time.time - last < KickHitWindow) return;

            Vector2 point = collision.GetContact(0).point;
            Vector2 pointVelocity = body.GetPointVelocity(point);
            if (pointVelocity.magnitude < data.HitSpeed) return;

            lastHitTime[health] = Time.time;
            health.TakeDamage(new DamageInfo(data.HitDamage, point, pointVelocity, data.HitKnockback, kicker, data.HitEffect));
        }

        /// <summary>
        /// 문틀(문간 양옆 벽)과 같은 문의 다른 문짝과는 부딪히지 않게 한다. 벽끼리도 부딪히는 설정이라,
        /// 안 그러면 닫힌 자리에서부터 문짝이 문틀에 끼어 떨린다. 열린 각도는 돌쩌귀 한계가 막는다.
        /// </summary>
        void IgnoreFrameAndSiblings(IReadOnlyList<DoorLeaf> siblings)
        {
            // 문짝이 문틀에 닿는 곳은 돌쩌귀 쪽 끝과 닫혔을 때의 반대쪽 끝이다. 그 두 곳에 닿아 있는 고정된 것(벽)만 뺀다.
            // 문 근처의 책상·차 같은 다른 것과는 그대로 부딪힌다.
            float length = 0f;
            foreach (var own in leafColliders)
            {
                Vector2 local = transform.InverseTransformPoint(own.bounds.center);
                length = Mathf.Max(length, local.x * 2f);
            }
            Vector2 hinge = transform.position;
            Vector2 tip = hinge + (Vector2)(transform.right * length);
            IgnoreStaticAround(hinge);
            IgnoreStaticAround(tip);

            foreach (var sibling in siblings)
            {
                if (sibling == this) continue;
                foreach (var own in leafColliders)
                    foreach (var theirs in sibling.GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(own, theirs, true);
            }
        }

        void IgnoreStaticAround(Vector2 point)
        {
            const float FrameRadius = 0.35f;
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.OverlapCircle(point, FrameRadius, filter, Nearby);
            for (int i = 0; i < count; i++)
            {
                Collider2D other = Nearby[i];
                if (other.attachedRigidbody != null) continue; // 몸·다른 문짝은 아니다
                if (other.transform.IsChildOf(owner.transform)) continue;
                if (ignoredWalls.Contains(other)) continue;
                ignoredWalls.Add(other);
                foreach (var own in leafColliders) Physics2D.IgnoreCollision(own, other, true);
            }
        }
    }
}
