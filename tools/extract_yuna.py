#!/usr/bin/env python3
"""
Sutemo PSD -> 유나 표정 스프라이트 추출기 (크로스플랫폼: Win/Mac/Linux)

사용법:
    1) Sutemo 캐릭터 PSD 를 내려받는다:
       https://sutemo.itch.io/female-character   (Female Sprite by Sutemo)
    2) 이 스크립트를 실행한다:
       python tools/extract_yuna.py  [PSD경로]
       - 경로를 안 주면 프로젝트 폴더 / Downloads 에서 "*Sutemo*.psd" 를 자동 탐색.
    3) 결과: LLMnpc/Assets/Art/Characters/yuna/expr/ 에 표정 PNG 11종 생성.
       Unity 가 자동 감지해서 PortraitController 에 매핑한다(YunaArtPostprocessor).

필요 패키지:  pip install psd-tools Pillow
"""
import os, sys, glob

def die(msg):
    print("\n[ERROR]", msg); sys.exit(1)

try:
    from psd_tools import PSDImage
    from PIL import Image
except ImportError:
    die("psd-tools / Pillow 가 필요합니다.  설치:  pip install psd-tools Pillow")

# ---- 경로 계산 ----
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
OUT  = os.path.join(REPO, "LLMnpc", "Assets", "Art", "Characters", "yuna", "expr")

def find_psd():
    if len(sys.argv) > 1:
        return sys.argv[1]
    spots = [REPO, os.path.expanduser("~/Downloads"), os.path.expanduser("~/Desktop"), os.getcwd()]
    for d in spots:
        hits = glob.glob(os.path.join(d, "*[Ss]utemo*.psd")) + glob.glob(os.path.join(d, "*Female*.psd"))
        if hits:
            return hits[0]
    return None

PSD_PATH = find_psd()
if not PSD_PATH or not os.path.exists(PSD_PATH):
    die("PSD 를 찾을 수 없습니다. 경로를 인자로 주세요:  python tools/extract_yuna.py \"<PSD 경로>\"")

print("[*] PSD :", PSD_PATH)
print("[*] OUT :", OUT)
psd = PSDImage.open(PSD_PATH)
W, H = psd.size

def find(path):
    node = psd
    for name in path:
        nxt = next((l for l in node if l.name == name), None)
        if nxt is None:
            die(f"레이어를 찾지 못함: '{name}'. PSD 버전이 다를 수 있습니다.")
        node = nxt
    return node

def paste(target, layer):
    layer.visible = True                       # 꺼진 레이어 강제 ON
    img = layer.composite(force=True)
    if img is None:
        return
    target.paste(img.convert("RGBA"), (layer.left, layer.top), img.convert("RGBA"))

# ---- 베이스(몸+머리+교복+볼터치+초커) ----
BASE = [
    ["Hair behind", "Twin Tail", "Silver"],
    ["Base Body"],
    ["Blush", "1"],
    ["Costume", "seifuku 2"],
    ["Hair front", "Twin tail / Short Hair", "Silver"],
    ["Accessories", "Choker"],
]
base = Image.new("RGBA", (W, H), (0, 0, 0, 0))
for p in BASE:
    paste(base, find(p))

# ---- 표정 11종 ----
EXPRESSIONS = ["Smile", "Shocked", "normal", "Delighted", "Sad",
               "Angry", "Smug", "Annoyed", "Sleepy", "Smile 2", "Laugh"]
os.makedirs(OUT, exist_ok=True)
for e in EXPRESSIONS:
    img = base.copy()
    paste(img, find(["Expression", e]))
    img.save(os.path.join(OUT, e.replace(" ", "_") + ".png"))
    print("    saved", e)

print(f"\n[OK] 표정 {len(EXPRESSIONS)}종 생성 완료 -> {OUT}")
print("     이제 Unity 를 열면 자동으로 매핑됩니다. (안 되면 Tools > LLM NPC > Expression Mapper > 씬에 적용)")
