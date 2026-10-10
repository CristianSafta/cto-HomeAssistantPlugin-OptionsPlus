using System.Text.Json;
using Loupedeck.HomeAssistantPlugin.Models;
using Loupedeck.HomeAssistantPlugin.Services;

namespace Loupedeck.HomeAssistantPlugin.Tests.Services;

public class KelvinTemperatureParsingTests
{
    [Theory]
    [InlineData(2500, 400)]
    [InlineData(4000, 250)]
    [InlineData(10000, 100)]
    public void ModernKelvinAttributes_ProvideCurrentValueAndReversedBounds(int kelvin, int expectedMired)
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new
            {
                entity_id = "light.modern",
                state = "on",
                attributes = new
                {
                    supported_color_modes = new[] { "color_temp" },
                    color_temp_kelvin = kelvin,
                    min_color_temp_kelvin = 2500,
                    max_color_temp_kelvin = 10000
                }
            }
        });
        var parser = new HomeAssistantDataParser(new CapabilityService());
        var registry = new ParsedRegistryData(new(), new(), new(), new(), new());
        var light = parser.ParseLightStates(json, registry).Single();

        light.Capabilities.ColorTemp.Should().BeTrue();
        light.MinMired.Should().Be(100);
        light.MaxMired.Should().Be(400);
        light.ColorTempMired.Should().Be(expectedMired);
    }

    [Fact]
    public void CapabilityFallback_RecognizesKelvinBoundsWhenLightIsOff()
    {
        using var document = JsonDocument.Parse("""
            { "min_color_temp_kelvin": 2500, "max_color_temp_kelvin": 10000 }
            """);
        LightCaps.FromAttributes(document.RootElement).ColorTemp.Should().BeTrue();
    }
}
