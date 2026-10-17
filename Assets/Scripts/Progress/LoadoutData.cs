using System;
using System.Collections.Generic;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 플레이어가 몸에 지닌 것 전부를 저장용으로 담은 것. 칸마다 이름표(PlayerGear.Keys)와 내용을 한 줄씩 적는다.
    /// 칸 번호를 지키려고 빈 칸도 빈 묶음으로 그대로 둔다.
    /// 새 칸이 생겨도 줄이 하나 늘 뿐이라 이 형식은 그대로다.
    /// </summary>
    [Serializable]
    public sealed class LoadoutData
    {
        public List<SavedSlots> slots = new();

        // 옛 형식(2026-10-16까지): 칸마다 따로 있던 칸. 옛 저장 파일을 읽을 때만 쓴다. 새로 저장하면 늘 비어 있다.
        public SavedStack[] weapons = Array.Empty<SavedStack>();
        public SavedStack[] support = Array.Empty<SavedStack>();
        public SavedStack[] body = Array.Empty<SavedStack>();
        public SavedStack[] bag = Array.Empty<SavedStack>();
        public SavedStack[] quick = Array.Empty<SavedStack>();

        public static LoadoutData Empty => new();

        /// <summary>그 이름표 칸의 내용. 새 형식에 없으면 옛 형식에서 찾는다. 둘 다 없으면 빈 배열.</summary>
        public SavedStack[] Get(string key)
        {
            foreach (var saved in slots)
            {
                if (saved.key == key) return saved.items ?? Array.Empty<SavedStack>();
            }
            return key switch
            {
                "weapons" => weapons,
                "support" => support,
                "body" => body,
                "bag" => bag,
                "quick" => quick,
                _ => null,
            } ?? Array.Empty<SavedStack>();
        }

        /// <summary>그 이름표 칸의 내용을 적는다(새 형식).</summary>
        public void Set(string key, SavedStack[] items)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].key != key) continue;
                slots[i] = new SavedSlots { key = key, items = items };
                return;
            }
            slots.Add(new SavedSlots { key = key, items = items });
        }
    }

    /// <summary>저장할 칸 하나: 이름표와 칸마다의 물건.</summary>
    [Serializable]
    public struct SavedSlots
    {
        public string key;
        public SavedStack[] items;
    }
}
