# HOPE 0.6.1 particle textures

Enable **Graphics > Particle draws (experimental)** with the Native renderer.
Particles default off; installation preserves your current choices. The
scene's transparent-draw setting must also be enabled (its default).

Captured particle quadlists use an original HOPE 128 x 128 RGBA dust sprite
with a billowed center and a transparent rim. The renderer generates and
uploads it once, then retains the GPU texture. Bilinear clamp sampling and
straight-alpha blending preserve soft edges. Particle depth writes stay
off, while depth testing hides particles behind opaque surfaces. The
diagnostic no-depth mode also retains alpha blending. Validated batch bounds
replace placeholder bounds for the existing far-to-near batch sorting.

This is an original fallback. Original effect texture selection,
per-particle color/lifetime alpha, individual quad sorting, and softened
depth intersections remain unfinished. Particles remain experimental
pending actual gameplay inspection. No frame-rate improvement is claimed.

The generator is in src/hope_particle_texture.h. Its test checks transparent
borders, gradual coverage, filtering-friendly RGB, bounded opacity,
deterministic output, and aligned upload pitch. Four scene color shader
variants compile for D3D12 and Vulkan, with descriptor bindings checked
against their previous SPIR-V. Vulkan variants use Microsoft's DXC v1.9.2609:
https://github.com/microsoft/DirectXShaderCompiler/releases/tag/v1.9.2609

HOPE (Hills, Ollies, Pavement, Expression) is based on Skate3Recomp by
mchughalex: https://github.com/mchughalex/skate3recomp. Supply your own
Skate 3 Xbox 360 ISO; HOPE provides no game download links.
