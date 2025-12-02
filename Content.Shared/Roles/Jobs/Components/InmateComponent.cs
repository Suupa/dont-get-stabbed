using Content.Shared.Gangs;

namespace Content.Shared.Roles.Jobs.Components;

[RegisterComponent]
public sealed partial class InmateComponent: Component
{
    /// <summary>
    ///     The Car this Inmate belongs to.
    /// </summary>
    [DataField]
    public CarPrototype Car { get; set; }
}
