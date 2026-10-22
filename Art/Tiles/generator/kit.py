import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from px import *
from tiles import wood_tile, wall_tile, window_tile, door_leaf
from scipy import ndimage

DIRS = [(n, e, s, w) for n in (0, 1) for e in (0, 1) for s in (0, 1) for w in (0, 1)]   # 16 조합, 순서 고정
def dkey(n, e, s, w): return n * 8 + e * 4 + s * 2 + w

# ================= 바닥 =================
def tile_floor(seed, ramp, size=16, wear=0, grout=None):
    """정사각 타일 바닥. ramp=(어두움, 몸, 밝음)"""
    rng = np.random.default_rng(seed); im = canvas(T, T)
    lo, body, hi = ramp; grout = grout or lo
    rect(im, 0, 0, T, T, body)
    for ty in range(0, T, size):
        for tx in range(0, T, size):
            b = body if rng.random() > 0.07 else lo
            rect(im, tx, ty, size - 1, size - 1, b)
            hline(im, tx, ty, size - 1, hi); vline(im, tx, ty, size - 1, hi)
            hline(im, tx, ty + size - 1, size, grout); vline(im, tx + size - 1, ty, size, grout)
    specks(im, rng, 8 + 10 * wear, [lo, C['pale'][0] if wear else lo])
    if wear >= 1: crack(im, rng, rng.integers(2, 30), rng.integers(2, 30), 12, grout)
    if wear >= 2 and size >= 16:
        tx, ty = rng.choice([0, 16]), rng.choice([0, 16])
        rect(im, tx, ty, 15, 15, C['asph'][1]); hline(im, tx + 1, ty + 1, 13, C['asph'][0]); vline(im, tx + 1, ty + 1, 13, C['asph'][0])
        for x in range(tx, tx + 15):
            if rng.random() < 0.5: px(im, x, ty, lo)
        specks(im, rng, 10, [C['con'][2], C['asph'][2]], region=(tx + 2, ty + 2, tx + 14, ty + 14))
    elif wear >= 2:
        cx, cy = rng.integers(6, 26, 2)
        yy, xx = np.mgrid[0:T, 0:T]; m = np.hypot(xx - cx, yy - cy) < 5
        for y, x in zip(*np.nonzero(m)): px(im, x, y, C['asph'][1] if rng.random() < .8 else C['asph'][0])
    return im
def slab_floor(seed, wear=0):
    rng = np.random.default_rng(seed); im = canvas(T, T); rect(im, 0, 0, T, T, C['con'][2])
    specks(im, rng, 40, [C['con'][1], C['con'][3], C['pale'][0]])
    for _ in range(1 + wear): crack(im, rng, rng.integers(0, 32), rng.integers(0, 32), 14, C['con'][1])
    if wear >= 2:
        for _ in range(12): px(im, *rng.integers(8, 24, 2), C['asph'][1])
    return im
def carpet_floor(seed, wear=0):
    rng = np.random.default_rng(seed); im = canvas(T, T); rect(im, 0, 0, T, T, C['denim'][1])
    for y in range(T):
        for x in range(T):
            if (x * 3 + y * 5) % 7 == 0: px(im, x, y, C['denim'][0])
    specks(im, rng, 6 + 14 * wear, [C['denim'][0], C['rag'][1], C['dirt'][1]])
    if wear >= 2:
        cx, cy = rng.integers(8, 24, 2); yy, xx = np.mgrid[0:T, 0:T]; m = np.hypot(xx - cx, (yy - cy) * 1.5) < 6
        for y, x in zip(*np.nonzero(m & ((xx + yy) % 2 == 0))): px(im, x, y, C['rag'][0])
    return im
FLOOR_KINDS = [   # 옛 견본 바닥 번호 순서와 맞춤
 ('wood',        lambda s, w: wood_tile(s, w)),
 ('tile_gray',   lambda s, w: tile_floor(s, (C['con'][2], C['con'][3], C['con'][4]), 16, w)),
 ('tile_light',  lambda s, w: tile_floor(s, (C['con'][3], C['pale'][1], C['light'][0]), 16, w)),
 ('tile_tan',    lambda s, w: tile_floor(s, (C['dirt'][2], C['dirt'][3], C['wood'][3]), 16, w)),
 ('tile_white',  lambda s, w: tile_floor(s, (C['con'][4], C['light'][0], C['light'][1]), 16, w, C['con'][3])),
 ('tile_dark',   lambda s, w: tile_floor(s, (C['con'][1], C['con'][2], C['con'][3]), 16, w)),
 ('tile_bath',   lambda s, w: tile_floor(s, (C['steel'][2], C['water'][2], C['light'][1]), 8, w, C['steel'][2])),
 ('concrete',    lambda s, w: slab_floor(s, w)),
 ('carpet',      lambda s, w: carpet_floor(s, w)),
]
def floor_tiles():
    out, names = [], []
    for k, (nm, f) in enumerate(FLOOR_KINDS):
        for wear in range(3):
            out.append(f(k * 101 + wear * 7, wear)); names.append(f'{nm}_{wear}')
    return out, names

