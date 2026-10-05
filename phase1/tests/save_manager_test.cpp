#include "../../src/skate3_save_manager.h"

#include <chrono>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>

#if defined(_WIN32)
#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif

namespace fs = std::filesystem;
static int checks = 0;
void Check(bool condition, const char* message) {
  ++checks;
  if (!condition) throw std::runtime_error(message);
}
void Write(const fs::path& path, const char* content) {
  fs::create_directories(path.parent_path());
  std::ofstream(path) << content;
}
std::string Read(const fs::path& path) {
  std::ifstream stream(path);
  return {std::istreambuf_iterator<char>(stream), std::istreambuf_iterator<char>()};
}

int main(int argc, char** argv) {
  auto parent = fs::temp_directory_path().lexically_normal();
  if (parent.filename().empty()) parent = parent.parent_path();
  const auto root = parent / ("skate3-save-test-" + std::to_string(
      std::chrono::steady_clock::now().time_since_epoch().count()));
  if (root.parent_path() != parent || !fs::create_directory(root)) return 2;
  try {
    auto packages = root / "profile" / "454108E6" / "00000001";
    auto headers = root / "profile" / "454108E6" / "Headers" / "00000001";
    Write(packages / "ALIAS_SKATER" / "SKATER.P", "initial career");
    Write(packages / "OTHER_SAVE" / "nested" / "other.dat", "other save");
    Write(packages / "ignore.file", "not a package");
    Write(headers / "ALIAS_SKATER.header", "career metadata");
    Write(headers / "OTHER_SAVE.header", "other metadata");
    Check(skate3::ListSaveSlots(packages, headers).size() == 2, "List packages only");
    const skate3::SaveSlot career{"ALIAS_SKATER", packages, headers};
    // Simulate a save completed after selection but before runtime teardown.
    Write(packages / "ALIAS_SKATER" / "SKATER.P", "latest career");
    auto result = skate3::ArchiveSave(career);
    Check(result.success, "Archive career");
    Check(!fs::exists(packages / "ALIAS_SKATER"), "Career removed from active storage");
    Check(!fs::exists(headers / "ALIAS_SKATER.header"), "Career header removed");
    Check(Read(result.recovery_path / "data" / "SKATER.P") == "latest career", "Archive latest bytes");
    Check(Read(result.recovery_path / "content.header") == "career metadata", "Archive metadata bytes");
    Check(Read(result.recovery_path / "RESTORE.txt").find("ALIAS_SKATER") != std::string::npos, "Recovery instructions");
    Check(Read(packages / "OTHER_SAVE" / "nested" / "other.dat") == "other save", "Preserve other save");
    Check(Read(headers / "OTHER_SAVE.header") == "other metadata", "Preserve other header");
    Check(skate3::ListSaveSlots(packages, headers).size() == 1, "Exclude recovery folders");
    Check(!skate3::ArchiveSave(career).success, "No repeat removal of missing save");
    for (const auto* bad : {"", "..", "../OTHER_SAVE", "OTHER_SAVE/", "Headers", ".pc-save-backups", "C:\\outside"}) {
      Check(!skate3::ArchiveSave({bad, packages, headers}).success, "Reject invalid slot name");
    }
    Check(fs::exists(packages / "OTHER_SAVE"), "Invalid names preserve save");
    Write(packages / "NO_HEADER" / "SKATER.P", "no header");
    result = skate3::ArchiveSave({"NO_HEADER", packages, root / "missing_headers"});
    Check(result.success, "Header optional");
    Check(Read(result.recovery_path / "data" / "SKATER.P") == "no header", "Optional-header bytes");
    // A header of the wrong type must leave the original data in place.
    Write(packages / "BAD_HEADER" / "SKATER.P", "keep me");
    fs::create_directories(headers / "BAD_HEADER.header");
    Check(!skate3::ArchiveSave({"BAD_HEADER", packages, headers}).success, "Reject directory header");
    Check(Read(packages / "BAD_HEADER" / "SKATER.P") == "keep me", "Invalid header preserves package");
#if defined(_WIN32)
    // Header rename fails after package rename; verify rollback restores data.
    HANDLE locked_header = CreateFileW((headers / "OTHER_SAVE.header").c_str(),
        GENERIC_READ, 0, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    Check(locked_header != INVALID_HANDLE_VALUE, "Lock test header");
    result = skate3::ArchiveSave({"OTHER_SAVE", packages, headers});
    CloseHandle(locked_header);
    Check(!result.success, "Locked header rejects removal");
    Check(Read(packages / "OTHER_SAVE" / "nested" / "other.dat") == "other save", "Rollback restores package");
    Check(Read(headers / "OTHER_SAVE.header") == "other metadata", "Locked header retained");
    HANDLE locked_data = CreateFileW((packages / "OTHER_SAVE" / "nested" / "other.dat").c_str(),
        GENERIC_READ, 0, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    Check(locked_data != INVALID_HANDLE_VALUE, "Lock test data");
    result = skate3::ArchiveSave({"OTHER_SAVE", packages, headers});
    CloseHandle(locked_data);
    Check(!result.success, "Open content handles prevent package rename");
    Check(fs::exists(packages / "OTHER_SAVE"), "Locked data preserved");
#endif
    auto portable = root / "saves" / "0123456789ABCDEF";
    Write(portable / "ALIAS_SKATER" / "SKATER.P", "portable career");
    Write(portable / "Headers" / "ALIAS_SKATER.header", "portable metadata");
    Check(skate3::ListSaveSlots(portable, portable / "Headers").size() == 1, "Portable list excludes Headers");
    result = skate3::ArchiveSave({"ALIAS_SKATER", portable, portable / "Headers"});
    Check(result.success, "Portable layout archive");
    Check(Read(result.recovery_path / "content.header") == "portable metadata", "Portable metadata retained");
    std::error_code link_error;
    fs::create_directory_symlink(packages / "OTHER_SAVE", packages / "LINKED_SAVE", link_error);
    if (!link_error) {
      Check(!skate3::ArchiveSave({"LINKED_SAVE", packages, headers}).success, "Reject linked package");
      fs::create_directory_symlink(root, packages / "OTHER_SAVE" / "nested" / "link", link_error);
      Check(!link_error, "Create nested link fixture");
      Check(!skate3::ArchiveSave({"OTHER_SAVE", packages, headers}).success, "Reject linked package contents");
      Check(fs::exists(packages / "OTHER_SAVE"), "Linked contents preserve original");
    } else {
      std::cout << "Symlink fixture unavailable: " << link_error.message() << '\n';
    }
    // Junction fixtures may be supplied by the Windows filesystem test
    // runner, since creating symlinks requires privileges on some machines.
    if (argc == 3 && std::string(argv[1]) == "--links") {
      const fs::path fixture = argv[2];
      const auto linked_packages = fixture / "packages";
      const auto linked_headers = fixture / "headers";
      Check(!skate3::ArchiveSave({"LINKED_SAVE", linked_packages, linked_headers}).success,
            "Reject junction package");
      Check(!skate3::ArchiveSave({"ORDINARY_SAVE", linked_packages, linked_headers}).success,
            "Reject nested junction contents");
      const auto listed = skate3::ListSaveSlots(linked_packages, linked_headers);
      Check(listed.size() == 1 && listed.front().name == "ORDINARY_SAVE",
            "List excludes junction package");
      Check(Read(fixture / "outside" / "SKATER.P") == "outside fixture",
            "Junction targets untouched");
    }
    fs::remove_all(root);
    std::cout << "Passed " << checks << " save-management checks.\n";
    return 0;
  } catch (const std::exception& error) {
    std::cerr << error.what() << " (fixture retained: " << root << ")\n";
    return 1;
  }
}
