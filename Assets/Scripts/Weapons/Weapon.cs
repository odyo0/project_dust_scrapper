using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 손에 든 장비 하나(총, 칼, 마법서, 글러브, 스태프, 빈손 등). 꺼내기·집어넣기와 꺼내는 시간을 맡고,
    /// 실제 동작은 기본 스킬·특수 스킬 칸에 끼운 스킬(Skill)이 한다.
    /// 왼손이면 기본 = 좌클릭·특수 = Space, 오른손이면 기본 = 우클릭·특수 = F. 어느 키인지는 든 쪽이 정하고 장비는 모른다.
    /// 그래서 두뇌나 적 AI는 손에 든 게 총인지 칼인지 마법인지 몰라도 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Weapon : MonoBehaviour
    {
        [Tooltip("기본 스킬 (좌클릭/우클릭). 같은 프리팹에 붙은 스킬 부품.")]
        [SerializeField] Skill primary;
        [Tooltip("특수 스킬 (Space/F). 없으면 비워 둔다.")]
        [SerializeField] Skill special;

        float drawTimer;

        public WeaponData Data { get; private set; }

        /// <summary>이 장비를 든 쪽.</summary>
        public IWeaponHolder Holder { get; private set; }

        public Skill Primary => primary;
        public Skill Special => special;

        /// <summary>꺼내는 중이 끝나 쓸 수 있는 상태인가.</summary>
        public bool IsDrawn => drawTimer <= 0f;

        /// <summary>지금 다른 동작(꺼내기, 재장전, 휘두르기, 시전)을 하느라 바쁜가. AI가 공격 중에 멈칫하거나 방향을 고정할 때 읽는다.</summary>
        public bool IsBusy => !IsDrawn || (primary != null && primary.IsBusy) || (special != null && special.IsBusy);

        /// <summary>마법을 시전하는 중인가.</summary>
        public bool IsCasting => (primary != null && primary.IsCasting) || (special != null && special.IsCasting);

        /// <summary>반대 손이 시전 중이라 공격을 막는가. 켜져 있는 동안 누름은 버리고, 재장전처럼 하던 일은 계속한다.</summary>
        public bool AttackBlocked { get; set; }

        /// <summary>
        /// 장비 데이터의 프리팹으로 장비를 만들어 손(parent)에 붙이고, 집어넣은 상태로 돌려준다.
        /// 왼손·오른손이 같은 방법으로 만들게 한 곳에 둔다. 프리팹이 없으면 null.
        /// </summary>
        public static Weapon Spawn(WeaponData data, Transform parent, IWeaponHolder holder)
        {
            if (data.Prefab == null)
            {
                Debug.LogError($"{data.name}에 장비 프리팹이 없다.", data);
                return null;
            }

            Weapon weapon = Instantiate(data.Prefab, parent);
            weapon.name = data.name;
            weapon.Initialize(data, holder);
            weapon.gameObject.SetActive(false);
            return weapon;
        }

        /// <summary>든 쪽이 장비를 만든 직후 한 번 부른다.</summary>
        public void Initialize(WeaponData data, IWeaponHolder holder)
        {
            Data = data;
            Holder = holder;
            if (primary != null) primary.Bind(this);
            if (special != null) special.Bind(this);
        }

        /// <summary>이 장비의 스킬 중 그 종류를 찾는다(예: 탄 표시가 총 쏘기 스킬을 찾을 때). 없으면 null.</summary>
        public T FindSkill<T>() where T : Skill
        {
            if (primary is T p) return p;
            if (special is T s) return s;
            return null;
        }

        /// <summary>꺼낸다. 이미 누르고 있던 키는 한 번 뗐다 눌러야 단발이 나간다.</summary>
        public void Draw(bool primaryHeld, bool specialHeld)
        {
            gameObject.SetActive(true);
            drawTimer = Data.DrawTime;
            AttackBlocked = false;
            if (primary != null) primary.ResetInput(primaryHeld);
            if (special != null) special.ResetInput(specialHeld);
        }

        /// <summary>집어넣는다. 하던 동작(재장전, 휘두르기, 시전)은 취소된다.</summary>
        public void Holster()
        {
            if (primary != null) primary.Cancel();
            if (special != null) special.Cancel();
            gameObject.SetActive(false);
        }

        public void SetPrimary(bool held)
        {
            if (primary != null) primary.SetHeld(held);
        }

        public void SetSpecial(bool held)
        {
            if (special != null) special.SetHeld(held);
        }

        /// <summary>R. 지금은 기본 스킬의 재장전이다. R에 특수 기능이 있는 장비가 생기면 여기서 나눈다.</summary>
        public void Reload()
        {
            if (IsDrawn && primary != null) primary.Reload();
        }

        void Update()
        {
            if (drawTimer > 0f) drawTimer -= Time.deltaTime;
            bool drawn = IsDrawn;
            float deltaTime = Time.deltaTime;
            if (primary != null) primary.Step(drawn, AttackBlocked, deltaTime);
            if (special != null) special.Step(drawn, AttackBlocked, deltaTime);
        }
    }
}
