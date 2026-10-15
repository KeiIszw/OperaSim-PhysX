# Livox MID-360 visual

Tessellated from Livox official MID-360 CAD (2023-01-10).
Source: https://www.livoxtech.com/mid-360/downloads
CAD: https://terra-1-g.djicdn.com/65c028cd298f4669a7f0e40e50ba1131/Mid360/mid-360-asm.stp
Manufacturer retains rights to the CAD geometry. Materials are approximate visual choices, not optical simulation.

Millimetres converted to metres; CAD Y-up retained, Z reflected for Unity handedness and triangle winding reversed. CAD origin is retained (near optical centre), not moved to the mounting base. Body specification 65 x 65 x 60 mm; connector protrudes beyond nominal width.

Editor installer creates Mesh, materials and reusable prefab, then parents it under the existing lidar1 at identity transform. Original Mid-360/Model renderers are disabled. No collider is added, and sensor/publisher transforms and settings remain intact.
Menu: Tools/OperaSim/Install MID-360 Visual. Automatic installation only runs for clean SimpleScene outside Play mode.

Revision 2 imports CAD surface shells as well as solids (1714 faces). Internal periodic face 1347 (19 mm^2, y=6.6-7.1 mm) could not be triangulated and is omitted. The outer housing and fins are included.
