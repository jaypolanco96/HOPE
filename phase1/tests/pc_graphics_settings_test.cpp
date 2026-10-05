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

REXCVAR_DEFINE_BOOL(skate3_native_render_scene_hdr, true, "Test", "HDR pipeline");
REXCVAR_DEFINE_BOOL(skate3_native_render_scene_ssr, false, "Test", "Reflections");
REXCVAR_DEFINE_BOOL(skate3_native_render_scene_quadlists, false, "Test", "Particles");
REXCVAR_DEFINE_BOOL(skate3_native_render_scene_tex_mips, true, "Test", "Mipmaps");
REXCVAR_DEFINE_BOOL(skate3_native_render_scene_decals, true, "Test", "Decals");
REXCVAR_DEFINE_INT32(skate3_native_render_scene_ssr_steps, 48, "Test", "Reflection quality");
REXCVAR_DEFINE_INT32(skate3_native_render_scene_shafts_steps, 64, "Test", "Shaft quality");
REXCVAR_DEFINE_DOUBLE(skate3_native_render_scene_ssr_intensity, 1.0, "Test", "Reflection strength");
REXCVAR_DEFINE_DOUBLE(skate3_native_render_scene_ssao_intensity, 1.4, "Test", "AO strength");
REXCVAR_DEFINE_DOUBLE(skate3_native_render_scene_ssao_radius, 0.8, "Test", "AO radius");
REXCVAR_DEFINE_DOUBLE(skate3_native_render_scene_bloom_intensity, 0.025, "Test", "Bloom strength");
REXCVAR_DEFINE_DOUBLE(skate3_native_render_scene_shadow_pcss_sun_deg, 2.5, "Test", "Softness");

int main() {
  const auto path = std::filesystem::temp_directory_path() /
      ("skate3-pc-settings-test-" + std::to_string(
          std::chrono::steady_clock::now().time_since_epoch().count()) + ".toml");
  try {
    std::ofstream(path) << "unrelated_setting = 123\nhope_pedestrian_style = 2\n";
    REXCVAR_SET(skate3_native_render_scene_fog, false);
    REXCVAR_SET(skate3_native_render_scene_ssao, false);
    REXCVAR_SET(skate3_native_render_scene_ssao_full_res, true);
    REXCVAR_SET(skate3_native_render_scene_haze, false);
    REXCVAR_SET(skate3_native_render_scene_shafts, true);
    REXCVAR_SET(skate3_native_render_scene_msaa, 8);
    REXCVAR_SET(skate3_native_render_scene_hdr, false);
    REXCVAR_SET(skate3_native_render_scene_ssr, true);
    REXCVAR_SET(skate3_native_render_scene_quadlists, true);
    REXCVAR_SET(skate3_native_render_scene_tex_mips, false);
    REXCVAR_SET(skate3_native_render_scene_decals, false);
    REXCVAR_SET(skate3_native_render_scene_ssr_steps, 56);
    REXCVAR_SET(skate3_native_render_scene_shafts_steps, 32);
    REXCVAR_SET(skate3_native_render_scene_ssr_intensity, 0.75);
    REXCVAR_SET(skate3_native_render_scene_ssao_intensity, 2.0);
    REXCVAR_SET(skate3_native_render_scene_ssao_radius, 0.4);
    REXCVAR_SET(skate3_native_render_scene_bloom_intensity, 0.035);
    REXCVAR_SET(skate3_native_render_scene_shadow_pcss_sun_deg, 1.5);
    rex::ui::SaveSimpleSettingsConfig(path);
    REXCVAR_SET(skate3_native_render_scene_fog, true);
    REXCVAR_SET(skate3_native_render_scene_ssao, true);
    REXCVAR_SET(skate3_native_render_scene_ssao_full_res, false);
    REXCVAR_SET(skate3_native_render_scene_haze, true);
    REXCVAR_SET(skate3_native_render_scene_shafts, false);
    REXCVAR_SET(skate3_native_render_scene_msaa, 1);
    REXCVAR_SET(skate3_native_render_scene_hdr, true);
    REXCVAR_SET(skate3_native_render_scene_ssr, false);
    REXCVAR_SET(skate3_native_render_scene_quadlists, false);
    REXCVAR_SET(skate3_native_render_scene_tex_mips, true);
    REXCVAR_SET(skate3_native_render_scene_decals, true);
    REXCVAR_SET(skate3_native_render_scene_ssr_steps, 8);
    REXCVAR_SET(skate3_native_render_scene_shafts_steps, 8);
    REXCVAR_SET(skate3_native_render_scene_ssr_intensity, 1.0);
    REXCVAR_SET(skate3_native_render_scene_ssao_intensity, 1.0);
    REXCVAR_SET(skate3_native_render_scene_ssao_radius, 1.0);
    REXCVAR_SET(skate3_native_render_scene_bloom_intensity, 1.0);
    REXCVAR_SET(skate3_native_render_scene_shadow_pcss_sun_deg, 1.0);
    rex::cvar::LoadConfig(path);
    if (REXCVAR_GET(skate3_native_render_scene_fog) ||
        REXCVAR_GET(skate3_native_render_scene_ssao) ||
        !REXCVAR_GET(skate3_native_render_scene_ssao_full_res) ||
        REXCVAR_GET(skate3_native_render_scene_haze) ||
        !REXCVAR_GET(skate3_native_render_scene_shafts) ||
        REXCVAR_GET(skate3_native_render_scene_msaa) != 8) {
      throw std::runtime_error("Graphics settings did not survive save/reload.");
    }
    if (REXCVAR_GET(skate3_native_render_scene_hdr) ||
        !REXCVAR_GET(skate3_native_render_scene_ssr) ||
        !REXCVAR_GET(skate3_native_render_scene_quadlists) ||
        REXCVAR_GET(skate3_native_render_scene_tex_mips) ||
        REXCVAR_GET(skate3_native_render_scene_decals) ||
        REXCVAR_GET(skate3_native_render_scene_ssr_steps) != 56 ||
        REXCVAR_GET(skate3_native_render_scene_shafts_steps) != 32 ||
        REXCVAR_GET(skate3_native_render_scene_ssr_intensity) != .75 ||
        REXCVAR_GET(skate3_native_render_scene_ssao_intensity) != 2.0 ||
        REXCVAR_GET(skate3_native_render_scene_ssao_radius) != .4 ||
        REXCVAR_GET(skate3_native_render_scene_bloom_intensity) != .035 ||
        REXCVAR_GET(skate3_native_render_scene_shadow_pcss_sun_deg) != 1.5)
      throw std::runtime_error("Advanced graphics did not survive save/reload.");
    std::ifstream stream(path);
    std::string content{std::istreambuf_iterator<char>(stream), std::istreambuf_iterator<char>()};
    if (content.find("unrelated_setting = 123") == std::string::npos || content.find("hope_pedestrian_style = 2") == std::string::npos) {
      throw std::runtime_error("Saving graphics settings discarded an unrelated setting.");
    }
    stream.close();
    std::filesystem::remove(path);
    std::cout << "Passed graphics persistence, 12 advanced controls and independent fog/haze/shafts checks.\n";
    return 0;
  } catch (const std::exception& error) {
    std::cerr << error.what() << " Fixture: " << path << '\n';
    return 1;
  }
}
