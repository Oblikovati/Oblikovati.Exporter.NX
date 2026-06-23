# Installing the Oblikovati NX exporter (Python journal edition)

This is the **journal** edition of the exporter. It runs as an NX Open **Python journal**,
so it needs **no compiled, code-signed DLL** — useful where loading a signed shared
library is not an option. It produces the same native Oblikovati `.opd`/`.oad` documents
as the C# add-in.

## What's in the zip

```
<this folder>/
  startup/
    oblikovati_export.men        NX menu entry that plays the journal
    oblikovati_export.py         the journal NX runs
    oblikovati_exporter_nx/      the exporter package (imported by the journal)
```

## Requirements

- A Siemens NX seat whose embedded Python can play journals (NX 12 and newer ship a
  CPython interpreter with the `NXOpen` module). The package uses only the standard
  library, so no `pip` packages are needed inside NX.
- A license that permits running NX Open journals. Note: an **unsigned** NX Open
  application requires an **Author**-class license to run; a journal is run through NX's
  journaling mechanism rather than loaded as a signed binary, which is why this edition
  avoids the code-signing certificate the DLL add-in would need. Confirm your seat's
  license against your Siemens agreement.

## Install — option A: play the journal (zero configuration)

1. Unzip anywhere.
2. In NX: **File ▸ Execute ▸ NX Open…** (or **Tools ▸ Journal ▸ Play**) and pick
   `startup/oblikovati_export.py`.

## Install — option B: add a menu button (via `UGII_USER_DIR`)

1. Unzip into a folder.
2. Point NX at it: set the environment variable `UGII_USER_DIR` to this folder, or add
   the folder to a `custom_dirs.dat` referenced by `UGII_CUSTOM_DIRECTORY_FILE`.
3. Start NX. An **Export to Oblikovati** item appears under the File menu; it plays the
   journal. (`UGII_USER_DIR` is shown in **Help ▸ NX Log File**.)

## Use

Open a part (or assembly), then run the exporter. It reads the active document through
the NX Open API and writes an `.opd` (part) / `.oad` (assembly) next to the source part
(or to the temp folder if the part is unsaved), keeping the parametric history. A summary
of what was exported — and any features that need attention — is written to the NX
listing window. The export is read-only: it sets an undo mark and rolls back any builder
churn, so it never modifies the open part.
