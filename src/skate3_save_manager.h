#pragma once

#include <filesystem>
#include <string>
#include <vector>

namespace skate3 {

struct SaveSlot {
  std::string name;
  std::filesystem::path package_root;
  std::filesystem::path header_root;
};

// Only immediate, ordinary save-package directories are returned.
std::vector<SaveSlot> ListSaveSlots(const std::filesystem::path& package_root,
                                  const std::filesystem::path& header_root);

struct ArchiveSaveResult {
  bool success = false;
  std::filesystem::path recovery_path;
  std::string error;
};

// Call only after runtime teardown. Moves the complete package and its
// optional content header together into a recovery folder on the same volume.
ArchiveSaveResult ArchiveSave(const SaveSlot& slot);

}  // namespace skate3
