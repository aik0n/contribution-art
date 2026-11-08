# Contribution Art (Blazor sample)

Paint on a copy of your GitHub contribution calendar and push the picture to a dedicated
repository as backdated, empty commits — a Blazor Server take on
[gitfiti](https://github.com/gelstudios/gitfiti).

## Run it

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd contribution-art
dotnet run --project src/ContributionArt
```

Open the URL it prints (https://localhost:61576 or http://localhost:61577).

## Before you push

1. **Create a new, empty repository** on GitHub (no README, no licence). The app refuses to
   push to a repository that already has commits.
2. **Create a fine-grained personal access token**
   (GitHub → Settings → Developer settings → Fine-grained tokens):
   - Repository access: *Only select repositories* → your art repository
   - Repository permissions: **Contents: Read and write** (Metadata: read is added automatically)
3. Paste the token, press **Connect**. The app fills in your login, your no-reply e-mail and a
   suggested "commits per level" based on your busiest day.

The token stays in server memory for the session only; it is never written to disk.

## How it works

| Piece | File |
| --- | --- |
| 53×7 calendar model, dates start on the Sunday ~1 year ago | `src/ContributionArt/Features/Canvas/ContributionGrid.cs` |
| Predefined preset shapes | `src/ContributionArt/presets.json` |
| Loading and saving presets | `src/ContributionArt/Features/Presets/PresetStore.cs` |
| 3×5 pixel font | `src/ContributionArt/Features/Canvas/PixelFont.cs` |
| GitHub REST/GraphQL calls (user, busiest day, empty-repo check), behind `IGitHubClient` | `src/ContributionArt/Features/GitHub/GitHubClient.cs` |
| GitHub API address, User-Agent and API version | `src/ContributionArt/appsettings.json` |
| Builds history with LibGit2Sharp and pushes once over HTTPS | `src/ContributionArt/Features/GitHub/GitPusher.cs` |
| gitfiti-style bash script export | `src/ContributionArt/Features/ScriptExport/ScriptExporter.cs` |
| The UI | `src/ContributionArt/Components/Pages/Home.razor` |

Each painted cell becomes `level × commits-per-level` empty commits dated 12:00 UTC on that
day. A README stating that the repository is contribution-graph art is committed first, dated
the day before the visible graph so it doesn't colour any pixel.

**Prefer to run git yourself?** Use *Download bash script* instead of *Push to GitHub*. The
script needs git 2.28+ and SSH access to GitHub.

## Presets

Built-in shapes are in `presets.json`. The app does not start if that file is missing or invalid.

*Save as preset* stores the current painting in `presets-user.json`, cropped to the painted weeks.
The file is in `%LocalAppData%/ContributionArt` (on Linux, `~/.local/share/ContributionArt`), so it
survives restarts and rebuilds. Set `Presets:UserFilePath` in `appsettings.json` to use another
location. Each row is one day, and each digit is the level for one week:

```json
{ "Presets": [ { "Name": "My drawing", "Rows": ["12", "12", "12", "12", "12", "12", "12"] } ] }
```

After editing the file by hand, press *Reload*. Saved presets are shared by everyone using the same
running app, so add access control before hosting it publicly.

## Notes

- Commits only appear on your graph if the author e-mail is linked to your account (the
  no-reply address always is) and they are on the default branch.
- GitHub's shading is relative to your own busiest days, so the result is approximate.
- To remove the art, delete the repository.
- Keep it honest: this is fine as visible art, but don't use it to suggest activity that
  didn't happen.

## Credits

Based on the original idea of [gitfiti](https://github.com/gelstudios/gitfiti) by
Eric Romano ([gelstudios](https://github.com/gelstudios)).
