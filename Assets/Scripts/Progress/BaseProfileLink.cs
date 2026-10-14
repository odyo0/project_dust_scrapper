using DustScrapper.Combat;
using DustScrapper.Glow;
using DustScrapper.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 기지 씬과 저장 내용(Profile)을 잇는다. 기지가 열리면 몸에 지닌 것과 보관함을 저장대로 채우고,
    /// 칸이나 돈이 바뀔 때마다 저장하고, 출발할 때 몸에 지닌 것을 들고 원정 씬을 연다.
    /// 칸이 바뀐 그 프레임 끝에 한 번만 저장해서, 여러 칸을 한꺼번에 옮겨도 파일을 한 번만 쓴다.
    /// 보관함은 이 씬의 Inventory 하나이고, 보관함 확장 업그레이드 단계만큼 칸을 늘린다(저장된 물건을 채우기 전에).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BaseProfileLink : MonoBehaviour
    {
        [Tooltip("기지 보관함. 내용물은 저장 파일에서 채운다(씬에 넣어 둔 물건은 지운다).")]
        [SerializeField] Inventory storage;

        [Tooltip("보관함 확장 업그레이드. 단계마다 storageSlotsPerLevel칸씩 늘어난다.")]
        [SerializeField] BaseUpgradeData storageUpgrade;
        [SerializeField, Min(0)] int storageSlotsPerLevel = 15;

        [Tooltip("기지에서 쓰러졌을 때(글로우병 등) 기지를 다시 열기까지 기다리는 시간 (초)")]
        [SerializeField, Min(0f)] float deathReloadDelay = 2f;

        PlayerLoadout player;
        GlowSickness sickness;
        Health health;
        bool dirty;

        void Start()
        {
            Time.timeScale = 1f;
            if (Profile.ResolveAbandonedRaid()) Debug.Log("원정 중에 게임이 꺼졌던 저장이라 쓰러진 것으로 처리했다.");

            GameObject playerObject = GameObject.FindWithTag("Player");
            player = PlayerLoadout.Of(playerObject);
            if (player == null)
            {
                Debug.LogError($"{name}: Player 태그가 붙은 플레이어가 없어 기지를 불러올 수 없다.", this);
                enabled = false;
                return;
            }

            player.Apply(Profile.Current.carried);
            // 장비 보너스가 붙은 뒤에 MP를 가득 채운다(장비를 끼는 것만으로는 MP가 차지 않으므로).
            if (playerObject.TryGetComponent(out Magic.Mana mana)) mana.RestoreFull();
            if (playerObject.TryGetComponent(out sickness))
            {
                sickness.Load(Profile.Current.glowSickness);
                sickness.Changed += MarkDirty;
            }
            if (playerObject.TryGetComponent(out health)) health.Died += OnPlayerDied;
            ApplyStorageSize();
            LoadStorage(Profile.Current.storage);

            player.Changed += MarkDirty;
            if (storage != null) storage.Changed += MarkDirty;
            Wallet.Changed += MarkDirty;
            BaseUpgrades.Changed += OnUpgraded;
            dirty = false;
        }

        void OnDestroy()
        {
            if (player != null) player.Changed -= MarkDirty;
            if (storage != null) storage.Changed -= MarkDirty;
            Wallet.Changed -= MarkDirty;
            BaseUpgrades.Changed -= OnUpgraded;
            if (sickness != null) sickness.Changed -= MarkDirty;
            if (health != null) health.Died -= OnPlayerDied;
        }

        /// <summary>기지에서 쓰러지면 원정에서 쓰러진 것과 같이 처리하고, 잠시 뒤 기지를 처음부터 다시 연다.</summary>
        void OnPlayerDied(DamageInfo info)
        {
            dirty = false;
            enabled = false;
            Profile.DieAtBase();
            Invoke(nameof(ReloadBase), deathReloadDelay);
        }

        void ReloadBase() => SceneManager.LoadScene(gameObject.scene.name);

        void OnUpgraded()
        {
            ApplyStorageSize();
            MarkDirty();
        }

        void ApplyStorageSize()
        {
            if (storage != null && storageUpgrade != null)
                storage.SetExtraSlots(BaseUpgrades.LevelOf(storageUpgrade) * storageSlotsPerLevel);
        }

        void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;
            WriteProfile();
            Profile.Save();
        }

        /// <summary>몸에 지닌 것을 들고 원정 씬으로 나간다.</summary>
        public void Depart(string raidScene)
        {
            WriteProfile();
            Profile.BeginRaid(player.Capture());
            SceneManager.LoadScene(raidScene);
        }

        void MarkDirty() => dirty = true;

        void WriteProfile()
        {
            Profile.Current.carried = player.Capture();
            if (sickness != null) Profile.Current.glowSickness = sickness.Value;
            if (storage == null) return;
            var saved = new SavedStack[storage.SlotCount];
            for (int i = 0; i < saved.Length; i++) saved[i] = SavedStack.From(storage.GetSlot(i));
            Profile.Current.storage = saved;
        }

        void LoadStorage(SavedStack[] saved)
        {
            if (storage == null) return;
            for (int i = storage.SlotCount - 1; i >= 0; i--) storage.TakeSlot(i);
            if (saved == null) return;

            for (int i = 0; i < saved.Length; i++)
            {
                ItemStack stack = saved[i].ToStack();
                if (stack.IsEmpty) continue;
                if (i < storage.SlotCount && storage.GetSlot(i).IsEmpty) storage.PlaceInEmptySlot(i, stack);
                else if (storage.Add(stack) > 0) Debug.LogWarning($"보관함 칸이 모자라 저장된 {stack.Item.name}을(를) 버렸다.");
            }
        }
    }
}
