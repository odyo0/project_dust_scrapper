import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from px import *
from scipy import ndimage

# ---------- 바닥 ----------
def floor_tile(seed, wear=0):
    """상가 타일 바닥: 한 칸에 16px 타일 4장. wear 0 깨끗 / 1 때 / 2 깨짐"""
    rng = np.random.default_rng(seed); im = canvas(T, T)
    rect(im, 0, 0, T, T, C['con'][3])
    for ty in (0, 16):
        for tx in (0, 16):
            base = C['con'][3] if rng.random() > 0.25 else C['pale'][1]
            rect(im, tx, ty, 15, 15, base)
            hline(im, tx, ty, 15, C['con'][4]); vline(im, tx, ty, 15, C['con'][4])
            hline(im, tx + 1, ty + 14, 14, C['con'][2]); vline(im, tx + 14, ty + 1, 14, C['con'][2])
            hline(im, tx, ty + 15, 16, C['con'][1]); vline(im, tx + 15, ty, 16, C['con'][1])
    specks(im, rng, 10, [C['con'][2], C['pale'][0]])
    if wear >= 1:
        specks(im, rng, 18, [C['con'][2], C['dirt'][2], C['pale'][0]])
        for _ in range(2):
            cx, cy = rng.integers(4, 28, 2); r = rng.uniform(2.5, 4.5)
            yy, xx = np.mgrid[0:T, 0:T]; m = np.hypot(xx - cx, yy - cy) < r
            for y, x in zip(*np.nonzero(m & ((xx + yy) % 2 == 0))): px(im, x, y, C['con'][2])
        crack(im, rng, rng.integers(2, 30), rng.integers(2, 30), 14, C['con'][1])
    if wear >= 2:   # 타일 한 장이 깨져 떨어져 나감
        tx, ty = rng.choice([0, 16]), rng.choice([0, 16])
        rect(im, tx, ty, 15, 15, C['asph'][1])
        for x in range(tx, tx + 15):
            if rng.random() < 0.5: px(im, x, ty, C['con'][2])
        for y in range(ty, ty + 15):
            if rng.random() < 0.5: px(im, tx, y, C['con'][2])
        hline(im, tx + 1, ty + 1, 13, C['asph'][0]); vline(im, tx + 1, ty + 1, 13, C['asph'][0])
        specks(im, rng, 12, [C['con'][2], C['con'][3], C['asph'][2]], region=(tx + 2, ty + 2, tx + 14, ty + 14))
    return im

def wood_tile(seed, wear=0):
    """나무 마루: 8px 널빤지 4줄, 이음매는 줄마다 엇갈림"""
    rng = np.random.default_rng(seed); im = canvas(T, T)
    seams = [5, 21, 13, 29]
    for r in range(4):
        y = r * 8
        body = C['wood'][1]; hi = C['wood'][2]; lo = C['wood'][0]
        rect(im, 0, y, T, 8, body)
        hline(im, 0, y, T, hi)
        hline(im, 0, y + 7, T, lo)
        s = seams[r]; vline(im, s, y + 1, 6, lo); px(im, s + 1, y + 1, hi)
        for _ in range(2):   # 나뭇결
            gx = rng.integers(0, T - 6); gy = y + rng.integers(2, 6)
            hline(im, gx, gy, rng.integers(3, 6), lo)
        if wear >= 1 and rng.random() < 0.3:   # 바랜 널빤지
            for x in range(0, T):
                for yy in range(y + 1, y + 7):
                    if (x + yy) % 2 == 0 and im[yy, x, :3].tolist() == list(body): px(im, x, yy, C['pack'][1])
    if wear >= 1:
        specks(im, rng, 8, [C['wood'][0], C['dirt'][1]])
        crack(im, rng, rng.integers(2, 30), rng.integers(2, 30), 10, C['wood'][0], ang=0.0)
    if wear >= 2:   # 널빤지 하나가 빠짐
        r = rng.integers(0, 4); y = r * 8; x0 = rng.integers(0, 12); w = rng.integers(12, 20)
        rect(im, x0, y, w, 8, C['ink'][1])
        hline(im, x0, y + 1, w, C['ink'][0])
        for x in range(x0, x0 + w, 5): vline(im, x, y + 2, 5, C['wood'][0])   # 아래 장선
        vline(im, x0, y, 8, C['wood'][3]); vline(im, x0 + w - 1, y, 8, C['wood'][0])
    return im

