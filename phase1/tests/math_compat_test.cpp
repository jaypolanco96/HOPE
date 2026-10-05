#include <bit>
#include <cfenv>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <iterator>
#include <limits>

extern "C" float roundevenf(float);

int main() {
  const int original_mode = std::fegetround();
  auto volatile reference = static_cast<double (*)(double)>(std::nearbyint);
  float (*volatile candidate)(float) = roundevenf;
  const int modes[] = {FE_TONEAREST, FE_UPWARD, FE_DOWNWARD, FE_TOWARDZERO};
  const float edges[] = {
      0.0f, -0.0f, 0.5f, -0.5f, 1.5f, -1.5f, 2.5f, -2.5f,
      3.5f, -3.5f, 0.49999997f, 0.50000006f, -0.49999997f, -0.50000006f,
      8388607.5f, -8388607.5f, 8388608.0f, -8388608.0f,
      std::numeric_limits<float>::max(), std::numeric_limits<float>::denorm_min(),
      -std::numeric_limits<float>::denorm_min(),
      std::numeric_limits<float>::infinity(), -std::numeric_limits<float>::infinity()};
  uint32_t rng = 0x12345678u;
  uint32_t checked = 0;
  for (uint32_t i = 0; i < 50000u + std::size(edges); ++i) {
    rng ^= rng << 13;
    rng ^= rng >> 17;
    rng ^= rng << 5;
    const float value = i < std::size(edges) ? edges[i] : std::bit_cast<float>(rng);
    if (std::isnan(value)) {
      if (!std::isnan(candidate(value))) return 1;
      continue;
    }
    if (std::fesetround(FE_TONEAREST) != 0) return 2;
    const float expected = static_cast<float>(reference(static_cast<double>(value)));
    for (const int mode : modes) {
      if (std::fesetround(mode) != 0) return 3;
      const float actual = candidate(value);
      if (std::bit_cast<uint32_t>(actual) != std::bit_cast<uint32_t>(expected)) {
        std::fprintf(stderr, "rounding mismatch input=%08x expected=%08x actual=%08x mode=%d\n",
                     std::bit_cast<uint32_t>(value), std::bit_cast<uint32_t>(expected),
                     std::bit_cast<uint32_t>(actual), mode);
        return 4;
      }
      ++checked;
    }
  }
  std::fesetround(original_mode);
  std::printf("Passed %u rounding checks across all four rounding modes, plus NaNs.\n", checked);
}
