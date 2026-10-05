#include "skate3_user_settings.h"
#include <rex/cvar.h>
#include <chrono>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>

std::string Bytes(const std::filesystem::path& path) {
  std::ifstream file(path, std::ios::binary);
  return {std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>()};
}
int main() {
  auto root = std::filesystem::temp_directory_path() / ("hope-profile-test-" +
    std::to_string(std::chrono::steady_clock::now().time_since_epoch().count()));
  int checks = 0;
  auto require = [&](bool ok, const char* reason) { if (!ok) throw std::runtime_error(reason); ++checks; };
  try {
    auto path = skate3::ProfilesFilePath(root);
    skate3::LocalProfileStore store;
    skate3::EnsureUsableProfileStore(store, "Player");
    require(store.profiles.front().xuid == 0xB13E07DFF9AB6772ull, "Default XUID compatibility changed");
    require(skate3::SaveProfiles(path, store), "Initial profile creation failed");
    auto loaded = skate3::LoadProfiles(path);
    require(loaded.profiles.size() == 1 && loaded.selected_profile == store.selected_profile,
            "Existing profile format did not round-trip");
    auto old = Bytes(path);
    HANDLE lock = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr,
                              OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    require(lock != INVALID_HANDLE_VALUE, "Could not lock fixture");
    store.profiles.front().gamertag = "changed";
    bool saved = skate3::SaveProfiles(path, store);
    CloseHandle(lock);
    require(!saved, "Locked replacement incorrectly reported success");
    require(Bytes(path) == old, "Failed replacement altered original bytes");
    int siblings = 0;
    for (const auto& entry : std::filesystem::directory_iterator(path.parent_path())) ++siblings;
    require(siblings == 1, "Failed replacement leaked staged files");
    store.profiles.front().gamertag = "Bay\nSkater\t\"quote\"\\path\x01";
    require(skate3::SaveProfiles(path, store), "Successful replacement failed");
    loaded = skate3::LoadProfiles(path);
    require(loaded.profiles.size() == 1 && loaded.profiles.front().gamertag == store.profiles.front().gamertag,
            "Control characters broke TOML round-trip");
    auto second = skate3::MakeDefaultProfile("Second Skater");
    store.profiles.push_back(second);
    store.selected_profile = second.id;
    require(skate3::SaveProfiles(path, store), "Multiple profile save failed");
    loaded = skate3::LoadProfiles(path);
    require(loaded.profiles.size() == 2 && skate3::FindSelectedProfile(loaded)->xuid == second.xuid,
            "Selected existing profile/XUID changed");
    loaded.selected_profile = "missing-id";
    skate3::EnsureUsableProfileStore(loaded, "Player");
    require(loaded.selected_profile == loaded.profiles.front().id, "Invalid selected profile ID not repaired");
    std::ofstream(path, std::ios::trunc) << "[[profiles\nbroken =";
    old = Bytes(path);
    require(!skate3::SaveProfiles(path, store), "Malformed profile was silently overwritten");
    require(Bytes(path) == old, "Malformed profile evidence lost");
    auto blocked = root / "not-a-directory";
    std::ofstream(blocked) << "sentinel";
    require(!skate3::SaveProfiles(blocked / "profiles.toml", store), "Invalid parent reported success");
    require(Bytes(blocked) == "sentinel", "Invalid parent changed");
    rex::cvar::SetFlagByName("xam_pc_local_player", "true");
    auto offline = store.profiles.front();
    offline.signed_in = false;
    offline.live_signed_in = true;
    skate3::ApplyProfileCvars(offline);
    require(rex::cvar::Query<bool>("user_profile_signed_in"), "PC local player remained signed out");
    require(!rex::cvar::Query<bool>("user_live_signed_in"), "PC player acquired online sign-in");
    require(rex::cvar::Query<std::string>("user_profile_xuid") == skate3::FormatXuid(offline.xuid), "PC player changed save identity");
    require(Bytes(path) == old, "PC runtime sign-in rewrote profile evidence");
    std::filesystem::remove_all(root);
    std::cout << "Passed " << checks << " profile reliability checks using disposable data.\n";
    return 0;
  } catch (const std::exception& error) {
    std::cerr << error.what() << '\n';
    std::filesystem::remove_all(root);
    return 1;
  }
}
