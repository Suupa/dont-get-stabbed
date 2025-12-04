using Robust.Shared.Prototypes;

namespace Content.Shared.Gangs;

/// <summary>
/// Prototype that defines a prison gang.
/// </summary>
[Prototype("gang")] // matches 'type:' gangs.yml
public sealed partial class GangPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField]
    public List<string> Nicknames { get; private set; } = new();

    [DataField(required: true)]
    public string Car { get; private set; } = default!;
}
