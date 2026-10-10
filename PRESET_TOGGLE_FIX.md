# Advanced light presets (1.2.2)

Advanced Toggle Lights now fetches current Home Assistant states on every press.
It applies the preset if any selected light is off or its supported settings
differ. It turns the selected lights off only when every light is already on and
matches the settings that will actually be sent.

For example: warm → cold → warm switches temperature directly without turning
the light off. Press warm again while warm is active to turn it off.

The decision uses the same capability-filtered payload construction as the
service calls. It accounts for brightness, temperature, hue/saturation, RGB,
RGBW and RGBWW white channels, and changes made outside the plugin. Unsupported
and unspecified settings are ignored. With no settings, it behaves as an ordinary
group toggle. A failed state read or missing entity prevents commands being sent.

Small device rounding differences are accepted: up to 50K for temperature and
one unit for brightness/color channels. Hue is compared around the 0°/360° wrap.
Temperature comparison accounts for the range Home Assistant clamps to and
supports older mired-based state attributes. The internal alternating on/off
flag was removed; the decision is local to the current press.

## Install and try

Double-click `../HomeAssistant_1.2.2_PresetToggleFix.lplug4` to install. This update
includes the earlier temperature API fix and preserves the HomeAssistant plugin
identity and existing action configuration. Restart Logi Plugin Service through
Options+ if the update is not immediately active.

Try cold → warm → cold. The light should stay on through each change. Press the
active preset again to turn it off, and again to turn it back on. With multiple
selected lights, any mismatch applies the preset to the whole group.

This local package targets the installed .NET 10 SDK and Logi Plugin Service
6.4.2 or later. Installation and physical hardware behavior need user testing.

## Validation

- Release build succeeded against the installed PluginApi 6.4.2.3414.
- All 52 temperature/preset regression cases passed: 36 new preset cases plus
  the 16 earlier temperature cases.
- Full suite: 1146 passed, 19 failed, 19 skipped. All 19 failing test names match
  the original-source baseline from the temperature investigation.
- Logitech's official `pack` and `verify` commands succeeded for the installer.
- `git diff --check` passed.

The same build commands in `TEMPERATURE_FIX.md` apply. Latest package staging is
`../artifacts/HomeAssistant-1.2.2-net10`.
