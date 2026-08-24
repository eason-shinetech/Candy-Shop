# -*- coding: utf-8 -*-
"""Programmatic art generator for Candy Shop (art bible compliant).

Draws all UI chrome / icons / thumbs / portraits with Pillow using the
art-bible palette, thick cocoa outlines and glossy highlights.
Run:  python tools/artgen/generate_art.py
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Art", "UI")

# ---- Palette (art bible section 2) ----
CREAM = "#FFF6E8"
SUGAR_PINK = "#FF8FB8"
BERRY = "#E85A8C"
SKY_MINT = "#7EE0C6"
LEMON = "#FFE07A"
COCOA = "#6B3F2A"
GRAPE = "#A78BFA"
ICE = "#B8E8FF"
MAGNET_RED = "#FF6B6B"
WIND = "#C8F5D4"

WHITE = "#FFFFFF"


def hx(c):
    if isinstance(c, (tuple, list)):
        return tuple(c[:3])
    c = c.lstrip("#")
    return tuple(int(c[i:i + 2], 16) for i in (0, 2, 4))


def lighten(c, k=0.35):
    r, g, b = hx(c) if isinstance(c, str) else c
    return (min(255, int(r + (255 - r) * k)), min(255, int(g + (255 - g) * k)),
            min(255, int(b + (255 - b) * k)))


def darken(c, k=0.25):
    r, g, b = hx(c) if isinstance(c, str) else c
    return (int(r * (1 - k)), int(g * (1 - k)), int(b * (1 - k)))


def canvas(size):
    return Image.new("RGBA", size, (0, 0, 0, 0))


def rounded(draw, box, radius, fill=None, outline=None, width=1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def save(img, rel, opaque=False):
    path = os.path.join(OUT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if opaque:
        bg = Image.new("RGBA", img.size, hx(CREAM) + (255,))
        bg.alpha_composite(img)
        img = bg
    img.save(path)
    print("wrote", os.path.relpath(path, ROOT))


# ---- Shared drawing helpers ----

def gloss_top(img, box, color, alpha=90):
    """Soft glossy highlight band in the upper half of a shape's bounding box."""
    ov = canvas(img.size)
    d = ImageDraw.Draw(ov)
    x0, y0, x1, y1 = box
    h = max(6, int((y1 - y0) * 0.32))
    d.ellipse([x0 + (x1 - x0) * 0.12, y0 + h * 0.1, x1 - (x1 - x0) * 0.12, y0 + h * 1.5],
              fill=hx(lighten(color, 0.7)) + (alpha,))
    ov = ov.filter(ImageFilter.GaussianBlur(max(2, (x1 - x0) // 40)))
    img.alpha_composite(ov)


def sparkle(img, cx, cy, r, color=WHITE):
    d = ImageDraw.Draw(img)
    d.line([cx - r, cy, cx + r, cy], fill=color, width=max(2, r // 3))
    d.line([cx, cy - r, cx, cy + r], fill=color, width=max(2, r // 3))
    rr = int(r * 0.45)
    d.line([cx - rr, cy - rr, cx + rr, cy + rr], fill=color, width=max(1, r // 4))
    d.line([cx - rr, cy + rr, cx + rr, cy - rr], fill=color, width=max(1, r // 4))


# ---- UI chrome ----

def icon_star(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    pts = []
    cx = cy = size / 2
    R, r = size * 0.42, size * 0.18
    for i in range(10):
        ang = -math.pi / 2 + i * math.pi / 5
        rad = R if i % 2 == 0 else r
        pts.append((cx + rad * math.cos(ang), cy + rad * math.sin(ang)))
    # outline pass then fill
    d.polygon(pts, fill=hx(COCOA))
    o = size * 0.02
    inner = [(x, y + o * ((y - cy) / R)) for x, y in pts]
    d.polygon([(x - 0, y) for x, y in pts], fill=hx(COCOA))
    d.polygon([(cx + (x - cx) * 0.92, cy + (y - cy) * 0.92) for x, y in pts], fill=hx(LEMON))
    gloss_top(img, (cx - R, cy - R * 0.9, cx + R, cy + R * 0.2), LEMON)
    sparkle(img, cx + R * 0.55, cy - R * 0.55, int(R * 0.18))
    return img


def icon_coin(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    m = size * 0.09
    d.ellipse([m, m, size - m, size - m], fill=hx(COCOA))
    d.ellipse([m * 1.8, m * 1.8, size - m * 1.8, size - m * 1.8], fill=hx(darken(LEMON, 0.12)))
    d.ellipse([m * 2.1, m * 2.1, size - m * 2.1, size - m * 2.1], fill=hx(LEMON))
    # star stamp
    cx = cy = size / 2
    pts = []
    R, r = size * 0.16, size * 0.07
    for i in range(10):
        ang = -math.pi / 2 + i * math.pi / 5
        rad = R if i % 2 == 0 else r
        pts.append((cx + rad * math.cos(ang), cy + rad * math.sin(ang)))
    d.polygon(pts, fill=hx(darken(LEMON, 0.28)))
    gloss_top(img, (m, m, size - m, size * 0.45), LEMON)
    return img


def icon_pause(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    w = size * 0.16
    for x in (size * 0.30, size * 0.58):
        rounded(d, [x, size * 0.22, x + w, size * 0.78], radius=int(w / 2),
                fill=hx(SUGAR_PINK), outline=hx(COCOA), width=size // 110)
        # candy-stick stripes
        for k in range(3):
            y = size * (0.32 + 0.14 * k)
            d.line([x + 3, y, x + w - 3, y + w * 0.35], fill=hx(WHITE), width=size // 90)
    return img


def icon_stamina(size=512):
    """Glossy sugar-pink heart candy."""
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    m = s * 0.10
    r = s * 0.22
    body = [
        (m + r, m + s * 0.06),
    ]
    # heart via two circles + triangle
    cx = s / 2
    top = s * 0.30
    rl, rr_ = s * 0.21, s * 0.21
    cl, cr = cx - rl * 0.95, cx + rr_ * 0.95
    d.ellipse([cl - rl, top - rl, cl + rl, top + rl], fill=hx(COCOA))
    d.ellipse([cr - rr_, top - rr_, cr + rr_, top + rr_], fill=hx(COCOA))
    tri = [(s * 0.08, s * 0.42), (s * 0.92, s * 0.42), (cx, s * 0.88)]
    d.polygon(tri, fill=hx(COCOA))
    # pink inner (slightly smaller)
    k = 0.90
    d.ellipse([cl - rl * k, top - rl * k, cl + rl * k, top + rl * k], fill=hx(BERRY))
    d.ellipse([cr - rr_ * k, top - rr_ * k, cr + rr_ * k, top + rr_ * k], fill=hx(BERRY))
    tri2 = [(s * 0.115, s * 0.43), (s * 0.885, s * 0.43), (cx, s * 0.82)]
    d.polygon(tri2, fill=hx(BERRY))
    # cover seams
    d.ellipse([cl - rl * k, top - rl * k, cl + rl * k, top + rl * k], fill=hx(BERRY))
    # gloss
    gl = canvas((s, s))
    gd = ImageDraw.Draw(gl)
    gd.ellipse([cl - rl * 0.55, top - rl * 0.75, cl + rl * 0.15, top - rl * 0.05],
               fill=hx(lighten(SUGAR_PINK, 0.75)) + (170,))
    gl = gl.filter(ImageFilter.GaussianBlur(s // 60))
    img.alpha_composite(gl)
    return img


def bar_timer_bg(w=512, h=128):
    img = canvas((w, h))
    d = ImageDraw.Draw(img)
    rounded(d, [4, 4, w - 4, h - 4], radius=h // 2 - 4,
            fill=hx(CREAM), outline=hx(COCOA), width=h // 20)
    return img


def bar_timer_fill(w=512, h=128):
    img = canvas((w, h))
    grad = canvas((w, h))
    gd = ImageDraw.Draw(grad)
    c0, c1 = hx(LEMON), hx(SKY_MINT)
    for x in range(w):
        t = x / max(1, w - 1)
        col = tuple(int(c0[i] + (c1[i] - c0[i]) * t) for i in range(3))
        gd.line([x, 0, x, h], fill=col + (255,))
    mask = canvas((w, h))
    md = ImageDraw.Draw(mask)
    rounded(md, [4, 4, w - 4, h - 4], radius=h // 2 - 4, fill=(255, 255, 255, 255))
    img.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(img)
    rounded(d, [4, 4, w - 4, h - 4], radius=h // 2 - 4, outline=hx(COCOA), width=h // 20)
    # sugar shine line
    d.line([h // 3, h * 0.32, w - h // 3, h * 0.32], fill=(255, 255, 255, 130), width=h // 16)
    return img


def btn_primary(size=256, color=SUGAR_PINK):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    b = int(size * 0.06)
    rounded(d, [b, b + size * 0.04, size - b, size - b], radius=size // 4, fill=hx(darken(color, 0.30)))
    rounded(d, [b, b, size - b, size - size * 0.06], radius=size // 4,
            fill=hx(color), outline=hx(COCOA), width=size // 60)
    # frosting scallops along the top edge
    n = 7
    for i in range(n):
        cx = b + (size - 2 * b) * (i + 0.5) / n
        r = (size - 2 * b) / n * 0.62
        d.ellipse([cx - r, b - r * 0.55, cx + r, b + r],
                  fill=hx(lighten(color, 0.25)), outline=hx(COCOA), width=size // 100)
    gloss_top(img, (b, b, size - b, size * 0.5), color)
    return img


def panel_cream(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    b = size * 0.07
    rounded(d, [b, b, size - b, size - b], radius=size // 6, fill=hx(CREAM),
            outline=hx(COCOA), width=size // 70)
    # pink frosting border inside
    rounded(d, [b * 2, b * 2, size - b * 2, size - b * 2], radius=size // 7,
            outline=hx(SUGAR_PINK), width=size // 60)
    # sprinkles
    rnd = random.Random(7)
    cols = [SUGAR_PINK, SKY_MINT, LEMON, GRAPE]
    for _ in range(24):
        x = rnd.uniform(b * 3, size - b * 3)
        y = rnd.uniform(b * 3, size - b * 3)
        ang = rnd.uniform(0, math.pi)
        ln = size * 0.02
        dx, dy = math.cos(ang) * ln, math.sin(ang) * ln
        d.line([x - dx, y - dy, x + dx, y + dy],
               fill=hx(rnd.choice(cols)) + (200,), width=size // 160)
    return img


def frame_star_empty(size=512):
    img = icon_star(size)
    # repaint lemon with cream to make the empty frame
    px = img.load()
    lemon = hx(LEMON)
    for y in range(img.height):
        for x in range(img.width):
            if px[x, y][:3] == lemon:
                px[x, y] = hx(CREAM) + (px[x, y][3],)
    return img


def bg_main_menu():
    w, h = 1080, 1920
    img = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(img)
    # vertical pastel gradient sky: ice -> cream -> sugar pink floor
    top, mid, bot = hx(ICE), hx(CREAM), hx("#FFE3EE")
    for y in range(h):
        t = y / (h - 1)
        if t < 0.5:
            k = t / 0.5
            c = tuple(int(top[i] + (mid[i] - top[i]) * k) for i in range(3))
        else:
            k = (t - 0.5) / 0.5
            c = tuple(int(mid[i] + (bot[i] - mid[i]) * k) for i in range(3))
        d.line([0, y, w, y], fill=c + (255,))
    # clouds
    rnd = random.Random(3)
    for _ in range(9):
        cx, cy = rnd.uniform(0, w), rnd.uniform(h * 0.05, h * 0.45)
        rw = rnd.uniform(90, 220)
        rh = rw * 0.45
        cloud = canvas((w, h))
        cd = ImageDraw.Draw(cloud)
        for k in range(4):
            ox = rnd.uniform(-rw * 0.5, rw * 0.5)
            cd.ellipse([cx + ox - rw / 2, cy - rh / 2, cx + ox + rw / 2, cy + rh / 2],
                       fill=(255, 255, 255, 235))
        img.alpha_composite(cloud.filter(ImageFilter.GaussianBlur(6)))
    # wooden counter bottom third
    counter_y = int(h * 0.68)
    d.rounded_rectangle([-40, counter_y, w + 40, h + 40], radius=80,
                        fill=hx("#E8C39A"), outline=hx(COCOA), width=14)
    # wood grain lines
    for k in range(6):
        y = counter_y + 60 + k * (h - counter_y - 60) / 6
        d.line([20, y, w - 20, y + 8], fill=hx(darken("#E8C39A", 0.12)), width=6)
    # glass jars of candy on the counter
    def jar(jx, jy, jw, jh, candy_col):
        jd = ImageDraw.Draw(img)
        # jar body
        jd.rounded_rectangle([jx, jy, jx + jw, jy + jh], radius=jw // 5,
                             fill=(230, 245, 250, 150), outline=hx(COCOA), width=8)
        # lid
        jd.rounded_rectangle([jx + jw * 0.08, jy - jh * 0.10, jx + jw * 0.92, jy + jh * 0.06],
                             radius=jw // 10, fill=hx(SUGAR_PINK), outline=hx(COCOA), width=6)
        # candies inside
        rndj = random.Random(int(jx))
        for _ in range(26):
            bx = rndj.uniform(jx + jw * 0.12, jx + jw * 0.88)
            by = rndj.uniform(jy + jh * 0.45, jy + jh * 0.86)
            br = rndj.uniform(jw * 0.07, jw * 0.13)
            jd.ellipse([bx - br, by - br, bx + br, by + br], fill=hx(candy_col))
        # shine
        jd.line([jx + jw * 0.18, jy + jh * 0.25, jx + jw * 0.18, jy + jh * 0.75],
                fill=(255, 255, 255, 180), width=10)

    jar(w * 0.10, counter_y - 260, 190, 240, SUGAR_PINK)
    jar(w * 0.38, counter_y - 300, 220, 280, GRAPE)
    jar(w * 0.68, counter_y - 250, 185, 235, SKY_MINT)
    # frosting swirl ground accents
    for k in range(5):
        fx = w * (0.12 + 0.19 * k)
        d2 = ImageDraw.Draw(img)
        d2.ellipse([fx - 46, counter_y - 26, fx + 46, counter_y + 26],
                   fill=hx(lighten(SUGAR_PINK, 0.4)))
    return img


def bg_game_shop():
    img = bg_main_menu()
    w, h = img.size
    # darker edges vignette; keep center clear for the 3D pile
    vig = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vig)
    steps = 26
    for i in range(steps):
        a = int(2 + 26 * (i / steps) ** 2)
        vd.rectangle([i * 10, i * 16, w - i * 10, h - i * 16],
                     outline=(106, 63, 42, a), width=40)
    vig = vig.filter(ImageFilter.GaussianBlur(50))
    img.alpha_composite(vig)
    return img


def popup_signin(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    # gift box
    b = size * 0.14
    rounded(d, [b, size * 0.34, size - b, size * 0.86], radius=size // 12,
            fill=hx(GRAPE), outline=hx(COCOA), width=size // 90)
    # lid
    rounded(d, [b * 0.72, size * 0.26, size - b * 0.72, size * 0.42], radius=size // 14,
            fill=hx(lighten(GRAPE, 0.2)), outline=hx(COCOA), width=size // 90)
    # ribbon
    d.line([size / 2, size * 0.26, size / 2, size * 0.86], fill=hx(LEMON), width=size // 22)
    d.line([b, size * 0.52, size - b, size * 0.52], fill=hx(LEMON), width=size // 22)
    # bow
    d.ellipse([size * 0.36, size * 0.13, size * 0.49, size * 0.27], fill=hx(LEMON),
              outline=hx(COCOA), width=size // 110)
    d.ellipse([size * 0.51, size * 0.13, size * 0.64, size * 0.27], fill=hx(LEMON),
              outline=hx(COCOA), width=size // 110)
    # candies peeking out
    rnd = random.Random(11)
    for _ in range(7):
        cx = rnd.uniform(b * 1.4, size - b * 1.4)
        cy = size * 0.27
        r = rnd.uniform(size * 0.03, size * 0.05)
        d.ellipse([cx - r, cy - r, cx + r, cy + r],
                  fill=hx(rnd.choice([SUGAR_PINK, SKY_MINT, LEMON])), outline=hx(COCOA),
                  width=size // 200)
    gloss_top(img, (b, size * 0.34, size - b, size * 0.55), GRAPE)
    return img


# ---- Power-up icons ----

def icon_magnet(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    t = s * 0.20          # arm thickness
    R = s * 0.30          # outer radius of the U
    cx, cy = s / 2, s * 0.44
    # U shape: outer arc minus inner arc plus two legs
    d.arc([cx - R, cy - R, cx + R, cy + R], start=180, end=360, fill=hx(MAGNET_RED), width=int(t))
    d.rectangle([cx - R, cy, cx - R + t, cy + s * 0.26], fill=hx(MAGNET_RED))
    d.rectangle([cx + R - t, cy, cx + R, cy + s * 0.26], fill=hx(MAGNET_RED))
    # silver tips
    tip_h = s * 0.09
    d.rectangle([cx - R, cy + s * 0.17, cx - R + t, cy + s * 0.17 + tip_h], fill=hx(CREAM),
                outline=hx(COCOA), width=s // 140)
    d.rectangle([cx + R - t, cy + s * 0.17, cx + R, cy + s * 0.17 + tip_h], fill=hx(CREAM),
                outline=hx(COCOA), width=s // 140)
    # outlines around arcs (approximate with wider dark arc underneath)
    d.arc([cx - R - 4, cy - R - 4, cx + R + 4, cy + R + 4], start=180, end=360,
          fill=hx(COCOA), width=int(t) + 8)
    d.arc([cx - R, cy - R, cx + R, cy + R], start=180, end=360, fill=hx(MAGNET_RED), width=int(t))
    d.rectangle([cx - R - 4, cy - 4, cx - R + t + 4, cy + s * 0.26 + 4], fill=hx(COCOA))
    d.rectangle([cx - R, cy, cx - R + t, cy + s * 0.26], fill=hx(MAGNET_RED))
    d.rectangle([cx + R - t - 4, cy - 4, cx + R + 4, cy + s * 0.26 + 4], fill=hx(COCOA))
    d.rectangle([cx + R - t, cy, cx + R, cy + s * 0.26], fill=hx(MAGNET_RED))
    d.rectangle([cx - R - 4, cy + s * 0.17 - 4, cx - R + t + 4, cy + s * 0.17 + tip_h + 4],
                fill=hx(COCOA))
    d.rectangle([cx - R, cy + s * 0.17, cx - R + t, cy + s * 0.17 + tip_h], fill=hx(CREAM))
    d.rectangle([cx + R - t - 4, cy + s * 0.17 - 4, cx + R + 4, cy + s * 0.17 + tip_h + 4],
                fill=hx(COCOA))
    d.rectangle([cx + R - t, cy + s * 0.17, cx + R, cy + s * 0.17 + tip_h], fill=hx(CREAM))
    sparkle(img, cx + R * 1.25, cy - R * 0.85, s // 34)
    sparkle(img, cx - R * 1.3, cy - R * 0.5, s // 44)
    return img


def icon_tornado(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    levels = [(0.16, 0.62), (0.30, 0.54), (0.44, 0.44), (0.58, 0.33), (0.71, 0.21)]
    for i, (fy, fw) in enumerate(levels):
        y = s * fy
        w2 = s * fw
        col = WIND if i % 2 == 0 else lighten(SKY_MINT, 0.15)
        rounded(d, [s / 2 - w2 / 2, y, s / 2 + w2 / 2, y + s * 0.085],
                radius=s * 0.042, fill=hx(col), outline=hx(COCOA), width=s // 120)
    # candy bits swirling
    rnd = random.Random(5)
    for _ in range(9):
        fy = rnd.uniform(0.14, 0.74)
        side = rnd.choice([-1, 1])
        cx = s / 2 + side * s * rnd.uniform(0.12, 0.30)
        r = rnd.uniform(s * 0.02, s * 0.04)
        d.ellipse([cx - r, fy * s - r, cx + r, fy * s + r],
                  fill=hx(rnd.choice([SUGAR_PINK, LEMON, BERRY])), outline=hx(COCOA), width=s // 220)
    sparkle(img, s * 0.78, s * 0.16, s // 36)
    return img


def icon_freeze(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    cx, cy = s / 2, s * 0.42
    # snowflake popsicle: stick + flake head
    rounded(d, [cx - s * 0.045, s * 0.66, cx + s * 0.045, s * 0.90], radius=int(s * 0.045),
            fill=hx("#F0D9B8"), outline=hx(COCOA), width=s // 130)
    # rounded square body
    rounded(d, [s * 0.24, s * 0.12, s * 0.76, s * 0.70], radius=s // 8,
            fill=hx(ICE), outline=hx(COCOA), width=s // 90)
    # snowflake arms
    arms = 6
    for i in range(arms):
        ang = i * math.pi / arms
        x2 = cx + math.cos(ang) * s * 0.155
        y2 = cy + math.sin(ang) * s * 0.155
        d.line([cx, cy, x2, y2], fill=hx(WHITE), width=s // 60)
        # small branches
        for t2 in (0.6,):
            bx, by = cx + (x2 - cx) * t2, cy + (y2 - cy) * t2
            for da in (-0.5, 0.5):
                ex = bx + math.cos(ang + da) * s * 0.05
                ey = by + math.sin(ang + da) * s * 0.05
                d.line([bx, by, ex, ey], fill=hx(WHITE), width=s // 80)
    d.ellipse([cx - s * 0.045, cy - s * 0.045, cx + s * 0.045, cy + s * 0.045], fill=hx(WHITE))
    # frost sparkles
    sparkle(img, s * 0.80, s * 0.20, s // 40, color=hx(ICE))
    gloss_top(img, (s * 0.24, s * 0.12, s * 0.76, s * 0.38), ICE)
    return img


# ---- Customer portraits ----

def portrait_customer(idx=1, size=512):
    cfg = {
        1: dict(hair=SUGAR_PINK, acc=None, mouth="excited"),
        2: dict(hair=SKY_MINT, acc="hoodie", mouth="shy"),
        3: dict(hair="#FFD966", acc="cap", mouth="wave"),
        4: dict(hair=GRAPE, acc="bow", mouth="smile"),
        5: dict(hair=COCOA, acc=None, mouth="smile"),
        6: dict(hair=ICE, acc="beanie", mouth="smile"),
    }[idx]
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    cx = s / 2
    skin = "#FFE0C8"
    outline_w = s // 90

    # shoulders / shirt
    shirt = {"hoodie": SKY_MINT, "cap": LEMON}.get(cfg["acc"], SUGAR_PINK)
    d.rounded_rectangle([s * 0.16, s * 0.72, s * 0.84, s], radius=s // 8,
                        fill=hx(shirt), outline=hx(COCOA), width=outline_w)

    # head
    hr = s * 0.26
    hy = s * 0.44
    d.ellipse([cx - hr, hy - hr, cx + hr, hy + hr], fill=hx(skin), outline=hx(COCOA),
              width=outline_w)
    # hair cap
    d.pieslice([cx - hr, hy - hr, cx + hr, hy + hr], start=180, end=360, fill=hx(cfg["hair"]))
    d.ellipse([cx - hr * 0.98, hy - hr * 1.02, cx + hr * 0.98, hy - hr * 0.30],
              fill=hx(cfg["hair"]))
    # side hair
    d.ellipse([cx - hr * 1.06, hy - hr * 0.5, cx - hr * 0.55, hy + hr * 0.75],
              fill=hx(cfg["hair"]), outline=hx(COCOA), width=outline_w)
    d.ellipse([cx + hr * 0.55, hy - hr * 0.5, cx + hr * 1.06, hy + hr * 0.75],
              fill=hx(cfg["hair"]), outline=hx(COCOA), width=outline_w)

    # accessories
    if cfg["acc"] == "cap":
        d.chord([cx - hr * 1.02, hy - hr * 1.15, cx + hr * 1.02, hy - hr * 0.05],
                start=180, end=360, fill=hx(LEMON), outline=hx(COCOA), width=outline_w)
        rounded(d, [cx - hr * 1.25, hy - hr * 0.28, cx + hr * 0.05, hy - hr * 0.12],
                radius=s * 0.03, fill=hx(darken(LEMON, 0.08)), outline=hx(COCOA), width=outline_w)
    elif cfg["acc"] == "beanie":
        d.chord([cx - hr * 1.02, hy - hr * 1.18, cx + hr * 1.02, hy - hr * 0.02],
                start=180, end=360, fill=hx(ICE), outline=hx(COCOA), width=outline_w)
        rounded(d, [cx - hr * 1.02, hy - hr * 0.30, cx + hr * 1.02, hy - hr * 0.10],
                radius=s * 0.04, fill=hx(lighten(ICE, 0.2)), outline=hx(COCOA), width=outline_w)
        d.ellipse([cx - s * 0.035, hy - hr * 1.30, cx + s * 0.035, hy - hr * 1.02],
                  fill=hx(WHITE), outline=hx(COCOA), width=outline_w)
    elif cfg["acc"] == "bow":
        bx, by = cx + hr * 0.9, hy - hr * 0.75
        for dx in (-1, 1):
            d.ellipse([bx - s * 0.075 * (dx < 0) - (0 if dx > 0 else s * 0.075), by - s * 0.045,
                       bx + s * 0.075 * dx + (s * 0.075 if dx < 0 else 0), by + s * 0.045],
                      fill=hx(GRAPE), outline=hx(COCOA), width=outline_w)
        d.ellipse([bx - s * 0.02, by - s * 0.02, bx + s * 0.02, by + s * 0.02], fill=hx(BERRY))

    # face
    ey = hy + s * 0.005
    er = s * 0.028
    for ex in (cx - hr * 0.38, cx + hr * 0.38):
        d.ellipse([ex - er, ey - er * 1.25, ex + er, ey + er * 1.25], fill=hx(COCOA))
        d.ellipse([ex - er * 0.35, ey - er * 0.85, ex + er * 0.05, ey - er * 0.35],
                  fill=hx(WHITE))
    # blush
    bl = hx(lighten(SUGAR_PINK, 0.35)) + (160,)
    d.ellipse([cx - hr * 0.72, ey + er * 2, cx - hr * 0.40, ey + er * 3.4], fill=bl)
    d.ellipse([cx + hr * 0.40, ey + er * 2, cx + hr * 0.72, ey + er * 3.4], fill=bl)
    # mouth
    mx, my = cx, hy + hr * 0.42
    if cfg["mouth"] == "excited":
        d.pieslice([mx - s * 0.055, my - s * 0.05, mx + s * 0.055, my + s * 0.05],
                   start=0, end=180, fill=hx(BERRY), outline=hx(COCOA), width=outline_w // 2)
    elif cfg["mouth"] == "shy":
        d.arc([mx - s * 0.035, my - s * 0.02, mx + s * 0.035, my + s * 0.035],
              start=20, end=160, fill=hx(COCOA), width=outline_w // 2)
    elif cfg["mouth"] == "wave":
        d.arc([mx - s * 0.05, my - s * 0.04, mx + s * 0.05, my + s * 0.04],
              start=15, end=165, fill=hx(COCOA), width=outline_w // 2)
    else:
        d.arc([mx - s * 0.04, my - s * 0.03, mx + s * 0.04, my + s * 0.03],
              start=15, end=165, fill=hx(COCOA), width=outline_w // 2)

    # waving hand for #3
    if cfg["acc"] == "cap":
        d.ellipse([s * 0.70, s * 0.60, s * 0.82, s * 0.72], fill=hx(skin),
                  outline=hx(COCOA), width=outline_w)
    return img


# ---- Recipe book icons ----

def icon_recipe_book(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    rounded(d, [s * 0.22, s * 0.14, s * 0.78, s * 0.88], radius=s // 12,
            fill=hx(GRAPE), outline=hx(COCOA), width=s // 90)
    # spine
    d.line([s * 0.32, s * 0.14, s * 0.32, s * 0.88], fill=hx(darken(GRAPE, 0.2)), width=s // 45)
    # pages
    rounded(d, [s * 0.76, s * 0.16, s * 0.82, s * 0.86], radius=s // 40, fill=hx(CREAM))
    # lollipop on the cover
    d.line([s / 2, s * 0.48, s / 2, s * 0.78], fill=hx(COCOA), width=s // 55)
    d.ellipse([s * 0.41, s * 0.26, s * 0.59, s * 0.50], fill=hx(SUGAR_PINK),
              outline=hx(COCOA), width=s // 90)
    d.arc([s * 0.44, s * 0.29, s * 0.56, s * 0.47], start=-60, end=120,
          fill=hx(WHITE), width=s // 70)
    gloss_top(img, (s * 0.22, s * 0.14, s * 0.78, s * 0.4), GRAPE, alpha=60)
    return img


def icon_lock(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    # shackle
    d.arc([s * 0.30, s * 0.12, s * 0.70, s * 0.52], start=180, end=360,
          fill=hx(COCOA), width=s // 22)
    # body
    rounded(d, [s * 0.22, s * 0.40, s * 0.78, s * 0.84], radius=s // 10,
            fill=hx(LEMON), outline=hx(COCOA), width=s // 70)
    d.ellipse([s * 0.46, s * 0.54, s * 0.54, s * 0.64], fill=hx(COCOA))
    d.rectangle([s * 0.487, s * 0.60, s * 0.513, s * 0.72], fill=hx(COCOA))
    gloss_top(img, (s * 0.22, s * 0.40, s * 0.78, s * 0.6), LEMON)
    return img


def icon_check(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    d.ellipse([s * 0.10, s * 0.10, s * 0.90, s * 0.90], fill=hx(SKY_MINT),
              outline=hx(COCOA), width=s // 70)
    d.line([s * 0.30, s * 0.53, s * 0.44, s * 0.68], fill=hx(WHITE), width=s // 16)
    d.line([s * 0.44, s * 0.68, s * 0.72, s * 0.34], fill=hx(WHITE), width=s // 16)
    gloss_top(img, (s * 0.10, s * 0.10, s * 0.90, s * 0.45), SKY_MINT)
    return img


def icon_ad(size=512):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    # rounded retro TV
    rounded(d, [s * 0.12, s * 0.22, s * 0.88, s * 0.78], radius=s // 9,
            fill=hx(SUGAR_PINK), outline=hx(COCOA), width=s // 70)
    # screen
    rounded(d, [s * 0.20, s * 0.30, s * 0.68, s * 0.70], radius=s // 14,
            fill=hx(CREAM), outline=hx(COCOA), width=s // 100)
    # play triangle
    d.polygon([(s * 0.38, s * 0.38), (s * 0.38, s * 0.62), (s * 0.60, s * 0.50)],
              fill=hx(BERRY))
    # antenna
    d.line([s * 0.30, s * 0.22, s * 0.20, s * 0.08], fill=hx(COCOA), width=s // 60)
    d.line([s * 0.70, s * 0.22, s * 0.80, s * 0.08], fill=hx(COCOA), width=s // 60)
    gloss_top(img, (s * 0.12, s * 0.22, s * 0.88, s * 0.42), SUGAR_PINK)
    return img


# ---- Candy order thumbnails ----
# One 256 thumb per catalog asset; style derived from the family name so each
# candy type gets a distinct but coherent look.

FAMILY_STYLE = {
    "lollipop": ("swirl", SUGAR_PINK), "popsicle": ("pop", ICE), "cane": ("cane", BERRY),
    "gummy": ("gummybear", SUGAR_PINK), "jelly": ("dome", "#FFB3C6"), "chocolate": ("bar", COCOA),
    "wafer": ("wafer", "#F0D9B8"), "waffer": ("wafer", "#F0D9B8"), "waffle": ("waffle", "#E8B96F"),
    "balloon": ("balloon", MAGNET_RED), "donut": ("donut", SUGAR_PINK), "macaron": ("macaron", GRAPE),
    "cupcake": ("cupcake", SUGAR_PINK), "cake": ("slice", "#FFE3EE"), "cookie": ("cookie", "#D9A05B"),
    "cottoncandy": ("cloud", "#FFD1DE"), "marshmallow": ("pill", WHITE), "mint": ("round", "#BFF0D8"),
    "candy": ("round", LEMON), "star": ("starcandy", LEMON), "heart": ("heartcandy", BERRY),
    "bean": ("oval", SKY_MINT), "ring": ("ring", LEMON), "swirl": ("swirl", GRAPE),
    "strawberry": ("berry", BERRY), "cherry": ("cherry", MAGNET_RED), "berry": ("berry", GRAPE),
    "fruit": ("round", SKY_MINT), "icecreamcone": ("cone", "#F0D9B8"), "icecream": ("cone", "#FFF3DA"),
    "milkshake": ("shake", "#FFE9F1"), "mm_": ("bean", LEMON), "pretzel": ("pretzel", "#E0A45C"),
    "swiss_roll": ("roll", "#F3D9C3"), "sweet_bread": ("bun", "#F0C98C"), "sandwich": ("sandwich", ICE),
}


def family_of(type_id):
    low = type_id.lower()
    keys = sorted(FAMILY_STYLE.keys(), key=len, reverse=True)
    for k in keys:
        if k.rstrip("_") in low or (k.endswith("_") and low.startswith(k)):
            return FAMILY_STYLE[k]
    return ("round", LEMON)


def draw_shape(shape, color, size=256):
    img = canvas((size, size))
    d = ImageDraw.Draw(img)
    s = size
    ow = max(3, s // 60)
    C = hx(color)
    Dk = hx(darken(color, 0.18))

    if shape == "swirl":
        d.ellipse([s*0.14, s*0.14, s*0.86, s*0.86], fill=C, outline=hx(COCOA), width=ow)
        cx = cy = s/2
        for k in range(2):
            r0 = s*(0.24+0.13*k)
            d.arc([cx-r0, cy-r0, cx+r0, cy+r0], start=k*140, end=k*140+220, fill=Dk, width=ow+1)
        d.line([cx, cy-s*0.30, cx, s*0.92], fill=hx("#F0D9B8"), width=ow+2)
        d.line([cx, cy-s*0.30, cx, s*0.92], fill=hx(COCOA), width=2)
    elif shape == "pop":
        rounded(d, [s*0.36, s*0.12, s*0.64, s*0.66], radius=s*0.12, fill=C,
                outline=hx(COCOA), width=ow)
        d.line([s/2-2, s*0.66, s/2-2, s*0.92], fill=hx("#F0D9B8"), width=ow+3)
        d.line([s/2-2, s*0.66, s/2-2, s*0.92], fill=hx(COCOA), width=2)
        gloss_top(img, (s*0.36, s*0.12, s*0.64, s*0.40), color)
    elif shape == "cane":
        d.arc([s*0.30, s*0.10, s*0.74, s*0.50], start=180, end=360, fill=C, width=s*0.10)
        d.line([s*0.30, s*0.30, s*0.30, s*0.90], fill=C, width=s*0.10)
        for k in range(5):
            y0 = s*(0.16+0.14*k)
            d.line([s*0.24, y0, s*0.40, y0+s*0.06], fill=hx(WHITE), width=s*0.035)
        d.arc([s*0.30, s*0.10, s*0.74, s*0.50], start=180, end=360, fill=C, width=s*0.10)
    elif shape == "dome":
        d.pieslice([s*0.16, s*0.24, s*0.84, s*1.0], start=180, end=360, fill=C,
                   outline=hx(COCOA), width=ow)
        d.ellipse([s*0.40, s*0.16, s*0.60, s*0.32], fill=Dk)
        gloss_top(img, (s*0.16, s*0.24, s*0.84, s*0.55), color)
    elif shape == "bar":
        rounded(d, [s*0.18, s*0.30, s*0.82, s*0.74], radius=s*0.05, fill=C,
                outline=hx(COCOA), width=ow)
        for k in range(3):
            d.line([s*(0.32+0.16*k), s*0.32, s*(0.32+0.16*k), s*0.72], fill=Dk, width=ow)
    elif shape == "wafer":
        rounded(d, [s*0.16, s*0.26, s*0.84, s*0.78], radius=s*0.04, fill=C,
                outline=hx(COCOA), width=ow)
        d.line([s*0.16, s*0.44, s*0.84, s*0.44], fill=Dk, width=ow//2)
        d.line([s*0.16, s*0.60, s*0.84, s*0.60], fill=Dk, width=ow//2)
        d.line([s*0.34, s*0.26, s*0.34, s*0.78], fill=Dk, width=ow//2)
        d.line([s*0.66, s*0.26, s*0.66, s*0.78], fill=Dk, width=ow//2)
    elif shape == "waffle":
        d.ellipse([s*0.14, s*0.22, s*0.86, s*0.82], fill=C, outline=hx(COCOA), width=ow)
        for gx in (0.34, 0.5, 0.66):
            d.line([s*gx, s*0.24, s*gx, s*0.80], fill=Dk, width=ow//2)
        for gy in (0.36, 0.5, 0.64):
            d.line([s*0.16, s*gy, s*0.84, s*gy], fill=Dk, width=ow//2)
    elif shape == "balloon":
        d.ellipse([s*0.22, s*0.10, s*0.78, s*0.74], fill=C, outline=hx(COCOA), width=ow)
        d.polygon([(s*0.47, s*0.72), (s*0.53, s*0.72), (s*0.50, s*0.78)], fill=Dk)
        d.arc([s*0.42, s*0.74, s*0.58, s*0.94], start=270, end=90, fill=hx(COCOA), width=ow//2)
        gloss_top(img, (s*0.22, s*0.10, s*0.78, s*0.40), color)
    elif shape == "donut":
        d.ellipse([s*0.12, s*0.24, s*0.88, s*0.88], fill=C, outline=hx(COCOA), width=ow)
        d.ellipse([s*0.40, s*0.46, s*0.60, s*0.66], fill=hx(CREAM), outline=hx(COCOA), width=ow//2)
        rnd = random.Random(hash(color) % 999)
        for _ in range(8):
            sx = rnd.uniform(s*0.2, s*0.8); sy = rnd.uniform(s*0.3, s*0.8)
            ang = rnd.uniform(0, math.pi)
            dx2, dy2 = math.cos(ang)*s*0.03, math.sin(ang)*s*0.02
            d.line([sx-dx2, sy-dy2, sx+dx2, sy+dy2],
                   fill=hx(rnd.choice([SKY_MINT, LEMON, GRAPE])), width=ow//2)
    elif shape == "macaron":
        d.ellipse([s*0.16, s*0.20, s*0.84, s*0.48], fill=C, outline=hx(COCOA), width=ow)
        rounded(d, [s*0.18, s*0.42, s*0.82, s*0.56], radius=s*0.03, fill=hx(lighten(color, 0.55)),
                outline=hx(COCOA), width=ow//2)
        d.ellipse([s*0.16, s*0.50, s*0.84, s*0.78], fill=C, outline=hx(COCOA), width=ow)
    elif shape == "cupcake":
        d.pieslice([s*0.20, s*0.14, s*0.80, s*0.62], start=180, end=360, fill=hx(lighten(color, 0.3)),
                   outline=hx(COCOA), width=ow)
        d.polygon([(s*0.26, s*0.44), (s*0.74, s*0.44), (s*0.66, s*0.86), (s*0.34, s*0.86)],
                  fill=hx("#E8A0B4"), outline=hx(COCOA), width=ow)
        d.cherry_on = None
        d.ellipse([s*0.46, s*0.08, s*0.54, s*0.17], fill=hx(BERRY), outline=hx(COCOA), width=ow//2)
    elif shape == "slice":
        d.pieslice([s*0.14, s*0.10, s*0.86, s*0.94], start=210, end=330, fill=hx(lighten(color, 0.2)),
                   outline=hx(COCOA), width=ow)
        d.line([s*0.30, s*0.30, s*0.70, s*0.30], fill=hx(BERRY), width=ow+1)
    elif shape == "cookie":
        d.ellipse([s*0.14, s*0.22, s*0.86, s*0.84], fill=C, outline=hx(COCOA), width=ow)
        rnd = random.Random(4)
        for _ in range(7):
            chx = rnd.uniform(s*0.26, s*0.74); chy = rnd.uniform(s*0.34, s*0.72)
            r = rnd.uniform(s*0.025, s*0.05)
            d.ellipse([chx-r, chy-r, chx+r, chy+r], fill=hx(darken(color, 0.35)))
    elif shape == "cloud":
        rnd = random.Random(len(color))
        for _ in range(6):
            cx2 = rnd.uniform(s*0.28, s*0.72); cy2 = rnd.uniform(s*0.32, s*0.62)
            r = rnd.uniform(s*0.12, s*0.20)
            d.ellipse([cx2-r, cy2-r, cx2+r, cy2+r], fill=C)
        d.ellipse([s*0.16, s*0.30, s*0.84, s*0.72], outline=hx(COCOA), width=2)
    elif shape == "pill":
        rounded(d, [s*0.22, s*0.34, s*0.78, s*0.68], radius=s*0.17, fill=C,
                outline=hx(COCOA), width=ow)
    elif shape == "round":
        d.ellipse([s*0.20, s*0.20, s*0.80, s*0.80], fill=C, outline=hx(COCOA), width=ow)
        gloss_top(img, (s*0.20, s*0.20, s*0.80, s*0.50), color)
    elif shape == "starcandy":
        cx = cy = s/2
        pts = []
        for i in range(10):
            ang = -math.pi/2 + i*math.pi/5
            rad = s*0.34 if i % 2 == 0 else s*0.15
            pts.append((cx+rad*math.cos(ang), cy+rad*math.sin(ang)))
        d.polygon(pts, fill=C, outline=hx(COCOA), width=ow)
    elif shape == "heartcandy":
        cx = s/2
        top = s*0.32
        rl = s*0.17
        cl, cr = cx-rl*0.95, cx+rl*0.95
        d.ellipse([cl-rl, top-rl, cl+rl, top+rl], fill=C, outline=hx(COCOA), width=ow)
        d.ellipse([cr-rl, top-rl, cr+rl, top+rl], fill=C, outline=hx(COCOA), width=ow)
        d.polygon([(s*0.16, s*0.42), (s*0.84, s*0.42), (cx, s*0.82)], fill=C)
        d.line([s*0.16+ow, s*0.42, s*0.84-ow, s*0.42], fill=C, width=2)
    elif shape == "oval":
        d.ellipse([s*0.24, s*0.28, s*0.76, s*0.72], fill=C, outline=hx(COCOA), width=ow)
        gloss_top(img, (s*0.24, s*0.28, s*0.76, s*0.50), color)
    elif shape == "ring":
        d.ellipse([s*0.16, s*0.24, s*0.84, s*0.80], fill=C, outline=hx(COCOA), width=ow)
        d.ellipse([s*0.36, s*0.38, s*0.64, s*0.66], fill=(0, 0, 0, 0))
        # punch hole
        hole = canvas((s, s))
        hd = ImageDraw.Draw(hole)
        hd.ellipse([s*0.38, s*0.40, s*0.62, s*0.64], fill=(0, 0, 0, 255))
        img.paste((0, 0, 0, 0), (0, 0), hole)
        dd = ImageDraw.Draw(img)
        dd.ellipse([s*0.16, s*0.24, s*0.84, s*0.80], outline=hx(COCOA), width=ow)
    elif shape == "berry":
        d.ellipse([s*0.22, s*0.26, s*0.78, s*0.84], fill=C, outline=hx(COCOA), width=ow)
        d.polygon([(s*0.44, s*0.30), (s*0.56, s*0.30), (s*0.50, s*0.20)], fill=hx(SKY_MINT))
        d.line([s*0.50, s*0.20, s*0.50, s*0.10], fill=hx(COCOA), width=ow//2)
        gloss_top(img, (s*0.22, s*0.26, s*0.78, s*0.5), color)
    elif shape == "cherry":
        d.line([s*0.52, s*0.14, s*0.40, s*0.44], fill=hx(COCOA), width=ow)
        d.line([s*0.52, s*0.14, s*0.66, s*0.48], fill=hx(COCOA), width=ow)
        d.ellipse([s*0.24, s*0.44, s*0.56, s*0.80], fill=C, outline=hx(COCOA), width=ow)
        d.ellipse([s*0.52, s*0.48, s*0.82, s*0.82], fill=C, outline=hx(COCOA), width=ow)
        gloss_top(img, (s*0.24, s*0.44, s*0.56, s*0.60), color)
    elif shape == "cone":
        d.polygon([(s*0.30, s*0.44), (s*0.70, s*0.44), (s*0.50, s*0.92)], fill=C,
                  outline=hx(COCOA), width=ow)
        d.ellipse([s*0.26, s*0.14, s*0.74, s*0.52], fill=hx(lighten(color, 0.5)),
                  outline=hx(COCOA), width=ow)
        d.arc([s*0.30, s*0.22, s*0.70, s*0.46], start=200, end=340, fill=hx(WHITE), width=ow)
    elif shape == "shake":
        rounded(d, [s*0.34, s*0.34, s*0.66, s*0.86], radius=s*0.06, fill=C,
                outline=hx(COCOA), width=ow)
        d.line([s*0.60, s*0.36, s*0.72, s*0.12], fill=hx(COCOA), width=ow)
        d.ellipse([s*0.36, s*0.22, s*0.64, s*0.38], fill=hx(lighten(color, 0.4)),
                  outline=hx(COCOA), width=ow)
        d.ellipse([s*0.46, s*0.12, s*0.56, s*0.24], fill=hx(BERRY), outline=hx(COCOA), width=ow//2)
    elif shape == "pretzel":
        d.arc([s*0.26, s*0.24, s*0.74, s*0.72], start=90, end=270, fill=C, width=int(s*0.09))
        d.arc([s*0.26, s*0.24, s*0.74, s*0.72], start=-90, end=90, fill=C, width=int(s*0.09))
        d.ellipse([s*0.40, s*0.40, s*0.60, s*0.62], outline=hx(COCOA), width=ow//2)
        rnd = random.Random(9)
        for _ in range(6):
            sx = rnd.uniform(s*0.3, s*0.7); sy = rnd.uniform(s*0.3, s*0.7)
            d.ellipse([sx-s*0.02, sy-s*0.02, sx+s*0.02, sy+s*0.02], fill=hx(WHITE))
    elif shape == "roll":
        rounded(d, [s*0.16, s*0.32, s*0.84, s*0.72], radius=s*0.10, fill=C,
                outline=hx(COCOA), width=ow)
        d.ellipse([s*0.66, s*0.36, s*0.80, s*0.68], fill=hx(darken(color, 0.1)),
                  outline=hx(COCOA), width=ow//2)
        d.spiral = None
        d.arc([s*0.68, s*0.40, s*0.78, s*0.64], start=-90, end=180, fill=hx(BERRY), width=ow//2)
    elif shape == "bun":
        d.ellipse([s*0.18, s*0.30, s*0.82, s*0.80], fill=C, outline=hx(COCOA), width=ow)
        for gx in (0.34, 0.5, 0.66):
            d.arc([s*(gx-0.08), s*0.34, s*(gx+0.08), s*0.52], start=200, end=340,
                  fill=Dk, width=ow//2)
    elif shape == "sandwich":
        rounded(d, [s*0.16, s*0.34, s*0.84, s*0.50], radius=s*0.05, fill=C,
                outline=hx(COCOA), width=ow)
        rounded(d, [s*0.16, s*0.52, s*0.84, s*0.68], radius=s*0.05, fill=hx(lighten(color, 0.3)),
                outline=hx(COCOA), width=ow)
        d.line([s*0.16, s*0.51, s*0.84, s*0.51], fill=hx(BERRY), width=ow//2)
    else:
        d.ellipse([s*0.20, s*0.20, s*0.80, s*0.80], fill=C, outline=hx(COCOA), width=ow)

    return img


def generate_candy_thumbs(catalog_dir):
    import re
    count = 0
    out_dir = os.path.join(OUT, "Candies")
    os.makedirs(out_dir, exist_ok=True)
    for f in sorted(os.listdir(catalog_dir)):
        if not f.endswith(".asset"):
            continue
        tid = os.path.splitext(f)[0]
        shape, color = family_of(tid)
        img = draw_shape(shape, color, 256)
        save(img, os.path.join("Candies", f"icon_candy_{tid}.png"))
        count += 1
    print("candy thumbs:", count)


def main():
    # UI chrome
    save(icon_star(), "icon_star.png")
    save(frame_star_empty(), "frame_star_empty.png")
    save(icon_coin(), "icon_coin.png")
    save(icon_pause(), "icon_pause.png")
    save(icon_stamina(), "icon_stamina.png")
    save(bar_timer_bg(), "bar_timer_bg.png")
    save(bar_timer_fill(), "bar_timer_fill.png")
    save(btn_primary(color=SUGAR_PINK), "btn_primary.png", opaque=True)
    save(btn_primary(color=GRAPE), "btn_secondary.png", opaque=True)
    save(panel_cream(), "panel_cream.png")
    save(bg_main_menu(), "bg_main_menu.png", opaque=True)
    save(bg_game_shop(), "bg_game_shop.png", opaque=True)
    save(popup_signin(), "popup_signin.png")

    # power-ups
    save(icon_magnet(), "icon_magnet.png")
    save(icon_tornado(), "icon_tornado.png")
    save(icon_freeze(), "icon_freeze.png")

    # customers
    for i in range(1, 7):
        save(portrait_customer(i), os.path.join("Customers", f"portrait_customer_{i:02d}.png"))

    # recipe book set
    save(icon_recipe_book(), "icon_recipe_book.png")
    save(icon_lock(), "icon_lock.png")
    save(icon_check(), "icon_check.png")
    save(icon_ad(), "icon_ad.png")

    # candy thumbs from catalog assets
    generate_candy_thumbs(os.path.join(ROOT, "Assets", "Resources", "Data", "Catalog"))


if __name__ == "__main__":
    main()
