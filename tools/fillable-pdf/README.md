# Fillable Housing Program intake PDF

Builds `PC2/wwwroot/PDF/HousingProgram/HomeOwnershipIntake-July2022-Fillable.pdf`, the fillable version of the Homeownership Intake packet linked from `/Home/HousingProgram`.

The source PDF is a scan with an OCR text layer and no form fields. `build_fillable.py` adds text fields, checkboxes and radio-button groups on top of the blanks. The page content itself is not changed.

This is a one-off maintenance script. It is not part of the website project or the solution.

## Setup

Requires Python 3.10+.

```bash
cd tools/fillable-pdf
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt    # Windows
# .venv/bin/pip install -r requirements.txt      # macOS/Linux
```

## Regenerate the fillable PDF

```bash
.venv/Scripts/python build_fillable.py
```

With no arguments, it reads the original PDF and overwrites the fillable PDF, both in `PC2/wwwroot/PDF/HousingProgram`. You can also pass paths: `build_fillable.py SOURCE_PDF OUTPUT_PDF`.

## When the client sends a new version of the form

Field positions are hard-coded to match the July 2022 form, so a new form needs them updated:

1. Run `find_blanks.py NEW_FORM.pdf`. It prints every `___` blank as `(x0, x1, bottom, text after it)` in PDF points, measured from the top-left corner.
2. Update the `text(...)`, `check(...)` and `radio(...)` calls in `build_fillable.py` to match. For example, `text(page, "FirstName", "First name", x0, x1, bottom)` puts a text box over a blank.
3. Rebuild, open the result in a browser and in Adobe Acrobat Reader, and check that the fields line up.
4. Update the PDF file names in `build_fillable.py` and the link in `PC2/Views/Home/HousingProgram.cshtml`.

If the new PDF has no text layer (the blanks don't show up in `find_blanks.py`), run OCR on it first, for example with Acrobat's "Scan & OCR".

## Notes

- Radio groups are used for pick-one questions. PyMuPDF writes each radio button as its own field, so `group_radios()` moves them under one parent field per group, with a separate export value for each option.
- Each field's second argument becomes its tooltip, which screen readers read aloud. Keep these labels descriptive.
- Signature lines are plain text fields where people type their name, not digital-signature fields.