# ================= 벽 =================
def wall_tiles():
    out, names = [], []
    for (n, e, s, w) in DIRS:
        out.append(wall_tile(bool(n), bool(e), bool(s), bool(w), dkey(n, e, s, w))); names.append(f'wall_{n}{e}{s}{w}')
    for hz in (True, False):
        for br in (False, True):
            out.append(window_tile(hz, br, 3)); names.append(f"window_{'h' if hz else 'v'}{'_broken' if br else ''}")
    leaf = door_leaf()
    for hz in (True, False):
        im = canvas(T, T); paste(im, leaf, 0, 12)
        out.append(im if hz else np.rot90(im, 1).copy()); names.append(f"door_wood_{'h' if hz else 'v'}")
    g = canvas(T, 8); rect(g, 1, 1, T - 2, 6, C['steel'][2]); hline(g, 1, 1, T - 2, C['steel'][3]); hline(g, 1, 6, T - 2, C['steel'][1])
    outline(g, C['ink'][1]); vline(g, T - 3, 1, 6, C['ink'][1]); px(g, 6, 3, C['water'][2])
    for hz in (True, False):
        im = canvas(T, T); paste(im, g, 0, 12)
        out.append(im if hz else np.rot90(im, 1).copy()); names.append(f"door_glass_{'h' if hz else 'v'}")
    return out, names

# ================= 이어지는 가구 (4방향 연결 16장) =================
def surface(n, e, s, w, base, hi, lo, inset=2):
    """연결된 쪽은 칸 끝까지, 끊긴 쪽은 inset만큼 띄우고 테두리"""
    im = canvas(T, T)
    x0 = 0 if w else inset; x1 = T if e else T - inset; y0 = 0 if n else inset; y1 = T if s else T - inset
    rect(im, x0, y0, x1 - x0, y1 - y0, base)
    if not n: hline(im, x0, y0, x1 - x0, hi)
    if not w: vline(im, x0, y0, y1 - y0, hi)
    if not s: hline(im, x0, y1 - 1, x1 - x0, lo)
    if not e: vline(im, x1 - 1, y0, y1 - y0, lo)
    m = mask(im)
    # 테두리(잉크)는 끊긴 쪽에만
    o = canvas(T, T)
    if not n: hline(o, x0, y0 - 1, x1 - x0, C['ink'][1])
    if not s: hline(o, x0, y1, x1 - x0, C['ink'][1])
    if not w: vline(o, x0 - 1, y0, y1 - y0, C['ink'][1])
    if not e: vline(o, x1, y0, y1 - y0, C['ink'][1])
    a = o[..., 3] > 0; im[a] = o[a]
    return im, (x0, y0, x1, y1)
GOODS = [C['rust'][1], C['sign'][1], C['blood'][1], C['olive'][2], C['denim'][2], C['water'][1], C['light'][0], C['pack'][2], C['grass'][3], C['brick'][3]]
def deco_wood(im, box, rng, lo):
    x0, y0, x1, y1 = box
    for _ in range(3): hline(im, rng.integers(x0 + 1, max(x0 + 2, x1 - 7)), rng.integers(y0 + 2, y1 - 2), rng.integers(3, 7), lo)
    specks(im, rng, 4, [lo], region=(x0 + 1, y0 + 1, x1 - 1, y1 - 1))
