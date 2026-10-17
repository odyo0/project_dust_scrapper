using System.Collections.Generic;
using DustScrapper.Combat;
using DustScrapper.Navigation;
using UnityEngine;

namespace DustScrapper.Structures
{
    /// <summary>
    /// 문짝 한 짝. 각도는 스크립트가 정한다: 닫힘(0도) 또는 한쪽으로 열림(±openAngle, 기획: 90도) 중 목표를 받아
    /// 정해진 빠르기로 돌고, 도착하면 그 자리에 고정된다(배그처럼 — 물리로 흔들리지 않는다).
    /// 문짝 충돌체(자식 Panel, 벽 레이어)는 시야·총알을 늘 각도대로 막는다. 사람(움직이는 몸)은
    /// 문짝이 움직이는 동안에는 통과하고, 멈춘 문짝에는 막힌다. 멈춘 순간 문짝에 끼인 사람은 몸 중심 쪽으로 살짝 밀려 나간다(끼인 채 통과하지 않게).
    /// 걷어차이면 아주 빠르게 열리며 쓸고 지나간 자리의 사람이 맞아 쓰러진다. 버티는 힘보다 세게 차이면
    /// 돌쩌귀가 떨어져 날아가고(이때만 물리 몸이 된다), 멈추면 그 자리에 낮은 엄폐물(Cover 레이어)로 남는다.
    /// 닫힌 자리 = 처음 놓인 자리. 처음 놓인 자리에서 이 물체(돌쩌귀)의 오른쪽(+x)이 문짝 방향이다.
    /// </summary>
    // 끼인 사람을 밀어낼 때 그 사람의 이동(ActorMotor)이 정한 속도를 고쳐야 해서, 이동 스크립트들보다 뒤에 돈다.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DoorLeaf : MonoBehaviour
    {
        /// <summary>날아가던 문짝이 이보다 느려지면 멈춘 것으로 친다 (칸/초).</summary>
        const float SettleSpeed = 0.4f;
        /// <summary>날아가는 문짝이 이보다 빠를 때만 부딪힌 사람이 맞는다 (칸/초).</summary>
        const float FlyHitSpeed = 3f;
        /// <summary>문간에서 F가 닿는 선을 양 끝에서 이만큼 줄인다(끝은 문틀 벽에 붙어 있어 손이 벽에 막힌 것으로 잡히므로).</summary>
        const float ReachInset = 0.15f;
        /// <summary>멈춘 문짝에 끼인 사람을 밀어내는 빠르기 (칸/초).</summary>
        const float PushOutSpeed = 4f;

        static readonly List<Collider2D> Nearby = new();
        static readonly ContactFilter2D SolidOnly = new() { useTriggers = false };

        Rigidbody2D body;
        Collider2D[] leafColliders;
        readonly List<Collider2D> passing = new();
        readonly HashSet<Health> hitThisKick = new();

        DoorData data;
        Door owner;
        GameObject kicker;
        float closedAngle;
        float angle;
        float targetAngle;
        float speed;
        bool kicked;
        float length;
        Vector2 hinge;

        /// <summary>돌쩌귀가 떨어져 문에서 빠졌는가(날아가는 중이거나 바닥에 떨어짐).</summary>
        public bool IsDetached { get; private set; }

        /// <summary>닫힌 자리에서 돈 각도(도, 부호 있음).</summary>
        public float Angle => angle;

        /// <summary>닫히는 중이거나 닫혀 있는가(F를 누르면 열 차례인가). 떨어진 문짝은 따지지 않는다.</summary>
        public bool IsClosing => !IsDetached && Mathf.Approximately(targetAngle, 0f);

        /// <summary>'열린 문'으로 칠 만큼 열렸는가. 떨어진 문짝은 늘 열림(문간이 뚫렸다).</summary>
        public bool IsOpen => IsDetached || Mathf.Abs(angle) > data.OpenThresholdAngle;

        bool IsMoving => !Mathf.Approximately(angle, targetAngle);

        /// <summary>문이 처음 켜질 때 한 번 부른다. 닫힌 채로 고정한다.</summary>
        public void Setup(DoorData doorData, Door door)
        {
            data = doorData;
            owner = door;
            body = GetComponent<Rigidbody2D>();
            leafColliders = GetComponentsInChildren<Collider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            closedAngle = body.rotation;
            hinge = transform.position;
            length = 0f;
            foreach (var own in leafColliders)
            {
                Vector2 local = transform.InverseTransformPoint(own.bounds.center);
                length = Mathf.Max(length, local.x * 2f);
            }
            angle = targetAngle = 0f;
        }

        /// <summary>from 반대쪽으로 문짝이 돌려면 몇 쪽(+1/-1)으로 돌아야 하는가(닫힌 자리 기준).</summary>
        public float SignAwayFrom(Vector2 from)
        {
            Vector2 along = Direction(0f);
            Vector2 swingSide = new(-along.y, along.x); // +로 돌 때 문짝이 가는 쪽
            Vector2 leafCenter = hinge + along * (length * 0.5f);
            return Vector2.Dot(swingSide, leafCenter - from) >= 0f ? 1f : -1f;
        }

        /// <summary>sign 쪽으로 연다(F, 다가오는 적).</summary>
        public void Open(float sign) => Swing(sign * data.OpenAngle, data.SwingSpeed);

        /// <summary>닫는다(F).</summary>
        public void Close() => Swing(0f, data.SwingSpeed);

        /// <summary>
        /// 걷어차인다. force가 버티는 힘(kickResistance) 이상이면 돌쩌귀가 떨어져 direction 쪽으로 날아가고,
        /// 아니면 sign 쪽으로 아주 빠르게 열리며 쓸고 지나간 자리의 사람을 맞힌다.
        /// </summary>
        public void Kick(float sign, Vector2 direction, float force, GameObject source)
        {
            if (IsDetached) return;
            kicker = source;
            hitThisKick.Clear();
            if (force >= data.KickResistance)
            {
                Detach(direction.normalized * force * data.FlySpeedPerForce, sign * data.KickSwingSpeed);
                return;
            }
            kicked = true;
            Swing(sign * data.OpenAngle, data.KickSwingSpeed);
        }

        /// <summary>
        /// from에서 가장 가까운 지점: 문간(닫힌 자리)과 지금 문짝 중 더 가까운 쪽. 문 옆에 서 있어도 F가 닿게.
        /// 문간 쪽은 문간 가운데 선에서 from 쪽 벽면까지(halfDepth) 당긴다 — 가운데 선까지 재면 옆에서 볼 때 문틀 벽 모서리에 막힌다.
        /// </summary>
        public Vector2 ClosestPoint(Vector2 from, float halfDepth)
        {
            Vector2 along = Direction(0f);
            Vector2 closedA = hinge + along * ReachInset;
            Vector2 closedB = hinge + along * Mathf.Max(ReachInset, length - ReachInset);
            Vector2 best = ClosestOnSegment(closedA, closedB, from);
            Vector2 normal = new(-along.y, along.x);
            float side = Vector2.Dot(from - best, normal);
            best += normal * Mathf.Clamp(side, -halfDepth, halfDepth);

            if (!IsDetached && Mathf.Abs(angle) > 0.5f)
            {
                Vector2 now = ClosestOnSegment(hinge + Direction(angle) * ReachInset, hinge + Direction(angle) * length, from);
                if ((now - from).sqrMagnitude < (best - from).sqrMagnitude) best = now;
            }
            return best;
        }

        void Swing(float target, float swingSpeed)
        {
            if (IsDetached) return;
            targetAngle = target;
            speed = swingSpeed;
            LetBodiesPass();
        }

        void FixedUpdate()
        {
            if (IsDetached)
            {
                UpdateFlying();
                return;
            }

            if (IsMoving)
            {
                float previous = angle;
                angle = Mathf.MoveTowards(angle, targetAngle, speed * Time.fixedDeltaTime);
                body.MoveRotation(closedAngle + angle);
                LetBodiesPass();
                if (kicked) HitSwept(previous, angle);
                if (!IsMoving) kicked = false;
                return;
            }

            BlockBodiesAgain();
        }

        /// <summary>도는 문짝 근처의 사람(움직이는 몸)과 부딪히지 않게 한다. 문짝이 사람을 밀거나 사람에게 막히지 않는다.</summary>
        void LetBodiesPass()
        {
            int count = Physics2D.OverlapCircle(hinge, length + 0.6f, SolidOnly, Nearby);
            for (int i = 0; i < count; i++)
            {
                Collider2D other = Nearby[i];
                if (!IsBody(other) || passing.Contains(other)) continue;
                passing.Add(other);
                foreach (var own in leafColliders) Physics2D.IgnoreCollision(own, other, true);
            }
        }

        /// <summary>
        /// 멈춘 문짝은 다시 사람을 막는다. 문짝에 겹친 채 끼인 사람은 몸 중심이 있는 쪽으로 살짝 밀어내고,
        /// 밀리는 동안 문을 뚫는 쪽으로 가는 움직임은 막는다(끼인 채 문을 통과하지 않게). 빠져나가면 다시 막는다.
        /// </summary>
        void BlockBodiesAgain()
        {
            for (int i = passing.Count - 1; i >= 0; i--)
            {
                Collider2D other = passing[i];
                if (other != null)
                {
                    Collider2D stuckIn = OverlappingLeafCollider(other);
                    if (stuckIn != null)
                    {
                        PushOut(other, stuckIn);
                        continue;
                    }
                    foreach (var own in leafColliders) Physics2D.IgnoreCollision(own, other, false);
                }
                passing.RemoveAt(i);
            }
        }

        Collider2D OverlappingLeafCollider(Collider2D other)
        {
            foreach (var own in leafColliders)
            {
                if (own.Distance(other).isOverlapped) return own;
            }
            return null;
        }

        /// <summary>끼인 사람을 문짝 면에 수직으로, 몸 중심이 있는 쪽으로 밀어낸다.</summary>
        void PushOut(Collider2D other, Collider2D stuckIn)
        {
            Rigidbody2D target = other.attachedRigidbody;
            if (target == null) return;
            Vector2 along = Direction(angle);
            Vector2 normal = new(-along.y, along.x);
            float side = Vector2.Dot(target.worldCenterOfMass - (Vector2)stuckIn.bounds.center, normal);
            Vector2 away = normal * (side >= 0f ? 1f : -1f);

            target.position += away * (PushOutSpeed * Time.fixedDeltaTime);
            float into = Vector2.Dot(target.linearVelocity, away);
            if (into < 0f) target.linearVelocity -= away * into;
        }

        /// <summary>차인 문짝이 이번 물리 프레임에 쓸고 지나간 부채꼴(from → to)에 들어온 사람을 맞힌다.</summary>
        void HitSwept(float from, float to)
        {
            float low = Mathf.Min(from, to);
            float high = Mathf.Max(from, to);
            float swingSign = Mathf.Sign(to - from);
            Vector2 closedDirection = Direction(0f);

            int count = Physics2D.OverlapCircle(hinge, length + 0.6f, SolidOnly, Nearby);
            for (int i = 0; i < count; i++)
            {
                Collider2D other = Nearby[i];
                if (!IsBody(other)) continue;
                Health health = other.GetComponentInParent<Health>();
                if (health == null || health.IsDead || hitThisKick.Contains(health)) continue;
                if (kicker != null && health.transform.IsChildOf(kicker.transform)) continue;

                Vector2 point = other.ClosestPoint(hinge);
                Vector2 fromHinge = (Vector2)other.bounds.center - hinge;
                float distance = fromHinge.magnitude;
                if (distance < 0.01f || (point - hinge).magnitude > length) continue;
                // 몸의 크기만큼 부채꼴을 넓혀 잰다(빠르게 돌 때 한 프레임에 몸을 건너뛰지 않게).
                float margin = Mathf.Rad2Deg * Mathf.Atan2(other.bounds.extents.x, distance);
                float at = Vector2.SignedAngle(closedDirection, fromHinge);
                if (at < low - margin || at > high + margin) continue;

                hitThisKick.Add(health);
                Vector2 push = new Vector2(-fromHinge.y, fromHinge.x).normalized * swingSign;
                health.TakeDamage(new DamageInfo(data.HitDamage, point, push, data.HitKnockback, kicker, data.HitEffect));
            }
        }

        /// <summary>돌쩌귀가 떨어진다. 문에서 빠져(길찾기가 더는 문으로 보지 않음) 물리 몸으로 날아간다.</summary>
        void Detach(Vector2 velocity, float spin)
        {
            IsDetached = true;
            kicked = false;
            foreach (var other in passing)
            {
                if (other == null) continue;
                foreach (var own in leafColliders) Physics2D.IgnoreCollision(own, other, false);
            }
            passing.Clear();

            transform.SetParent(owner.transform.parent, true);
            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearDamping = 3f;
            body.angularDamping = 3f;
            body.linearVelocity = velocity;
            body.angularVelocity = spin;
            owner.NotifyLeafDetached(this);
        }

        void UpdateFlying()
        {
            if (body.bodyType != RigidbodyType2D.Dynamic) return;
            if (body.linearVelocity.magnitude >= FlyHitSpeed) HitWhileFlying();
            if (body.linearVelocity.magnitude < SettleSpeed && Mathf.Abs(body.angularVelocity) < 30f) Settle();
        }

        /// <summary>날아가는 문짝에 닿은 사람이 맞는다(한 번씩).</summary>
        void HitWhileFlying()
        {
            foreach (var own in leafColliders)
            {
                int count = own.Overlap(SolidOnly, Nearby);
                for (int i = 0; i < count; i++)
                {
                    Collider2D other = Nearby[i];
                    if (!IsBody(other)) continue;
                    Health health = other.GetComponentInParent<Health>();
                    if (health == null || health.IsDead || hitThisKick.Contains(health)) continue;
                    if (kicker != null && health.transform.IsChildOf(kicker.transform)) continue;
                    hitThisKick.Add(health);
                    Vector2 point = other.ClosestPoint(body.position);
                    health.TakeDamage(new DamageInfo(data.HitDamage, point, body.linearVelocity, data.HitKnockback, kicker, data.HitEffect));
                }
            }
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

        /// <summary>사람·적처럼 스스로 움직이는 몸인가. 벽·다른 문짝(움직이지 않거나 스크립트가 돌리는 몸)은 아니다.</summary>
        bool IsBody(Collider2D other) =>
            other.attachedRigidbody != null && other.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic
            && !other.transform.IsChildOf(transform);

        Vector2 Direction(float degreesFromClosed) => Quaternion.Euler(0f, 0f, closedAngle + degreesFromClosed) * Vector3.right;

        static Vector2 ClosestOnSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return a + ab * t;
        }
    }
}
