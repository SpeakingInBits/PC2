"""Add AcroForm fields to the scanned PC2 Homeownership Intake packet.

Coordinates come from the underscore "blanks" in the PDF's OCR text layer
(PDF points, origin top-left, as reported by find_blanks.py).

Usage: python build_fillable.py [SOURCE_PDF] [OUTPUT_PDF]
Defaults to the intake packet in PC2/wwwroot/PDF/HousingProgram.
"""
import io
import sys
from pathlib import Path

import pymupdf as fitz
from pypdf import PdfReader, PdfWriter
from pypdf.generic import ArrayObject, DictionaryObject, NameObject, NumberObject, TextStringObject

PDF_DIR = Path(__file__).resolve().parents[2] / "PC2" / "wwwroot" / "PDF" / "HousingProgram"
SRC = sys.argv[1] if len(sys.argv) > 1 else str(PDF_DIR / "HomeOwnershipIntake-July2022.pdf")
DST = sys.argv[2] if len(sys.argv) > 2 else str(PDF_DIR / "HomeOwnershipIntake-July2022-Fillable.pdf")

doc = fitz.open(SRC)
FONT_SIZE = 10


def text(page, name, label, x0, x1, bottom, h=13, maxlen=0):
    w = fitz.Widget()
    w.field_type = fitz.PDF_WIDGET_TYPE_TEXT
    w.field_name = name
    w.field_label = label  # tooltip / screen-reader name (/TU)
    w.rect = fitz.Rect(x0 + 1, bottom - h, x1 - 1, bottom)
    w.text_font = "Helv"
    w.text_fontsize = FONT_SIZE
    w.text_color = (0, 0, 0.55)
    w.border_width = 0
    if maxlen:
        w.text_maxlen = maxlen
    page.add_widget(w)


def check(page, name, label, x0, x1, bottom, radio_value=None):
    """Checkbox centred on a ___ blank. radio_value => radio button in group `name`."""
    size = 10
    cx = (x0 + x1) / 2
    w = fitz.Widget()
    w.field_type = fitz.PDF_WIDGET_TYPE_RADIOBUTTON if radio_value else fitz.PDF_WIDGET_TYPE_CHECKBOX
    # Radio kids get a temporary "Group#Value" name; group_radios() fixes them up.
    w.field_name = f"{name}#{radio_value}" if radio_value else name
    w.field_label = label
    w.rect = fitz.Rect(cx - size / 2, bottom - size - 1, cx + size / 2, bottom - 1)
    w.border_color = (0.35, 0.35, 0.35)
    w.border_width = 0.75
    w.text_color = (0, 0, 0.55)
    w.field_value = False
    page.add_widget(w)
    return w


def radio(page, group, label, options, bottom):
    """options: list of (value, x0, x1)."""
    for value, x0, x1 in options:
        check(page, group, f"{label}: {value}", x0, x1, bottom, radio_value=value)


# ---------------------------------------------------------------- Page 2
p = doc[1]
text(p, "TodaysDate", "Today's date", 435, 545, 180)
text(p, "FirstName", "First name", 99.0, 245.8, 201.9)
text(p, "MiddleInitial", "Middle initial", 313.1, 349.7, 201.9, maxlen=3)
text(p, "LastName", "Last name", 415.2, 562.0, 201.9)
text(p, "DOB_Month", "Date of birth - month", 106.4, 155.3, 224.5, maxlen=2)
text(p, "DOB_Day", "Date of birth - day", 158.3, 207.2, 224.5, maxlen=2)
text(p, "DOB_Year", "Date of birth - year", 210.3, 259.2, 224.5, maxlen=4)
text(p, "HomePhone", "Home phone", 109.4, 213.4, 247.2)
text(p, "WorkPhone", "Work phone", 283.1, 393.1, 247.2)
text(p, "CellPhone", "Cell phone", 453.1, 563.2, 247.2)
text(p, "Email", "Email address", 116.2, 562.7, 269.8)
radio(p, "PreferredContact", "Preferred method of contact",
      [("Home", 216.0, 234.3), ("Work", 288.0, 306.4), ("Cell", 360.0, 378.3), ("Email", 432.0, 450.3)], 292.5)
