#pragma once
#include <array>
#include <cmath>
#include <cstdint>
namespace hope {
// Original soft dust sprite; RGB is straight alpha, including invisible texels
// to avoid dark fringes under bilinear filtering. Generated once per device.
inline constexpr uint32_t kParticleSize = 128;
inline auto MakeParticleTexture() {
  std::array<uint8_t, kParticleSize * kParticleSize * 4> pixels{};
  for (uint32_t y = 0; y < kParticleSize; ++y) {
    for (uint32_t x = 0; x < kParticleSize; ++x) {
      const float u = (2.0f * x / (kParticleSize - 1)) - 1.0f;
      const float v = (2.0f * y / (kParticleSize - 1)) - 1.0f;
      const float r2 = u*u + v*v;
      // Compact support gives an exactly transparent rim; the squared
      // envelope has a zero derivative at its boundary (no clipped disk).
      const float rim = std::fmax(0.0f, 1.0f - r2);
      const float billow = 0.82f + 0.18f * std::cos(u*9.0f + std::sin(v*7.0f))
                                           * std::cos(v*11.0f - u*3.0f);
      const float alpha = rim*rim * std::exp(-2.0f*r2) * billow;
      const auto i = (y*kParticleSize+x)*4;
      pixels[i] = 218; pixels[i+1] = 210; pixels[i+2] = 194;
      pixels[i+3] = static_cast<uint8_t>(std::lround(alpha*180.0f));
    }
  }
  return pixels;
}
} // namespace hope
