# October 7 texture assertion correction

The player's dialog reports `graphics/pipeline/texture/info.cpp`, line 45,
expression `!fetch.stacked`. This is the 1D branch of `TextureInfo::Prepare`:
the guest texture descriptor also sets the stacked flag, which is valid
only for the 2D/array dimension.

The parser now rejects this invalid combination with `false` before layout
calculation. The native renderer's three callers already check that result
and return failure without decoding/uploading the invalid texture. Invalid
3D/cube stacked flags and cube face counts receive the same validation.
Ordinary 1D, 2D, 3D, cube and supported stacked 2D fetches retain their
existing layout behavior. Assertions elsewhere remain enabled.

This treats malformed guest input as a recoverable parse failure; it does
not establish why the descriptor became invalid. A stale descriptor during
texture teardown is possible, but is not confirmed by this screenshot.
Live retesting is needed to confirm the dialog stops and visuals remain
correct. Skipping an invalid texture may leave the renderer's fallback or
previous texture visible for that draw.

`hope_texture_fetch_test` exercises the actual parser across 64 combinations
of dimensions, stacked flags and depths, checking rejection as well as
dimensions, array flags and usable memory layouts for accepted textures.
