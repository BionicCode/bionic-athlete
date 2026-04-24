name: Update Garmin FIT Profile

on:
  workflow_dispatch:
  schedule:
    - cron: "17 4 * * 1"

permissions:
  contents: write
  pull-requests: write

jobs:
  update-profile:
    name: Update Profile.xlsx from Garmin fit-sdk-tools
    runs-on: ubuntu-latest

    steps:
      - name: Check out repository
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Install Git LFS
        shell: bash
        run: |
          set -euo pipefail
          sudo apt-get update
          sudo apt-get install -y git-lfs
          git lfs install --skip-repo
          git lfs version

      - name: Update Garmin FIT Profile reference
        shell: pwsh
        run: |
          ./scripts/Update-GarminFitProfile.ps1 -RepositoryUrl "https://github.com/garmin/fit-sdk-tools.git" -RepositoryFilePath "Profile.xlsx" -ProfilePath "docs/reference/garmin-fit/Profile.xlsx" -MetadataPath "docs/reference/garmin-fit/Profile.source.json"

      - name: Detect changes
        id: changes
        shell: bash
        run: |
          set -euo pipefail

          status="$(git status --porcelain -- docs/reference/garmin-fit/Profile.xlsx docs/reference/garmin-fit/Profile.source.json)"

          if [ -z "$status" ]; then
            echo "changed=false" >> "$GITHUB_OUTPUT"
            echo "No Profile.xlsx updates found."
          else
            echo "changed=true" >> "$GITHUB_OUTPUT"
            echo "Detected changes:"
            git status --short -- docs/reference/garmin-fit/Profile.xlsx docs/reference/garmin-fit/Profile.source.json
          fi

      - name: Create or update pull request
        if: steps.changes.outputs.changed == 'true'
        env:
          GH_TOKEN: ${{ github.token }}
        shell: bash
        run: |
          set -euo pipefail

          branch_name="automation/update-garmin-fit-profile"
          base_branch="${{ github.event.repository.default_branch }}"

          tag="$(pwsh -NoLogo -NoProfile -Command "(Get-Content 'docs/reference/garmin-fit/Profile.source.json' -Raw | ConvertFrom-Json).upstream.tag")"
          sha256="$(pwsh -NoLogo -NoProfile -Command "(Get-Content 'docs/reference/garmin-fit/Profile.source.json' -Raw | ConvertFrom-Json).hashes.sha256")"
          repository_url="$(pwsh -NoLogo -NoProfile -Command "(Get-Content 'docs/reference/garmin-fit/Profile.source.json' -Raw | ConvertFrom-Json).upstream.repositoryUrl")"
          file_path="$(pwsh -NoLogo -NoProfile -Command "(Get-Content 'docs/reference/garmin-fit/Profile.source.json' -Raw | ConvertFrom-Json).upstream.filePath")"
          local_path="$(pwsh -NoLogo -NoProfile -Command "(Get-Content 'docs/reference/garmin-fit/Profile.source.json' -Raw | ConvertFrom-Json).localPath")"

          git config user.name "github-actions[bot]"
          git config user.email "41898282+github-actions[bot]@users.noreply.github.com"

          git switch -c "$branch_name"

          git add docs/reference/garmin-fit/Profile.xlsx docs/reference/garmin-fit/Profile.source.json
          git commit -m "Update Garmin FIT Profile to ${tag}"
          git push --force-with-lease origin "HEAD:${branch_name}"

          title="Update Garmin FIT Profile to ${tag}"
          body_file="$(mktemp)"

          {
            echo "Updates the vendored Garmin FIT Profile reference workbook."
            echo
            echo "- Upstream repository: ${repository_url}"
            echo "- Upstream tag: ${tag}"
            echo "- Upstream file: ${file_path}"
            echo "- Local file: ${local_path}"
            echo "- SHA-256: ${sha256}"
            echo
            echo "This file is used as the public standard FIT profile reference for decoder/export validation."
            echo "It must not be used to reject developer fields or unknown/vendor fields that are present in actual FIT files."
          } > "$body_file"

          existing_pr_number="$(gh pr list --head "$branch_name" --base "$base_branch" --state open --json number --jq '.[0].number')"

          if [ -z "$existing_pr_number" ]; then
            gh pr create --base "$base_branch" --head "$branch_name" --title "$title" --body-file "$body_file"
          else
            gh pr edit "$existing_pr_number" --title "$title" --body-file "$body_file"
          fi