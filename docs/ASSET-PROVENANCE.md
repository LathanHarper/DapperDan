# Asset provenance

## RichButton feedback sounds

The three files below were cut on 2026-08-20 from a fresh CodeCrafty recording of physical color-printer button presses. The private original recording remains outside this repository and was not modified.

| File | Source window | Purpose | SHA-256 |
| --- | ---: | --- | --- |
| `rich_touch.wav` | 11.035–11.170 s | normal touch | `5441EEE748B78505EE3F4418A2CAFA5FC2B0B565A42531517DCCD198AF78237A` |
| `rich_long_touch.wav` | 12.315–12.635 s | long touch; natural press/release pair | `EE67665C28C01B3B2803EFC48EC07892D302FD55827C69015C4C2926B2ECD848` |
| `rich_negative_feedback.wav` | 7.115–7.265 s | rejected/bunk feedback | `EBD00638DEC565FB0A17C184073E3F11A38F9AFF56C7AB8753E84F0C80BEB658` |

The cleaner microphone channel was isolated, then each clip received conservative band limiting, fixed-floor noise reduction, a gentle gate, short endpoint fades, and approximately -4 dBFS peak headroom. The negative sound also received a small pitch drop and low-frequency lift so rejection stays recognizable. Exports are mono 48 kHz 16-bit PCM WAV. No speech or later handling/noise section was included.
