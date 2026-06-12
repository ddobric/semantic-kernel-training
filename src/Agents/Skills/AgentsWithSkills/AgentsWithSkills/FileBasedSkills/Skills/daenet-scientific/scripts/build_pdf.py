#!/usr/bin/env python3
"""
daenet TechTalk – Scientific Article PDF Builder
Usage:
    python build_pdf.py \
        --title "Article Title" \
        --subtitle "Optional subtitle" \
        --author "First Last" \
        --affiliation "Organisation · City · email" \
        --cover /path/to/cover.jpg \
        --content /path/to/content.json \
        --output /path/to/out.pdf \
        [--year 2026]

content.json format:
[
  {"type": "section", "text": "1. Introduction"},
  {"type": "body",    "text": "Body paragraph text..."},
  {"type": "subsection", "text": "1.1 Background"},
  {"type": "codebox", "lines": ["line1", "line2"], "caption": "Listing 1 – ..."},
  {"type": "contributions", "text": "Authors. Affiliation note."}
]
"""

import argparse, json, os, sys
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT, TA_JUSTIFY
from reportlab.platypus import (
    BaseDocTemplate, PageTemplate, Frame, Paragraph,
    Spacer, PageBreak, Flowable, NextPageTemplate
)

# ── daenet brand colours ───────────────────────────────────────────────────
DAENET_BLUE  = colors.HexColor("#003DA5")
DAENET_LIGHT = colors.HexColor("#0066CC")
ACCENT       = colors.HexColor("#00AEEF")
CODE_BG      = colors.HexColor("#EEF3FA")
TEXT_DARK    = colors.HexColor("#1A1A2E")
GREY_TEXT    = colors.HexColor("#555566")
WHITE        = colors.white

# ── Page geometry ──────────────────────────────────────────────────────────
PAGE_W, PAGE_H = A4
M_SIDE = 18*mm
M_TOP  = 22*mm
M_BOT  = 20*mm
COL_GAP = 6*mm
BODY_W  = PAGE_W - 2*M_SIDE
col_w   = (BODY_W - COL_GAP) / 2
BODY_H  = PAGE_H - M_TOP - M_BOT
HDR_H   = 104*mm
COL_H   = BODY_H - HDR_H

# ── Custom Flowables ───────────────────────────────────────────────────────
class HRule(Flowable):
    def __init__(self, width, color=DAENET_BLUE, thick=0.75):
        super().__init__()
        self.width = width; self.color = color
        self.thick = thick; self.height = thick + 3
    def draw(self):
        self.canv.setStrokeColor(self.color)
        self.canv.setLineWidth(self.thick)
        self.canv.line(0, 0, self.width, 0)

class CodeBox(Flowable):
    def __init__(self, lines, width, caption=None):
        super().__init__()
        self.lines = lines; self.bwidth = width; self.caption = caption
        self.pad = 6; self._font = "Courier"; self._fsize = 7.2
        lh = self._fsize * 1.35
        self.height = len(lines)*lh + self.pad*2 + (14 if caption else 0)
    def draw(self):
        c = self.canv; pad = self.pad; fs = self._fsize; lh = fs*1.35
        cap_h = 14 if self.caption else 0
        box_h = len(self.lines)*lh + pad*2
        c.setFillColor(CODE_BG)
        c.roundRect(0, 0, self.bwidth, box_h+cap_h, 4, fill=1, stroke=0)
        c.setFillColor(DAENET_BLUE)
        c.rect(0, 0, 3, box_h+cap_h, fill=1, stroke=0)
        if self.caption:
            c.setFillColor(DAENET_BLUE)
            c.rect(3, box_h, self.bwidth-3, cap_h, fill=1, stroke=0)
            c.setFillColor(WHITE); c.setFont("Helvetica-Bold", 6.5)
            c.drawString(8, box_h+4, self.caption)
        c.setFillColor(TEXT_DARK); c.setFont(self._font, fs)
        y = box_h - pad - fs
        for line in self.lines:
            c.drawString(8, y, line); y -= lh

