# Clothing capture and recovery correction

The player confirmed that the shirt still detaches with particles disabled
on game 2.0.0.37-dev.g66ac4f3. The previous particle-material hotfix did not
resolve this report. The gameplay appearance of this correction still
requires the affected player's movement test.

The latest local log records clothing mesh captures with c7 flag.x values
such as -0.093, 0.982, 0.793 and -0.488. Those are bone rotation coefficients,
not the authored cloth simulation flag (0 or 1). The old sign test sent a
positive foreign coefficient through the skinned path and a negative one
through the rigid path. This is evidence of incorrect mode classification;
it does not by itself prove every cause of the visible shirt problem.

The corrected capture prioritizes the owning entity's garment table and a
recent completed cloth job. That provides the shirt's current rigid world
even when the shader bank is foreign or unclassifiable. Fallback banks must
carry an actual 0/1 flag. Materials and original character shaders are kept.

Related bugs corrected:

- A healed old pose could refresh its cache timestamp and stay alive
  indefinitely. Recovery now keeps the original observation age, expires
  after three missed frames, and verifies context and vertex extent.
- Geometry decoded for rigid cloth could be reused with a skinned pose,
  including equal-payload mode changes. Decode deduplication, cache commits,
  scene/shadow draws and shape blending now enforce mode/owner consistency.
  Both directions of a mode change reset the interpolation history.
- The strict rigid-matrix score accepted samples behind the camera.
  Projection checks now require finite values and positive clip w.
- Unreadable garment table entries could falsely report a removed shirt.
  They now return uncertainty instead of removal.
- Instance palette validation checked rotations but missed nonfinite or
  recycled translation components. All packed components are now checked.

Clothing state policies have compiled regression scenarios including values
from the reported session, repeated rescue expiry, character/outfit changes,
simulation-mode transitions, affine conversion, behind-camera samples and
invalid palette translations. These are not live gameplay appearance tests.

No save, appearance selection, graphics setting or particle preference is
changed. There is no new shader compilation requirement for this update.

Based on Skate3Recomp by mchughalex.
