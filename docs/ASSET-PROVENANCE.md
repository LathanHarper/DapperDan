# Asset provenance

## RichButton feedback sounds

The three files below were cut on 2026-08-20 from a fresh CodeCrafty recording of physical color-printer button presses. The private original recording remains outside this repository and was not modified.

| File | Source window | Purpose | SHA-256 |
| --- | ---: | --- | --- |
| `rich_touch.wav` | 11.035–11.170 s | normal touch | `5441EEE748B78505EE3F4418A2CAFA5FC2B0B565A42531517DCCD198AF78237A` |
| `rich_long_touch.wav` | 12.315–12.635 s | long touch; natural press/release pair | `EE67665C28C01B3B2803EFC48EC07892D302FD55827C69015C4C2926B2ECD848` |
| `rich_negative_feedback.wav` | 7.115–7.265 s | rejected/bunk feedback | `EBD00638DEC565FB0A17C184073E3F11A38F9AFF56C7AB8753E84F0C80BEB658` |

The cleaner microphone channel was isolated, then each clip received conservative band limiting, fixed-floor noise reduction, a gentle gate, short endpoint fades, and approximately -4 dBFS peak headroom. The negative sound also received a small pitch drop and low-frequency lift so rejection stays recognizable. Exports are mono 48 kHz 16-bit PCM WAV. No speech or later handling/noise section was included.

## SQLite canary seed

`src/DapperDan/Resources/Raw/dapper-dan-seed-v1.db3` is generated entirely from the public `DapperDanDbContext`, `Keiki`, and `KeikiMemory` source by `tools/DapperDan.DatabaseTool/Generate.ps1`. It contains one fixed neutral Kai row and two fixed public memories; no private schema or data enters the tool.

- SQLite `application_id`: `1145131088` (ASCII `DAPP`)
- Schema `user_version`: `1`
- Journal mode: `DELETE`; no WAL or SHM sidecars are packaged
- SHA-256: `A124C3A2CB880E49F8A52EBB7DD279A9CE4020E09A83E978F9C62A390A36A14D`

The generator normalizes EF's create script to LF before SQLite stores it in `sqlite_schema`, so Windows and Unix hosts produce the same reviewed bytes. Public CI regenerates the file, rejects byte or model drift, and validates SQLite integrity and foreign keys before the macOS build starts.

## Billboard layout canary

`billboard_canary_scene.svg`, `billboard_canary_a.svg`, and `billboard_canary_b.svg` were independently authored for Dapper Dan as public geometric test fixtures. They contain no customer identity, private footage, product screenshot, or third-party artwork. The scene deliberately provides a fixed 960×540 coordinate space; the two 960×288 faces make edge loss obvious while exercising stacked native MAUI images and opacity cross-fades.
