# 건물별 가구 배치 (칸 좌표). K=이어지는 가구 묶음, S=단품(r=반시계 90도 회전 수), P=소품
L = {}
def B(i):
    L[i] = dict(K=[], S=[], P=[]); return L[i]
def K(b, kind, x, y, w=1, h=1, g=None): b['K'].append((kind, x, y, w, h, g))
def S(b, nm, x, y, r=None): b['S'].append((nm, x, y, r))
def P(b, nm, x, y): b['P'].append((nm, x, y))
def row(b, nm, xs, y, r=None):
    for x in xs: S(b, nm, x, y, r)
def col(b, nm, x, ys, r=None):
    for y in ys: S(b, nm, x, y, r)
R = range
# 의자: r0 등받이 위(아래를 봄), r1 등받이 왼쪽(오른쪽을 봄), r2 등받이 아래(위를 봄), r3 등받이 오른쪽(왼쪽을 봄)

b = B(1)   # 학원
K(b, 'lockers', 1, 1, 1, 2); S(b, 'plant', 6, 1)
col(b, 'chair_office', 2, (2, 3), 1); col(b, 'chair_office', 5, (2, 3), 3); K(b, 'table', 3, 2, 2, 2)
for (wx, wy, ww, tx, ty, xs, ys) in ((9, 1, 3, 10, 2, R(8, 12), (3, 4)), (15, 1, 3, 16, 2, R(14, 18), (3, 4)),
                                     (8, 9, 3, 9, 10, R(8, 12), (11, 12)), (14, 9, 3, 15, 10, R(14, 18), (11, 12)), (20, 9, 4, 22, 10, R(20, 25), (11, 12))):
    K(b, 'whiteboard', wx, wy, ww, 1); S(b, 'teacher_desk', tx, ty)
    for y in ys: row(b, 'student_desk', xs, y, 0)
row(b, 'pc_desk', (20, 21), 1, 0); row(b, 'chair_office', (20, 21), 2, 2); K(b, 'lockers', 23, 1); S(b, 'purifier', 24, 1)
K(b, 'counter', 3, 10, 1, 2); P(b, 'pos', 3, 10)   # 안내 데스크: 정문 옆 세로
K(b, 'sofa_dark', 1, 10, 1, 2); K(b, 'sofa_dark', 5, 10, 1, 2); S(b, 'plant', 5, 12)

b = B(2)   # 카페·식당
K(b, 'counter', 3, 2, 6, 1); row(b, 'stool', R(3, 9), 3)
K(b, 'counter', 10, 1, 1, 7, 'L'); K(b, 'counter', 11, 7, 5, 1, 'L')
P(b, 'coffee', 10, 3); P(b, 'dishes', 10, 5); P(b, 'dishes', 12, 7)
K(b, 'counter', 14, 11, 1, 2); P(b, 'register', 14, 12)   # 계산대: 정문 옆 세로
K(b, 'kitchen', 11, 1, 7, 1); P(b, 'stove', 12, 1); P(b, 'sink', 15, 1); S(b, 'fridge', 18, 1)
for ty in (5, 9):
    K(b, 'table', 1, ty, 2, 1); S(b, 'chair_wood', 1, ty - 1, 0); S(b, 'chair_wood', 1, ty + 1, 2)
for tx in (6, 11):
    K(b, 'table', tx, 8, 1, 3); col(b, 'chair_wood', tx - 1, (8, 10), 1); col(b, 'chair_wood', tx + 1, (8, 10), 3)
K(b, 'table', 19, 10, 2, 1); S(b, 'chair_wood', 18, 10, 1); S(b, 'chair_wood', 21, 10, 3)
for (x, y) in ((16, 9), (1, 12), (24, 12)): S(b, 'plant', x, y)
K(b, 'sofa_brown', 24, 7, 1, 4)
for x in (20, 23): S(b, 'toilet', x, 1, 0); S(b, 'basin', x + 1, 1, 0)

b = B(3)   # 편의점
K(b, 'counter', 1, 1, 6, 1, 'A'); K(b, 'counter', 6, 2, 1, 3, 'A'); K(b, 'counter', 2, 4, 4, 1, 'A')
P(b, 'coffee', 2, 1); P(b, 'microwave', 3, 1); P(b, 'dishes', 4, 1); P(b, 'pos', 6, 2); P(b, 'dishes', 3, 4)
K(b, 'counter', 6, 10, 1, 3); P(b, 'register', 6, 12)   # 계산대: 정문 옆 세로
K(b, 'shelf', 8, 1, 4, 1); K(b, 'cooler', 12, 1, 6, 1); K(b, 'shelf', 18, 1, 4, 1)
K(b, 'shelf', 8, 4, 12, 2); K(b, 'shelf', 8, 8, 12, 2); K(b, 'shelf', 1, 6, 1, 4)
col(b, 'stool', 23, (3, 5, 7, 9)); K(b, 'counter', 24, 2, 1, 9)
K(b, 'cooler', 10, 12, 4, 1); K(b, 'shelf', 15, 12, 4, 1); S(b, 'recycle', 23, 12)

b = B(4)   # 옷가게
K(b, 'rack', 3, 1, 4, 1); K(b, 'rack', 4, 6, 3, 1); K(b, 'rack', 4, 9, 3, 1)
S(b, 'shirt_blue', 1, 3); S(b, 'shirt_red', 1, 6); S(b, 'shirt_purple', 1, 9); S(b, 'folded_blue', 8, 2); S(b, 'folded_yellow', 8, 8)
S(b, 'box_big', 11, 1); S(b, 'box_small', 12, 1); S(b, 'stool', 14, 3); K(b, 'kitchen', 16, 2); S(b, 'mirror', 15, 1, 0)
S(b, 'toilet', 18, 1, 0); S(b, 'basin', 20, 2, 3); K(b, 'lockers', 20, 7, 1, 2); S(b, 'mirror', 20, 10, 3)
K(b, 'counter', 11, 10, 1, 3); P(b, 'pos', 11, 11)   # 계산대: 정문 옆 세로
K(b, 'sofa_beige', 4, 12, 2, 1); S(b, 'plant', 1, 12)

