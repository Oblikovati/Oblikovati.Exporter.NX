# Installing the Oblikovati NX exporter

This folder is the layout NX loads from `UGII_USER_DIR`.

```
<this folder>/
  startup/      NX auto-loads .men + .dll here at launch
  application/  dialog resources (reserved)
```

## Install

1. Download and unzip the release.
2. Point NX at it. Either:
   - set the environment variable `UGII_USER_DIR` to this folder, or
   - add this folder to a `custom_dirs.dat` referenced by `UGII_CUSTOM_DIRECTORY_FILE`.
3. Start NX. An **Export to Oblikovati** item appears under the File menu.

## Use

Open a part (or assembly), then choose **Export to Oblikovati**. The exporter reads
the active document through the NX API and writes an `.opd` (part) / `.oad` (assembly)
that opens directly in Oblikovati, keeping the parametric history.
