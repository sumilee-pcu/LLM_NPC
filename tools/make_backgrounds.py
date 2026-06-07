#!/usr/bin/env python3
"""미연시용 배경 생성기 v2 — 음영/깊이감/색보정으로 고급스럽게 (자체 제작, 라이선스 free).
출력: LLMnpc/Assets/Art/Generated/bg_{campus,classroom,park,sunset,hallway,cafe}.png
사용: python tools/make_backgrounds.py
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance, ImageChops

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(HERE), "LLMnpc", "Assets", "Art", "Generated")
os.makedirs(OUT, exist_ok=True)

W, H = 1920, 1080
SS = 2
CW, CH = W * SS, H * SS

def canvas():
    return Image.new("RGBA", (CW, CH), (255, 255, 255, 255))

def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

def vfill(img, box, top, bot):
    x0, y0, x1, y1 = box; d = ImageDraw.Draw(img); span = max(1, y1 - y0)
    for y in range(y0, y1):
        d.line([(x0, y), (x1, y)], fill=lerp(top, bot, (y - y0) / span) + (255,))

def sky3(img, top, mid, bot, h0, h1):
    vfill(img, (0, 0, CW, (h0 + h1) // 2), top, mid)
    vfill(img, (0, (h0 + h1) // 2, CW, h1), mid, bot)

def soft_shadow(img, box, alpha=90, blur=30):
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(lay).ellipse(box, fill=(18, 22, 32, alpha))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(blur * SS)))

def glow(img, cx, cy, r, color, alpha=120, blur=60):
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(lay).ellipse([cx - r, cy - r, cx + r, cy + r], fill=color + (alpha,))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(blur * SS)))

def clip(img, overlay, mask):
    overlay.putalpha(ImageChops.multiply(overlay.getchannel("A"), mask))
    img.alpha_composite(overlay)

def ellipse_mask(box):
    m = Image.new("L", (CW, CH), 0); ImageDraw.Draw(m).ellipse(box, fill=255); return m

def rect_mask(box):
    m = Image.new("L", (CW, CH), 0); ImageDraw.Draw(m).rectangle(box, fill=255); return m

def shade_box(img, box, mask, bot_a=120, top_a=0):
    """box 영역에 위→아래 어두워지는 음영을 mask 모양으로 입힘 (볼륨감)."""
    x0, y0, x1, y1 = box; lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    span = max(1, y1 - y0)
    for y in range(y0, y1):
        a = int(top_a + (bot_a - top_a) * ((y - y0) / span))
        d.line([(x0, y), (x1, y)], fill=(15, 18, 30, a))
    clip(img, lay, mask)

def cloud(img, cx, cy, s, col=(255, 255, 255)):
    soft_shadow(img, [cx - int(1.6 * s), cy + int(0.5 * s), cx + int(1.6 * s), cy + int(1.0 * s)], alpha=30, blur=20)
    d = ImageDraw.Draw(img)
    for dx, dy, r in [(-1.4, 0, 0.9), (-0.5, -0.45, 1.15), (0.6, -0.35, 1.05), (1.5, 0.05, 0.85), (0.4, 0.28, 1.0)]:
        rr = int(r * s); x, y = int(cx + dx * s), int(cy + dy * s)
        d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=col + (255,))
    # 밑면 살짝 음영
    box = [cx - int(1.6 * s), cy - int(0.6 * s), cx + int(1.7 * s), cy + int(0.6 * s)]
    shade_box(img, box, ellipse_mask(box), bot_a=55)

def tree(img, cx, base_y, s, foliage, trunk=(120, 86, 60)):
    soft_shadow(img, [cx - int(1.0 * s), base_y - int(0.12 * s), cx + int(1.5 * s), base_y + int(0.22 * s)], alpha=95, blur=22)
    d = ImageDraw.Draw(img); tw = int(0.16 * s)
    d.rectangle([cx - tw, base_y - int(1.05 * s), cx + tw, base_y], fill=trunk + (255,))
    d.rectangle([cx, base_y - int(1.05 * s), cx + tw, base_y], fill=tuple(int(c * 0.78) for c in trunk) + (255,))
    cy = base_y - int(1.5 * s); R = int(1.04 * s); box = [cx - R, cy - R, cx + R, cy + R]
    d.ellipse(box, fill=foliage + (255,))
    m = ellipse_mask(box)
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([box[0] + int(0.3 * s), box[1] + int(0.5 * s), box[2] + int(0.35 * s), box[3] + int(0.5 * s)],
                               fill=tuple(int(c * 0.62) for c in foliage) + (190,))
    clip(img, sh, m)
    hl = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(hl).ellipse([box[0] - int(0.2 * s), box[1] - int(0.25 * s), cx + int(0.1 * s), cy + int(0.1 * s)],
                               fill=(255, 255, 240, 70))
    clip(img, hl, m)

def vignette(img, strength=0.40):
    w, h = img.size
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).ellipse([-int(w * 0.12), -int(h * 0.12), int(w * 1.12), int(h * 1.12)], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(int(w * 0.10)))
    inv = ImageChops.invert(mask).point(lambda p: int(p * strength))
    dark = Image.new("RGBA", (w, h), (0, 0, 0, 0)); dark.putalpha(inv)
    img.alpha_composite(dark)

def finish(img, name):
    img = img.resize((W, H), Image.LANCZOS).convert("RGB")
    img = ImageEnhance.Color(img).enhance(0.90)      # 채도 살짝 낮춤(고급)
    img = ImageEnhance.Contrast(img).enhance(1.10)   # 대비 ↑
    img = ImageEnhance.Brightness(img).enhance(0.95) # 살짝 어둡게
    rgba = img.convert("RGBA"); vignette(rgba); rgba.convert("RGB").save(os.path.join(OUT, name))
    print("saved", name)

def haze(img, horizon, color=(225, 232, 236), band=0.10):
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(lay).rectangle([0, horizon - int(CH * band), CW, horizon + int(CH * 0.02)], fill=color + (120,))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(int(CH * 0.05))))

# ---------------- 캠퍼스 ----------------
def campus():
    img = canvas(); d = ImageDraw.Draw(img); horizon = int(CH * 0.62)
    sky3(img, (86, 150, 206), (150, 196, 230), (206, 222, 232), 0, horizon)
    glow(img, int(CW * 0.84), int(CH * 0.16), int(0.10 * CW), (255, 246, 210), 150, 70)
    d.ellipse([int(CW*0.80),int(CH*0.07),int(CW*0.90),int(CH*0.07)+int(0.10*CW)], fill=(255,248,214,255))
    cloud(img, int(CW * 0.22), int(CH * 0.15), 64 * SS); cloud(img, int(CW * 0.60), int(CH * 0.10), 50 * SS)
    haze(img, horizon)
    vfill(img, (0, horizon, CW, CH), (140, 184, 116), (96, 142, 86))  # 잔디 그라데이션
    # 건물
    bx0, bx1 = int(CW*0.30), int(CW*0.70); by0, by1 = int(CH*0.30), horizon
    soft_shadow(img, [bx0-int(0.03*CW), by1-int(0.02*CH), bx1+int(0.05*CW), by1+int(0.05*CH)], alpha=110, blur=30)
    d.polygon([(bx0-int(0.02*CW),by0),(bx1+int(0.02*CW),by0),(bx1,by0-int(0.06*CH)),(bx0,by0-int(0.06*CH))], fill=(168,92,82,255))
    rbox=[bx0-int(0.02*CW),by0-int(0.06*CH),bx1+int(0.02*CW),by0]; shade_box(img,rbox,rect_mask(rbox),bot_a=90)
    d.rectangle([bx0,by0,bx1,by1], fill=(226,212,188,255))
    wallbox=[bx0,by0,bx1,by1]; shade_box(img,wallbox,rect_mask(wallbox),bot_a=110,top_a=10)
    d.rectangle([bx0,by0,bx0+int(0.012*CW),by1], fill=(200,186,162,255))  # 좌측 엣지광
    cxx=(bx0+bx1)//2; ccy=by0+int(0.05*CH); cr=int(0.024*CH)
    d.ellipse([cxx-cr,ccy-cr,cxx+cr,ccy+cr], fill=(244,244,238,255), outline=(110,84,76), width=3*SS)
    cols,rows=6,3; pad=int((bx1-bx0)*0.06); gw=bx1-bx0-pad*2; gh=by1-by0-int(0.12*CH)
    cw=gw//cols; ch=gh//rows; oy=by0+int(0.10*CH)
    for r in range(rows):
        for c in range(cols):
            wx=bx0+pad+c*cw+int(cw*0.15); wy=oy+r*ch+int(ch*0.15)
            ww,wh=int(cw*0.7),int(ch*0.6)
            vfill(img,(wx,wy,wx+ww,wy+wh),(120,168,200),(168,206,230))
            d.line([(wx,wy),(wx+int(ww*0.5),wy+int(wh*0.5))], fill=(225,238,248,200), width=3*SS)  # 유리 반사
            d.rectangle([wx,wy,wx+ww,wy+wh], outline=(238,234,226), width=2*SS)
    dw=int((bx1-bx0)*0.10); d.rectangle([cxx-dw,by1-int(0.14*CH),cxx+dw,by1], fill=(132,98,80,255))
    # 길 (그라데이션)
    pbox=[(cxx-int(0.05*CW),by1),(cxx+int(0.05*CW),by1),(cxx+int(0.16*CW),CH),(cxx-int(0.16*CW),CH)]
    d.polygon(pbox, fill=(206,192,160,255))
    tree(img, int(CW*0.12), horizon+int(0.06*CH), 150*SS, (224,168,188)); tree(img, int(CW*0.88), horizon+int(0.06*CH), 150*SS, (224,168,188))
    tree(img, int(CW*0.21), horizon+int(0.11*CH), 120*SS, (92,150,92))
    finish(img, "bg_campus.png")

# ---------------- 교실 ----------------
def classroom():
    img = canvas(); d = ImageDraw.Draw(img); floor_y = int(CH*0.66)
    vfill(img,(0,0,CW,floor_y),(226,218,204),(208,200,186))     # 벽 음영
    d.rectangle([0,0,CW,int(0.06*CH)], fill=(196,188,174,255))  # 천장
    vfill(img,(0,floor_y,CW,CH),(196,166,118),(150,120,82))     # 마루
    d.rectangle([0,floor_y,CW,floor_y+int(0.012*CH)], fill=(120,94,62,255))
    wx0,wx1=int(CW*0.50),int(CW*0.96); wy0,wy1=int(CH*0.12),int(CH*0.52)
    soft_shadow(img,[wx0,wy1-int(0.01*CH),wx1,wy1+int(0.03*CH)],alpha=70,blur=18)
    d.rectangle([wx0-12*SS,wy0-12*SS,wx1+12*SS,wy1+12*SS], fill=(238,238,238,255))
    vfill(img,(wx0,wy0,wx1,wy1),(150,196,228),(212,232,246))
    glow(img,int((wx0+wx1)*0.5),int(wy0+0.06*CH),int(0.06*CW),(255,252,230),120,50)
    cloud(img,int((wx0+wx1)*0.40),int(wy0+0.10*CH),40*SS); cloud(img,int((wx0+wx1)*0.66),int(wy0+0.24*CH),32*SS)
    for k in range(1,4):
        x=wx0+(wx1-wx0)*k//4; d.line([(x,wy0),(x,wy1)], fill=(238,238,238,255), width=8*SS)
    midy=(wy0+wy1)//2; d.line([(wx0,midy),(wx1,midy)], fill=(238,238,238,255), width=8*SS)
    d.line([(wx0,wy0),(int((wx0+wx1)*0.4),midy)], fill=(255,255,255,90), width=5*SS)  # 유리 빛
    bx0,bx1=int(CW*0.06),int(CW*0.44); by0,by1=int(CH*0.14),int(CH*0.46)
    d.rectangle([bx0-12*SS,by0-12*SS,bx1+12*SS,by1+12*SS], fill=(158,116,76,255))
    vfill(img,(bx0,by0,bx1,by1),(82,120,100),(60,96,78))
    d.line([(bx0+int(0.03*CW),by1-int(0.05*CH)),(bx0+int(0.18*CW),by1-int(0.05*CH))], fill=(225,228,218,220), width=4*SS)
    for dx in [0.16,0.40,0.64,0.84]:
        ddx=int(CW*dx); ty=int(CH*0.74)
        soft_shadow(img,[ddx-int(0.07*CW),ty+int(0.12*CH),ddx+int(0.07*CW),ty+int(0.16*CH)],alpha=80,blur=14)
        d.rectangle([ddx-int(0.06*CW),ty,ddx+int(0.06*CW),ty+int(0.04*CH)], fill=(214,194,158,255))
        d.rectangle([ddx-int(0.06*CW),ty,ddx+int(0.06*CW),ty+int(0.012*CH)], fill=(228,210,176,255))
        d.rectangle([ddx-int(0.05*CW),ty+int(0.04*CH),ddx-int(0.035*CW),ty+int(0.14*CH)], fill=(168,138,100,255))
        d.rectangle([ddx+int(0.035*CW),ty+int(0.04*CH),ddx+int(0.05*CW),ty+int(0.14*CH)], fill=(168,138,100,255))
    finish(img, "bg_classroom.png")

# ---------------- 공원 ----------------
def park():
    img = canvas(); d = ImageDraw.Draw(img); horizon = int(CH*0.58)
    sky3(img,(120,150,196),(196,196,206),(240,224,206),0,horizon)
    glow(img,int(CW*0.5),int(CH*0.30),int(0.16*CW),(255,236,196),150,90)
    d.ellipse([int(CW*0.42),int(CH*0.18),int(CW*0.58),int(CH*0.18)+int(0.16*CW)], fill=(255,238,200,255))
    cloud(img,int(CW*0.18),int(CH*0.14),58*SS,(250,242,236)); cloud(img,int(CW*0.80),int(CH*0.20),48*SS,(250,242,236))
    haze(img,horizon,(232,224,214))
    vfill(img,(0,horizon,CW,CH),(132,176,110),(92,138,82))
    # 연못
    soft_shadow(img,[int(CW*0.60),int(CH*0.78),int(CW*0.92),int(CH*0.82)],alpha=60,blur=20)
    d.ellipse([int(CW*0.60),int(CH*0.66),int(CW*0.92),int(CH*0.80)], fill=(110,170,210,255))
    vfill(img,(int(CW*0.61),int(CH*0.67),int(CW*0.91),int(CH*0.79)),(150,200,232),(96,150,194))
    d.ellipse([int(CW*0.63),int(CH*0.685),int(CW*0.80),int(CH*0.72)], fill=(190,222,240,120))
    pbox=[(int(CW*0.42),horizon),(int(CW*0.50),horizon),(int(CW*0.70),CH),(int(CW*0.30),CH)]
    d.polygon(pbox, fill=(204,190,158,255))
    tree(img,int(CW*0.12),horizon+int(0.10*CH),160*SS,(224,168,188))
    tree(img,int(CW*0.30),horizon+int(0.04*CH),130*SS,(92,150,92))
    tree(img,int(CW*0.86),horizon+int(0.02*CH),120*SS,(92,150,92))
    bx,by=int(CW*0.20),int(CH*0.82); bw,bh=int(0.13*CW),int(0.018*CH)
    soft_shadow(img,[bx-int(0.01*CW),by+int(0.08*CH),bx+bw+int(0.02*CW),by+int(0.10*CH)],alpha=80,blur=12)
    d.rectangle([bx,by,bx+bw,by+bh], fill=(150,108,76,255)); d.rectangle([bx,by-int(0.06*CH),bx+bw,by-int(0.06*CH)+bh], fill=(150,108,76,255))
    for lx in [bx+int(0.01*CW),bx+bw-int(0.02*CW)]:
        d.rectangle([lx,by,lx+int(0.012*CW),by+int(0.08*CH)], fill=(120,84,58,255))
    finish(img, "bg_park.png")

# ---------------- 노을 ----------------
def sunset():
    img = canvas(); d = ImageDraw.Draw(img); horizon = int(CH*0.60)
    sky3(img,(112,86,138),(232,128,96),(255,196,128),0,horizon)
    glow(img,int(CW*0.5),int(CH*0.46),int(0.22*CW),(255,210,150),170,110)
    d.ellipse([int(CW*0.40),int(CH*0.34),int(CW*0.60),int(CH*0.34)+int(0.20*CW)], fill=(255,226,168,255))
    haze(img,horizon,(255,196,150))
    vfill(img,(0,horizon,CW,CH),(96,118,92),(60,78,64))
    bx0,bx1=int(CW*0.30),int(CW*0.70); by0=int(CH*0.34)
    soft_shadow(img,[bx0-int(0.03*CW),horizon-int(0.02*CH),bx1+int(0.05*CW),horizon+int(0.05*CH)],alpha=120,blur=30)
    d.polygon([(bx0-int(0.02*CW),by0),(bx1+int(0.02*CW),by0),(bx1,by0-int(0.05*CH)),(bx0,by0-int(0.05*CH))], fill=(70,58,78,255))
    d.rectangle([bx0,by0,bx1,horizon], fill=(96,80,104,255))
    wb=[bx0,by0,bx1,horizon]; shade_box(img,wb,rect_mask(wb),bot_a=130)
    for r in range(3):
        for c in range(6):
            wx=bx0+int((bx1-bx0)*(0.06+c*0.155)); wy=by0+int(0.05*CH)+r*int(0.07*CH)
            d.rectangle([wx,wy,wx+int(0.05*CW),wy+int(0.04*CH)], fill=(255,222,150,255))
            glow(img,wx+int(0.025*CW),wy+int(0.02*CH),int(0.03*CW),(255,224,150),60,18)
    tree(img,int(CW*0.12),horizon+int(0.06*CH),140*SS,(70,86,72)); tree(img,int(CW*0.88),horizon+int(0.06*CH),140*SS,(70,86,72))
    finish(img, "bg_sunset.png")

# ---------------- 복도 ----------------
def hallway():
    img = canvas(); d = ImageDraw.Draw(img); floor_y=int(CH*0.66)
    vfill(img,(0,0,CW,floor_y),(224,216,202),(206,198,184))
    d.rectangle([0,0,CW,int(0.05*CH)], fill=(196,188,174,255))
    vfill(img,(0,floor_y,CW,CH),(200,172,130),(150,120,84))
    d.rectangle([0,floor_y,CW,floor_y+int(0.012*CH)], fill=(120,94,62,255))
    for k in range(4):
        x0=int(CW*0.04)+k*int(CW*0.115)
        d.rectangle([x0-6*SS,int(CH*0.12)-6*SS,x0+int(CW*0.09)+6*SS,floor_y-int(0.04*CH)+6*SS], fill=(238,238,238,255))
        vfill(img,(x0,int(CH*0.12),x0+int(CW*0.09),floor_y-int(0.04*CH)),(150,196,228),(212,232,246))
        d.line([(x0,int(CH*0.12)),(x0+int(CW*0.05),int(CH*0.30))], fill=(255,255,255,90), width=5*SS)
        # 바닥에 창문 빛
        glow(img,x0+int(CW*0.045),floor_y+int(0.04*CH),int(0.05*CW),(255,250,220),50,30)
    for k in range(5):
        x0=int(CW*0.55)+k*int(CW*0.085)
        col=[(120,160,190),(190,140,140),(170,180,130),(150,150,190),(190,170,130)][k%5]
        soft_shadow(img,[x0,floor_y-int(0.01*CH),x0+int(CW*0.07),floor_y+int(0.02*CH)],alpha=70,blur=12)
        d.rectangle([x0,int(CH*0.30),x0+int(CW*0.07),floor_y], fill=col+(255,))
        lb=[x0,int(CH*0.30),x0+int(CW*0.07),floor_y]; shade_box(img,lb,rect_mask(lb),bot_a=90,top_a=10)
        d.rectangle([x0,int(CH*0.30),x0+int(CW*0.07),floor_y], outline=(110,110,118), width=4*SS)
        d.ellipse([x0+int(CW*0.052),int(CH*0.46),x0+int(CW*0.060),int(CH*0.475)], fill=(80,80,86,255))
    finish(img, "bg_hallway.png")

# ---------------- 카페 ----------------
def cafe():
    img = canvas(); d = ImageDraw.Draw(img); floor_y=int(CH*0.66)
    vfill(img,(0,0,CW,floor_y),(226,206,184),(206,184,160))
    vfill(img,(0,floor_y,CW,CH),(168,128,94),(132,98,70))
    wx0,wx1=int(CW*0.58),int(CW*0.94); wy0,wy1=int(CH*0.14),int(CH*0.50)
    vfill(img,(wx0,wy0,wx1,wy1),(176,210,236),(224,232,224))
    glow(img,int((wx0+wx1)*0.5),int((wy0+wy1)*0.5),int(0.10*CW),(255,250,228),90,60)
    d.rectangle([wx0-10*SS,wy0-10*SS,wx1+10*SS,wy1+10*SS], outline=(110,82,62), width=12*SS)
    d.line([((wx0+wx1)//2,wy0),((wx0+wx1)//2,wy1)], fill=(110,82,62,255), width=8*SS)
    d.line([(wx0,wy0),(int((wx0+wx1)*0.45),wy1)], fill=(255,255,255,70), width=6*SS)
    cb=[int(CW*0.06),int(CH*0.40),int(CW*0.40),floor_y]
    soft_shadow(img,[cb[0],floor_y-int(0.01*CH),cb[2],floor_y+int(0.03*CH)],alpha=90,blur=18)
    d.rectangle(cb, fill=(132,94,68,255)); shade_box(img,cb,rect_mask(cb),bot_a=120,top_a=10)
    d.rectangle([cb[0],cb[1],cb[2],cb[1]+int(0.02*CH)], fill=(150,110,80,255))
    for cx in [0.18,0.30,0.72,0.84]:
        x=int(CW*cx); d.line([(x,0),(x,int(CH*0.12))], fill=(80,62,52,255), width=5*SS)
        d.ellipse([x-int(0.022*CW),int(CH*0.11),x+int(0.022*CW),int(CH*0.165)], fill=(60,50,46,255))
        glow(img,x,int(CH*0.17),int(0.04*CW),(255,216,140),110,26)
    for cx in [0.50,0.74]:
        x=int(CW*cx); ty=int(CH*0.74)
        soft_shadow(img,[x-int(0.06*CW),ty+int(0.10*CH),x+int(0.06*CW),ty+int(0.13*CH)],alpha=80,blur=12)
        d.ellipse([x-int(0.05*CW),ty,x+int(0.05*CW),ty+int(0.05*CH)], fill=(196,156,116,255))
        d.ellipse([x-int(0.05*CW),ty,x+int(0.05*CW),ty+int(0.018*CH)], fill=(212,176,136,255))
        d.rectangle([x-int(0.006*CW),ty+int(0.05*CH),x+int(0.006*CW),ty+int(0.12*CH)], fill=(140,104,76,255))
    finish(img, "bg_cafe.png")

if __name__ == "__main__":
    campus(); classroom(); park(); sunset(); hallway(); cafe()
    print("done ->", OUT)
