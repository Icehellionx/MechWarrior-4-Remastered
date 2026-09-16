# Manual cleanup pipeline

Raw scans under the ignored root `Manuals/` are local inputs. Cleaned PDFs under ignored `output/pdf/` and launcher cover renders under ignored `output/manual-covers/` are local artifacts and are not published until redistribution rights are documented.

Install the pinned tools into the ignored workspace-local directory:

```powershell
python -m pip install --target .local/python -r tools/manuals/requirements.txt
$env:PYTHONPATH = (Resolve-Path '.local/python').Path
```

Build and verify the three qualified manuals:

```powershell
python tools/manuals/clean_manuals.py Manuals output/pdf
python tools/manuals/render_covers.py output/pdf output/manual-covers
python tools/manuals/verify_manuals.py Manuals output/pdf tools/manuals/manuals.lock.json --cover-directory output/manual-covers
python tools/manuals/inspect_manuals.py output/pdf tmp/pdfs-final --dpi 90
```

`manuals.lock.json` identifies the exact reviewed source scans and deterministic outputs. A different scan is not silently treated as equivalent; inspect it, update the transform if necessary, render every page, and deliberately qualify a new lock entry.
