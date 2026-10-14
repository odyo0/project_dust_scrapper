using System;
using System.Collections.Generic;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 플레이어가 몸에 지닌 것 전부를 저장용으로 담은 것: 무기 칸, 보조 칸, 몸 장비 칸, 가방.
    /// 칸 번호를 지키려고 빈 칸도 빈 묶음으로 그대로 둔다.
    /// </summary>
    [Serializable]
    public sealed class LoadoutData
    {
        public SavedStack[] weapons = Array.Empty<SavedStack>();
        public SavedStack[] support = Array.Empty<SavedStack>();
        public SavedStack[] body = Array.Empty<SavedStack>();
        public SavedStack[] bag = Array.Empty<SavedStack>();

        public static LoadoutData Empty => new();

        public IEnumerable<SavedStack> All()
        {
            foreach (var s in weapons) yield return s;
            foreach (var s in support) yield return s;
            foreach (var s in body) yield return s;
            foreach (var s in bag) yield return s;
        }
    }
}
