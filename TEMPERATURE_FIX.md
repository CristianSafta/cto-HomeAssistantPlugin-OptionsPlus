# Temperature compatibility fix (1.2.1)

The supplied log shows Home Assistant rejecting Advanced Toggle Lights requests
such as `{"color_temp":154}` with:

> not a valid option, did you mean 'color_temp_kelvin' or 'color_name'? at 'color_temp'

Both Advanced Toggle Lights and Area Toggle Lights converted the entered Kelvin
value to mireds and used the removed service argument. They now send
`{"color_temp_kelvin":6500}` directly and retain the existing separate brightness
and temperature requests. The temperature dial already sent Kelvin correctly.

Initial state parsing and live event parsing now read `min_color_temp_kelvin` and
`max_color_temp_kelvin`, converting their reversed bounds for the internal mired
dial. Legacy state attributes remain readable for older Home Assistant servers.

## Try the compiled plugin

1. Double-click `../HomeAssistant_1.2.1_TemperatureFix.lplug4` and install the update
   through Logitech's installer.
2. If necessary, use Options+ Settings → Logi Services → Restart Logi Plugin Service.
3. Try an Advanced Multi-Light action with temperature 2500K, then 6500K. Also try
   Area-Based Light Control and the existing temperature dial.
4. In `HomeAssistant.log`, the temperature service call should contain
   `color_temp_kelvin` and report success. The previous `color_temp` validation
   error should disappear.

This local package targets .NET 10 and Logi Plugin Service 6.4.2 or later, matching
the installed SDK (PluginApi 6.4.2.3414). It preserves the HomeAssistant plugin ID.
Installation and physical light behavior still need to be checked by the user.

## Build and validation

The repository retains .NET 8 as its default. To build with a newer installed
Logitech SDK, pass `-p:HomeAssistantTargetFramework=net10.0` to both restore and
build/test. Use `-p:RegisterPlugin=false` to build without registering or reloading
the installed plugin. Build the project directly to keep its intermediate paths
consistent between restore and build.

```powershell
dotnet restore tests/HomeAssistantPlugin.UnitTests/HomeAssistantPlugin.UnitTests.csproj -p:HomeAssistantTargetFramework=net10.0
dotnet build src/HomeAssistantPlugin.csproj -c Release --no-restore -p:HomeAssistantTargetFramework=net10.0 -p:RegisterPlugin=false
dotnet test tests/HomeAssistantPlugin.UnitTests/HomeAssistantPlugin.UnitTests.csproj -c Release --no-restore -p:HomeAssistantTargetFramework=net10.0 -p:RegisterPlugin=false
```

The local SDK and packaging tool are in `../.tools/dotnet10` and `../.tools/logi`.
The installer staging directory is `../artifacts/HomeAssistant-1.2.1-net10`.
It includes the plugin assembly, dependencies manifest, debug symbols, action
icons, metadata and license. Logitech host assemblies are provided by the service.
Its minimum service version is adjusted to 6.4.2 for the .NET 10 build.

Validation results:

- Release plugin compiled successfully against the installed SDK.
- All 16 new regression cases passed, covering both actions, exact Kelvin
  payloads, input limits, unsupported lights, separate brightness requests,
  modern state bounds, and temperature capability detection.
- Full fixed suite: 1110 passed, 19 failed, 19 skipped.
- Original source baseline on the same SDK: 1094 passed, 19 failed, 19 skipped.
  The names of all 19 failing tests are identical between runs.
- `logiplugintool pack` and `logiplugintool verify` succeeded.

## References

- [Home Assistant: removal of deprecated light features](https://developers.home-assistant.io/blog/2026/02/23/remove-deprecate-light-features/)
- [Logitech: building and testing C# plugins](https://logitech.github.io/actions-sdk-docs/csharp/plugin-development/introduction/)
- [Logitech: packaging and verification](https://logitech.github.io/actions-sdk-docs/csharp/plugin-development/distributing-the-plugin/)
