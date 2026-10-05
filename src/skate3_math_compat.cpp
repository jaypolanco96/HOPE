#if defined(_WIN32) && defined(__clang__)

#include <bit>
#include <cstdint>

// LLVM can lower SIMDe's nearest-even rounding to this C23 function.
// Windows UCRT does not provide it. Work on the IEEE-754 representation
// so the result is independent of the thread's active rounding mode.
extern "C" float roundevenf(float value) {
  const uint32_t bits = std::bit_cast<uint32_t>(value);
  const uint32_t sign = bits & 0x80000000u;
  const uint32_t magnitude = bits & 0x7fffffffu;
  if (magnitude >= 0x4b000000u) {
    // Values >= 2^23 are already integral. Arithmetic quiets signaling NaNs.
    return magnitude > 0x7f800000u ? value + value : value;
  }
  if (magnitude <= 0x3f000000u) {
    return std::bit_cast<float>(sign);  // Halfway to one rounds to even zero.
  }
  if (magnitude < 0x3f800000u) {
    return std::bit_cast<float>(sign | 0x3f800000u);
  }
  const uint32_t exponent = (magnitude >> 23) - 127u;
  const uint32_t unit = 1u << (23u - exponent);
  const uint32_t fraction = magnitude & (unit - 1u);
  uint32_t integral = magnitude & ~(unit - 1u);
  if (fraction > unit / 2u ||
      (fraction == unit / 2u && (integral & unit) != 0u)) {
    integral += unit;
  }
  return std::bit_cast<float>(sign | integral);
}

#endif
