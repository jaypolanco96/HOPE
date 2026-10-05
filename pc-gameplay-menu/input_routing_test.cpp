#include <rex/input/pc_menu_input.h>
#include <iostream>
#include <stdexcept>
int main() {
  rex::input::PcMenuInput input;
  int checks = 0;
  auto check = [&](bool value, const char* message) { if (!value) throw std::runtime_error(message); ++checks; };
  constexpr uint16_t start = 0x10, a = 0x1000, rb = 0x200, chord = rb | start;
  try {
    auto result = input.Update(start, chord, true);
    check(!result.open_menu && result.guest_buttons == start, "Start reaches original Skate menu");
    result = input.Update(start, chord, true);
    check(!result.open_menu && result.guest_buttons == start, "Held Start remains guest input");
    input.Update(0, chord, true);
    result = input.Update(start | a, chord, true);
    check(!result.open_menu && result.guest_buttons == (start | a), "Other original menu controls preserved");
    input.Update(0, chord, true);
    result = input.Update(rb, chord, true);
    check(!result.open_menu && result.guest_buttons == rb, "RB alone reaches game");
    result = input.Update(chord, chord, true);
    check(result.open_menu && result.guest_buttons == 0, "RB then Start opens PC settings and consumes chord");
    check(!input.Update(chord, chord, true).open_menu, "Held chord does not repeat");
    input.Update(0, chord, true);
    check(input.Update(chord, chord, true).open_menu, "Released chord opens again");
    input.Update(0, chord, true);
    result = input.Update(chord, chord, false);
    check(!result.open_menu && result.guest_buttons == chord, "No callback preserves input");
    input.Reset();
    result = input.Update(chord, chord, true);
    check(!result.open_menu && result.guest_buttons == 0, "Reconnect held chord cannot activate PC settings");
    input.Update(0, chord, true);
    check(input.Update(chord, chord, true).open_menu, "Fresh chord after reconnect opens PC settings");
    input.Update(0, chord, true);
    result = input.Update(a, chord, true);
    check(!result.open_menu && result.guest_buttons == a, "Normal gameplay buttons unchanged");
    input.Reset();
    result = input.Update(start, chord, true);
    check(!result.open_menu && result.guest_buttons == start, "Reconnect Start still reaches original game");
    std::cout << "Passed " << checks << " original-menu / PC chord routing checks.\n";
  } catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}
