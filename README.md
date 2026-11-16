# MemeGen

A tiny native Windows meme generator. Load an image, type your top and bottom text, and get the classic meme look: white Impact text with a thick black outline.

## Features

- **Live preview**: the meme updates as you type
- **Auto-fitting text**: captions shrink and wrap to fit the image, and words never get split
- **Many ways to load an image**: Open button, drag & drop, paste from clipboard (Ctrl+V), or drop an image onto `MemeGen.exe`
- **Export**: save as PNG or JPG, or copy straight to the clipboard to paste into Discord, Telegram, etc.
- **Multi-line captions**: press Enter in a text box to force a line break
- Phone photos are rotated correctly (EXIF orientation)
- **Themes**: System (follows your Windows light/dark setting), Light or Dark. Pick one in the sidebar and it is remembered.
- Single portable `.exe`, no install needed

## Download

Get `MemeGen.exe` from the [Releases](../../releases) page and run it. It needs Windows 10 or 11 (x64). Releases are built from this repository by [GitHub Actions](.github/workflows/release.yml); see the [code signing policy](#code-signing-policy).

If Windows shows **"Windows protected your PC"**, click **More info → Run anyway**. That warning appears for new apps that haven't built up a download history with Microsoft yet.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| Ctrl+O | Open image |
| Ctrl+V | Paste image from clipboard |
| Ctrl+S | Save meme |
| Ctrl+Shift+C | Copy meme to clipboard |

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). It builds on Windows, Linux or macOS.

```sh
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o dist
```

The executable ends up in `dist/MemeGen.exe`.

## How it works

It's a WinForms app. `MemeRenderer.cs` turns the caption into a GDI+ `GraphicsPath` using the Impact font, then strokes it in black and fills it in white. The font size starts large and steps down until the wrapped text fits within the image width and takes up at most 30% of its height. `MainForm.cs` contains the UI.

## Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

- Committers and reviewers: [nddevel](https://github.com/nddevel)
- Approvers: [nddevel](https://github.com/nddevel)

Release binaries are built from the source in this repository by GitHub Actions, and each release is manually approved before signing.

## Privacy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

MemeGen works entirely offline. Apart from the images you save, the only file it writes is your theme choice, stored in `%APPDATA%\MemeGen\theme.txt`.

## License

[MIT](LICENSE)
