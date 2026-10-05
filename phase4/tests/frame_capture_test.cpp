#include "skate3_frame_capture.h"
#include <chrono>
#include <atomic>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <thread>
using skate3::frame_capture::Samples;
namespace capture = skate3::frame_capture;
int main() {
  int checks = 0;
  auto require = [&](bool value, const char* message) { ++checks; if (!value) throw std::runtime_error(message); };
  const auto root = std::filesystem::temp_directory_path() /
      ("hope-frame-fixture-" + std::to_string(std::chrono::steady_clock::now().time_since_epoch().count()));
  try {
    Samples samples; samples.Start(3, 1000000000);
    require(samples.Add(0, 1, true) && samples.rows().empty(), "First frame is only a baseline");
    require(samples.Add(16666000, 1, true), "Normal cadence sample");
    require(samples.rows()[0].frame_time_us == 16666 && !samples.rows()[0].transition, "Microsecond timing conversion");
    require(samples.Add(15000000, 1, true) && samples.rows().size() == 1, "Nonmonotonic timestamp ignored");
    samples.Add(33333000, 0, true);
    require(samples.rows()[1].transition, "Gameplay to menu transition flagged");
    require(!samples.Add(50000000, 0, false) && samples.full(), "Bounded capture stops at limit");
    require(samples.rows()[2].transition && !samples.rows()[2].native, "Renderer switch flagged");
    require(!samples.Add(60000000, 1, true) && samples.rows().size() == 3, "No extra allocation after cap");
    samples.Start(100, 1000000000); samples.Add(100, 1, true);
    require(!samples.Add(1000000101, 1, true) && samples.rows().empty(), "Duration limit stops capture");
    samples.Start(0); require(!samples.Add(1, 1, true), "Empty sample budget rejected");
    samples.Start(); samples.Add(0, 1, true); samples.Add(900, 1, true);
    require(samples.rows().empty(), "Submicrosecond intervals cannot create invalid CSV rows");
    require(!capture::IsRecording(), "Capture is off by default");
    std::filesystem::create_directories(root);
    std::ofstream(root / "blocked") << "sentinel";
    require(capture::Toggle(root / "blocked", "").find("Could not") == 0 && !capture::IsRecording(), "Invalid output folder leaves recorder disabled");
    auto destination = root / "capture";
    require(capture::Toggle(destination, "# fixture=synthetic\n").find("started") != std::string::npos, "Capture starts");
    capture::Record(1, true);
    std::this_thread::sleep_for(std::chrono::milliseconds(2)); capture::Record(1, true);
    // Remove the destination folder without deleting it, forcing a write failure.
    std::filesystem::rename(destination, root / "moved");
    std::ofstream(destination) << "sentinel";
    require(capture::Finish().find("Could not") == 0 && !capture::IsRecording(), "Failed write retains pending samples");
    std::filesystem::remove(destination);
    std::filesystem::rename(root / "moved", destination);
    require(capture::Finish().find("saved") != std::string::npos, "Pending capture retries successfully");
    auto file = *std::filesystem::directory_iterator(destination);
    std::ifstream in(file.path());
    std::string text((std::istreambuf_iterator<char>(in)), {});
    require(text.find("# fixture=synthetic") != std::string::npos && text.find("gameplay_context,renderer,transition") != std::string::npos, "Capture metadata and schema preserved");
    require(text.find(",1,Native,0") != std::string::npos, "Recorded sample survives failed write");
    require(capture::Finish().empty(), "Completed capture is not saved twice");
    capture::Toggle(destination, "# fixture=concurrent\n");
    std::atomic<bool> started{false};
    std::thread worker([&] { for (int i=0; i<2000; ++i) { capture::Record(1, true); if (i==2) started.store(true); } });
    while (!started.load()) std::this_thread::yield();
    const auto concurrent = capture::Finish(); worker.join();
    require(concurrent.find("saved") != std::string::npos && !capture::IsRecording(), "Stop is safe while guest samples arrive");
    size_t count = 0; for (const auto& entry : std::filesystem::directory_iterator(destination)) ++count;
    require(count == 2, "Distinct capture filenames and no staging leftovers");
    std::filesystem::remove_all(root);
    std::cout << "Passed " << checks << " bounded frame-capture checks using disposable data.\n";
  } catch (const std::exception& error) {
    std::cerr << error.what() << '\n'; std::filesystem::remove_all(root); return 1;
  }
}
