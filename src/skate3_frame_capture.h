#pragma once
#include <cstdint>
#include <filesystem>
#include <string>
#include <vector>

namespace skate3::frame_capture {
struct Sample {
  int64_t elapsed_us, frame_time_us;
  uint32_t context;
  bool native, transition;
};
// Guest swap cadence, not measured display presentation or CPU/GPU execution time.
// Keep collection bounded and disk I/O outside the guest frame callback.
class Samples {
 public:
  void Start(size_t limit = 20000, int64_t duration_ns = 60000000000LL);
  bool Add(int64_t now_ns, uint32_t context, bool native);
  const std::vector<Sample>& rows() const { return rows_; }
  bool full() const { return full_; }
 private:
  std::vector<Sample> rows_;
  size_t limit_ = 0;
  int64_t duration_ns_ = 0, first_ns_ = 0, previous_ns_ = 0;
  uint32_t previous_context_ = 0;
  bool previous_native_ = false, baseline_ = false, full_ = false;
};
std::string Toggle(const std::filesystem::path& directory, const std::string& metadata);
void Record(uint32_t context, bool native);
bool IsRecording();
std::string Finish();
}
