# HOPE particle texture

dust.png is an original 128 x 128 straight-alpha RGBA sprite exported from
hope::MakeParticleTexture() in src/hope_particle_texture.h. The native
renderer generates the same pixels once and retains the GPU texture; it
does not load this review PNG from disk.

All captured particle quadlists currently use this dust fallback. Smoke,
sparks, spray, and other original game effects have not been mapped.
