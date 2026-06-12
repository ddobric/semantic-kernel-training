---
name: daenet-scientific-pdf
description: >
  Creates a professional daenet TechTalk-branded two-column scientific article PDF
  from a Word document (.docx) and a cover image. Use this skill whenever the user
  wants to produce a scientific or technical magazine-style PDF in daenet style,
  convert a DOCX to a formatted daenet article, create a two-column article PDF
  with a cover page, or generate a professional publication from a Word document.
  Trigger even when the user says things like "make it look like a daenet article",
  "format this as a TechTalk PDF", "create a professional PDF from my doc", or
  "produce a magazine-style article". The output is always a polished PDF saved
  to the user's connected folder.
---

# daenet Scientific Article PDF

Turn a Word document into a professional daenet TechTalk two-column scientific
article PDF -- cover page, running headers/footers, code boxes, and full daenet
branding -- in one workflow.

## What you produce

- **Page 1**: Full-bleed cover image with a translucent dark-blue title band
  (article title, subtitle, author, year, "daenet -- an ACP Digital company" strip)
- **Page 2+**: daenet-branded two-column layout
  - Full-width header zone: article title, subtitle, author/affiliation, abstract, keywords
  - Two-column body: numbered sections, subsections, body text, styled code boxes
  - Running header (journal title left / article title right) and footer (page number, copyright)
- **Final section**: Contributions block (if provided)

## Workflow

### Step 1 -- Gather inputs

You need four things. Ask for any that are missing:

1. **DOCX file** -- the article text. Read it with `python-docx`.
2. **Cover image** -- The official daenet TechTalk cover is bundled at
   `assets/techtalk-cover.jpg`. Use it as the default cover unless the user
   explicitly provides a different image.
3. **Article metadata** -- title, subtitle (optional), author name(s), affiliation line,
   year (default 2026).
4. **Output path** -- where to save the PDF (default: user's connected folder).

To find the skill's own directory (so you can reference `assets/techtalk-cover.jpg`),
locate the SKILL.md file:

```bash
find /sessions -name "SKILL.md" -path "*daenet-scientific-pdf*" 2>/dev/null | head -1
```

The cover image is at `<skill-dir>/assets/techtalk-cover.jpg`.

If the user provides a custom cover image inline in the conversation, it will not
be saved to disk automatically. Extract it from the conversation JSONL:

```python
import json, base64
# find with: find /sessions -name "*.jsonl" -path "*projects*" | head -1
images = []
with open(jsonl_path) as f:
    for line in f:
        try: obj = json.loads(line)
        except: continue
        def collect(o, depth=0):
            if depth > 15: return
            if isinstance(o, dict):
                if o.get("type") == "image" and o.get("source", {}).get("type") == "base64":
                    images.append(o["source"])
                for v in o.values(): collect(v, depth+1)
            elif isinstance(o, list):
                for v in o: collect(v, depth+1)
        collect(obj)
best = max(images, key=lambda s: len(s.get("data", "")))
with open("cover_custom.jpg", "wb") as f:
    f.write(base64.b64decode(best["data"]))
```

### Step 2 -- Extract article content from the DOCX

Install `python-docx` if needed (`pip install python-docx --break-system-packages -q`),
then read every non-blank paragraph. Map paragraphs to content types:

| Heuristic | Type |
|-----------|------|
| Starts with a digit and a period or space, OR all-caps short line | `"section"` |
| Starts with a digit.digit | `"subsection"` |
| Paragraph style contains "Heading" | `"section"` or `"subsection"` |
| Otherwise | `"body"` |

Look for an "abstract" paragraph (often a short italic paragraph near the top, or
labelled "Abstract"). Look for a "keywords" line. Identify any code-like blocks
(monospace runs, or lines with `Assert.`, `{`, `}`, `//`).

Build a `content.json` list:

```json
[
  {"type": "abstract",  "text": "..."},
  {"type": "keywords",  "text": "Keywords: ..."},
  {"type": "section",   "text": "1.  Introduction"},
  {"type": "body",      "text": "..."},
  {"type": "codebox",   "lines": ["line1", "line2"], "caption": "Listing 1 - ..."},
  {"type": "contributions", "text": "Authors. Affiliation note."}
]
```

**Important**: Use only ASCII characters in codebox lines. Replace:
- Unicode symbols like theta, middle-dot, double-bar, minus-sign, arrow
  with plain ASCII equivalents: `a`, `.`, `||`, `-`, `->`
Courier (the codebox font) does not support Unicode and will render black boxes.

### Step 3 -- Run the builder script

The bundled script at `scripts/build_pdf.py` does all the ReportLab work.
Install reportlab first if needed (`pip install reportlab --break-system-packages -q`).

```bash
python scripts/build_pdf.py \
  --title   "Full Article Title" \
  --subtitle "Optional subtitle line" \
  --author  "First Last" \
  --affiliation "Organisation - City - email@domain" \
  --cover   <skill-dir>/assets/techtalk-cover.jpg \
  --content /path/to/content.json \
  --output  /path/to/output.pdf \
  --year    2026
```

### Step 4 -- Present the PDF

Save the PDF to the user's connected folder and use `present_files` to share it.

## Layout constants (for reference / debugging)

- A4: 595 x 842 pt
- Side margins: 18 mm; top: 22 mm; bottom: 20 mm
- Column gap: 6 mm -> each column ~246 pt wide
- Header zone height (first content page): 104 mm
- daenet blue: `#003DA5`; accent: `#00AEEF`; code bg: `#EEF3FA`

## Common issues

| Symptom | Fix |
|---------|-----|
| Black boxes in code boxes | Non-ASCII chars in `lines` -- replace with ASCII |
| Header zone content overflows | Reduce abstract length or shrink `HDR_H` in the script |
| Cover image not found | Check `assets/techtalk-cover.jpg` relative to SKILL.md |
| Text crosses column boundary lines | Header/footer rules drawn by `on_content()`