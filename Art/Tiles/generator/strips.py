# 상가 묶음: 작은 가게 3~4개가 벽을 같이 쓰며 옆으로 붙은 건물
import sys, os, json; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kit import *
OUT = sys.argv[1]
ft, fn = floor_tiles(); wt, wn = wall_tiles(); ut, un = furniture_tiles(); pt, pn = prop_tiles()
FI = {n: i + 1 for i, n in enumerate(fn)}; WI = {n: i + 1 for i, n in enumerate(wn)}; UI = {n: i + 1 for i, n in enumerate(un)}; PI = {n: i + 1 for i, n in enumerate(pn)}
IW = 6                     # 가게 안쪽 폭
H = 11                     # 0 뒷벽 / 1-3 창고·주방 / 4 칸막이 벽 / 5-9 매장 / 10 앞벽
def shop_layout(kind, x0, add, flip_back_door):
    K, S, P = add
    if kind == 'convenience':
        floors = ('tile_white', 'concrete')
        S('box_big', x0, 1); S('box_big', x0 + 1, 1); S('box_small', x0, 2); K('shelf', x0 + 3, 1, 2, 1)
        K('shelf', x0, 5, 1, 4); K('shelf', x0 + 2, 6, 2, 2); K('cooler', x0 + 5, 5, 1, 2)
        K('counter', x0 + 4, 8, 1, 2); P('register', x0 + 4, 9); S('recycle', x0, 9)
        back = x0 + 5
    elif kind == 'cafe':
        floors = ('wood', 'tile_dark')
        K('kitchen', x0, 1, 4, 1); P('sink', x0 + 1, 1); P('coffee', x0 + 3, 1); S('fridge', x0 + 5, 1)
        K('counter', x0 + 4, 7, 1, 3); P('coffee', x0 + 4, 7); P('register', x0 + 4, 9)
        for ty in (6, 8): K('table', x0 + 1, ty); S('chair_wood', x0, ty, 1); S('chair_wood', x0 + 2, ty, 3)
        S('plant', x0 + 5, 5)
        back = x0 + 4
    elif kind == 'convenience2':
        floors = ('tile_light', 'concrete')
        K('shelf', x0, 1, 1, 3); S('box_big', x0 + 3, 1); S('box_small', x0 + 4, 2)
        K('cooler', x0 + 5, 5, 1, 4); K('shelf', x0 + 1, 6, 1, 2); K('shelf', x0 + 3, 6, 1, 2)
        K('counter', x0 + 2, 8, 1, 2); P('register', x0 + 2, 9)
        back = x0 + 5
    elif kind == 'cafe2':
        floors = ('wood', 'tile_dark')
        K('kitchen', x0, 1, 3, 1); P('coffee', x0 + 1, 1); P('sink', x0 + 2, 1); S('fridge', x0 + 5, 1)
        K('counter', x0 + 1, 6, 1, 4); P('coffee', x0 + 1, 6); P('register', x0 + 1, 9)
        for y in (6, 7, 8): S('stool', x0 + 2, y)
        K('sofa_beige', x0 + 5, 5, 1, 3); K('table', x0 + 4, 6); S('chair_wood', x0 + 4, 5, 0); S('chair_wood', x0 + 4, 7, 2)
        S('plant', x0 + 5, 9)
        back = x0 + 4
    elif kind == 'restaurant2':
        floors = ('tile_gray', 'tile_dark')
        K('kitchen', x0, 1, 4, 1); P('stove', x0, 1); P('sink', x0 + 2, 1); K('kitchen', x0, 2); S('fridge', x0 + 5, 1)
        K('sofa_brown', x0 + 3, 5, 1, 2); K('table', x0 + 4, 5, 1, 2); K('sofa_brown', x0 + 5, 5, 1, 2)
        K('sofa_brown', x0, 6, 1, 2); K('table', x0 + 1, 6, 1, 2)
        K('counter', x0 + 4, 8, 1, 2); P('register', x0 + 4, 9); S('plant', x0, 9)
        back = x0 + 4
    else:   # restaurant
        floors = ('tile_tan', 'tile_dark')
        K('kitchen', x0, 1, 4, 1); P('stove', x0 + 1, 1); P('stove', x0 + 2, 1); P('sink', x0 + 3, 1); S('fridge', x0 + 5, 1)
        K('kitchen', x0, 2, 1, 2); P('dishes', x0, 3)
        K('table', x0, 6, 2, 1); S('chair_wood', x0, 5, 0); S('chair_wood', x0 + 1, 5, 0); S('chair_wood', x0, 7, 2); S('chair_wood', x0 + 1, 7, 2)
        K('table', x0 + 4, 5, 2, 1); S('chair_wood', x0 + 4, 6, 2); S('chair_wood', x0 + 5, 6, 2)
        K('counter', x0 + 4, 8, 1, 2); P('register', x0 + 4, 9); S('trash', x0, 9)
        back = x0 + 4
    return floors, back

