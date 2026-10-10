using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Loupedeck.HomeAssistantPlugin.Services;

namespace Loupedeck.HomeAssistantPlugin.Tests.Actions;

public class LightTemperatureActionTests
{
    [Theory]
    [InlineData(false, 2000, 2000)]
    [InlineData(false, 2500, 2500)]
    [InlineData(false, 6500, 6500)]
    [InlineData(false, 1000, 2000)]
    [InlineData(false, 9000, 6500)]
    [InlineData(true, 2000, 2000)]
    [InlineData(true, 2500, 2500)]
    [InlineData(true, 6500, 6500)]
    [InlineData(true, 1000, 2000)]
    [InlineData(true, 9000, 6500)]
    public void TemperaturePreset_SendsSeparateKelvinRequest(bool areaAction, int input, int expected)
    {
        var calls = RunAction(areaAction, input, supportsTemperature: true);

        calls.Should().HaveCount(2);
        calls[0].GetProperty("brightness").GetInt32().Should().Be(255);
        calls[0].TryGetProperty("color_temp_kelvin", out _).Should().BeFalse();
        calls[1].EnumerateObject().Should().ContainSingle();
        calls[1].GetProperty("color_temp_kelvin").GetInt32().Should().Be(expected);
        foreach (var data in calls)
        {
            data.TryGetProperty("color_temp", out _).Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnsupportedTemperature_IsOmitted(bool areaAction)
    {
        var calls = RunAction(areaAction, 2500, supportsTemperature: false);
        calls.Should().ContainSingle();
        calls[0].GetProperty("brightness").GetInt32().Should().Be(255);
        calls[0].TryGetProperty("color_temp_kelvin", out _).Should().BeFalse();
    }

    private static List<JsonElement> RunAction(bool areaAction, int kelvin, bool supportsTemperature)
    {
        var calls = new List<JsonElement>();
        var client = Substitute.For<IHaClient>();
        client.CallServiceAsync("light", "turn_on", "light.test", Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls.Add(call.ArgAt<JsonElement?>(3)!.Value.Clone());
                return Task.FromResult<(bool, string?)>((true, null));
            });
        using var service = new LightControlService(client, 100, 100, 100);

        // Exercise the actual action and service code without constructing the
        // Logitech UI controls, which require a running plugin host.
        var type = areaAction ? typeof(AreaToggleLightsAction) : typeof(AdvancedToggleLightsAction);
        var action = RuntimeHelpers.GetUninitializedObject(type);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        type.GetField("_lightSvc", flags)!.SetValue(action, service);
        if (areaAction)
        {
            type.GetField("_areaLightsState", flags)!.SetValue(action, true);
        }

        var parameters = new List<object?> { "light.test", new LightCaps(false, true, supportsTemperature, false), 255, kelvin, null, null, null, null };
        if (!areaAction)
        {
            parameters.Add(true);
        }
        var result = type.GetMethod("ProcessSingleLight", flags)!.Invoke(action, parameters.ToArray());
        result.Should().Be(true);
        return calls;
    }
}