# ---------- 벽 (이웃 4방향 자동 연결 16장) ----------
WALL_FILL = [C['asph'][1], C['asph'][1], C['asph'][1], C['asph'][0], C['asph'][2]]
def wall_tile(n, e, s, w, seed=0):
    """n,e,s,w: 그쪽 이웃도 벽이면 True. 벽이 아닌 쪽에만 테두리."""
    rng = np.random.default_rng(1000 + seed); im = canvas(T, T)
    rect(im, 0, 0, T, T, C['asph'][1])
    for _ in range(40):
        x, y = rng.integers(0, T, 2); px(im, x, y, WALL_FILL[rng.integers(len(WALL_FILL))])
    for _ in range(2):   # 위면 금
        crack(im, rng, rng.integers(4, 28), rng.integers(4, 28), 8, C['asph'][0])
    def side(cond, draw):
        if not cond: draw()
    side(n, lambda: (hline(im, 0, 0, T, C['ink'][0]), hline(im, 0, 1, T, C['con'][1]), hline(im, 0, 2, T, C['asph'][2])))
    side(s, lambda: (hline(im, 0, T - 1, T, C['ink'][0]), hline(im, 0, T - 2, T, C['con'][0])))
    side(w, lambda: (vline(im, 0, 0, T, C['ink'][0]), vline(im, 1, 0, T, C['con'][1])))
    side(e, lambda: (vline(im, T - 1, 0, T, C['ink'][0]), vline(im, T - 2, 0, T, C['con'][0])))
    # 겉 모서리 깨진 자국
    for (cn, cw, x, y) in ((n, w, 0, 0), (n, e, T - 3, 0), (s, w, 0, T - 3), (s, e, T - 3, T - 3)):
        if not cn and not cw: rect(im, x, y, 3, 3, C['ink'][0]); px(im, x + 1, y + 1, C['ink'][0])
    if not n:   # 벽 윗면 가장자리 이빨 빠짐
        for _ in range(rng.integers(1, 3)):
            x = rng.integers(4, T - 6); rect(im, x, 0, rng.integers(2, 4), 2, C['ink'][0])
    return im

def window_tile(horizontal=True, broken=False, seed=0):
    rng = np.random.default_rng(2000 + seed)
    im = wall_tile(not horizontal, horizontal, not horizontal, horizontal, seed)
    if horizontal:
        rect(im, 0, 11, T, 10, C['ink'][1])
        rect(im, 0, 12, T, 8, C['steel'][2]); hline(im, 0, 12, T, C['steel'][3]); hline(im, 0, 19, T, C['steel'][1])
        for x in (0, T - 1): vline(im, x, 11, 10, C['ink'][1])
        for i in range(3):  # 반사
            x = rng.integers(3, 24); hline(im, x, 14 + i % 2, rng.integers(2, 5), C['water'][2])
        specks(im, rng, 6, [C['con'][3], C['pale'][1]], region=(0, 12, T, 20))
        if broken:
            rect(im, 1, 12, T - 2, 8, C['asph'][0])
            for x in range(1, T - 1):
                if rng.random() < 0.45: px(im, x, 12, C['steel'][3])
                if rng.random() < 0.35: px(im, x, 19, C['steel'][2])
            for _ in range(5): px(im, rng.integers(2, 30), rng.integers(13, 19), C['steel'][3])
        else:
            crack(im, rng, rng.integers(6, 26), 16, 10, C['steel'][1], m=np.pad(np.ones((8, T), bool), ((12, 12), (0, 0))))
    else:
        im = np.rot90(window_tile(True, broken, seed), 1).copy()
    return im