def build_strip(kinds, seed, back_doors):
    rng = np.random.default_rng(seed)
    W = 1 + len(kinds) * (IW + 1)
    wallk = [[None] * W for _ in range(H)]    # 'w' 벽 / 'win' 창 / 'dw' 나무문 / 'dg' 유리문
    floorn = [[None] * W for _ in range(H)]
    lay = dict(K=[], S=[], P=[])
    add = (lambda kind, x, y, w=1, h=1, g=None: lay['K'].append((kind, x, y, w, h, g)),
           lambda nm, x, y, r=None: lay['S'].append((nm, x, y, r)),
           lambda nm, x, y: lay['P'].append((nm, x, y)))
    for x in range(W): wallk[0][x] = 'w'; wallk[H - 1][x] = 'w'
    for y in range(H): wallk[y][0] = 'w'; wallk[y][W - 1] = 'w'
    for k, kind in enumerate(kinds):
        x0 = 1 + k * (IW + 1)
        if k > 0:
            for y in range(H): wallk[y][x0 - 1] = 'w'
        for x in range(x0, x0 + IW): wallk[4][x] = 'w'
        pd = x0 + (3 if kind.endswith('~') else 2)
        wallk[4][pd] = 'dw'
        mir = kind.endswith('~'); kind = kind.rstrip('~')
        mx = (lambda x, w=1: x0 + IW - (x - x0) - w) if mir else (lambda x, w=1: x)
        mr = (lambda r: {1: 3, 3: 1}.get(r, r)) if mir else (lambda r: r)
        madd = (lambda kd, x, y, w=1, h=1, g=None: add[0](kd, mx(x, w), y, w, h, g),
                lambda nm, x, y, r=None: add[1](nm, mx(x), y, mr(r)),
                lambda nm, x, y: add[2](nm, mx(x), y))
        (ffront, fback), back = shop_layout(kind, x0, madd, False); back = mx(back)
        for y in range(1, 4):
            for x in range(x0, x0 + IW): floorn[y][x] = fback
        for y in range(5, 10):
            for x in range(x0, x0 + IW): floorn[y][x] = ffront
        floorn[4][pd] = fback
        for x in range(x0, x0 + IW): wallk[H - 1][x] = 'win'
        fd = x0 + (2 if mir else 3)
        wallk[H - 1][fd] = 'dg'; floorn[H - 1][fd] = ffront
        if k in back_doors: wallk[0][back] = 'dw'; floorn[0][back] = fback
    isw = lambda r, c: 0 <= r < H and 0 <= c < W and wallk[r][c] in ('w', 'win')
    floor = [[0] * W for _ in range(H)]; wall = [[0] * W for _ in range(H)]
    for r in range(H):
        for c in range(W):
            if floorn[r][c]:
                u = rng.random(); wear = 0 if u < 0.6 else (1 if u < 0.96 else 2)
                floor[r][c] = FI[f'{floorn[r][c]}_{wear if not wallk[r][c] else 0}']
            k = wallk[r][c]
            if k == 'w': wall[r][c] = WI['wall_%d%d%d%d' % (isw(r - 1, c), isw(r, c + 1), isw(r + 1, c), isw(r, c - 1))]
            elif k == 'win': wall[r][c] = WI['window_h_broken' if rng.random() < 0.3 else 'window_h']
            elif k == 'dw': wall[r][c] = WI['door_wood_h']
            elif k == 'dg': wall[r][c] = WI['door_glass_h']
    furn = [[0] * W for _ in range(H)]; props = [[0] * W for _ in range(H)]; grp = {}
    for gi, (kind, x, y, w, h, g) in enumerate(lay['K']):
        for yy in range(y, y + h):
            for xx in range(x, x + w): grp[(xx, yy)] = (kind, g or f'_{gi}')
    def setf(x, y, v, what):
        if furn[y][x] or wallk[y][x]: print('  ! 겹침', what, x, y)
        furn[y][x] = v
    for (xx, yy), key in grp.items():
        same = lambda a, b: grp.get((a, b)) == key
        setf(xx, yy, UI['%s_%d%d%d%d' % (key[0], same(xx, yy - 1), same(xx + 1, yy), same(xx, yy + 1), same(xx - 1, yy))], key[0])
    for (nm, x, y, r) in lay['S']: setf(x, y, UI[nm if r is None else f'{nm}_r{r}'], nm)
    for (nm, x, y) in lay['P']:
        if not furn[y][x]: print('  ! 소품 밑 가구 없음', nm, x, y)
        props[y][x] = PI[nm]
    # 문 앞뒤가 막혔는지
    for r in range(H):
        for c in range(W):
            if wallk[r][c] in ('dw', 'dg'):
                for rr in (r - 1, r + 1):
                    if 0 <= rr < H and furn[rr][c]: print('  ! 문 앞 막힘', c, r)
    from collections import deque
    passable = lambda r, c: 0 <= r < H and 0 <= c < W and floorn[r][c] and not furn[r][c] and wallk[r][c] in (None, 'dw', 'dg')
    doors = [(r, c) for r in range(H) for c in range(W) if wallk[r][c] in ('dw', 'dg')]
    for (r0, c0) in [d for d in doors if wallk[d[0]][d[1]] == 'dg']:
        seen = {(r0, c0)}; q = deque([(r0, c0)])
        while q:
            r, c = q.popleft()
            for rr, cc in ((r + 1, c), (r - 1, c), (r, c + 1), (r, c - 1)):
                if (rr, cc) not in seen and passable(rr, cc): seen.add((rr, cc)); q.append((rr, cc))
        x0 = 1 + ((c0 - 1) // (IW + 1)) * (IW + 1)
        for d in doors:
            if x0 <= d[1] < x0 + IW and d not in seen: print('  ! 길 막힘', kinds, '정문', c0, '-> 문', d)
        for r in range(H):
            for c in range(x0, x0 + IW):
                if props[r][c] in (PI['register'], PI['pos']):
                    for cc in (c - 1, c + 1):
                        if (r, cc) not in seen: print('  ! 계산대 옆 설 자리 없음/못 감', kinds, (c, r), '->', (cc, r))
    return dict(w=W * T, h=H * T, cols=W, rows=H, Floor=floor, Wall=wall, Furniture=furn, Props=props)

strips = [build_strip(['convenience', 'cafe', 'restaurant', 'cafe~'], 11, {0, 2}),
          build_strip(['restaurant~', 'convenience', 'cafe'], 12, {1})]
R = np.random.default_rng(2026)
BASE = [['convenience', 'convenience2'], ['cafe', 'cafe2'], ['restaurant', 'restaurant2']]
for k in range(10):
    n = 4 if k % 2 == 0 else 3
    groups = list(R.permutation(3))
    while len(groups) < n: groups.append(int(R.integers(3)))
    groups = list(R.permutation(groups))
    kinds = [BASE[g][int(R.integers(2))] + ('~' if R.random() < 0.5 else '') for g in groups]
    backs = set(int(x) for x in R.choice(n, size=int(R.integers(1, 3)), replace=False))
    print(k + 3, kinds, sorted(backs))
    strips.append(build_strip(kinds, 100 + k, backs))
for i, b in enumerate(strips, 1):
    pv = canvas(b['w'], b['h'])
    for grid, ts in ((b['Floor'], ft), (b['Wall'], wt), (b['Furniture'], ut), (b['Props'], pt)):
        for r in range(b['rows']):
            for c in range(b['cols']):
                if grid[r][c]: paste(pv, ts[grid[r][c] - 1], c * T, r * T)
    check_palette(pv); Image.fromarray(pv).save(f'{OUT}/strip_{i}.png')
json.dump(dict(b=strips), open(f'{OUT}/strips.json', 'w'))
print('ok')
