#!/bin/bash
# Assembles the org repository template into a directory ready to push.
#
#   ./create-template-repo.sh <target-directory>
#       Copies the payload verbatim, placeholders intact. This is what you push to
#       Glory2Him/Glory2Him.Template and then tick "Template repository" on.
#
#   ./create-template-repo.sh <target-directory> <repository-name> [year]
#       Also substitutes {{REPOSITORY_NAME}} and {{YEAR}}, for scaffolding a real
#       repository directly instead of going through the "Use this template" button.
#
# The two images are copied from this repository's Resources/Images rather than kept
# in the template, so the org has one copy of each icon instead of one per repository.

set -euo pipefail

scriptDirectory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repositoryRoot="$(cd "$scriptDirectory/../.." && pwd)"

targetDirectory="${1:-}"
repositoryName="${2:-}"
year="${3:-$(date +%Y)}"

if [[ -z "$targetDirectory" ]]; then
    echo "Usage: $0 <target-directory> [repository-name] [year]" >&2
    exit 1
fi

if [[ -e "$targetDirectory" && -n "$(ls -A "$targetDirectory" 2>/dev/null)" ]]; then
    echo "Error: $targetDirectory exists and is not empty." >&2
    exit 1
fi

mkdir -p "$targetDirectory"
targetDirectory="$(cd "$targetDirectory" && pwd)"

cp -R "$scriptDirectory/template/." "$targetDirectory/"

mkdir -p "$targetDirectory/Resources/Images"
cp "$repositoryRoot/Resources/Images/Glory2Him.ico" "$targetDirectory/Resources/Images/"
cp "$repositoryRoot/Resources/Images/Glory2Him-Square.png" "$targetDirectory/Resources/Images/"

if [[ -n "$repositoryName" ]]; then
    for file in "$targetDirectory/README.md" "$targetDirectory/LICENSE.txt"; do
        sed -i.bak \
            -e "s/{{REPOSITORY_NAME}}/$repositoryName/g" \
            -e "s/{{YEAR}}/$year/g" \
            "$file"

        rm -f "$file.bak"
    done

    echo "Substituted {{REPOSITORY_NAME}} -> $repositoryName and {{YEAR}} -> $year"
fi

echo "Assembled into $targetDirectory"
echo
echo "Next:"
echo "  cd \"$targetDirectory\""
echo "  git init -b main && git add -A && git commit -m 'INFRA: Add repository template'"

if [[ -z "$repositoryName" ]]; then
    echo "  gh repo create Glory2Him/Glory2Him.Template --public --source . --push"
    echo "  gh repo edit Glory2Him/Glory2Him.Template --template"
else
    echo "  gh repo create Glory2Him/$repositoryName --public --source . --push"
fi

echo "  gh workflow run Labels --repo Glory2Him/<repository>   # if it did not fire on the first push"