# ---------- 문짝 (물체 그림, 타일 아님) ----------
def door_leaf(seed=0):
    rng = np.random.default_rng(3000 + seed); im = canvas(T, 8)
    rect(im, 1, 1, T - 2, 6, C['wood'][1]); hline(im, 1, 1, T - 2, C['wood'][2]); hline(im, 1, 6, T - 2, C['wood'][0])
    outline(im, C['ink'][1])
    vline(im, 3, 2, 4, C['steel'][1])          # 경첩
    px(im, T - 5, 3, C['steel'][3]); px(im, T - 5, 4, C['steel'][2])   # 손잡이
    specks(im, rng, 5, [C['wood'][0], C['wood'][3]])
    rect(im, 12, 1, 3, 1, C['ink'][1])  # 깨진 홈
    return im

# ---------- 가구 (칸 단위: 1x1, 2x1) ----------
def desk(seed=0):
    rng = np.random.default_rng(4000 + seed); im = canvas(64, 32)
    rect(im, 2, 3, 60, 24, C['wood'][2]); outline(im, C['ink'][1])
    hline(im, 2, 3, 60, C['wood'][3]); hline(im, 2, 26, 60, C['wood'][1]); vline(im, 61, 3, 24, C['wood'][1])
    for _ in range(7): hline(im, rng.integers(4, 50), rng.integers(6, 24), rng.integers(4, 10), C['wood'][1])
    # 모니터 (위에서 본 얇은 판 + 받침)
    rect(im, 8, 5, 18, 4, C['asph'][0]); hline(im, 8, 5, 18, C['asph'][2]); rect(im, 15, 9, 4, 3, C['asph'][1])
    # 종이 몇 장
    rect(im, 34, 10, 9, 11, C['light'][0]); hline(im, 35, 13, 6, C['con'][3]); hline(im, 35, 15, 5, C['con'][3]); hline(im, 35, 17, 6, C['con'][3])
    hline(im, 34, 20, 9, C['con'][4]); rect(im, 41, 8, 8, 10, C['light'][1]); vline(im, 48, 8, 10, C['con'][4])
    # 컵
    rect(im, 52, 18, 4, 4, C['con'][4]); px(im, 53, 19, C['dirt'][0]); px(im, 54, 19, C['dirt'][0]); px(im, 56, 19, C['con'][3])
    specks(im, rng, 14, [C['con'][4], C['wood'][1]], region=(3, 4, 61, 26))
    crack(im, rng, 24, 22, 12, C['wood'][0])
    return im

def chair(seed=0):
    rng = np.random.default_rng(5000 + seed); im = canvas(T, T)
    rrect(im, 8, 10, 16, 15, C['denim'][1]); rrect(im, 7, 5, 18, 6, C['denim'][0])   # 좌석·등받이
    outline(im, C['ink'][1])
    hline(im, 9, 10, 14, C['denim'][2]); hline(im, 8, 5, 16, C['denim'][1])
    vline(im, 5, 13, 8, C['asph'][1]); vline(im, 26, 13, 8, C['asph'][1])    # 팔걸이
    for x in (4, 27): vline(im, x, 13, 8, C['ink'][1])
    # 찢어진 쿠션 (속 솜)
    rect(im, 13, 15, 5, 3, C['light'][0]); px(im, 14, 16, C['light'][1]); px(im, 12, 16, C['denim'][0]); px(im, 18, 15, C['denim'][0])
    specks(im, rng, 5, [C['denim'][0], C['con'][3]], region=(8, 10, 24, 25))
    return im