b = B(5)   # 도서관
for (x, y, w, h) in ((2, 1, 4, 1), (9, 1, 6, 1), (9, 3, 6, 2), (1, 2, 1, 3), (1, 8, 1, 4), (8, 12, 10, 1)): K(b, 'bookshelf', x, y, w, h)
K(b, 'partition', 18, 1, 1, 3); K(b, 'partition', 19, 7, 1, 5)
row(b, 'pc_desk', R(19, 23), 1, 0); row(b, 'chair_office', R(19, 23), 2, 2)
K(b, 'table', 2, 6, 6, 1); P(b, 'monitor', 3, 6); P(b, 'monitor', 6, 6); row(b, 'chair_office', (4, 5), 5, 0)
K(b, 'table', 9, 8, 8, 2); row(b, 'chair_wood', R(9, 17), 7, 0); row(b, 'chair_wood', R(9, 17), 10, 2)
K(b, 'table', 22, 8, 1, 4); col(b, 'chair_wood', 21, R(8, 12), 1); col(b, 'chair_wood', 23, R(8, 12), 3)
S(b, 'plant', 24, 1); S(b, 'plant', 1, 12)
K(b, 'counter', 5, 10, 1, 3); P(b, 'pos', 5, 11)   # 대출 데스크: 정문 옆 세로

b = B(6)   # 마트
K(b, 'shelf', 3, 1, 6, 1); K(b, 'cooler', 9, 1, 4, 1); K(b, 'counter', 14, 1, 6, 1); P(b, 'dishes', 18, 1)
K(b, 'shelf', 1, 3, 1, 8); K(b, 'shelf', 5, 5, 2, 5); K(b, 'shelf', 9, 5, 2, 5)
K(b, 'counter', 13, 10, 1, 3); P(b, 'register', 13, 11); K(b, 'counter', 17, 10, 1, 3); P(b, 'register', 17, 11)   # 계산대 두 줄: 정문 양옆 세로
S(b, 'box_big', 14, 8); S(b, 'box_small', 14, 9); S(b, 'fruit_crate', 19, 8); S(b, 'fruit_crate', 20, 8); S(b, 'plant', 1, 12)
for (x, y) in ((23, 1), (24, 1), (22, 5), (23, 8), (24, 8), (23, 11), (24, 11)): S(b, 'box_big', x, y)
for (x, y) in ((24, 2), (24, 5), (24, 9)): S(b, 'box_small', x, y)

b = B(7)   # PC방
K(b, 'kitchen', 1, 1, 4, 1); P(b, 'sink', 2, 1); P(b, 'stove', 4, 1); K(b, 'cooler', 1, 4, 2, 1)
S(b, 'card_stand', 5, 7); S(b, 'card_stand', 5, 8)
K(b, 'counter', 3, 10, 1, 3); P(b, 'dishes', 3, 10); P(b, 'register', 3, 11)   # 계산대: 정문 옆 세로; S(b, 'purifier', 1, 11)
xs = R(8, 18)
row(b, 'pc_desk', xs, 1, 0); row(b, 'chair_gaming', xs, 2, 2)
row(b, 'chair_gaming', xs, 4, 0); row(b, 'pc_desk', xs, 5, 2); row(b, 'pc_desk', xs, 6, 0); row(b, 'chair_gaming', xs, 7, 2)
row(b, 'chair_gaming', xs, 9, 0); row(b, 'pc_desk', xs, 10, 2); row(b, 'pc_desk', xs, 11, 0); row(b, 'chair_gaming', xs, 12, 2)
K(b, 'sofa_dark', 24, 1, 1, 3); S(b, 'trash', 22, 2)
S(b, 'toilet', 21, 5, 0); S(b, 'toilet', 23, 5, 0); K(b, 'partition', 22, 5, 1, 2); S(b, 'basin', 24, 7, 3)
S(b, 'box_big', 24, 10); S(b, 'box_small', 24, 12); S(b, 'recycle', 22, 12)

b = B(8)   # 대형식당
K(b, 'kitchen', 1, 1, 8, 1); P(b, 'stove', 2, 1); P(b, 'stove', 3, 1); P(b, 'sink', 6, 1); S(b, 'fridge', 9, 1)
K(b, 'kitchen', 1, 3, 1, 2); P(b, 'stove', 1, 3)
K(b, 'counter', 11, 1, 8, 1); P(b, 'microwave', 13, 1); P(b, 'dishes', 16, 1); S(b, 'fridge', 19, 1)
K(b, 'counter', 14, 5, 5, 1); P(b, 'dishes', 15, 5)
K(b, 'counter', 21, 11, 1, 2); P(b, 'register', 21, 12)   # 계산대: 정문 옆 세로
for tx in (3, 7, 11):
    K(b, 'table', tx, 8, 1, 4); col(b, 'chair_wood', tx - 1, R(8, 12), 1); col(b, 'chair_wood', tx + 1, R(8, 12), 3)
K(b, 'table', 14, 9, 4, 1); row(b, 'chair_wood', R(14, 18), 8, 0); row(b, 'chair_wood', R(14, 18), 10, 2)
K(b, 'sofa_brown', 24, 7, 1, 5); S(b, 'plant', 16, 12)
S(b, 'box_big', 23, 1); S(b, 'box_small', 24, 1); S(b, 'box_big', 24, 3)
