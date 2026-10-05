#include <rex/input/pc_menu_input.h>
#include <iostream>
#include <stdexcept>
int main() {
  rex::input::PcMenuInput input;
  int checks = 0;
  auto check = [&](bool value, const char* message) { if (!value) throw std::runtime_error(message); ++checks; };
  constexpr uint16_t start = 0x10, a = 0x1000, chord = 0x210;
  try {
    auto result = input.Update(start, false, chord, true);
    check(!result.open_menu && result.guest_buttons == start, "Frontend Start must reach the game");
    result = input.Update(start, true, chord, true);
    check(!result.open_menu && result.guest_buttons == 0, "Held frontend Start must not auto-open pause on entering gameplay");
    input.Update(0, true, chord, true);
    result = input.Update(start | a, true, chord, true);
    check(result.open_menu && result.guest_buttons == 0, "Gameplay Menu opens PC menu without leaking to retail input");
    result = input.Update(start, true, chord, true);
    check(!result.open_menu && result.guest_buttons == 0, "Held Menu does not reopen or leak");
    input.Update(0, true, chord, true);
    result = input.Update(chord, true, chord, true);
    check(result.open_menu && result.guest_buttons == 0, "Chord and Menu trigger one callback");
    check(!input.Update(chord, true, chord, true).open_menu, "Chord does not repeat while held");
    input.Update(0, false, chord, true);
    result = input.Update(chord, false, chord, true);
    check(result.open_menu && result.guest_buttons == 0, "Explicit PC chord remains available in frontend");
    input.Update(0, true, chord, true);
    result = input.Update(start, true, chord, false);
    check(!result.open_menu && result.guest_buttons == start, "No callback preserves existing input behavior");
    input.Reset();
    result = input.Update(start, true, chord, true);
    check(!result.open_menu && result.guest_buttons == 0, "Reconnect with held Menu establishes baseline without activation");
    input.Update(0, true, chord, true);
    check(input.Update(start, true, chord, true).open_menu, "Fresh Menu press after reconnect opens PC menu");
    input.Update(0, true, chord, true);
    result = input.Update(a, true, chord, true);
    check(!result.open_menu && result.guest_buttons == a, "Normal gameplay buttons unchanged");
    std::cout << "Passed " << checks << " PC gameplay menu routing checks.\n";
  } catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}
