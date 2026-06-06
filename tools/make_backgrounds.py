#!/usr/bin/env python3
"""미연시용 배경 생성기 (자체 제작, 라이선스 free).
출력: LLMnpc/Assets/Art/Generated/bg_campus.png, bg_classroom.png, bg_park.png
사용: python tools/make_backgrounds.py
"""
import os
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(HERE), "LLMnpc", "Assets", "Art", "Generated")
os.makedirs(OUT, exist_ok=True)

W, H = 1920, 1080
SS = 2                      # 슈퍼샘플링 배율
CW, CH = W * SS, H * SS

def new_canvas():
    return Image.new("RGB", (CW, CH), (255, 255, 255))

def vgradient(img, top, bot, y0=0, y1=None):
    y1 = CH if y1 is None else y1
    d = ImageDraw.Draw(img)
    span = max(1, y1 - y0)
    for y in range(y0, y1):
        t = (y - y0) / span
        c = tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3))
        d.line([(0, y), (CW, y)], fill=c)

def cloud(d, cx, cy, s, col=(255, 255, 255)):
    for dx, dy, r in [(-1.4,0,0.9),(-0.5,-0.4,1.1),(0.6,-0.3,1.0),(1.5,0.05,0.85),(0.4,0.25,1.0)]:
        rr = int(r*s)
        x, y = int(cx+dx*s), int(cy+dy*s)
        d.ellipse([x-rr, y-rr, x+rr, y+rr], fill=col)

def tree(d, cx, base_y, s, foliage=(110,185,110), trunk=(150,110,80)):
    tw = int(0.18*s)
    d.rectangle([cx-tw, base_y-int(1.1*s), cx+tw, base_y], fill=trunk)
    for dx, dy, r in [(0,-1.5,1.0),(-0.7,-1.1,0.8),(0.7,-1.1,0.8),(0,-0.9,0.9)]:
        rr=int(r*s); x,y=int(cx+dx*s),int(base_y-1.2*s+dy*s*0.4)
        d.ellipse([x-rr,y-rr,x+rr,y+rr], fill=foliage)

def finish(img, name):
    img = img.resize((W, H), Image.LANCZOS)
    img.save(os.path.join(OUT, name))
    print("saved", name)

# ---------------- 캠퍼스 (학교 외경) ----------------
def campus():
    img = new_canvas(); d = ImageDraw.Draw(img)
    horizon = int(CH*0.62)
    vgradient(img, (164,221,255), (226,245,255), 0, horizon)
    # 해 + 구름
    d.ellipse([CW-int(2.2*SS*120), int(0.5*SS*120), CW-int(0.4*SS*120), int(2.3*SS*120)], fill=(255,246,200))
    s=110*SS//100
    cloud(d, int(CW*0.22), int(CH*0.16), 70*SS, ); cloud(d, int(CW*0.62), int(CH*0.10), 55*SS)
    # 잔디
    d.rectangle([0,horizon,CW,CH], fill=(150,210,128))
    d.rectangle([0,horizon,CW,horizon+int(0.02*CH)], fill=(168,222,140))
    # 건물 (중앙 뒤)
    bx0,bx1 = int(CW*0.30), int(CW*0.70); by0,by1 = int(CH*0.30), horizon
    d.polygon([(bx0-int(0.02*CW),by0),(bx1+int(0.02*CW),by0),(bx1,by0-int(0.06*CH)),(bx0,by0-int(0.06*CH))], fill=(190,120,110)) # 지붕
    d.rectangle([bx0,by0,bx1,by1], fill=(245,232,212))
    # 시계
    cxx=(bx0+bx1)//2; ccy=by0+int(0.05*CH); cr=int(0.025*CH)
    d.ellipse([cxx-cr,ccy-cr,cxx+cr,ccy+cr], fill=(255,255,255), outline=(120,90,80), width=3*SS)
    # 창문 격자
    cols,rows=6,3; pad=int((bx1-bx0)*0.06)
    gw=(bx1-bx0-pad*2); gh=(by1-by0-int(0.12*CH))
    cw=gw//cols; ch=gh//rows; oy=by0+int(0.10*CH)
    for r in range(rows):
        for c in range(cols):
            wx=bx0+pad+c*cw+int(cw*0.15); wy=oy+r*ch+int(ch*0.15)
            d.rectangle([wx,wy,wx+int(cw*0.7),wy+int(ch*0.6)], fill=(150,200,235), outline=(235,235,235), width=2*SS)
    # 문
    dw=int((bx1-bx0)*0.10)
    d.rectangle([cxx-dw,by1-int(0.14*CH),cxx+dw,by1], fill=(150,110,90))
    # 길
    d.polygon([(cxx-int(0.05*CW),by1),(cxx+int(0.05*CW),by1),(cxx+int(0.16*CW),CH),(cxx-int(0.16*CW),CH)], fill=(236,226,196))
    # 벚나무 양옆
    tree(d, int(CW*0.12), horizon+int(0.06*CH), 150*SS, foliage=(250,200,215))
    tree(d, int(CW*0.88), horizon+int(0.06*CH), 150*SS, foliage=(250,200,215))
    tree(d, int(CW*0.20), horizon+int(0.10*CH), 120*SS)
    finish(img, "bg_campus.png")