def shelf(seed=0):
    """진열대 2x1: 위에서 본 철제 선반과 물건"""
    rng = np.random.default_rng(6000 + seed); im = canvas(64, 32)
    rect(im, 1, 4, 62, 24, C['con'][2]); outline(im, C['ink'][1])
    rect(im, 1, 4, 62, 2, C['steel'][2]); rect(im, 1, 26, 62, 2, C['steel'][1])
    vline(im, 1, 4, 24, C['steel'][1]); vline(im, 62, 4, 24, C['steel'][0]); vline(im, 32, 4, 24, C['steel'][1])
    x = 3
    while x < 60:
        kind = rng.integers(0, 4); y = rng.integers(6, 12)
        if kind == 0:   # 상자
            w = rng.integers(7, 11); h = rng.integers(8, 14)
            if x + w > 61: break
            rect(im, x, y, w, h, C['pack'][2]); hline(im, x, y, w, C['light'][0]); hline(im, x, y + h - 1, w, C['pack'][1]); vline(im, x + w // 2, y, h, C['pack'][1])
            x += w + 1
        elif kind == 1:  # 캔 묶음
            if x + 10 > 61: break
            col = [C['rust'][1], C['sign'][0], C['blood'][1], C['olive'][1]][rng.integers(4)]
            for cy in (y, y + 6):
                for cx in (x, x + 5):
                    rrect(im, cx, cy, 4, 4, col); px(im, cx + 1, cy + 1, C['con'][4])
            x += 11
        elif kind == 2:  # 빈칸 (털린 자리), 먼지
            specks(im, rng, 4, [C['steel'][0], C['con'][2]], region=(x, 6, x + 6, 26)); x += 7
        else:  # 병
            if x + 12 > 61: break
            for i in range(rng.integers(2, 4)):
                rrect(im, x + i * 4, y + 2, 3, 3, C['water'][1]); px(im, x + i * 4 + 1, y + 2, C['water'][2])
            x += 13
    # 쓰러진 병 하나
    hline(im, 20, 24, 6, C['water'][1]); px(im, 25, 24, C['water'][2])
    return im

def cabinet(seed=0):
    rng = np.random.default_rng(7000 + seed); im = canvas(T, T)
    rect(im, 3, 3, 26, 26, C['steel'][1]); outline(im, C['ink'][1])
    hline(im, 3, 3, 26, C['steel'][2]); hline(im, 3, 28, 26, C['steel'][0]); vline(im, 28, 3, 26, C['steel'][0])
    hline(im, 5, 15, 22, C['steel'][0])
    for (rx, ry) in ((8, 8), (20, 20), (22, 6)):
        rect(im, rx, ry, 3, 2, C['rust'][1]); px(im, rx + 1, ry + 1, C['rust'][0])
    specks(im, rng, 8, [C['steel'][2], C['rust'][2]], region=(4, 4, 28, 28))
    return im

def sofa(seed=0):
    rng = np.random.default_rng(8000 + seed); im = canvas(64, 32)
    rrect(im, 2, 4, 60, 24, C['brick'][1], 1)
    rect(im, 2, 4, 60, 7, C['brick'][1]); rect(im, 2, 4, 6, 24, C['brick'][1]); rect(im, 56, 4, 6, 24, C['brick'][1])
    outline(im, C['ink'][1])
    for cx in (8, 32):   # 방석 두 개
        rect(im, cx, 11, 24, 16, C['brick'][2]); hline(im, cx, 11, 24, C['brick'][3]); hline(im, cx, 26, 24, C['brick'][1]); vline(im, cx + 23, 11, 16, C['brick'][1])
    hline(im, 3, 4, 58, C['brick'][2]); vline(im, 2, 5, 22, C['brick'][2])
    # 찢어짐, 얼룩
    rect(im, 40, 15, 7, 5, C['light'][0]); px(im, 42, 16, C['light'][1]); px(im, 44, 17, C['light'][1]); hline(im, 39, 15, 9, C['brick'][0]); hline(im, 39, 20, 9, C['brick'][0])
    for y, x in zip(*np.nonzero(np.hypot(*np.mgrid[0:32, 0:64] - np.array([20, 16])[:, None, None]) < 5)):
        if (x + y) % 2 == 0 and im[y, x, 3]: px(im, x, y, C['brick'][0])
    specks(im, rng, 8, [C['brick'][0], C['con'][3]], region=(8, 11, 56, 27))
    return im
