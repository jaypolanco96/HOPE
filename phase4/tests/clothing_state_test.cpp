#include "hope_clothing_state.h"
#include <array>
#include <algorithm>
#include <iostream>
#include <limits>
#include <stdexcept>

int main() {
  int checks = 0;
  auto check = [&](bool ok, const char* name) {
    if (!ok) throw std::runtime_error(name);
    ++checks;
  };
  using hope::ClothMode;
  check(hope::ClothFlagMode(0) == ClothMode::kSimulated, "Cloth simulation flag");
  check(hope::ClothFlagMode(1) == ClothMode::kSkinned, "Inactive simulation flag");
  // Rotation coefficients recorded in skate3_018.log must never switch modes.
  for (float x : {-0.093f, -0.110f, 0.982f, 0.793f, 0.780f, 0.272f, 0.958f, -0.488f})
    check(hope::ClothFlagMode(x) == ClothMode::kUnknown, "Recorded foreign bank flag");
  check(hope::ClothFlagMode(std::numeric_limits<float>::quiet_NaN()) == ClothMode::kUnknown, "NaN flag");
  check(hope::ClothFlagMode(std::numeric_limits<float>::infinity()) == ClothMode::kUnknown, "Infinite flag");
  const float rows[12] = {0,-1,0,400, 1,0,0,70, 0,0,1,-320};
  float world[16]; hope::ClothWorldFromRows(rows, world);
  check(world[12] == 400 && world[13] == 70 && world[14] == -320 && world[15] == 1, "Owner translation preserved");
  check(world[0] == 0 && world[1] == 1 && world[4] == -1 && world[5] == 0, "Rotation transposed once");
  check(world[3] == 0 && world[7] == 0 && world[11] == 0, "Homogeneous affine");
  for (uint64_t frame = 100; frame <= 103; ++frame)
    check(hope::ClothCacheUsable(frame,100,7,7,1024,1024), "Short capture gap rescued");
  check(!hope::ClothCacheUsable(104,100,7,7,1024,1024), "Repeated rescue expires at original observation");
  check(!hope::ClothCacheUsable(99,100,7,7,1024,1024), "Reset clock cannot resurrect cache");
  check(!hope::ClothCacheUsable(101,100,8,7,1024,1024), "Other character cannot inherit shirt pose");
  check(!hope::ClothCacheUsable(101,100,7,7,2048,1024), "Outfit extent change cannot inherit pose");
  check(hope::ClothDecodeMatches(false,7,false,7), "Simulated shape matches simulated pose");
  check(hope::ClothDecodeMatches(true,7,true,7), "Skinned shape matches skinned pose");
  check(!hope::ClothDecodeMatches(true,7,false,7), "Wake-up cannot skin a retained drape");
  check(!hope::ClothDecodeMatches(false,7,true,7), "Simulation cannot use bind-pose weights");
  check(!hope::ClothDecodeMatches(false,8,false,7), "Clone decode cannot supply another character");
  float clip[4] = {0,0,.5f,1};
  check(hope::ClothSampleInClip(clip,1.5f), "In-front sample accepted");
  clip[3] = -1; check(!hope::ClothSampleInClip(clip,1.5f), "Behind-camera sample rejected");
  clip[3] = 0; check(!hope::ClothSampleInClip(clip,1.5f), "Zero projection divisor rejected");
  clip[3] = 1; clip[0] = 4;
  check(!hope::ClothSampleInClip(clip,1.5f) && hope::ClothSampleInClip(clip,6), "Near-camera relaxed band");
  clip[0] = std::numeric_limits<float>::quiet_NaN();
  check(!hope::ClothSampleInClip(clip,6), "Torn sample rejected");
  std::array<float,12> palette; std::copy(rows,rows+12,palette.begin());
  check(hope::PackedPaletteSane(palette.data(),1), "Valid owner palette");
  palette[3] = std::numeric_limits<float>::quiet_NaN();
  check(!hope::PackedPaletteSane(palette.data(),1), "NaN translation rejected");
  palette[3] = std::numeric_limits<float>::infinity();
  check(!hope::PackedPaletteSane(palette.data(),1), "Infinite translation rejected");
  palette[3] = 30000;
  check(!hope::PackedPaletteSane(palette.data(),1), "Recycled translation rejected");
  palette.fill(0); check(!hope::PackedPaletteSane(palette.data(),1), "Zero palette rejected");
  check(!hope::PackedPaletteSane(palette.data(),0), "Empty palette rejected");
  std::cout << checks << " clothing state regression checks passed\n";
}
