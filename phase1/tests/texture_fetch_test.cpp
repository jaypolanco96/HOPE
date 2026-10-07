#include <rex/graphics/pipeline/texture/info.h>
#include <cstdio>
#include <cstdlib>

using namespace rex::graphics;
using namespace rex::graphics::xenos;

static void require(bool condition, const char* message) {
  if (!condition) {
    std::fprintf(stderr, "FAIL: %s\n", message);
    std::exit(1);
  }
}

int main() {
  unsigned cases = 0;
  for (auto dimension : {DataDimension::k1D, DataDimension::k2DOrStacked,
                         DataDimension::k3D, DataDimension::kCube}) {
    for (unsigned stacked = 0; stacked <= 1; ++stacked) {
      for (unsigned depth = 0; depth <= 7; ++depth) {
        xe_gpu_texture_fetch_t fetch{};
        fetch.type = FetchConstantType::kTexture;
        fetch.format = TextureFormat::k_8_8_8_8;
        fetch.dimension = dimension;
        fetch.stacked = stacked;
        fetch.base_address = 1;
        fetch.pitch = 1;
        if (dimension == DataDimension::k1D) {
          fetch.size_1d.width = 31;
        } else if (dimension == DataDimension::k3D) {
          fetch.size_3d.width = 31;
          fetch.size_3d.height = 15;
          fetch.size_3d.depth = depth;
        } else {
          fetch.size_2d.width = 31;
          fetch.size_2d.height = 15;
          fetch.size_2d.stack_depth = depth;
        }
        TextureInfo info{};
        const bool expected =
            (!stacked || dimension == DataDimension::k2DOrStacked) &&
            (dimension != DataDimension::kCube || depth == 5);
        require(TextureInfo::Prepare(fetch, &info) == expected,
                "fetch shape acceptance matches dimension rules");
        if (expected) {
          require(info.width == 31, "valid width retained");
          require(info.height == (dimension == DataDimension::k1D ? 0u : 15u),
                  "valid height retained");
          require(info.is_stacked == (dimension == DataDimension::k2DOrStacked && stacked),
                  "valid array flag retained");
          require(info.depth == ((dimension == DataDimension::k1D ||
                                  (dimension == DataDimension::k2DOrStacked && !stacked))
                                     ? 0u : depth), "valid depth retained");
          require(info.memory.base_address == 4096 && info.memory.base_size > 0,
                  "valid texture still has usable memory layout");
        } else {
          require(info.memory.base_address == 0 && info.memory.base_size == 0,
                  "rejected texture exposes no usable memory layout");
        }
        ++cases;
      }
    }
  }
  std::printf("PASS: %u texture fetch cases, including stacked 1D rejection\n", cases);
}