# ── Styles ─────────────────────────────────────────────────────────────────
def S(name, **kw): return ParagraphStyle(name, **kw)
ST = {
    "article_title":    S("article_title",    fontName="Helvetica-Bold",      fontSize=16, leading=20, textColor=DAENET_BLUE,  alignment=TA_LEFT,    spaceAfter=4),
    "article_subtitle": S("article_subtitle", fontName="Helvetica-Oblique",   fontSize=9.5,leading=13, textColor=DAENET_LIGHT, alignment=TA_LEFT,    spaceAfter=4),
    "author":           S("author",           fontName="Helvetica-BoldOblique",fontSize=9, leading=12, textColor=TEXT_DARK,    spaceAfter=1),
    "affiliation":      S("affiliation",      fontName="Helvetica",            fontSize=7.5,leading=10, textColor=GREY_TEXT,    spaceAfter=0),
    "abstract_head":    S("abstract_head",    fontName="Helvetica-Bold",       fontSize=8,  leading=10, textColor=DAENET_BLUE,  spaceBefore=6,        spaceAfter=3),
    "abstract_body":    S("abstract_body",    fontName="Helvetica-Oblique",    fontSize=8,  leading=11.5,textColor=GREY_TEXT,   alignment=TA_JUSTIFY, spaceAfter=4),
    "keywords":         S("keywords",         fontName="Helvetica",            fontSize=7.5,leading=10, textColor=GREY_TEXT,    spaceAfter=0),
    "section":          S("section",          fontName="Helvetica-Bold",       fontSize=10, leading=13, textColor=DAENET_BLUE,  spaceBefore=9,        spaceAfter=4),
    "subsection":       S("subsection",       fontName="Helvetica-Bold",       fontSize=9,  leading=12, textColor=DAENET_LIGHT, spaceBefore=6,        spaceAfter=3),
    "body":             S("body",             fontName="Helvetica",            fontSize=8.5,leading=12.5,textColor=TEXT_DARK,   alignment=TA_JUSTIFY, spaceAfter=5),
    "contributions":    S("contributions",    fontName="Helvetica",            fontSize=8,  leading=12, textColor=GREY_TEXT,   alignment=TA_JUSTIFY, spaceAfter=0),
}

# ── Page callbacks ─────────────────────────────────────────────────────────
def make_on_cover(cover_path, title, subtitle, author, affiliation, year):
    def on_cover(canvas, doc):
        canvas.saveState()
        w, h = A4
        canvas.drawImage(cover_path, 0, 0, width=w, height=h, preserveAspectRatio=False)
        bh = 72*mm
        canvas.setFillColor(colors.Color(0, 0.04, 0.22, alpha=0.88))
        canvas.rect(0, 0, w, bh, fill=1, stroke=0)
        canvas.setFillColor(ACCENT)
        canvas.rect(0, bh-1.5, w, 1.5, fill=1, stroke=0)
        canvas.setFont("Helvetica-Bold", 17); canvas.setFillColor(WHITE)
        canvas.drawString(M_SIDE, bh-13*mm, title)
        if subtitle:
            canvas.setFont("Helvetica-Bold", 11); canvas.setFillColor(ACCENT)
            canvas.drawString(M_SIDE, bh-22*mm, subtitle)
        canvas.setFont("Helvetica-BoldOblique", 9)
        canvas.setFillColor(colors.HexColor("#AACCFF"))
        canvas.drawString(M_SIDE, bh-31*mm, author)
        canvas.setFont("Helvetica", 8)
        canvas.setFillColor(colors.HexColor("#7799BB"))
        canvas.drawString(M_SIDE, bh-40*mm, f"DAENET GmbH  ·  An ACP Digital Company  ·  {year}")
        canvas.setFillColor(colors.HexColor("#0D0D28"))
        canvas.rect(0, 0, w, 11*mm, fill=1, stroke=0)
        canvas.setFont("Helvetica", 7.5); canvas.setFillColor(colors.HexColor("#8899CC"))
        canvas.drawCentredString(w/2, 4*mm, "daenet  –  an  ACP  Digital  company")
        canvas.restoreState()
    return on_cover

def make_on_content(title):
    def on_content(canvas, doc):
        canvas.saveState()
        w, h = A4
        canvas.setStrokeColor(DAENET_BLUE); canvas.setLineWidth(2)
        canvas.line(M_SIDE, h-M_TOP+5*mm, w-M_SIDE, h-M_TOP+5*mm)
        canvas.setFont("Helvetica-Bold", 7); canvas.setFillColor(DAENET_BLUE)
        canvas.drawString(M_SIDE, h-M_TOP+6.5*mm, "daenet TechTalk  ·  Artificial Intelligence")
        canvas.setFont("Helvetica", 7); canvas.setFillColor(GREY_TEXT)
        canvas.drawRightString(w-M_SIDE, h-M_TOP+6.5*mm, title)
        canvas.setStrokeColor(DAENET_BLUE); canvas.setLineWidth(0.75)
        canvas.line(M_SIDE, M_BOT-4*mm, w-M_SIDE, M_BOT-4*mm)
        canvas.setFont("Helvetica", 7); canvas.setFillColor(GREY_TEXT)
        canvas.drawCentredString(w/2, M_BOT-8*mm, f"— {doc.page - 1} —")
        canvas.drawString(M_SIDE, M_BOT-8*mm, "© 2026 daenet GmbH  ·  An ACP Digital Company")
        canvas.drawRightString(w-M_SIDE, M_BOT-8*mm, "daenet.de")
        canvas.restoreState()
    return on_content

