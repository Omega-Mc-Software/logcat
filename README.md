# LogCat 🐾

Keyboard-first daily log for Windows. One plain markdown file per day, automatic
rollover at midnight, and instant search across every file you've ever written.

By [Neko Omega](https://neko.omegamc.uk/) · Omega-Mc-Software.

## Why

Because the good daily-log tools are either cloud subscriptions or six menus deep.
LogCat is one portable `.exe`: your log lives in plain `.md` files in a folder you
own, next to nothing to learn — type, hit Enter, keep working.

## Features

- **Plain text files** — one markdown file per day, readable by anything
- **Auto rollover** — a new file starts itself when the date changes
- **Instant search** — `Ctrl+K` searches every log file at once
- **Quick command palette** — `Ctrl+L` for commands, Enter to run
- **Export** — pull any range out as a single file
- **Portable** — no installer, no account, no cloud, no telemetry

## Download

Grab the latest exe from [neko.omegamc.uk/products/](https://neko.omegamc.uk/products/)
(SHA-256 checksum published alongside every build), or from the
[Actions artifacts](https://github.com/Omega-Mc-Software/logcat/actions) of any tagged release.

Windows may show a SmartScreen warning on unsigned builds — the checksum on the
download page lets you verify the file is exactly the one we built.

## Building

Requires the .NET 8 SDK.

```
dotnet publish src/LogCat.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Tagged releases (`v*`) build automatically on GitHub Actions.

## License

MIT — see [LICENSE](LICENSE).
