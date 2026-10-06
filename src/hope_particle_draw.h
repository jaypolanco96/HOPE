#pragma once
#include <array>
#include <cmath>
#include <cstdint>
#include <string_view>
namespace hope {
struct SpriteVertex { float p[3]; float uv[2]; };
// Quad topology alone is not a material identity: moving garments also
// submit non-indexed geometry. Accept only small, complete sprite cards.
template<class Read> bool IsSpriteBatch(uint32_t vertices, Read read) {
  if (vertices < 4 || vertices % 4 != 0) return false;
  for (uint32_t first=0; first<vertices; first+=4) {
    std::array<SpriteVertex,4> quad;
    uint32_t corners=0;
    for (uint32_t k=0;k<4;++k) {
      quad[k]=read(first+k);
      for (float p:quad[k].p) if (!std::isfinite(p)) return false;
      uint32_t corner=0;
      for (uint32_t axis=0;axis<2;++axis) {
        const float uv=quad[k].uv[axis];
        if (!std::isfinite(uv)) return false;
        if (std::abs(uv-1.0f)<0.001f) corner|=1u<<axis;
        else if (std::abs(uv)>=0.001f) return false;
      }
      if (corners & (1u<<corner)) return false;
      corners |= 1u<<corner;
    }
    if (corners != 15) return false;
    for (uint32_t a=0;a<4;++a) for (uint32_t b=a+1;b<4;++b) {
      float d2=0;
      for (uint32_t axis=0;axis<3;++axis) {
        float d=quad[a].p[axis]-quad[b].p[axis]; d2+=d*d;
      }
      // Previously inspected sprites were 2-4 cm. Reject garment-sized
      // geometry, degenerate cards and unknown/large effects conservatively.
      if (!std::isfinite(d2) || d2<1e-10f || d2>0.25f*0.25f) return false;
    }
  }
  return true;
}
inline bool IsGarmentShader(std::string_view path) {
  return path.find("ropa")!=path.npos || path.find("cloth")!=path.npos ||
         path.find("character")!=path.npos || path.find("cac_")!=path.npos ||
         path.find("hair")!=path.npos;
}
}
