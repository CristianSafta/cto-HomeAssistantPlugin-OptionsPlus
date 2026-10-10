namespace Loupedeck.HomeAssistantPlugin.Services
{
    using System;
    using System.Text.Json;

    /// <summary>Compares only the supported settings that an action will send.</summary>
    internal static class LightPresetMatcher
    {
        public static Boolean Matches(JsonElement state, JsonElement requested)
        {
            if (!state.TryGetProperty("state", out var status) || status.GetString() != "on")
            {
                return false;
            }

            foreach (var setting in requested.EnumerateObject())
            {
                if (!state.TryGetProperty("attributes", out var attrs) || attrs.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                // Color temperature attributes may remain present while RGB mode is
                // active. In that case an equal temperature value is not a match.
                var mode = attrs.TryGetProperty("color_mode", out var colorMode) && colorMode.ValueKind == JsonValueKind.String
                    ? colorMode.GetString() : null;
                if (setting.Name == "color_temp_kelvin" && mode != null && mode != "color_temp")
                {
                    return false;
                }
                if (setting.Value.ValueKind == JsonValueKind.Array && mode == "color_temp")
                {
                    return false;
                }

                if (!attrs.TryGetProperty(setting.Name, out var current))
                {
                    if (setting.Name == "color_temp_kelvin" && attrs.TryGetProperty("color_temp", out var legacyTemp) &&
                        legacyTemp.ValueKind == JsonValueKind.Number && legacyTemp.TryGetInt32(out var mired) && mired > 0)
                    {
                        current = JsonSerializer.SerializeToElement(ColorTemp.MiredToKelvin(mired));
                    }
                    else
                    {
                        return false;
                    }
                }

                if (setting.Value.ValueKind == JsonValueKind.Number)
                {
                    // HA/device conversions can round temperature and brightness.
                    var tolerance = setting.Name == "color_temp_kelvin" ? 50.0 : 1.0;
                    var expected = setting.Value.GetDouble();
                    if (setting.Name == "color_temp_kelvin")
                    {
                        // HA clamps temperature to the light's supported range.
                        if (attrs.TryGetProperty("min_color_temp_kelvin", out var min) && min.ValueKind == JsonValueKind.Number)
                        {
                            expected = Math.Max(expected, min.GetDouble());
                        }
                        if (attrs.TryGetProperty("max_color_temp_kelvin", out var max) && max.ValueKind == JsonValueKind.Number)
                        {
                            expected = Math.Min(expected, max.GetDouble());
                        }
                    }
                    if (!NumberMatches(current, expected, tolerance))
                    {
                        return false;
                    }
                }
                else if (setting.Value.ValueKind == JsonValueKind.Array)
                {
                    if (current.ValueKind != JsonValueKind.Array || current.GetArrayLength() != setting.Value.GetArrayLength())
                    {
                        return false;
                    }

                    for (var i = 0; i < current.GetArrayLength(); i++)
                    {
                        if (setting.Name == "hs_color" && i == 0)
                        {
                            if (current[i].ValueKind != JsonValueKind.Number)
                            {
                                return false;
                            }
                            var distance = Math.Abs(HSBHelper.Wrap360(current[i].GetDouble()) - HSBHelper.Wrap360(setting.Value[i].GetDouble()));
                            if (Math.Min(distance, 360 - distance) > 1.0)
                            {
                                return false;
                            }
                        }
                        else if (!NumberMatches(current[i], setting.Value[i].GetDouble(), 1.0))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static Boolean NumberMatches(JsonElement current, Double requested, Double tolerance) =>
            current.ValueKind == JsonValueKind.Number && Math.Abs(current.GetDouble() - requested) <= tolerance;
    }
}
