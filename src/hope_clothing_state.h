#pragma once
#include <cmath>
#include <cstddef>
#include <cstdint>

namespace hope {
enum class ClothMode { kUnknown, kSimulated, kSkinned };

// The guest writes 0 or 1, not a signed rotation coefficient. The other
// lanes may contain unwritten data, so only the authored x lane is tested.
inline ClothMode ClothFlagMode(float x) {
  if (!std::isfinite(x)) return ClothMode::kUnknown;
  if (std::fabs(x) <= 0.0001f) return ClothMode::kSimulated;
  if (std::fabs(x - 1.0f) <= 0.0001f) return ClothMode::kSkinned;
  return ClothMode::kUnknown;
}

inline void ClothWorldFromRows(const float rows[12], float world[16]) {
  for (int i = 0; i < 3; ++i) {
    for (int j = 0; j < 3; ++j) world[i * 4 + j] = rows[j * 4 + i];
    world[i * 4 + 3] = 0.0f;
    world[12 + i] = rows[i * 4 + 3];
  }
  world[15] = 1.0f;
}

inline bool ClothCacheUsable(uint64_t now, uint64_t captured,
                             uint32_t ctx, uint32_t cached_ctx,
                             uint32_t bytes, uint32_t cached_bytes) {
  // Rescue bridges a missed capture; it is not a new observation.
  return ctx == cached_ctx && bytes == cached_bytes && now >= captured &&
         now - captured <= 3;
}

inline bool ClothDecodeMatches(bool skinned, uint32_t ctx,
                               bool decoded_skinned, uint32_t decoded_ctx) {
  return skinned == decoded_skinned && ctx == decoded_ctx;
}

inline bool ClothSampleInClip(const float clip[4], float band) {
  for (int i = 0; i < 4; ++i) if (!std::isfinite(clip[i])) return false;
  if (clip[3] <= 0.0f) return false;
  const float w = std::fmax(clip[3], 1.0f);
  return std::fabs(clip[0]) <= band * w && std::fabs(clip[1]) <= band * w;
}

inline bool PackedPaletteSane(const float* rows, size_t count) {
  for (size_t b = 0; b < count; ++b) {
    float norm = 0.0f;
    for (int r = 0; r < 3; ++r) {
      for (int c = 0; c < 4; ++c) {
        const float f = rows[b * 12 + r * 4 + c];
        if (!std::isfinite(f)) return false;
        if (c < 3) norm += f * f;
        else if (std::fabs(f) >= 20000.0f) return false;
      }
    }
    if (!(norm > 0.0016f && norm < 2000.0f)) return false;
  }
  return count != 0;
}
}  // namespace hope