text(p, "StreetAddress", "Street address", 108.8, 561.5, 315.1)
text(p, "CityState", "City, state", 96.0, 224.4, 337.8)
text(p, "Zip", "Zip code", 255.0, 322.2, 337.8, maxlen=10)
text(p, "AddressYears", "Length of time at current address - years", 203.5, 258.6, 360.4)
text(p, "AddressMonths", "Length of time at current address - months", 294.7, 343.6, 360.4)
radio(p, "ResidencyStatus", "Residency status",
      [("Rent", 130.8, 155.2), ("Own", 216.0, 240.4), ("Other", 288.0, 312.4)], 383.1)
check(p, "Eligibility", "I am a person with developmental disabilities (DD)", 36.0, 54.3, 451.0, radio_value="PersonWithDD")
check(p, "Eligibility", "I am a family member of a person with DD who lives with me", 36.0, 54.3, 463.6, radio_value="FamilyMember")
check(p, "Eligibility", "I am none of the above", 36.0, 54.3, 498.9, radio_value="None")
text(p, "DD_Ages", "Age(s) of person with DD", 183.0, 238.1, 476.3)
text(p, "DD_Names", "Name(s) of person with DD", 289.5, 564.7, 476.3)
radio(p, "CurrentlyOwnHome", "Do you currently own a home",
      [("Yes", 191.3, 203.5 + 3), ("No", 252.0, 270.3)], 614.8)
radio(p, "EverOwnedHome", "Have you ever owned a home",
      [("Yes", 479.3, 497.7), ("No", 528.9, 547.2)], 614.8)
text(p, "HomeSoldDate", "Month and year the home was sold", 305.1, 562.0, 637.5)
text(p, "PurchaseWhere", "Where would you like to purchase a home", 243.8, 561.9, 660.1)
text(p, "PurchaseWhen", "When are you hoping to purchase a home", 243.8, 561.9, 682.8)
check(p, "HomeType_SingleFamily", "Home type: Single family detached house", 36.0, 54.3, 728.1)
check(p, "HomeType_Townhome", "Home type: Townhome", 220.6, 238.9, 728.1)
check(p, "HomeType_Condominium", "Home type: Condominium", 324.0, 342.3, 728.1)
check(p, "HomeType_Manufactured", "Home type: Manufactured home on purchased land", 36.0, 54.3, 750.7)

# ---------------------------------------------------------------- Page 3
p = doc[2]
text(p, "NumberOfDependents", "Number of dependents", 159.0, 214.0, 105.7)
text(p, "HouseholdSize", "Total household size", 332.0, 380.9, 105.7)
text(p, "CurrentRent", "Current rent", 105.2, 215.2, 128.4)
radio(p, "Section8", "Section 8 subsidy", [("Yes", 325.9, 344.1), ("No", 375.4, 393.6)], 128.4)
text(p, "Income_Employment", "Monthly income - employment (gross)", 176.6, 286.6, 173.7)
text(p, "Income_ChildSupport", "Monthly income - child support", 432.7, 536.7, 173.7)
text(p, "Income_SSDI", "Monthly income - SS Disability (SSDI)", 175.9, 285.9, 196.3)
text(p, "Income_SSI", "Monthly income - SSI", 432.0, 535.9, 196.3)
text(p, "Income_GAS", "Monthly income - GA-S (Pregnancy)", 171.0, 287.2, 219.0)
text(p, "Income_AFDC_DSHS", "Monthly income - AFDC/DSHS", 432.0, 535.9, 219.0)
text(p, "Income_GAU_ADATSA", "Monthly income - GAU/ADATSA", 170.5, 286.7, 241.6)
text(p, "Income_Unemployment", "Monthly income - unemployment", 437.1, 540.9, 241.6)
text(p, "Income_OtherSource", "Other income source", 105.6, 185.1, 264.2)
text(p, "Income_OtherAmount", "Monthly income - other", 191.2, 289.1, 264.2)
debt_rows = [(1, "Auto loan", 331.0), (2, "Student loan", 353.6), (3, "Credit cards", 376.3),
             (4, None, 398.9), (5, None, 421.6)]
