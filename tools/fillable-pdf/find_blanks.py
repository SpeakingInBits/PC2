"""Print the position of every underscore "blank" in a PDF's text layer.

Use the output to place fields in build_fillable.py. Each blank is printed as
(x0, x1, bottom, text after the blank) in PDF points, origin top-left. Thin
rectangles (drawn underlines, e.g. the debts table) are printed as RECT lines.

Usage: python find_blanks.py SOURCE_PDF
"""
import sys

import pdfplumber


def main(src):
    with pdfplumber.open(src) as pdf:
        for page_number, page in enumerate(pdf.pages, start=1):
            print(f"=== PAGE {page_number}")

            # Group characters into lines by their top coordinate.
            lines = []
            for c in sorted(page.chars, key=lambda c: (round(c["top"]), c["x0"])):
                if lines and abs(lines[-1][0] - c["top"]) < 3:
                    lines[-1][1].append(c)
                else:
                    lines.append((c["top"], [c]))

            for top, chars in lines:
                chars.sort(key=lambda c: c["x0"])
                line_text = "".join(c["text"] for c in chars)
                if "_" not in line_text:
                    continue
                print(f"top={top:.1f} | {line_text[:60]!r}")
                i = 0
                while i < len(chars):
                    if chars[i]["text"] != "_":
                        i += 1
                        continue
                    j = i
                    while (j + 1 < len(chars) and chars[j + 1]["text"] == "_"
                           and chars[j + 1]["x0"] - chars[j]["x1"] < 2):
                        j += 1
                    after = "".join(c["text"] for c in chars[j + 1:j + 25]).strip()
                    print("    ", (round(chars[i]["x0"], 1), round(chars[j]["x1"], 1),
                                   round(chars[i]["bottom"], 1), after[:18]))
                    i = j + 1

            for r in page.rects:
                if r["height"] < 2 and r["width"] > 20:
                    print("  RECT", round(r["x0"], 1), round(r["x1"], 1), round(r["top"], 1))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    sys.stdout.reconfigure(encoding="utf-8")
    main(sys.argv[1])
