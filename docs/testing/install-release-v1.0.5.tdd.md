# Install and release v1.0.5 — background-music evidence

## Package change

The Windows Release package now includes `Build/DJ_MUSIC/nhacnen.MP3` because the distributable game expects background music in that directory.

- Music size: `25,538,610` bytes
- Music SHA-256: `1b1268a32ebc5a944784fcdb4767eeb2fbb2c0aa4643a80e5164cece453145a3`

## Full clean-package journey

1. Built the candidate ZIP only from the share repository.
2. Extracted it into a new directory with no `node_modules`.
3. Verified the extracted music file size and SHA-256 against the share repository.
4. Ran exactly `cmd /d /c run.bat` with default configuration.
5. Confirmed `npm ci` created `node_modules` automatically.
6. Confirmed `/api/health`, `/control.html`, and `/favicon.svg` returned HTTP 200 on port 3000.
7. Ran `npm run security:smoke` successfully and confirmed `npm audit --omit=dev` reported zero vulnerabilities.
8. Confirmed exactly one `TIKTOK_LIVE_BAR` process with window title `TikTokLiveGameUnity`.
9. Confirmed Unity `Player.log` contained both:
   - `Connected to TikTok Node bridge at ws://127.0.0.1:3000`
   - `Music playing 1/1: ...\\Build\\DJ_MUSIC\\nhacnen.MP3`
10. Stopped the test game and bridge, then confirmed zero game processes and zero listeners on port 3000.

## Result

PASS. The v1.0.5 candidate completes the one-click Windows installation and launch flow, including verified background-music playback.
