#!/bin/bash
set -e

echo "=== Building mod (Release) ==="
dotnet build -c Release

echo "=== Building documentation ==="
cp changes.md docs_src/src/changes.md
mdbook build docs_src

echo "=== Adding docs to release zip ==="
python -c "
import zipfile, os
with zipfile.ZipFile('SayTheSpire2.zip', 'a') as zf:
    for root, dirs, files in os.walk('docs_src/book'):
        for f in files:
            src = os.path.join(root, f)
            arc = 'SayTheSpire2Docs/' + os.path.relpath(src, 'docs_src/book').replace(os.sep, '/')
            zf.write(src, arc)
"

echo "=== Done ==="
echo "Release zip: SayTheSpire2.zip"
echo "Installer:   SayTheSpire2Installer.exe"
