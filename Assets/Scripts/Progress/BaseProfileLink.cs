using DustScrapper.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 기지 씬과 저장 내용(Profile)을 잇는다. 기지가 열리면 몸에 지닌 것과 보관함을 저장대로 채우고,
    /// 칸이나 돈이 바뀔 때마다 저장하고, 출발할 때 몸에 지닌 것을 들고 원정 씬을 연다.
    /// 칸이 바뀐 그 프레임 끝에 한 번만 저장해서, 여러 칸을 한꺼번에 옮겨도 파일을 한 번만 쓴다.
    /// 보관함은 이 씬의 Inventory 하나다(보관함 확장이 생기면 그 칸 수를 여기서 정한다).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BaseProfileLink : MonoBehaviour
    {
        [Tooltip("기지 보관함. 내용물은 저장 파일에서 채운다(씬에 넣어 둔 물건은 지운다).")]
        [SerializeField] Inventory storage;

        PlayerLoadout player;
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
            LoadStorage(Profile.Current.storage);

            player.Changed += MarkDirty;
            if (storage != null) storage.Changed += MarkDirty;
            Wallet.Changed += MarkDirty;
            dirty = false;
        }

        void OnDestroy()
        {
            if (player != null) player.Changed -= MarkDirty;
            if (storage != null) storage.Changed -= MarkDirty;
            Wallet.Changed -= MarkDirty;
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
