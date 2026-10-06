# HOPE 0.6.2 clothing / particle hotfix

**Superseded:** the player confirmed the shirt still detaches with particles
disabled on this build. See [the subsequent clothing state correction](CLOTHING-STATE-FIX.md).
The description below records the earlier particle isolation work.

Installed with game 2.0.0.37-dev.g66ac4f3. Seven program payload hashes were
verified. Ten existing save/settings/profile/mod and original/shared files
retained their pre-install hashes after installation and launcher tests.
Program rollback backups: HOPE/updates/hope-update-af423ede-cb8c-4b6d-a9f9-6835aa776431.

After 0.6.1, the player shirt was reported to detach or disappear during
movement. Investigation found an unsafe assumption: every captured
non-indexed quad list was given the new particle texture and alpha material.
Quad topology is not sufficient evidence of a particle draw. The exact
in-game cause has not been reproduced, so the fix still needs a movement
test on the affected player.

This build restores the original scene/character HLSL and complete original
scene SPIR-V table to the pre-particle f583fcc versions. Dust now uses its
own pixel shader and pipelines, selected only by an explicit particle tag.
Known cloth/character/hair shader names are rejected at capture. Every
accepted quad must be small, nondegenerate and have all four full sprite UV
corners. Garment UVs, larger panels, malformed or partial batches are skipped.
Synthetic particle cache keys are separate from guest mesh pointer keys.
Existing shirt pose interpolation and garment materials are unchanged.

Validation: 15 compiled particle-versus-cloth checks, 10 texture checks,
two dedicated particle shaders compiled for D3D12 and Vulkan, verified UV
varying and resource bindings, and exact original scene shader comparison.
Launcher 0.6.2 builds without warnings/errors and passes 109 fixture checks.
Native gameplay appearance is not verified by those checks.

Retest the same shirt while skating, turning, jumping and landing, with
particles enabled. If the shirt still moves incorrectly, the remaining
cloth pose/capture issue will need separate investigation; this build does
not claim a completed live-game verification.

HOPE is based on Skate3Recomp by mchughalex:
https://github.com/mchughalex/skate3recomp
