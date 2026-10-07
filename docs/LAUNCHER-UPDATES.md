# Updating from the launcher

Open **Help & recovery → Check for updates**. If a newer public Windows x64
release is available, choose **Download & install**. Finish saving and close
Skate 3 first. HOPE downloads the package, verifies GitHub's SHA-256 archive
digest, prepares program files, then closes and restarts to install them.

The updater preserves game data, DLC, career saves, profiles, mods, backgrounds
and settings. Only launcher/native program files and bundled .NET runtime
files are eligible. Native executable/runtime copies in ready careers are
updated together. Program backups and a restore map are stored in `updates`.
Installation preflights exclusive file locks, verifies backups and installed
bytes, and restores changed existing files if installation fails. The restarted
launcher shows the result under Help & recovery.

Prereleases are excluded; published releases with legacy `preview` names are
accepted. A release needs one Windows x64 ZIP and GitHub's archive digest.
Older/equal versions are not offered. The installed launcher version is 0.6.6;
the currently published 0.6.5 package will therefore not downgrade it.
Committing source does not publish a downloadable update: a newer numbered
Windows package must still be built and attached to a public GitHub release.

The download is staged in a unique OS temporary folder; retail files and user
configuration in the ZIP are excluded from extraction/installation. No scripts
from the downloaded archive are executed. The installer is embedded in the
installed launcher. SHA-256 verification checks download integrity against
the release metadata; it is not an independent publisher signature.

Validation includes release selection, unsafe metadata, archive traversal,
user-file exclusions, bundled runtime extraction, exclusive-lock refusal,
checksum rejection, career updates, backups and save/settings preservation.
Tests use disposable fixture data; live self-update from a newer public
release remains to be exercised when such a package is published.