for n, kind, line in debt_rows:
    desc = kind or f"Debt {n}"
    if kind is None:
        text(p, f"Debt{n}_Type", f"Debt {n} type", 50.0, 180.0, line)
    text(p, f"Debt{n}_TotalOwed", f"{desc} - total amount owed", 216.0, 324.0, line)
    text(p, f"Debt{n}_MonthlyPayment", f"{desc} - monthly payment", 360.0, 468.0, line)
race = [("Race_AmericanIndianAlaskanNative", "American Indian/Alaskan Native", 72.0, 90.3, 571.6),
        ("Race_Asian", "Asian", 288.1, 306.4, 571.6),
        ("Race_BlackAfricanAmerican", "Black/African American", 396.0, 414.4, 571.6),
        ("Race_HawaiianPacificIslander", "Hawaiian/Pacific Islander", 72.0, 90.3, 584.2),
        ("Race_HispanicLatino", "Hispanic/Latino", 288.0, 306.3, 584.2),
        ("Race_WhiteCaucasian", "White/Caucasian", 396.0, 414.3, 584.2),
        ("Race_Other", "Other", 72.0, 90.3, 596.8)]
for name, label, x0, x1, b in race:
    check(p, name, f"Race/ethnicity: {label}", x0, x1, b)
text(p, "Race_OtherDescription", "Race/ethnicity: other (describe)", 124.0, 252.4, 596.8)
radio(p, "MaritalStatus", "Marital status",
      [("Married", 72.0, 90.3), ("Separated", 136.8, 155.1), ("Unmarried", 216.0, 234.3),
       ("Widowed", 324.0, 348.4), ("Divorced", 399.8, 424.2), ("Other", 474.4, 493.8)], 648.4)
radio(p, "Gender", "Gender", [("Female", 72.0, 90.3), ("Male", 139.3, 157.6), ("Other", 216.0, 234.3)], 688.9)

# ---------------------------------------------------------------- Page 4
p = doc[3]
radio(p, "Citizenship", "Citizenship",
      [("USCitizen", 131.9, 150.2), ("PermanentResident", 216.0, 234.3), ("NonResident", 360.0, 378.4)], 105.7)
text(p, "CountryOfOrigin", "Country of origin", 158.3, 305.1, 128.4)
text(p, "PreferredLanguage", "Preferred language", 409.1, 555.9, 128.4)
radio(p, "Education", "Education",
      [("NoDiploma", 72.0, 88.7), ("HSDiploma", 152.0, 168.7), ("GED", 241.5, 258.2),
       ("SomeCollege", 293.7, 310.3), ("VocationalCertificate", 454.3, 471.0)], 172.5)
radio(p, "Education", "Education",
      [("Associates", 72.0, 88.7), ("Bachelors", 183.2, 199.9), ("Masters", 293.2, 309.9),
       ("Doctoral", 394.3, 411.0)], 194.0)