# ---------------- 교실 (실내) ----------------
def classroom():
    img = new_canvas(); d = ImageDraw.Draw(img)
    floor_y=int(CH*0.66)
    d.rectangle([0,0,CW,floor_y], fill=(236,228,214))        # 벽
    d.rectangle([0,0,CW,int(0.06*CH)], fill=(222,214,200))    # 천장 띠
    d.rectangle([0,floor_y,CW,CH], fill=(206,172,124))        # 바닥
    d.rectangle([0,floor_y,CW,floor_y+int(0.012*CH)], fill=(150,120,80))
    # 큰 창문 (오른쪽) - 하늘
    wx0,wx1=int(CW*0.50),int(CW*0.96); wy0,wy1=int(CH*0.12),int(CH*0.52)
    d.rectangle([wx0-12*SS,wy0-12*SS,wx1+12*SS,wy1+12*SS], fill=(245,245,245))
    vgradient_box(d,wx0,wy0,wx1,wy1,(170,215,250),(225,242,255))
    cloud(d,int((wx0+wx1)*0.42),int(wy0+0.10*CH),46*SS); cloud(d,int((wx0+wx1)*0.62),int(wy0+0.22*CH),38*SS)
    # 창틀 격자
    for k in range(1,4):
        x=wx0+(wx1-wx0)*k//4; d.line([(x,wy0),(x,wy1)], fill=(245,245,245), width=8*SS)
    midy=(wy0+wy1)//2; d.line([(wx0,midy),(wx1,midy)], fill=(245,245,245), width=8*SS)
    # 칠판 (왼쪽)
    bx0,bx1=int(CW*0.06),int(CW*0.44); by0,by1=int(CH*0.14),int(CH*0.46)
    d.rectangle([bx0-10*SS,by0-10*SS,bx1+10*SS,by1+10*SS], fill=(168,126,84))  # 나무 틀
    d.rectangle([bx0,by0,bx1,by1], fill=(74,112,92))
    d.line([(bx0+int(0.03*CW),by1-int(0.05*CH)),(bx0+int(0.18*CW),by1-int(0.05*CH))], fill=(220,225,215), width=4*SS)
    # 책상 실루엣 (앞)
    for i,dx in enumerate([0.16,0.40,0.64,0.84]):
        ddx=int(CW*dx); ty=int(CH*0.74)
        d.rectangle([ddx-int(0.06*CW),ty,ddx+int(0.06*CW),ty+int(0.04*CH)], fill=(225,205,170))
        d.rectangle([ddx-int(0.05*CW),ty+int(0.04*CH),ddx-int(0.035*CW),ty+int(0.14*CH)], fill=(180,150,110))
        d.rectangle([ddx+int(0.035*CW),ty+int(0.04*CH),ddx+int(0.05*CW),ty+int(0.14*CH)], fill=(180,150,110))
    finish(img, "bg_classroom.png")

def vgradient_box(d, x0,y0,x1,y1, top, bot):
    span=max(1,y1-y0)
    for y in range(y0,y1):
        t=(y-y0)/span
        c=tuple(int(top[i]+(bot[i]-top[i])*t) for i in range(3))
        d.line([(x0,y),(x1,y)], fill=c)

# ---------------- 공원 ----------------
def park():
    img = new_canvas(); d = ImageDraw.Draw(img)
    horizon=int(CH*0.58)
    vgradient(img,(255,214,196),(255,242,230),0,horizon)   # 노을빛
    d.ellipse([int(CW*0.36),int(CH*0.10),int(CW*0.64),int(CH*0.10)+int(0.28*CW)], fill=(255,228,170))  # 큰 해
    cloud(d,int(CW*0.18),int(CH*0.14),60*SS,(255,238,228)); cloud(d,int(CW*0.78),int(CH*0.20),50*SS,(255,238,228))
    # 잔디
    d.rectangle([0,horizon,CW,CH], fill=(150,205,130))
    d.rectangle([0,horizon,CW,horizon+int(0.02*CH)], fill=(170,218,145))
    # 연못
    d.ellipse([int(CW*0.60),int(CH*0.66),int(CW*0.92),int(CH*0.80)], fill=(140,200,232))
    d.ellipse([int(CW*0.60),int(CH*0.66),int(CW*0.92),int(CH*0.72)], fill=(170,216,240))
    # 길 (구불)
    d.polygon([(int(CW*0.42),horizon),(int(CW*0.50),horizon),(int(CW*0.70),CH),(int(CW*0.30),CH)], fill=(236,226,196))
    # 나무들
    tree(d,int(CW*0.12),horizon+int(0.10*CH),160*SS,foliage=(250,200,215))
    tree(d,int(CW*0.30),horizon+int(0.04*CH),130*SS)
    tree(d,int(CW*0.86),horizon+int(0.02*CH),120*SS)
    # 벤치
    bx,by=int(CW*0.20),int(CH*0.82); bw,bh=int(0.13*CW),int(0.018*CH)
    d.rectangle([bx,by,bx+bw,by+bh], fill=(160,116,84))
    d.rectangle([bx,by-int(0.06*CH),bx+bw,by-int(0.06*CH)+bh], fill=(160,116,84))
    for lx in [bx+int(0.01*CW),bx+bw-int(0.02*CW)]:
        d.rectangle([lx,by,lx+int(0.012*CW),by+int(0.08*CH)], fill=(120,86,60))
    finish(img, "bg_park.png")

if __name__ == "__main__":
    campus(); classroom(); park()
    print("done ->", OUT)
