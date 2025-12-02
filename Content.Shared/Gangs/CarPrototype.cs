using Robust.Shared.Prototypes;

namespace Content.Shared.Gangs;

/// <summary>
/// Prototype that defines a car: prisoner grouping.
/// </summary>
[Prototype("car")] // matches 'type:' cars.yml
public sealed class CarPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; set; } = default!;
}