def deco_goods(im, box, rng, nesw):
    """털린 진열대: 선반 판과 가격표 줄만 남고 물건은 거의 없음"""
    x0, y0, x1, y1 = box; n, e, s, w = nesw
    horiz = (e or w) or not (n or s)
    if horiz:
        for y in range(y0 + 7, y1 - 1, 8): hline(im, x0, y, x1 - x0, C['pack'][0]); hline(im, x0, y - 1, x1 - x0, C['pack'][2])
    else:
        for x in range(x0 + 7, x1 - 1, 8): vline(im, x, y0, y1 - y0, C['pack'][0]); vline(im, x - 1, y0, y1 - y0, C['pack'][2])
    specks(im, rng, 10, [C['pack'][0], C['dirt'][1], C['con'][2]], region=(x0 + 1, y0 + 1, x1 - 1, y1 - 1))
    for _ in range(rng.integers(0, 3)):      # 남은 물건 몇 개 (일부는 쓰러짐)
        col = GOODS[rng.integers(len(GOODS))]
        x = rng.integers(x0 + 2, x1 - 6); y = rng.integers(y0 + 2, y1 - 6)
        if rng.random() < 0.5: rect(im, x, y, 3, 4, col); px(im, x, y, light_of(col))
        else: rect(im, x, y, 5, 2, col); px(im, x, y, light_of(col))
    if rng.random() < 0.4:                    # 찢어진 상자 조각
        x = rng.integers(x0 + 2, x1 - 7); y = rng.integers(y0 + 2, y1 - 5); rect(im, x, y, 5, 3, C['pack'][2]); px(im, x + 4, y + 2, C['pack'][0])
def light_of(c):
    for v in C.values():
        if c in v:
            i = v.index(c); return v[min(i + 1, len(v) - 1)]
    return c
def dark_of(c):
    for v in C.values():
        if c in v:
            i = v.index(c); return v[max(i - 1, 0)]
    return c
def deco_books(im, box, rng, nesw):
    x0, y0, x1, y1 = box; n, e, s, w = nesw
    horiz = (e or w) or not (n or s)
    cols = [C['blood'][1], C['denim'][2], C['olive'][2], C['sign'][1], C['light'][0], C['brick'][3], C['steel'][2], C['pack'][2]]
    if horiz:
        x = x0 + 1
        while x < x1 - 1:
            wdt = rng.integers(2, 4); h = rng.integers(14, 22); col = cols[rng.integers(len(cols))]
            if rng.random() < 0.12: x += wdt; continue
            yb = y1 - 3; rect(im, x, yb - h, wdt, h, col); vline(im, x, yb - h, h, light_of(col)); x += wdt
    else:
        y = y0 + 1
        while y < y1 - 1:
            hh = rng.integers(2, 4); wdt = rng.integers(14, 22); col = cols[rng.integers(len(cols))]
            if rng.random() < 0.12: y += hh; continue
            xb = x0 + 3; rect(im, xb, y, wdt, hh, col); hline(im, xb, y, wdt, light_of(col)); y += hh
def deco_cooler(im, box, rng, nesw):
    x0, y0, x1, y1 = box
    rect(im, x0 + 2, y0 + 2, x1 - x0 - 4, y1 - y0 - 4, C['steel'][1])
    for y in range(y0 + 7, y1 - 3, 7): hline(im, x0 + 2, y, x1 - x0 - 4, C['steel'][0])   # 빈 선반
    specks(im, rng, 6, [C['steel'][0], C['steel'][2]], region=(x0 + 2, y0 + 2, x1 - 2, y1 - 2))
    for _ in range(rng.integers(0, 2)):
        col = GOODS[rng.integers(len(GOODS))]; x = rng.integers(x0 + 4, x1 - 6); y = rng.integers(y0 + 4, y1 - 7)
        rect(im, x, y, 2, 4, col); px(im, x, y, C['light'][1])
    if rng.random() < 0.5:                    # 깨진 유리
        x = rng.integers(x0 + 4, x1 - 8); crack(im, rng, x, y0 + 4, 10, C['water'][2], ang=1.2)
    for x in range(x0 + 3, x1 - 3, 9): px(im, x, y0 + 3, C['water'][2]); px(im, x + 1, y0 + 4, C['water'][2])
def deco_kitchen(im, box, rng, nesw):
    x0, y0, x1, y1 = box; n, e, s, w = nesw
    if e: vline(im, x1 - 1, y0 + 1, y1 - y0 - 2, C['con'][3])
    if s: hline(im, x0 + 1, y1 - 1, x1 - x0 - 2, C['con'][3])
    specks(im, rng, 6, [C['con'][3], C['rust'][1]], region=(x0 + 1, y0 + 1, x1 - 1, y1 - 1))
def deco_cushion(im, box, rng, nesw, base, lo, hi):
    x0, y0, x1, y1 = box
    rect(im, x0 + 4, y0 + 4, x1 - x0 - 8, y1 - y0 - 8, base); hline(im, x0 + 4, y0 + 4, x1 - x0 - 8, hi); hline(im, x0 + 4, y1 - 5, x1 - x0 - 8, lo)
    vline(im, x1 - 5, y0 + 4, y1 - y0 - 8, lo)
    if rng.random() < 0.35:
        cx, cy = rng.integers(x0 + 8, x1 - 8), rng.integers(y0 + 8, y1 - 8); rect(im, cx, cy, 4, 3, C['light'][0]); px(im, cx + 1, cy + 1, C['light'][1])
