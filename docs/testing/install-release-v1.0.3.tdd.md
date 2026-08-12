# Install and release v1.0.3 — TDD evidence

## Source

No plan file was provided. Journeys and acceptance criteria were derived from reports that a fresh GitHub download could not be installed successfully. All production and release inputs came only from the share repository at `D:\codevip\TOOL TIKTOK LIVE-20260806T062457Z-1-001\TOOL TIKTOK LIVE\TIKTOK_LIVE_BAR_DIA_PHU`.

The share repository has no `.gitnexus/` or `.codegraph/` index, so graph-based impact analysis was unavailable in the authorized scope.

## User journeys

1. As a Windows user, I can download one release ZIP, extract it, install Node dependencies, and launch the bridge and compiled game without installing Unity.
2. As an operator, settings in `TikTokBridge/.env` are loaded, while operating-system variables retain priority.
3. As a Control Panel user, the ÔNG CHÚ MMO logo loads instead of returning 404.
4. As a Windows user, batch launchers retain CRLF line endings after a GitHub checkout.

## RED/GREEN evidence

| Behavior | RED evidence | GREEN evidence | Guarantee |
|---|---|---|---|
| Load `.env` safely | `node --test test/environment.test.js` failed with `Cannot find module '../src/config/environment'` | Same command passed 3/3; full suite passed | Parses `.env`, preserves OS overrides, validates port range |
| Control Panel logo exists | `node --test test/public-assets.test.js` reported `/favicon.svg` missing three times | Same command passed; `/favicon.svg` returned HTTP 200 and `image/svg+xml` | Local UI assets referenced by HTML exist |
| Windows launchers use CRLF | Regression test reported a bare LF in `run.bat` | `node --test test/windows-package.test.js` passed after normalization | Git checkout and release retain Windows-compatible batch line endings |

## Verification results

| Check | Command or action | Result |
|---|---|---|
| Fresh GitHub clone | `git clone --branch main --single-branch ...` | PASS at `e9a65b8`; favicon present and `run.bat` CRLF |
| Clean dependency install | `npm ci` with no `node_modules` | PASS in 4.55 seconds |
| Full tests | `npm test` | PASS, 18/18 tests |
| Security smoke | `PORT=3106 npm run security:smoke` | PASS |
| Dependency audit | `npm audit --omit=dev` | PASS, 0 vulnerabilities |
| Release contents | Inspect 244 ZIP entries | PASS; required EXE/bridge/logo present, no Unity source, node_modules, or DJ media |
| Extracted release install | `npm ci` inside extracted ZIP | PASS in 4.68 seconds |
| Extracted bridge | HTTP `/api/health` and `/favicon.svg` on port 3107 | PASS |
| Extracted game | Start `Build/TIKTOK_LIVE_BAR.exe`, wait 6 seconds | PASS; process remained running with title `TikTokLiveGameUnity` |
| Release checksum | Local and GitHub asset digest | PASS: `91e54e5f8b58fedc91a1fa9bd1bc698c0438b458b352be107765097741425b9a` |

## Coverage and known gaps

The project uses Node's built-in test runner and has no configured coverage script, so no percentage was claimed. The new environment parser, public-asset references, and Windows line endings have direct regression tests. Unity source was not rebuilt because Unity `6000.2.10f1` is not installed; the existing compiled share-folder game was smoke-tested from the final extracted ZIP. The compiled game requires bridge port 3000.
