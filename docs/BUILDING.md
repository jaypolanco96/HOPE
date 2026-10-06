# Building HOPE on Windows

HOPE's launcher is Windows WPF. The native game supports D3D12 and Vulkan.
Other-platform HOPE builds have not been validated.

## Requirements

- Visual Studio Build Tools with the Windows SDK and an x64 developer shell.
- LLVM/Clang, CMake 3.25 or newer, Ninja, and Python 3. The local diagnostic
  build uses Clang 23.1.2 with clang-cl.
- .NET 8 SDK to build the launcher; .NET 8 Windows Desktop runtime to run it.
- Your own extracted Skate 3 Xbox 360 game, including default.xex and
  data/webkit/EAWebkit.xex, plus the appropriate Title Update 3 package.

No retail game dump, title-update package, generated retail recompilation
output, gameplay backgrounds or installed binaries are committed here.

## Checkout

The HOPE and HOPE-SDK repositories are private. Authenticate with an account
that has access before cloning. For GitHub CLI users:

```powershell
gh auth login
gh auth setup-git
git clone --recurse-submodules https://github.com/jaypolanco96/HOPE.git
cd HOPE
```

The pinned HOPE SDK carries compatibility, input, save and PC settings changes.
Do not replace it with the upstream SDK pin. Original dependency sources and
notices remain credited. The full historical compatibility patch is also
preserved in phase1/sdk-build-compatibility.patch.

## Native game

Run these commands from an x64 Visual Studio developer environment with
LLVM and Ninja on PATH. Substitute your own paths:

```powershell
cmake --preset relwithdebinfo -DCMAKE_C_COMPILER=clang-cl -DCMAKE_CXX_COMPILER=clang-cl -DSKATE3_GAME_DATA_ROOT="C:/path/to/game" -DSKATE3_TITLE_UPDATE_PACKAGE="C:/path/to/TU_12K2276_000000C000000.00000000000O3"
cmake --build --preset relwithdebinfo --target generate-all --parallel 4
cmake --preset relwithdebinfo
cmake --build --preset relwithdebinfo --target skate3 --parallel 4
```

The second configure sees the generated recompilation sources. The first
configure's game/update paths remain in the build cache. If Python resolves
to the Windows Store alias, explicitly set PYTHON_EXECUTABLE and
Python3_EXECUTABLE to your installed Python executable on the first configure.

Outputs are under out/build/relwithdebinfo. The game executable and matching
rexruntimerd.dll must be kept together, along with staged title-update patches
and your supplied game data. The recorded local diagnostic directory
out/build/phase1 is a developer-specific build tree, not a required location.
Use config/skate3.example.toml when preparing a new runtime configuration;
do not copy another player's absolute paths, settings or save data.

## Launcher

```powershell
dotnet build launcher/HOPE.Launcher.csproj -c Release -o out/hope-launcher
```

Copy all launcher output files into the game bundle. Use portable.txt when
preparing an isolated portable career; it keeps user data in that bundle.
Preserve any existing career before switching storage locations.

The original icon is embedded in the EXE/window. Authentic backgrounds are
not committed. Prepare your own with launcher/Prepare-Artwork.ps1 and local
FFmpeg, or select your own screenshots in Game setup.

## Verification

```powershell
out/hope-launcher/HOPE.exe --self-test --test-report out/hope-launcher-tests.txt
cmake --build --preset relwithdebinfo --target hope_clothing_state_test hope_particle_draw_test hope_particle_texture_test --parallel 4
out/build/relwithdebinfo/hope_clothing_state_test.exe
out/build/relwithdebinfo/hope_particle_draw_test.exe
out/build/relwithdebinfo/hope_particle_texture_test.exe
python -m unittest discover -s phase4/tests -p "test_*.py" -v
```

Close Skate 3 before launcher fixtures: their career-safety checks detect
running games globally. Use temporary roots with portable.txt for new save
or settings fixtures. Link/junction coverage needs the optional --links
fixture described in launcher/SelfTests.cs; report skipped coverage honestly.

Particle shader regeneration uses phase4/compile_particle_shaders.py with
a SPIR-V-capable DXC executable. It compiles only the dedicated particle
shader, verifies varying/resource bindings, and checks that the original
scene/cloth shader source and table still match the pre-particle baseline.
Never regenerate the character shader table merely to change particle art.

These commands describe the existing build setup. A fresh remote checkout
still needs game files and has not been rebuilt end-to-end on another machine.
