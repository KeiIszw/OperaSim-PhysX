# Embedded UnitySensors

`com.frj.unity-sensors` is embedded to keep its Editor assembly out of Player builds.

- Upstream: https://github.com/Field-Robotics-Japan/UnitySensors
- Source directory: `Assets/UnitySensors`
- Source commit: `b1a50503bb38017f902cb232b489de2de334d62b`
- Package version: `2.0.4`
- Local change: `Editor/UnitySensorsEditor.asmdef` sets `includePlatforms` to `["Editor"]`.

All original package assets and metadata are retained to preserve GUID references.
The embedded package takes precedence over the UnitySensorsROS dependency.
When updating this package, preserve the Editor-only assembly setting and verify a Player build.
