#pragma once
#include "skate3_native_scene.h"
#include <algorithm>
#include <cmath>
#include <unordered_map>

namespace hope {
// Render-only changes to copied frame data. Never write guest state, saves,
// simulation, physics or the renderer's retained original-pose caches.
inline bool IsCrowdPedestrian(const skate3::native_scene::DrawItem& item) {
  return item.lw_entity != 0 && (item.char_family == 3 || item.char_family == 5);
}
inline bool ValidPalette(const std::vector<float>& bones) {
  return bones.size() >= 12 && bones.size() % 12 == 0 && bones.size() <= 256 * 12 &&
         std::all_of(bones.begin(), bones.end(), [](float f) { return std::isfinite(f); });
}
inline void ApplyCrowdStyle(std::vector<skate3::native_scene::DrawItem>& items, int style) {
  if (style < 1 || style > 3) return;
  struct Anchor { float x, y, z; };
  std::unordered_map<uint32_t, Anchor> anchors;
  if (style != 1) {
    for (const auto& item : items) {
      if (!IsCrowdPedestrian(item) || item.char_family != 3 || !item.skinned || !ValidPalette(item.bones)) continue;
      const auto& b = item.bones;
      Anchor anchor{b[3], b[7], b[11]};
      for (size_t i = 0; i < b.size(); i += 12) {
        const float dx=b[i+3]-anchor.x, dy=b[i+7]-b[7], dz=b[i+11]-anchor.z;
        if (dx*dx+dy*dy+dz*dz < 9.0f) anchor.y=std::min(anchor.y,b[i+7]);
      }
      anchors.try_emplace(item.lw_entity,anchor);
    }
  }
  for (auto& item : items) {
    if (!IsCrowdPedestrian(item)) continue;
    if (style == 1) { item.hope_neon_crowd = true; continue; }
    auto it=anchors.find(item.lw_entity);
    if (it == anchors.end() || !item.skinned || !ValidPalette(item.bones)) continue;
    const float scale=style==2 ? 0.5f : 2.0f;
    const float anchor[3]={it->second.x,it->second.y,it->second.z};
    for (size_t i=0;i<item.bones.size();i+=12) {
      for (int r=0;r<3;++r) {
        for (int c=0;c<3;++c) item.bones[i+r*4+c]*=scale;
        auto& t=item.bones[i+r*4+3]; t=anchor[r]+(t-anchor[r])*scale;
      }
    }
  }
}
inline void NeonTint(float* constants) {
  constants[32]=0.84f; constants[33]=1.0f; constants[34]=0.28f; constants[35]=1.0f;
  // Green stays nonzero: the existing vertex shader must keep skinning.
}
}