text(p, "Initials_ShareInfo", "Initials: authorize PC2 to share homeownership information", 73.3, 103.8, 329.9, maxlen=4)
text(p, "Initials_SendCopies", "Initials: send copies of all materials", 73.3, 103.8, 342.6, maxlen=4)
text(p, "GuardianName", "Guardian name", 119.8, 340.0, 365.2)
text(p, "GuardianPhone", "Guardian phone", 397.9, 556.9, 365.2)
text(p, "Advocate1Name", "Advocate 1 name", 97.2, 341.9, 387.8)
text(p, "Advocate1Phone", "Advocate 1 phone", 400.9, 560.0, 387.8)
text(p, "Advocate2Name", "Advocate 2 name", 100.2, 344.9, 410.5)
text(p, "Advocate2Phone", "Advocate 2 phone", 400.9, 559.9, 410.5)
text(p, "Initials_PhotoRelease", "Initials: photography release", 76.4, 113.0, 455.8, maxlen=4)
text(p, "Signature", "Signature (type full name)", 108.0, 358.8, 549.0)
text(p, "SignatureDate", "Signature date", 428.3, 538.4, 549.0)
# One field per printed comment line so the filled form prints like the paper one.
for i, (x0, bottom) in enumerate([(72.0, 584.4), (36.0, 607.0), (36.0, 629.6),
                                  (36.0, 652.3), (36.0, 674.9), (36.0, 697.6)], start=1):
    text(p, f"Comments{i}", f"Comments, line {i}", x0, 556.0, bottom)

# ---------------------------------------------------------------- Page 5
p = doc[4]
text(p, "ClientName", "Client name", 130.1, 516.8, 210.5)
text(p, "Applicant1Signature", "Applicant 1 signature (type full name)", 54.0, 227.4, 696.1)
text(p, "Applicant2Signature", "Applicant 2 signature (type full name)", 244.4, 451.0, 696.1)
text(p, "AuthorizationDate", "Authorization date", 467.9, 554.5, 696.1)
text(p, "ApplicantAddress", "Applicant address", 54.0, 445.0, 737.5)
text(p, "ApplicantCityStateZip", "Applicant city, state, zip", 450.0, 554.1, 737.5)

doc.set_metadata({**doc.metadata, "title": "PC2 Homeownership Intake Packet (Fillable)",
                  "author": "Pierce County Coalition for Developmental Disabilities (PC2)"})


def group_radios(pdf_bytes):
    """PyMuPDF writes each radio widget as its own field with on-state /Yes, so
    every button in a "group" toggles together. Re-parent them under one field
    per group and give each kid a distinct on-state (its export value)."""
    writer = PdfWriter(clone_from=PdfReader(io.BytesIO(pdf_bytes)))
    fields = writer._root_object["/AcroForm"]["/Fields"]
    groups = {}
    for page in writer.pages:
        for ref in page.get("/Annots", []):
            annot = ref.get_object()
            name = annot.get("/T")
            if not name or "#" not in name:
                continue
            group, value = name.split("#", 1)
            if group not in groups:
                parent = DictionaryObject({
                    NameObject("/FT"): NameObject("/Btn"),
                    NameObject("/Ff"): NumberObject(1 << 15),  # Radio (clicking again clears it)
                    NameObject("/T"): TextStringObject(group),
                    NameObject("/V"): NameObject("/Off"),
                    NameObject("/Kids"): ArrayObject(),
                })
                groups[group] = writer._add_object(parent)
                fields.append(groups[group])
            parent_ref = groups[group]
            on = NameObject("/" + value)
            for state in ("/N", "/D"):
                ap = annot["/AP"].get(state)
                if ap is not None and "/Yes" in ap:
                    ap = ap.get_object()
                    ap[on] = ap.pop("/Yes")
            for key in ("/T", "/FT", "/Ff", "/V"):
                annot.pop(key, None)
            annot[NameObject("/AS")] = NameObject("/Off")
            annot[NameObject("/Parent")] = parent_ref
            parent_ref.get_object()["/Kids"].append(ref)
            fields.remove(ref)
    out = io.BytesIO()
    writer.write(out)
    return out.getvalue()


with open(DST, "wb") as f:
    f.write(group_radios(doc.tobytes(garbage=3, deflate=True)))
print("saved", DST)