def deco_lockers(im, box, rng, nesw):
    x0, y0, x1, y1 = box; n, e, s, w = nesw
    if not (n or s):
        vline(im, x0 + (x1 - x0) // 2, y0 + 1, y1 - y0 - 2, C['steel'][0])
        for x in (x0 + (x1 - x0) // 2 - 3, x0 + (x1 - x0) // 2 + 2): vline(im, x, y0 + 4, 4, C['con'][1])
    else:
        hline(im, x0 + 1, y0 + (y1 - y0) // 2, x1 - x0 - 2, C['steel'][0])
        for y in (y0 + (y1 - y0) // 2 - 3, y0 + (y1 - y0) // 2 + 2): hline(im, x0 + 4, y, 4, C['con'][1])
    for _ in range(2): r = rng.integers(x0 + 3, x1 - 5); q = rng.integers(y0 + 3, y1 - 5); rect(im, r, q, 2, 2, C['rust'][1])
def deco_rack(im, box, rng, nesw):
    x0, y0, x1, y1 = box; n, e, s, w = nesw
    cols = [C['denim'][2], C['blood'][1], C['olive'][2], C['light'][0], C['rag'][2], C['sign'][1], C['steel'][2]]
    if (e or w) or not (n or s):
        for x in range(x0 + 1, x1 - 1, 2):
            if rng.random() < 0.15: continue
            col = cols[rng.integers(len(cols))]; vline(im, x, y0 + 3, y1 - y0 - 6, col); px(im, x, y0 + 3, light_of(col))
        hline(im, x0, y0 + (y1 - y0) // 2, x1 - x0, C['steel'][1])
    else:
        for y in range(y0 + 1, y1 - 1, 2):
            if rng.random() < 0.15: continue
            col = cols[rng.integers(len(cols))]; hline(im, x0 + 3, y, x1 - x0 - 6, col)
        vline(im, x0 + (x1 - x0) // 2, y0, y1 - y0, C['steel'][1])

def kind_tiles(name, seed):
    rng0 = np.random.default_rng(seed); out = []
    for (n, e, s, w) in DIRS:
        rng = np.random.default_rng(seed * 31 + dkey(n, e, s, w))
        nesw = (n, e, s, w)
        if name == 'counter':
            im, b = surface(n, e, s, w, C['wood'][1], C['wood'][2], C['wood'][0]); deco_wood(im, b, rng, C['wood'][0])
        elif name == 'table':
            im, b = surface(n, e, s, w, C['wood'][2], C['wood'][3], C['wood'][1], 3); deco_wood(im, b, rng, C['wood'][1])
        elif name == 'kitchen':
            im, b = surface(n, e, s, w, C['con'][4], C['light'][0], C['con'][3], 1); deco_kitchen(im, b, rng, nesw)
        elif name == 'shelf':
            im, b = surface(n, e, s, w, C['pack'][1], C['pack'][2], C['pack'][0], 1); deco_goods(im, b, rng, nesw)
        elif name == 'bookshelf':
            im, b = surface(n, e, s, w, C['rag'][0], C['wood'][0], C['ink'][1], 1); deco_books(im, b, rng, nesw)
        elif name == 'cooler':
            im, b = surface(n, e, s, w, C['steel'][3], C['light'][1], C['steel'][2], 1); deco_cooler(im, b, rng, nesw)
        elif name == 'sofa_dark':
            im, b = surface(n, e, s, w, C['asph'][2], C['con'][1], C['asph'][0], 2); deco_cushion(im, b, rng, nesw, C['con'][1], C['asph'][1], C['con'][2])
        elif name == 'sofa_brown':
            im, b = surface(n, e, s, w, C['rag'][0], C['wood'][0], C['ink'][1], 3); deco_cushion(im, b, rng, nesw, C['wood'][1], C['wood'][0], C['wood'][2])
        elif name == 'sofa_beige':
            im, b = surface(n, e, s, w, C['dirt'][3], C['pack'][2], C['dirt'][2], 2); deco_cushion(im, b, rng, nesw, C['light'][0], C['pale'][1], C['light'][1])
        elif name == 'lockers':
            im, b = surface(n, e, s, w, C['steel'][2], C['steel'][3], C['steel'][1], 2); deco_lockers(im, b, rng, nesw)
        elif name == 'rack':
            im = canvas(T, T); b = (0 if w else 2, 6, T if e else T - 2, T - 6) if ((e or w) or not (n or s)) else (6, 0 if n else 2, T - 6, T if s else T - 2)
            deco_rack(im, b, rng, nesw)
        elif name == 'partition':
            im = canvas(T, T)
            if (n or s) and not (e or w): rect(im, 13, 0 if n else 2, 5, (T if s else T - 2) - (0 if n else 2), C['dirt'][3]); vline(im, 13, 0, T, C['wood'][3])
            else: rect(im, 0 if w else 2, 13, (T if e else T - 2) - (0 if w else 2), 5, C['dirt'][3]); hline(im, 0, 13, T, C['wood'][3])
            outline(im, C['ink'][1])
        elif name == 'whiteboard':
            im = canvas(T, T); x0 = 0 if w else 2; x1 = T if e else T - 2
            rect(im, x0, 2, x1 - x0, 7, C['light'][1]); hline(im, x0, 8, x1 - x0, C['con'][3]); outline(im, C['con'][1])
            hline(im, x0 + 3, 4, rng.integers(4, 12), C['blood'][1] if rng.random() < .5 else C['denim'][1])
            specks(im, rng, 3, [C['con'][4]], region=(x0, 3, x1, 8))
        else:
            raise KeyError(name)
        out.append(im)
    return out
KINDS = ['counter', 'table', 'kitchen', 'shelf', 'bookshelf', 'cooler', 'sofa_dark', 'sofa_brown', 'sofa_beige', 'lockers', 'rack', 'partition', 'whiteboard']

# ================= 단품 가구 (1칸) =================
def chair_base(seat, back, seat_hi):
    im = canvas(T, T)
    rrect(im, 9, 10, 14, 13, seat); rrect(im, 8, 5, 16, 5, back)
    outline(im, C['ink'][1]); hline(im, 10, 10, 12, seat_hi)
    return im
def chair_wood(): im = chair_base(C['wood'][2], C['wood'][1], C['wood'][3]); hline(im, 10, 16, 11, C['wood'][1]); return im
def chair_office():
    im = chair_base(C['con'][2], C['con'][1], C['con'][3])
    vline(im, 6, 12, 8, C['asph'][1]); vline(im, 25, 12, 8, C['asph'][1]); vline(im, 5, 12, 8, C['ink'][1]); vline(im, 26, 12, 8, C['ink'][1])
    rect(im, 13, 15, 5, 3, C['light'][0]); return im
def chair_gaming():
    im = chair_base(C['asph'][1], C['asph'][0], C['asph'][2])
    hline(im, 9, 6, 14, C['blood'][1]); vline(im, 9, 11, 11, C['blood'][1]); vline(im, 22, 11, 11, C['blood'][1])
    vline(im, 6, 11, 9, C['asph'][0]); vline(im, 25, 11, 9, C['asph'][0]); vline(im, 5, 11, 9, C['ink'][1]); vline(im, 26, 11, 9, C['ink'][1])
    return im
def stool():
    im = canvas(T, T); yy, xx = np.mgrid[0:T, 0:T]; m = np.hypot(xx - 15.5, yy - 15.5) < 7.5
    for y, x in zip(*np.nonzero(m)): px(im, x, y, C['wood'][1])
    for y, x in zip(*np.nonzero(np.hypot(xx - 14, yy - 14) < 4.5)): px(im, x, y, C['wood'][2])
    outline(im, C['ink'][1]); return im
def desk_unit(with_chair=True, paper=True):
    im = canvas(T, T); rect(im, 3, 3, 26, 14, C['wood'][2]); hline(im, 3, 3, 26, C['wood'][3]); hline(im, 3, 16, 26, C['wood'][1])
    if paper: rect(im, 11, 6, 9, 7, C['light'][1]); hline(im, 12, 8, 6, C['con'][3]); hline(im, 12, 10, 5, C['con'][3])
    if with_chair: rrect(im, 10, 19, 12, 9, C['con'][2]); hline(im, 11, 19, 10, C['con'][3]); rrect(im, 9, 27, 14, 3, C['con'][1])
    outline(im, C['ink'][1]); return im
def teacher_desk():
    im = canvas(T, T); rect(im, 2, 4, 28, 24, C['wood'][1]); hline(im, 2, 4, 28, C['wood'][2]); hline(im, 2, 27, 28, C['wood'][0])
    rect(im, 8, 9, 9, 12, C['light'][1]); hline(im, 9, 12, 6, C['con'][3]); hline(im, 9, 14, 6, C['con'][3]); hline(im, 9, 16, 5, C['con'][3])
    vline(im, 22, 9, 10, C['denim'][1]); outline(im, C['ink'][1]); return im
def pc_desk():
    im = canvas(T, T); rect(im, 1, 3, 30, 24, C['pack'][2]); hline(im, 1, 3, 30, C['light'][0]); hline(im, 1, 26, 30, C['pack'][1])
    rect(im, 8, 5, 16, 3, C['asph'][0]); hline(im, 8, 5, 16, C['asph'][2]); rect(im, 14, 8, 4, 2, C['asph'][1])   # 모니터
    rect(im, 8, 14, 15, 4, C['asph'][1]); hline(im, 9, 15, 13, C['asph'][2])                                    # 키보드
    rect(im, 25, 15, 2, 3, C['asph'][1])                                                                         # 마우스
    rect(im, 2, 6, 4, 18, C['asph'][0]); px(im, 3, 8, C['grass'][3])                                             # 본체
    outline(im, C['ink'][1]); return im
def fridge():
    im = canvas(T, T); rect(im, 2, 2, 28, 28, C['light'][0]); hline(im, 2, 2, 28, C['light'][1]); hline(im, 2, 29, 28, C['con'][4]); vline(im, 15, 2, 28, C['con'][3])
    vline(im, 12, 18, 6, C['con'][2]); vline(im, 19, 18, 6, C['con'][2]); outline(im, C['ink'][1])
    rect(im, 22, 6, 3, 3, C['rust'][1]); return im
def purifier():
    im = canvas(T, T); rect(im, 4, 4, 24, 24, C['light'][0]); outline(im, C['ink'][1])
    yy, xx = np.mgrid[0:T, 0:T]
    for y, x in zip(*np.nonzero(np.hypot(xx - 15.5, yy - 14.5) < 8)): px(im, x, y, C['water'][1])
    for y, x in zip(*np.nonzero(np.hypot(xx - 13, yy - 12) < 3)): px(im, x, y, C['water'][2])
    px(im, 8, 25, C['blood'][1]); px(im, 23, 25, C['denim'][1]); return im
def toilet():
    im = canvas(T, T); rect(im, 9, 2, 14, 7, C['light'][1]); hline(im, 9, 8, 14, C['con'][4])
    yy, xx = np.mgrid[0:T, 0:T]; m = np.hypot((xx - 15.5) / 7, (yy - 19) / 9) < 1
    for y, x in zip(*np.nonzero(m)): px(im, x, y, C['light'][1])
    for y, x in zip(*np.nonzero(np.hypot((xx - 15.5) / 4, (yy - 20) / 5.5) < 1)): px(im, x, y, C['water'][1])
    outline(im, C['ink'][1]); px(im, 14, 20, C['dirt'][1]); return im
def basin():
    im = canvas(T, T); rect(im, 5, 2, 22, 18, C['light'][1]); hline(im, 5, 19, 22, C['con'][4])
    rect(im, 9, 6, 14, 10, C['con'][4]); rect(im, 10, 7, 12, 8, C['water'][2]); px(im, 15, 11, C['ink'][1]); vline(im, 15, 3, 3, C['steel'][2])
    outline(im, C['ink'][1]); return im
def plant():
    rng = np.random.default_rng(9); im = canvas(T, T); yy, xx = np.mgrid[0:T, 0:T]
    for y, x in zip(*np.nonzero(np.hypot(xx - 15.5, yy - 17) < 9)): px(im, x, y, C['brick'][1])
    for y, x in zip(*np.nonzero(np.hypot(xx - 15.5, yy - 17) < 7)): px(im, x, y, C['dirt'][0])
    outline(im, C['ink'][1])
    for _ in range(5):   # 시든 잎
        a = rng.uniform(0, 6.28); cx, cy = 15.5 + np.cos(a) * 6, 15 + np.sin(a) * 6
        for y, x in zip(*np.nonzero(np.hypot(xx - cx, yy - cy) < 3.2)): px(im, x, y, C['olive'][rng.integers(0, 3)])
    for y, x in zip(*np.nonzero(np.hypot(xx - 15.5, yy - 15) < 2.5)): px(im, x, y, C['dirt'][3])
    return im
def trash():
    im = canvas(T, T); yy, xx = np.mgrid[0:T, 0:T]
    for y, x in zip(*np.nonzero(np.hypot(xx - 15.5, yy - 15.5) < 8)): px(im, x, y, C['asph'][2])
    for y, x in zip(*np.nonzero(np.hypot(xx - 15.5, yy - 15.5) < 6)): px(im, x, y, C['asph'][0])
    outline(im, C['ink'][1]); rect(im, 13, 13, 4, 3, C['light'][0]); px(im, 17, 15, C['rust'][1]); return im
def recycle():
    im = canvas(T, T)
    for i, col in enumerate((C['grass'][2], C['denim'][2], C['blood'][1])):
        x = 3 + i * 9; rect(im, x, 8, 8, 18, col); hline(im, x, 8, 8, light_of(col)); rect(im, x + 2, 12, 4, 3, C['light'][0])
    outline(im, C['ink'][1]); return im
def box_big():
    im = canvas(T, T); rect(im, 3, 3, 26, 26, C['pack'][2]); hline(im, 3, 3, 26, C['light'][0]); vline(im, 15, 3, 26, C['pack'][1]); rect(im, 14, 3, 4, 26, C['dirt'][3])
    hline(im, 3, 28, 26, C['pack'][1]); outline(im, C['ink'][1]); px(im, 8, 20, C['pack'][1]); px(im, 24, 9, C['pack'][1]); return im
def box_small():
    im = canvas(T, T)
    for (x, y) in ((3, 3), (14, 14)):
        rect(im, x, y, 14, 14, C['pack'][2]); hline(im, x, y, 14, C['light'][0]); vline(im, x + 7, y, 14, C['pack'][1]); hline(im, x, y + 13, 14, C['pack'][1])
    outline(im, C['ink'][1]); return im
def fruit_crate():
    rng = np.random.default_rng(5); im = canvas(T, T); rect(im, 2, 3, 28, 26, C['wood'][1]); vline(im, 15, 3, 26, C['wood'][0]); outline(im, C['ink'][1])
    for (x0, col) in ((4, C['blood'][1]), (17, C['grass'][3])):
        for y in range(5, 26, 5):
            for x in range(x0, x0 + 10, 5):
                if rng.random() < 0.8: continue
                c = col if rng.random() > 0.2 else C['dirt'][1]
                rect(im, x, y, 4, 4, c); px(im, x, y, light_of(c))
    return im
def shirt_stand(col):
    im = canvas(T, T); rect(im, 8, 7, 16, 18, col); rect(im, 4, 7, 4, 7, col); rect(im, 24, 7, 4, 7, col)
    hline(im, 8, 7, 16, light_of(col)); rect(im, 13, 7, 6, 2, dark_of(col)); outline(im, C['ink'][1]); return im
def folded(col):
    im = canvas(T, T); rect(im, 6, 10, 20, 12, C['steel'][1]); outline(im, C['ink'][1]); rect(im, 9, 12, 14, 8, col); hline(im, 9, 12, 14, light_of(col)); hline(im, 9, 16, 14, dark_of(col)); return im
def card_stand():
    im = canvas(T, T); rect(im, 3, 2, 26, 28, C['blood'][1]); outline(im, C['ink'][1]); rect(im, 5, 4, 22, 5, C['sign'][1])
    for i, col in enumerate((C['sign'][1], C['grass'][3], C['light'][0], C['denim'][2], C['rust'][2], C['blood'][0])):
        rect(im, 5 + (i % 3) * 8, 12 + (i // 3) * 8, 6, 6, col)
    return im
def mirror():
    im = canvas(T, T); rect(im, 6, 2, 20, 4, C['water'][2]); px(im, 9, 3, C['light'][1]); outline(im, C['con'][1]); return im
SINGLES = {
 'chair_wood': chair_wood, 'chair_office': chair_office, 'chair_gaming': chair_gaming,   # 아래(남쪽)를 보는 그림, 돌려서 4방향
 'stool': stool, 'student_desk': lambda: desk_unit(), 'teacher_desk': teacher_desk, 'pc_desk': pc_desk,
 'fridge': fridge, 'purifier': purifier, 'toilet': toilet, 'basin': basin, 'plant': plant, 'trash': trash, 'recycle': recycle,
 'box_big': box_big, 'box_small': box_small, 'fruit_crate': fruit_crate,
 'shirt_blue': lambda: shirt_stand(C['denim'][2]), 'shirt_red': lambda: shirt_stand(C['blood'][1]), 'shirt_purple': lambda: shirt_stand(C['denim'][1]),
 'folded_blue': lambda: folded(C['denim'][2]), 'folded_yellow': lambda: folded(C['sign'][1]), 'card_stand': card_stand, 'mirror': mirror,
}
ROTATE = {'chair_wood', 'chair_office', 'chair_gaming', 'student_desk', 'pc_desk', 'toilet', 'basin', 'mirror'}

# ================= 소품 (가구 위에 올림) =================
def register():
    im = canvas(T, T); rect(im, 8, 9, 16, 14, C['asph'][1]); hline(im, 8, 9, 16, C['asph'][2]); rect(im, 10, 11, 12, 4, C['water'][1])
    for y in (17, 19):
        for x in range(10, 22, 3): px(im, x, y, C['con'][3])
    outline(im, C['ink'][1]); return im
def pos_monitor():
    im = canvas(T, T); rect(im, 7, 9, 18, 4, C['asph'][0]); hline(im, 7, 9, 18, C['asph'][2]); rect(im, 14, 13, 4, 3, C['asph'][1])
    rect(im, 9, 18, 14, 3, C['asph'][1]); outline(im, C['ink'][1]); return im
def dishes():
    im = canvas(T, T); yy, xx = np.mgrid[0:T, 0:T]
    for (cx, cy) in ((10, 12), (16, 10), (22, 13)):
        for y, x in zip(*np.nonzero(np.hypot(xx - cx, yy - cy) < 3.5)): px(im, x, y, C['light'][1])
        px(im, cx, cy, C['con'][3])
    rect(im, 9, 19, 14, 3, C['light'][0]); outline(im, C['con'][1]); return im
def microwave():
    im = canvas(T, T); rect(im, 5, 7, 22, 17, C['light'][0]); rect(im, 7, 9, 13, 13, C['asph'][0]); px(im, 9, 10, C['steel'][2])
    for y in range(10, 20, 3): px(im, 23, y, C['asph'][1])
    outline(im, C['ink'][1]); return im
def coffee():
    im = canvas(T, T); rect(im, 7, 6, 18, 20, C['asph'][0]); hline(im, 7, 6, 18, C['asph'][2]); rect(im, 20, 9, 3, 2, C['blood'][1])
    yy, xx = np.mgrid[0:T, 0:T]
    for y, x in zip(*np.nonzero(np.hypot(xx - 15.5, yy - 19) < 3.2)): px(im, x, y, C['light'][1])
    outline(im, C['ink'][1]); return im
def stove():
    im = canvas(T, T); rect(im, 2, 2, 28, 26, C['asph'][1]); hline(im, 2, 2, 28, C['asph'][2]); yy, xx = np.mgrid[0:T, 0:T]
    for (cx, cy) in ((9, 9), (22, 9), (9, 20), (22, 20)):
        for y, x in zip(*np.nonzero(np.hypot(xx - cx, yy - cy) < 5)): px(im, x, y, C['ink'][0])
        for y, x in zip(*np.nonzero(np.hypot(xx - cx, yy - cy) < 2)): px(im, x, y, C['asph'][2])
    px(im, 6, 27, C['blood'][1]); px(im, 9, 27, C['con'][3]); outline(im, C['ink'][1]); return im
def sink():
    im = canvas(T, T); rect(im, 6, 8, 20, 16, C['steel'][2]); rect(im, 8, 10, 16, 12, C['steel'][1]); px(im, 15, 16, C['ink'][1])
    vline(im, 15, 3, 6, C['steel'][3]); outline(im, C['con'][1]); px(im, 12, 18, C['rust'][1]); return im
def monitor():
    im = canvas(T, T); rect(im, 7, 6, 18, 4, C['asph'][0]); hline(im, 7, 6, 18, C['asph'][2]); rect(im, 14, 10, 4, 3, C['asph'][1])
    rect(im, 9, 17, 14, 3, C['asph'][1]); outline(im, C['ink'][1]); return im
PROPS = {'register': register, 'pos': pos_monitor, 'dishes': dishes, 'microwave': microwave, 'coffee': coffee, 'stove': stove, 'sink': sink, 'monitor': monitor}

def rot(im, d):   # d: 0 남쪽(원본), 1 동쪽, 2 북쪽, 3 서쪽 을 바라봄
    return np.rot90(im, {0: 0, 1: 1, 2: 2, 3: 3}[d]).copy()

def furniture_tiles():
    out, names = [], []
    for k, kind in enumerate(KINDS):
        for t, (n, e, s, w) in zip(kind_tiles(kind, k + 1), DIRS):
            out.append(t); names.append(f'{kind}_{n}{e}{s}{w}')
    for nm, f in SINGLES.items():
        base = f()
        if nm in ROTATE:
            for r in range(4):
                out.append(rot(base, r)); names.append(f'{nm}_r{r}')
        else:
            out.append(base); names.append(nm)
    return out, names
def prop_tiles():
    out, names = [], []
    for nm, f in PROPS.items(): out.append(f()); names.append(nm)
    return out, names
