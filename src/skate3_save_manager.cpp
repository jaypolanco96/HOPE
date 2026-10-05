#include "skate3_save_manager.h"

#include <algorithm>
#include <chrono>
#include <fstream>
#include <system_error>

#if defined(_WIN32)
#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif

namespace skate3 {
namespace {

bool SafeName(const std::string& name) {
  if (name.empty() || name.front() == '.' || name == "Headers") return false;
  return std::all_of(name.begin(), name.end(), [](unsigned char c) {
    return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
           (c >= '0' && c <= '9') || c == '_' || c == '-' || c == ' ';
  });
}

// Reject symlinks/junctions in every existing component, including parents.
bool OrdinaryPath(const std::filesystem::path& path, bool allow_missing = false) {
  std::error_code ec;
  auto absolute = std::filesystem::absolute(path, ec).lexically_normal();
  if (ec) return false;
  std::filesystem::path current;
  for (const auto& part : absolute) {
    current /= part;
#if defined(_WIN32)
    const auto attributes = GetFileAttributesW(current.c_str());
    if (attributes != INVALID_FILE_ATTRIBUTES &&
        (attributes & FILE_ATTRIBUTE_REPARSE_POINT)) return false;
#endif
    auto status = std::filesystem::symlink_status(current, ec);
    if (ec == std::errc::no_such_file_or_directory && allow_missing) {
      ec.clear();
      continue;
    }
    if (ec || std::filesystem::is_symlink(status) ||
        (!std::filesystem::exists(status) && !allow_missing)) return false;
  }
  return true;
}

bool OrdinaryTree(const std::filesystem::path& path) {
  if (!OrdinaryPath(path)) return false;
  std::error_code ec;
  auto iterator = std::filesystem::recursive_directory_iterator(path, ec);
  if (ec) return false;
  for (auto end = std::filesystem::recursive_directory_iterator(); iterator != end;
       iterator.increment(ec)) {
    if (ec) return false;
    auto status = iterator->symlink_status(ec);
    if (ec || !OrdinaryPath(iterator->path()) || std::filesystem::is_symlink(status) ||
        (!std::filesystem::is_directory(status) &&
         !std::filesystem::is_regular_file(status))) return false;
  }
  return !ec;
}

}  // namespace

std::vector<SaveSlot> ListSaveSlots(const std::filesystem::path& package_root,
                                  const std::filesystem::path& header_root) {
  std::vector<SaveSlot> slots;
  if (!OrdinaryPath(package_root)) return slots;
  std::error_code ec;
  auto iterator = std::filesystem::directory_iterator(package_root, ec);
  if (ec) return slots;
  for (auto end = std::filesystem::directory_iterator(); iterator != end;
       iterator.increment(ec)) {
    if (ec) break;
    auto name = iterator->path().filename().string();
    auto status = iterator->symlink_status(ec);
    if (ec) break;
    if (SafeName(name) && std::filesystem::is_directory(status) &&
        !std::filesystem::is_symlink(status) && OrdinaryPath(iterator->path())) {
      slots.push_back({name, package_root, header_root});
    }
  }
  std::sort(slots.begin(), slots.end(), [](const auto& a, const auto& b) {
    return a.name < b.name;
  });
  return slots;
}

ArchiveSaveResult ArchiveSave(const SaveSlot& slot) {
  ArchiveSaveResult result;
  const auto package = slot.package_root / slot.name;
  const auto header = slot.header_root / (slot.name + ".header");
  // Keep recovery data outside the guest's package enumeration directory.
  // Include that directory's name to isolate profiles in the portable layout.
  const auto backups = slot.package_root.parent_path() / ".pc-save-backups" /
                       slot.package_root.filename();
  if (!SafeName(slot.name) || !OrdinaryTree(package) ||
      !OrdinaryPath(slot.header_root, true) || !OrdinaryPath(backups, true)) {
    result.error = "Save paths are missing, inaccessible, or contain links. Nothing was moved.";
    return result;
  }
  std::error_code ec;
  const bool has_header = std::filesystem::exists(header, ec);
  if (ec || (has_header && (!OrdinaryPath(header) ||
                           !std::filesystem::is_regular_file(header, ec)))) {
    result.error = "The save header is inaccessible or invalid. Nothing was moved.";
    return result;
  }
  std::filesystem::create_directories(backups, ec);
  if (ec) { result.error = ec.message(); return result; }
  const auto stamp = std::chrono::system_clock::now().time_since_epoch().count();
  for (int attempt = 0; attempt < 100; ++attempt) {
    result.recovery_path = backups / (slot.name + "-" + std::to_string(stamp) +
                                     "-" + std::to_string(attempt));
    if (std::filesystem::create_directory(result.recovery_path, ec)) break;
    if (ec || attempt == 99) {
      result.error = ec ? ec.message() : "Could not allocate a recovery folder.";
      return result;
    }
  }
  // Write recovery instructions before moving either original item.
  std::ofstream manifest(result.recovery_path / "RESTORE.txt");
  manifest << "Close Skate 3 before restoring this save.\n"
           << "Move the data directory to: " << package.string() << "\n"
           << "If present, move content.header to: " << header.string() << "\n"
           << "Do not overwrite an existing save; preserve it separately first.\n";
  manifest.close();
  if (!manifest) { result.error = "Could not write recovery instructions."; return result; }
  std::filesystem::rename(package, result.recovery_path / "data", ec);
  if (ec) { result.error = "Save removal failed: " + ec.message(); return result; }
  if (has_header) {
    std::filesystem::rename(header, result.recovery_path / "content.header", ec);
    if (ec) {
      result.error = "Header removal failed: " + ec.message();
      std::error_code rollback;
      std::filesystem::rename(result.recovery_path / "data", package, rollback);
      if (rollback) result.error += "; save data remains in recovery folder: " +
                                    result.recovery_path.string();
      return result;
    }
  }
  result.success = true;
  return result;
}

}  // namespace skate3
