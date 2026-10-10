using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Loupedeck.HomeAssistantPlugin.Services;

namespace Loupedeck.HomeAssistantPlugin.Tests.Actions;

public class AdvancedLightPresetToggleTests
{
    [Fact]
    public void AlternatingWarmAndColdPresets_AppliesChangesWithoutTurningOff()
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "off", 2500);
        var warm = fixture.CreateAction();
        var cold = fixture.CreateAction();

        fixture.Press(warm, 2500).Should().BeTrue();
        fixture.Press(cold, 6500).Should().BeTrue();
        fixture.Press(warm, 2500).Should().BeTrue();
        fixture.Press(cold, 6500).Should().BeTrue();

        fixture.Calls.Should().OnlyContain(call => call.Service == "turn_on");
        fixture.Lights["light.test"]["attributes"]!["color_temp_kelvin"]!.GetValue<int>().Should().Be(6500);
        fixture.Reads.Should().Be(4);

        fixture.Calls.Clear();
        fixture.Press(cold, 6500).Should().BeTrue();
        fixture.Calls.Should().ContainSingle().Which.Service.Should().Be("turn_off");
    }

    [Fact]
    public void SameActionWithDifferentParameters_AlsoSwitchesWithoutTurningOff()
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "off", 2500);
        var action = fixture.CreateAction();
        fixture.Press(action, 2500).Should().BeTrue();
        fixture.Press(action, 6500).Should().BeTrue();
        fixture.Calls.Should().OnlyContain(call => call.Service == "turn_on");
    }

    [Fact]
    public void SamePresetTogglesOff_ThenTurnsBackOn()
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "on", 6500);
        var action = fixture.CreateAction();
        fixture.Press(action, 6500).Should().BeTrue();
        fixture.Calls.Should().ContainSingle().Which.Service.Should().Be("turn_off");

        fixture.Calls.Clear();
        fixture.Press(action, 6500).Should().BeTrue();
        fixture.Calls.Should().OnlyContain(call => call.Service == "turn_on");
    }

    [Fact]
    public void ExternalChangeIsReadBeforeDecidingWhetherToTurnOff()
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "off", 2500);
        var action = fixture.CreateAction();
        fixture.Press(action, 2500).Should().BeTrue();
        fixture.Lights["light.test"]["attributes"]!["color_temp_kelvin"] = 6500;
        fixture.Calls.Clear();

        fixture.Press(action, 2500).Should().BeTrue();
        fixture.Calls.Should().OnlyContain(call => call.Service == "turn_on");
    }

    [Theory]
    [InlineData("on", 6500)]
    [InlineData("off", 2500)]
    public void MixedGroup_AppliesPresetToAllLights(string secondState, int secondKelvin)
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.first", "on", 2500);
        fixture.AddLight("light.second", secondState, secondKelvin);
        var action = fixture.CreateAction();
        var ids = new[] { "light.first", "light.second" };

        fixture.Press(action, 2500, ids: ids).Should().BeTrue();
        fixture.Calls.Should().HaveCount(4).And.OnlyContain(call => call.Service == "turn_on");

        fixture.Calls.Clear();
        fixture.Press(action, 2500, ids: ids).Should().BeTrue();
        fixture.Calls.Should().HaveCount(2).And.OnlyContain(call => call.Service == "turn_off");
    }

    [Fact]
    public void ChangedBrightnessKeepsLightOn_EvenWhenTemperatureMatches()
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "on", 2500);
        fixture.Press(fixture.CreateAction(), 2500, brightness: 128).Should().BeTrue();
        fixture.Calls.Should().OnlyContain(call => call.Service == "turn_on");
    }

    [Fact]
    public void MissingTemperatureFeedback_ReappliesInsteadOfTurningOff()
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "on", 2500);
        ((JsonObject)fixture.Lights["light.test"]["attributes"]!).Remove("color_temp_kelvin");
        fixture.Press(fixture.CreateAction(), 2500).Should().BeTrue();
        fixture.Calls.Should().OnlyContain(call => call.Service == "turn_on");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownCurrentState_DoesNotSendCommands(bool failedRead)
    {
        using var fixture = new Fixture();
        fixture.FailRead = failedRead;
        fixture.Press(fixture.CreateAction(), 2500).Should().BeFalse();
        fixture.Calls.Should().BeEmpty();
    }

    [Fact]
    public void NoParameters_PreservesOrdinaryToggleBehavior()
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "on", 2500);
        var action = fixture.CreateAction();
        fixture.Press(action, null, brightness: null).Should().BeTrue();
        fixture.Calls.Should().ContainSingle().Which.Service.Should().Be("turn_off");
        fixture.Calls.Clear();
        fixture.Press(action, null, brightness: null).Should().BeTrue();
        fixture.Calls.Should().ContainSingle().Which.Service.Should().Be("turn_on");
    }

    [Fact]
    public void UnsupportedPresetSettings_AreIgnoredForToggleDecision()
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "on", 2500);
        fixture.Lights["light.test"]["attributes"]!["supported_color_modes"] = new JsonArray("onoff");
        fixture.Press(fixture.CreateAction(), 6500, brightness: 128).Should().BeTrue();
        fixture.Calls.Should().ContainSingle().Which.Service.Should().Be("turn_off");
    }

    [Theory]
    [InlineData("hs")]
    [InlineData("rgb")]
    [InlineData("rgbw")]
    [InlineData("rgbww")]
    public void ColorPresets_ApplyChangedColorThenToggleOffWhenItMatches(string mode)
    {
        using var fixture = new Fixture();
        fixture.AddLight("light.test", "on", 2500);
        fixture.Lights["light.test"]["attributes"]!["supported_color_modes"] = new JsonArray(mode);
        var action = fixture.CreateAction();
        fixture.Press(action, null, hue: 0, saturation: 100, white: 50, coldWhite: 100).Should().BeTrue();
        fixture.Press(action, null, hue: 180, saturation: 100, white: 100, coldWhite: 50).Should().BeTrue();
        fixture.Calls.Should().OnlyContain(call => call.Service == "turn_on");

        fixture.Calls.Clear();
        fixture.Press(action, null, hue: 180, saturation: 100, white: 100, coldWhite: 50).Should().BeTrue();
        fixture.Calls.Should().ContainSingle().Which.Service.Should().Be("turn_off");
    }

    private sealed class Fixture : IDisposable
    {
        public Dictionary<string, JsonObject> Lights { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<(string Service, string Entity, JsonElement? Data)> Calls { get; } = new();
        public int Reads { get; private set; }
        public bool FailRead { get; set; }
        private readonly IHaClient _client = Substitute.For<IHaClient>();
        private readonly LightControlService _service;
        private readonly IHomeAssistantDataService _data;

        public Fixture()
        {
            _client.EnsureConnectedAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(true);
            _client.RequestAsync("get_states", Arg.Any<CancellationToken>()).Returns(_ =>
            {
                Reads++;
                return Task.FromResult<(bool, string?, string?)>(FailRead
                    ? (false, null, "Test connection failure")
                    : (true, JsonSerializer.Serialize(Lights.Values), null));
            });
            _client.CallServiceAsync("light", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var service = call.ArgAt<string>(1);
                    var id = call.ArgAt<string>(2);
                    var data = call.ArgAt<JsonElement?>(3);
                    Calls.Add((service, id, data?.Clone()));
                    Lights[id]["state"] = service == "turn_off" ? "off" : "on";
                    if (data.HasValue)
                    {
                        foreach (var property in data.Value.EnumerateObject())
                        {
                            Lights[id]["attributes"]![property.Name] = JsonNode.Parse(property.Value.GetRawText());
                            if (property.Name.EndsWith("_color", StringComparison.Ordinal))
                            {
                                Lights[id]["attributes"]!["color_mode"] = property.Name[..^6];
                            }
                            else if (property.Name == "color_temp_kelvin")
                            {
                                Lights[id]["attributes"]!["color_mode"] = "color_temp";
                            }
                        }
                    }
                    return Task.FromResult<(bool, string?)>((true, null));
                });
            _service = new LightControlService(_client, 100, 100, 100);
            _data = new HomeAssistantDataService(_client);
        }

        public void AddLight(string id, string state, int kelvin)
        {
            Lights[id] = new JsonObject
            {
                ["entity_id"] = id,
                ["state"] = state,
                ["attributes"] = new JsonObject
                {
                    ["supported_color_modes"] = new JsonArray("color_temp"),
                    ["color_mode"] = "color_temp",
                    ["brightness"] = 255,
                    ["color_temp_kelvin"] = kelvin
                }
            };
        }

        public AdvancedToggleLightsAction CreateAction()
        {
            var type = typeof(AdvancedToggleLightsAction);
            var action = (AdvancedToggleLightsAction)RuntimeHelpers.GetUninitializedObject(type);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_lightSvc", flags)!.SetValue(action, _service);
            type.GetField("_dataService", flags)!.SetValue(action, _data);
            return action;
        }

        public bool Press(AdvancedToggleLightsAction action, int? kelvin, int? brightness = 255, string[]? ids = null,
            double? hue = null, double? saturation = null, int? white = null, int? coldWhite = null)
        {
            var result = typeof(AdvancedToggleLightsAction).GetMethod("ProcessLightsIndividually", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(action, new object?[] { ids ?? new[] { "light.test" }, brightness, kelvin, hue, saturation, white, coldWhite });
            return (bool)result!;
        }

        public void Dispose() => _service.Dispose();
    }
}
