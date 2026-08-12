# Install and release v1.0.4 — end-to-end evidence

## Reproduced defect

Running `run.bat` from a clean v1.0.3 Release ZIP installed 114 npm packages successfully, but the launcher then printed `npm install khong thanh cong` and exited. `%ERRORLEVEL%` was expanded when the parenthesized batch block was parsed rather than immediately after `npm ci` completed.

## RED/GREEN

- RED: `node --test test/windows-package.test.js` failed because `run.bat` contained `NPM_RESULT=%ERRORLEVEL%`.
- GREEN: the launcher now uses `if errorlevel 1` immediately after npm; the targeted tests pass 2/2 and the full suite passes 19/19.

## Full clean-package journey

1. Stopped the unrelated local service occupying port 3000.
2. Built a candidate ZIP only from the share repository.
3. Extracted it into a new directory with no `node_modules`.
4. Ran exactly `cmd /d /c run.bat` with default configuration.
5. Confirmed `npm ci` created `node_modules`.
6. Confirmed `/api/health`, `/control.html`, and `/favicon.svg` returned successfully on port 3000.
7. Ran `npm run security:smoke` and `npm audit --omit=dev` successfully.
8. Confirmed one `TIKTOK_LIVE_BAR` process remained running with window title `TikTokLiveGameUnity`.
9. Confirmed Unity Player log contained `Connected to TikTok Node bridge at ws://127.0.0.1:3000`.
10. Stopped all game, bridge, and test command processes after verification.

## Result

PASS. The v1.0.4 candidate completes the intended one-click Windows installation and launch flow. The package intentionally excludes Unity source, `node_modules`, and personal DJ media.
