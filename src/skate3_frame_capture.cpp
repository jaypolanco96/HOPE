#include "skate3_frame_capture.h"
#include <atomic>
#include <chrono>
#include <fstream>
#include <mutex>
#include <sstream>

namespace skate3::frame_capture {
void Samples::Start(size_t limit, int64_t duration_ns) {
  limit_ = limit; duration_ns_ = duration_ns;
  rows_.clear(); rows_.reserve(limit);
  baseline_ = false; full_ = limit == 0 || duration_ns <= 0;
}
bool Samples::Add(int64_t now_ns, uint32_t context, bool native) {
  if (full_) return false;
  if (!baseline_) {
    first_ns_ = previous_ns_ = now_ns;
    previous_context_ = context; previous_native_ = native; baseline_ = true;
    return true;
  }
  if (now_ns <= previous_ns_ || now_ns - previous_ns_ < 1000) return true;
  if (now_ns - first_ns_ > duration_ns_) { full_ = true; return false; }
  rows_.push_back({(now_ns-first_ns_)/1000, (now_ns-previous_ns_)/1000,
                   context, native, context != previous_context_ || native != previous_native_});
  previous_ns_ = now_ns; previous_context_ = context; previous_native_ = native;
  full_ = rows_.size() >= limit_;
  return !full_;
}
namespace {
std::mutex mutex;
std::atomic<bool> active{false};
Samples samples;
std::filesystem::path path;
std::string description;
bool pending = false;
uint64_t sequence = 0;

std::string SaveLocked() {
  active.store(false, std::memory_order_release);
  if (!pending) return {};
  // Retain data after a failed write so F8 can retry; no truncated result remains.
  auto staging = path; staging += ".partial";
  std::ofstream out(staging, std::ios::binary | std::ios::trunc);
  if (!out) return "Could not write performance capture. Press F8 to retry.";
  out << "# measurement=guest_swap_cadence_not_display_fps\n" << description;
  out << "frame,elapsed_us,frame_time_us,gameplay_context,renderer,transition\n";
  size_t frame = 0;
  for (const auto& row : samples.rows()) {
    out << ++frame << ',' << row.elapsed_us << ',' << row.frame_time_us << ','
        << row.context << ',' << (row.native ? "Native" : "Emulated") << ',' << row.transition << '\n';
  }
  out.flush(); out.close(); const bool good = out.good();
  std::error_code ec;
  if (!good) { std::filesystem::remove(staging, ec); return "Performance capture write failed; press F8 to retry."; }
  if (std::filesystem::exists(path, ec)) return "Capture filename already exists; data retained.";
  std::filesystem::rename(staging, path, ec);
  if (ec) return "Could not finish performance capture; press F8 to retry.";
  pending = false;
  return "Performance capture saved: " + path.string();
}
}
std::string Toggle(const std::filesystem::path& directory, const std::string& metadata) {
  std::lock_guard lock(mutex);
  if (pending) return SaveLocked();
  std::error_code ec;
  std::filesystem::create_directories(directory, ec);
  if (ec) return "Could not create performance capture folder.";
  const auto timestamp = std::chrono::system_clock::now().time_since_epoch();
  const auto us = std::chrono::duration_cast<std::chrono::microseconds>(timestamp).count();
  path = directory / ("hope_frame_capture_" + std::to_string(us) + "_" + std::to_string(++sequence) + ".csv");
  description = metadata;
  samples.Start(); pending = true;
  active.store(true, std::memory_order_release);
  return "Performance capture started (up to 60 seconds). Press F8 again to save.";
}
void Record(uint32_t context, bool native) {
  if (!active.load(std::memory_order_acquire)) return;
  std::lock_guard lock(mutex);
  if (!active.load(std::memory_order_relaxed)) return;
  const auto now = std::chrono::steady_clock::now().time_since_epoch();
  const auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(now).count();
  if (!samples.Add(ns, context, native))
    active.store(false, std::memory_order_release);
}
bool IsRecording() { return active.load(std::memory_order_acquire); }
std::string Finish() { std::lock_guard lock(mutex); return SaveLocked(); }
}
