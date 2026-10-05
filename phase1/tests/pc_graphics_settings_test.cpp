#include <rex/cvar.h>
#include <rex/ui/overlay/simple_settings_overlay.h>

#include <chrono>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>

REXCVAR_DEFINE_BOOL(skate3_native_render_scene_fog, true, "Test", "Fog");
REXCVAR_DEFINE_BOOL(skate3_native_render_scene_ssao, true, "Test", "AO");
REXCVAR_DEFINE_BOOL(skate3_native_render_scene_ssao_full_res, false, "Test", "AO quality");
REXCVAR_DEFINE_BOOL(skate3_native_render_scene_haze, true, "Test", "Haze");
REXCVAR_DEFINE_BOOL(skate3_native_render_scene_shafts, true, "Test", "Shafts");
REXCVAR_DEFINE_INT32(skate3_native_render_scene_msaa, 4, "Test", "MSAA");

int main() {
  const auto path = std::filesystem::temp_directory_path() /
      ("skate3-pc-settings-test-" + std::to_string(
          std::chrono::steady_clock::now().time_since_epoch().count()) + ".toml");
  try {
    std::ofstream(path) << "unrelated_setting = 123\n";
    REXCVAR_SET(skate3_native_render_scene_fog, false);
    REXCVAR_SET(skate3_native_render_scene_ssao, false);
    REXCVAR_SET(skate3_native_render_scene_ssao_full_res, true);
    REXCVAR_SET(skate3_native_render_scene_haze, false);
    REXCVAR_SET(skate3_native_render_scene_shafts, true);
    REXCVAR_SET(skate3_native_render_scene_msaa, 8);
    rex::ui::SaveSimpleSettingsConfig(path);
    REXCVAR_SET(skate3_native_render_scene_fog, true);
    REXCVAR_SET(skate3_native_render_scene_ssao, true);
    REXCVAR_SET(skate3_native_render_scene_ssao_full_res, false);
    REXCVAR_SET(skate3_native_render_scene_haze, true);
    REXCVAR_SET(skate3_native_render_scene_shafts, false);
    REXCVAR_SET(skate3_native_render_scene_msaa, 1);
    rex::cvar::LoadConfig(path);
    if (REXCVAR_GET(skate3_native_render_scene_fog) ||
        REXCVAR_GET(skate3_native_render_scene_ssao) ||
        !REXCVAR_GET(skate3_native_render_scene_ssao_full_res) ||
        REXCVAR_GET(skate3_native_render_scene_haze) ||
        !REXCVAR_GET(skate3_native_render_scene_shafts) ||
        REXCVAR_GET(skate3_native_render_scene_msaa) != 8) {
      throw std::runtime_error("Graphics settings did not survive save/reload.");
    }
    std::ifstream stream(path);
    std::string content{std::istreambuf_iterator<char>(stream), std::istreambuf_iterator<char>()};
    if (content.find("unrelated_setting = 123") == std::string::npos) {
      throw std::runtime_error("Saving graphics settings discarded an unrelated setting.");
    }
    stream.close();
    std::filesystem::remove(path);
    std::cout << "Passed graphics persistence and independent fog/haze/shafts checks.\n";
    return 0;
  } catch (const std::exception& error) {
    std::cerr << error.what() << " Fixture: " << path << '\n';
    return 1;
  }
}