# ── Main build function ────────────────────────────────────────────────────
def build(args):
    content = json.load(open(args.content))
    on_cover   = make_on_cover(args.cover, args.title, args.subtitle,
                               args.author, args.affiliation, args.year)
    on_content = make_on_content(args.title)

    doc = BaseDocTemplate(
        args.output, pagesize=A4,
        leftMargin=M_SIDE, rightMargin=M_SIDE,
        topMargin=M_TOP, bottomMargin=M_BOT,
        title=args.title, author=args.author,
        subject="daenet TechTalk – Artificial Intelligence",
        creator="daenet GmbH",
    )

    cover_frame   = Frame(0, 0, PAGE_W, PAGE_H, leftPadding=0, rightPadding=0,
                          topPadding=0, bottomPadding=0)
    hdr_frame     = Frame(M_SIDE, M_BOT+COL_H, BODY_W, HDR_H,
                          leftPadding=0, rightPadding=0, topPadding=0, bottomPadding=4, id="header")
    fp_left       = Frame(M_SIDE, M_BOT, col_w, COL_H,
                          leftPadding=0, rightPadding=3, id="fp_left")
    fp_right      = Frame(M_SIDE+col_w+COL_GAP, M_BOT, col_w, COL_H,
                          leftPadding=3, rightPadding=0, id="fp_right")
    left_frame    = Frame(M_SIDE, M_BOT, col_w, BODY_H,
                          leftPadding=0, rightPadding=3, id="left")
    right_frame   = Frame(M_SIDE+col_w+COL_GAP, M_BOT, col_w, BODY_H,
                          leftPadding=3, rightPadding=0, id="right")

    doc.addPageTemplates([
        PageTemplate(id="Cover",     frames=[cover_frame], onPage=on_cover),
        PageTemplate(id="FirstPage", frames=[hdr_frame, fp_left, fp_right], onPage=on_content),
        PageTemplate(id="Content",   frames=[left_frame, right_frame], onPage=on_content),
    ])

    P  = lambda txt, sty: Paragraph(txt, ST[sty])
    SP = lambda h: Spacer(1, h*mm)

    story = []
    # Cover
    story.append(Spacer(PAGE_W, PAGE_H))
    story.append(NextPageTemplate("FirstPage"))
    story.append(PageBreak())

    # Header zone
    story.append(SP(2))
    story.append(P(args.title, "article_title"))
    if args.subtitle:
        story.append(P(args.subtitle, "article_subtitle"))
    story.append(P(args.author, "author"))
    story.append(P(args.affiliation, "affiliation"))

    # Abstract (look for it in content)
    abstract_items = [c for c in content if c.get("type") == "abstract"]
    if abstract_items:
        story.append(SP(3))
        story.append(P("ABSTRACT", "abstract_head"))
        story.append(P(abstract_items[0]["text"], "abstract_body"))
    keywords_items = [c for c in content if c.get("type") == "keywords"]
    if keywords_items:
        story.append(P(keywords_items[0]["text"], "keywords"))

    # Body content
    story.append(NextPageTemplate("Content"))
    for item in content:
        t = item.get("type", "body")
        if t in ("abstract", "keywords"):
            continue
        elif t == "section":
            story.append(P(item["text"], "section"))
        elif t == "subsection":
            story.append(P(item["text"], "subsection"))
        elif t == "body":
            story.append(P(item["text"], "body"))
        elif t == "codebox":
            story.append(SP(1.5))
            story.append(CodeBox(item["lines"], col_w, caption=item.get("caption")))
            story.append(SP(1.5))
        elif t == "contributions":
            story.append(SP(4))
            story.append(HRule(col_w, DAENET_BLUE, 0.75))
            story.append(P("Contributions", "section"))
            story.append(P(item["text"], "body"))

    doc.build(story)
    print(f"PDF written → {args.output}")

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--title",       required=True)
    ap.add_argument("--subtitle",    default="")
    ap.add_argument("--author",      required=True)
    ap.add_argument("--affiliation", required=True)
    ap.add_argument("--cover",       required=True)
    ap.add_argument("--content",     required=True)
    ap.add_argument("--output",      required=True)
    ap.add_argument("--year",        default="2026")
    build(ap.parse_args())
