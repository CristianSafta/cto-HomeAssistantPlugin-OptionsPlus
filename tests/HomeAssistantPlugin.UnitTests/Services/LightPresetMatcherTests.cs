using System.Text.Json;
using Loupedeck.HomeAssistantPlugin.Services;

namespace Loupedeck.HomeAssistantPlugin.Tests.Services;

public class LightPresetMatcherTests
{
    [Theory]
    [InlineData("{\"color_temp_kelvin\":6494,\"color_mode\":\"color_temp\"}", "{\"color_temp_kelvin\":6500}", true)]
    [InlineData("{\"color_temp_kelvin\":2500}", "{\"color_temp_kelvin\":6500}", false)]
    [InlineData("{\"color_temp_kelvin\":2500,\"color_mode\":\"hs\"}", "{\"color_temp_kelvin\":2500}", false)]
    [InlineData("{\"color_temp_kelvin\":null}", "{\"color_temp_kelvin\":2500}", false)]
    [InlineData("{\"color_temp\":400}", "{\"color_temp_kelvin\":2500}", true)]
    [InlineData("{\"color_temp_kelvin\":2200,\"min_color_temp_kelvin\":2200}", "{\"color_temp_kelvin\":2000}", true)]
    [InlineData("{\"brightness\":254}", "{\"brightness\":255}", true)]
    [InlineData("{\"brightness\":128}", "{\"brightness\":255}", false)]
    [InlineData("{\"hs_color\":[359.8,50]}", "{\"hs_color\":[0.0001,50.0001]}", true)]
    [InlineData("{\"hs_color\":[180,50]}", "{\"hs_color\":[0,50]}", false)]
    [InlineData("{\"hs_color\":[0,100]}", "{\"hs_color\":[0,50]}", false)]
    [InlineData("{\"hs_color\":[0,50],\"color_mode\":\"color_temp\"}", "{\"hs_color\":[0,50]}", false)]
    [InlineData("{\"rgbww_color\":[255,0,0,100,50]}", "{\"rgbww_color\":[255,0,0,100,50]}", true)]
    [InlineData("{\"rgbww_color\":[255,0,0,100,50]}", "{\"rgbww_color\":[255,0,0,50,100]}", false)]
    [InlineData("{\"rgbw_color\":[255,0,0,100]}", "{\"rgbw_color\":[255,0,0,100]}", true)]
    [InlineData("{\"rgbw_color\":[255,0,0,100]}", "{\"rgbw_color\":[255,0,0,50]}", false)]
    [InlineData("{\"rgb_color\":[255,0,0]}", "{\"rgb_color\":[255,0,0]}", true)]
    [InlineData("{\"rgb_color\":[255,0,0]}", "{\"rgb_color\":[0,255,0]}", false)]
    [InlineData("{}", "{\"brightness\":255}", false)]
    [InlineData("{}", "{}", true)]
    public void ComparesOnlyRequestedSettings(string attributes, string requested, bool expected)
    {
        using var state = JsonDocument.Parse("{\"state\":\"on\",\"attributes\":" + attributes + "}");
        using var data = JsonDocument.Parse(requested);
        LightPresetMatcher.Matches(state.RootElement, data.RootElement).Should().Be(expected);
    }
}
