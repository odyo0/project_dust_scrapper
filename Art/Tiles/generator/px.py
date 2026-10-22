# 도트 그리기 도구 + 팔레트 (재질 62색만)
import numpy as np
from PIL import Image
T = 32
C = dict(
 ink=[(20,19,18),(31,29,27)], con=[(62,60,55),(85,82,74),(110,106,95),(138,133,119),(168,162,147)],
 asph=[(43,42,40),(55,53,50),(69,66,61)], dirt=[(59,46,34),(85,67,47),(110,90,63),(136,112,79)],
 grass=[(47,58,31),(69,83,42),(95,110,53),(126,138,69),(163,163,90)], brick=[(63,38,32),(90,52,38),(116,67,47),(140,86,57)],
 wood=[(76,53,36),(107,76,48),(143,106,66),(176,138,90)], rust=[(90,58,38),(138,85,48),(172,111,57)],
 steel=[(44,57,66),(73,91,101),(111,133,140),(157,176,176)], water=[(56,72,82),(90,113,122),(201,212,208)],
 denim=[(45,54,70),(68,80,103),(100,112,138)], olive=[(61,68,38),(89,96,58),(122,125,79)],
 pack=[(62,52,35),(92,77,51),(125,106,72)], rag=[(46,38,34),(70,58,51),(97,83,74)], pale=[(110,104,98),(148,139,128)],
 blood=[(78,23,20),(122,35,28)], sign=[(138,106,36),(201,154,58)], light=[(216,201,168),(239,230,207)])
ALL = {c for v in C.values() for c in v}
def canvas(w, h): return np.zeros((h, w, 4), np.uint8)
def px(im, x, y, c):
    if 0 <= y < im.shape[0] and 0 <= x < im.shape[1]: im[y, x, :3] = c; im[y, x, 3] = 255
def rect(im, x, y, w, h, c):
    for yy in range(y, y + h):
        for xx in range(x, x + w): px(im, xx, yy, c)
def hline(im, x, y, w, c): rect(im, x, y, w, 1, c)
def vline(im, x, y, h, c): rect(im, x, y, 1, h, c)
def rrect(im, x, y, w, h, c, r=1):
    rect(im, x, y, w, h, c)
    if r >= 1:
        for (cx, cy) in ((x, y), (x + w - 1, y), (x, y + h - 1), (x + w - 1, y + h - 1)):
            if 0 <= cy < im.shape[0] and 0 <= cx < im.shape[1]: im[cy, cx, 3] = 0
def mask(im): return im[..., 3] > 0
def outline(im, c, m=None):
    m = mask(im) if m is None else m
    from scipy import ndimage
    o = ndimage.binary_dilation(m, structure=[[0,1,0],[1,1,1],[0,1,0]]) & ~m
    for y, x in zip(*np.nonzero(o)): px(im, x, y, c)
def edge_inner(im, c_top, c_bot, m=None):
    # 윗변·왼변 1px 밝게, 아랫변·오른변 1px 어둡게 (입체감, 그림자 아님)
    m = mask(im) if m is None else m
    up = np.zeros_like(m); up[1:] = m[:-1]; dn = np.zeros_like(m); dn[:-1] = m[1:]
    lf = np.zeros_like(m); lf[:, 1:] = m[:, :-1]; rt = np.zeros_like(m); rt[:, :-1] = m[:, 1:]
    for y, x in zip(*np.nonzero(m & ~dn)):
        if c_bot: px(im, x, y, c_bot)
    for y, x in zip(*np.nonzero(m & ~rt)):
        if c_bot: px(im, x, y, c_bot)
    for y, x in zip(*np.nonzero(m & ~up)):
        if c_top: px(im, x, y, c_top)
    for y, x in zip(*np.nonzero(m & ~lf)):
        if c_top: px(im, x, y, c_top)
def specks(im, rng, n, cols, m=None, region=None):
    m = mask(im) if m is None else m
    ys, xs = np.nonzero(m)
    if region is not None:
        x0, y0, x1, y1 = region; k = (xs >= x0) & (xs < x1) & (ys >= y0) & (ys < y1); ys, xs = ys[k], xs[k]
    for _ in range(n):
        if not len(ys): return
        j = rng.integers(len(ys)); px(im, xs[j], ys[j], cols[rng.integers(len(cols))])
def crack(im, rng, x, y, length, col, m=None, ang=None):
    m = mask(im) if m is None else m
    ang = rng.uniform(0, 6.28) if ang is None else ang
    for _ in range(length):
        ang += rng.normal(0, 0.5); x += np.cos(ang); y += np.sin(ang)
        xi, yi = int(round(x)), int(round(y))
        if not (0 <= yi < im.shape[0] and 0 <= xi < im.shape[1]) or not m[yi, xi]: return
        px(im, xi, yi, col)
def paste(dst, src, x, y):
    h, w = src.shape[:2]; a = src[..., 3] > 0
    sub = dst[y:y+h, x:x+w]; sub[a] = src[a]
def check_palette(im):
    a = im[..., 3] > 0
    bad = {tuple(p) for p in im[a][:, :3]} - ALL
    assert not bad, bad
def save(im, path, scale=1):
    Image.fromarray(im).resize((im.shape[1]*scale, im.shape[0]*scale), Image.NEAREST).save(path)
