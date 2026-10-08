using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 칸에서 칸으로 물건 옮기기 규칙. 가방·상자·무기 슬롯 어디끼리든 같은 규칙을 쓴다(IItemSlots만 본다).
    /// 화면(끌어 놓기, 우클릭)은 "어디서 어디로"만 정하고 규칙은 여기에 둔다.
    /// </summary>
    public static class ItemTransfer
    {
        /// <summary>
        /// from의 i칸을 to의 j칸으로 옮긴다. 빈 칸이면 그대로 옮기고, 같은 물건이면 겹치고(넘친 만큼은 제자리),
        /// 다른 물건이면 서로 바꾼다(양쪽 다 놓일 수 있을 때만). 무엇이든 옮겼으면 true.
        /// </summary>
        public static bool Move(IItemSlots from, int i, IItemSlots to, int j)
        {
            if (from == to && i == j) return false;

            ItemStack a = from.GetSlot(i);
            if (a.IsEmpty || !to.CanPlace(j, a)) return false;
            ItemStack b = to.GetSlot(j);

            if (b.IsEmpty)
            {
                to.PlaceInEmptySlot(j, from.TakeSlot(i));
                return true;
            }

            if (b.Item == a.Item && a.Item.MaxStack > 1)
            {
                int put = Mathf.Min(a.Count, a.Item.MaxStack - b.Count);
                if (put <= 0) return false;
                from.TakeSlot(i);
                to.TakeSlot(j);
                to.PlaceInEmptySlot(j, b.WithCount(b.Count + put));
                if (a.Count > put) from.PlaceInEmptySlot(i, a.WithCount(a.Count - put));
                return true;
            }

            if (!from.CanPlace(i, b)) return false;
            from.TakeSlot(i);
            to.TakeSlot(j);
            to.PlaceInEmptySlot(j, a);
            from.PlaceInEmptySlot(i, b);
            return true;
        }

        /// <summary>
        /// from의 i칸을 to의 알맞은 자리로 옮긴다: 같은 물건 묶음부터 채우고, 그다음 빈 칸.
        /// 다 못 옮기면 남은 만큼 제자리에 남는다. 무엇이든 옮겼으면 true.
        /// </summary>
        public static bool QuickMove(IItemSlots from, int i, IItemSlots to)
        {
            if (from == to) return false;

            ItemStack a = from.GetSlot(i);
            if (a.IsEmpty) return false;

            int max = a.Item.MaxStack;
            int left = a.Count;

            if (max > 1)
            {
                for (int j = 0; j < to.SlotCount && left > 0; j++)
                {
                    ItemStack b = to.GetSlot(j);
                    if (b.Item != a.Item || b.Count >= max) continue;
                    int put = Mathf.Min(left, max - b.Count);
                    to.TakeSlot(j);
                    to.PlaceInEmptySlot(j, b.WithCount(b.Count + put));
                    left -= put;
                }
            }

            for (int j = 0; j < to.SlotCount && left > 0; j++)
            {
                if (!to.GetSlot(j).IsEmpty || !to.CanPlace(j, a)) continue;
                int put = Mathf.Min(left, max);
                to.PlaceInEmptySlot(j, a.WithCount(put));
                left -= put;
            }

            if (left == a.Count) return false;
            from.TakeSlot(i);
            if (left > 0) from.PlaceInEmptySlot(i, a.WithCount(left));
            return true;
        }
    }
}
