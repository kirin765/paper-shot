# Paper Shot release — 2026-10-17

- itch.io: public and playable — https://kwan765.itch.io/paper-shot
- Google Play: version 1.0 (code 1) submitted for review; console showed Changes in review with quick checks running. Available in 178 countries/regions after approval. Managed publishing is off.
- Toss: latest bundle uploaded, listing draft saved; device test passed (owner confirmed); review pending official game-rating/registration evidence.

## Toss remaining work

The owner confirmed the mobile device test passed. Supply the published Google Play listing or rating certificate and matching official Korean registration details (registrant/business, representative, address/contact, rating date/number, producer registration, signature and comparison screenshots). These fields were left blank rather than guessed.

Latest test link:
intoss-private://paper-shot?_deploymentId=01a14a1e-b318-7a40-9b80-747122c14734&host=appsInTossHost

QR: store-assets/toss-test-qr.svg. Console links: links.json.

## Build artifacts

- Builds/Android/PaperShot.aab: release signature verified, accepted by Google Play.
- Builds/PaperShot-WebGL.zip: latest package uploaded and saved on itch.io.
- toss/paper-shot.ait: latest package uploaded to Toss.
- Unity 6000.6.2f1; Node 26.7.0 for Toss.

## Changes and verification

Removed obsolete TMP font-generation calls from build entrypoints; the game uses built-in TextMesh/IMGUI fonts. Added Privacy Policy button in Settings. Policy: https://gist.github.com/kirin765/325433caa3a451c6a32d49e5e2d43b63

Release signing key and credentials are protected under ~/.config/paper-shot-signing/. Retain for future updates. Project signing properties are git-ignored. Android release builds now fail for missing signing configuration rather than silently using debug signing.

Both Unity builds passed. Browser/itch.io game loaded, opened Settings and started practice; screenshots visually checked. Google Play AAB/listing validation passed. All declarations saved; ratings Everyone / PEGI 3 / Korean All Ages, target audience 13+. Production preview showed only non-blocking missing mapping/debug-symbol warnings. git diff --check passed.

Builds used /tmp/paper-shot-release to preserve the open Unity editor. No user gameplay edits were reverted or committed.
