#include <rex/stream.h>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <limits>
#define NOMINMAX
#include <Windows.h>

void require(bool ok, const char* message) {
  if (!ok) { std::fprintf(stderr, "FAIL: %s\n", message); std::exit(1); }
}

int main() {
  SYSTEM_INFO info{};
  GetSystemInfo(&info);
  const size_t page = info.dwPageSize;
  auto* memory = static_cast<uint8_t*>(VirtualAlloc(
      nullptr, page * 2, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE));
  require(memory != nullptr, "allocate guarded buffer");
  DWORD old = 0;
  require(VirtualProtect(memory + page, page, PAGE_NOACCESS, &old), "guard page");
  auto* packet = memory + page - 2048;
  rex::stream::BitStream stream(packet, 16384);
  require(!stream.TrySetOffset(16416), "reject exact reported XMA offset");
  require(stream.offset_bits() == 0, "invalid offset does not mutate cursor");
  require(!stream.TrySetOffset(std::numeric_limits<size_t>::max()), "reject huge offset");
  require(stream.TrySetOffset(16384), "accept exact end");
  require(stream.Peek(0) == 0, "zero-bit peek at inaccessible end");
  uint32_t checked = 0;
  for (size_t bytes = 1; bytes <= 8; ++bytes) {
    auto* tail = memory + page - bytes;
    for (size_t i = 0; i < bytes; ++i) tail[i] = uint8_t(0xA7 + i * 17);
    for (size_t offset = 0; offset < bytes * 8; ++offset) {
      for (size_t count = 0; count <= 57 && count <= bytes * 8 - offset; ++count) {
        uint64_t expected = 0;
        for (size_t bit = offset; bit < offset + count; ++bit)
          expected = (expected << 1) | ((tail[bit / 8] >> (7 - bit % 8)) & 1);
        rex::stream::BitStream reader(tail, bytes * 8);
        reader.SetOffset(offset);
        require(reader.Peek(count) == expected, "guarded tail agrees with bit reference");
        require(reader.offset_bits() == offset, "peek preserves cursor");
        require(reader.Read(count) == expected, "read agrees with reference");
        require(reader.offset_bits() == offset + count, "read advances exactly");
        ++checked;
      }
    }
  }
  VirtualFree(memory, 0, MEM_RELEASE);
  std::printf("PASS: XMA offset regression and %u guarded bit reads\n", checked);
}
