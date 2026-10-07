# October 6 gameplay crash investigation

The player's runtime assertion reports `BitStream::SetOffset`, line 27,
`!(offset_bits > size_bits_)`. Local log `skate3_024.log` records XMA
context 105 requesting offset 16,416 in a 16,384-bit input buffer.

The active old XMA decoder checked this condition **after** calling
`SetOffset`. The correction validates the cursor through `TrySetOffset`
first and uses the existing input-buffer recovery path on rejection.
Strict assertions remain enabled for programmer errors.

An adjacent packet-reader defect is also corrected: `Peek` previously
loaded eight bytes unconditionally, including at short packet tails.
It now copies only the bytes needed and returns zero for a zero-bit read
without touching memory or shifting by 64.

The Windows regression target `hope_bit_stream_bounds_test` checks the
reported offset, oversized offsets, exact-end cursors, zero-bit reads,
and 6,932 bit reads against an independent bit-by-bit reference. Each
tail is directly adjacent to an inaccessible memory page, so overreads
cause a real test failure. The regression passed locally.

The same session ends with a separate invalid guest-function call to
`0x40C43E20`. Its cause is not established. The runtime now logs the
link register, count register, first argument and stack pointer at that
trap to help identify the caller. This diagnostic change does not fix
or suppress the invalid call. Graphics-pipeline creation errors also
occur earlier in the session; their relationship to the fatal call
is unknown.

Live gameplay/audio soak testing remains required. This correction
addresses the reported assertion and packet-tail overreads, and does
not establish that all gameplay crashes are resolved. Installing it
replaces only game executables and their matching runtime libraries;
saves and settings are preserved, with program rollback copies.
