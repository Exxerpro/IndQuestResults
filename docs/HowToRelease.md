# How to Release to NuGet

## One-time setup
1. Create a NuGet.org API key (scoped to push new packages).
2. Add it as a GitHub repo secret: `NUGET_API_KEY`.

## Version bump
- Edit `Src/Code/src/IndQuestResults/IndQuestResults.csproj` and update:
  - `<PackageVersion>`
  - `<AssemblyVersion>`

## Tag and push
```bash
# commit version bump
git add Src/Code/src/IndQuestResults/IndQuestResults.csproj
git commit -m "chore(release): vX.Y.Z"

# tag the release
git tag vX.Y.Z

# push code and tag
git push origin main --tags
```

Example (Conventional Commits):
- Commit message: chore(release): v1.0.7
- Tag: v1.0.7

## What happens next
- GitHub Action `.github/workflows/release.yml` builds, packs, and publishes to NuGet using `NUGET_API_KEY`.
- A GitHub Release is created with the `.nupkg` attached.

## Notes
- CI uses .NET 10 and enforces link checks; mutation testing is not run due to MTP + xUnit v3 limitations.
- Keep `CHANGELOG.md` updated before tagging.
