# Community preview packages

HOPE preview packages are fresh installations, not save migrations. Extract
into a new writable directory and provide your own Skate 3 Xbox 360 ISO or
compatible installed game files. Retail game files, DLC, personal careers and
gameplay backgrounds are not distributed in these packages.

| Platform | Package scope | Minimum target |
| --- | --- | --- |
| Windows x64 | HOPE launcher and native game; bundled .NET launcher runtime | Windows 10/11 x64 |
| Linux x64 experimental | Native game only; GTK3, Vulkan drivers and curl required | Ubuntu 24.04 or compatible |
| macOS arm64 experimental | Native game with MoltenVK; ad-hoc signed, not notarized | Apple Silicon, macOS 15+ |

The Windows WPF launcher is not ported to Linux or macOS. Native previews do
not provide launcher career/save management, launcher mod import or launcher
DLC import. The native runtime can load owned DLC from `dlc` beside `skate3`.
Intel macOS and Linux ARM64 packages are not part of this preview.

Package checks verify archive hashes, expected architecture, third-party
notices and the absence of game/user data. The Windows launcher runs its
fixture suite against temporary portable roots. Native CI runs isolated
clothing, particle-draw and texture tests. None of those checks certify live
gameplay or successful installation on every computer. Keep the known issues
and experimental scope visible in release notes.

## Rebuilding native previews

The native source requires locally generated recompilation inputs from the
maintainer's supplied game and TU3. They are intentionally excluded from Git.
The manually dispatched `native-preview.yml` workflow receives an encrypted
archive in a draft release, with its key stored separately as the temporary
`HOPE_CODEGEN_KEY` Actions secret. No ISO, title-update package or extracted
retail game data is sent to the build jobs. Decrypted generated inputs are used
only for compilation and are never attached as release artifacts.

Use `release/codegen_inputs.py seal` locally and provide `--key-file` under
an ignored private staging directory. Never commit or print that key. Stream
the key file to `gh secret set`; upload the encrypted input to a draft release;
dispatch the native workflow with that draft's release tag. Only trusted
maintainer dispatches should receive the secret.

CI explicitly sets `SKATE3_PREGENERATED_TU3=ON`. This selects TU3 runtime
hooks and installation requirements when compiling existing TU3 inputs
without shipping the original update package. Leave that option off when
generating a normal base-game build. It must not be used for retail-generated
sources; mixing layouts can select the wrong guest hooks.

Before publication: inspect build/test results and platform dependencies,
validate every archive with `release/validate_package.py`, verify uploaded
digests, delete the encrypted input asset and temporary Actions secret, then
publish the release as a **prerelease**. Keep original Skate3Recomp by
mchughalex, ReXGlue/Xenia and EA credits readable. A build-only native preview
must not be described as a fully tested HOPE launcher port.
