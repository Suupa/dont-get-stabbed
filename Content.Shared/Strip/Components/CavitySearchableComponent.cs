using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Strip.Components;
using Robust.Shared.GameStates;

[RegisterComponent, NetworkedComponent]
public sealed partial class CavitySearchableComponent : Component
{
    /// <summary>
    ///     The cavity search delay for hands.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("handDelay")]
    public TimeSpan CavitySearchDelay = TimeSpan.FromSeconds(4f);
}

/// <summary>
///     Organizes the behavior of DoAfters for Cavity search.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CavitySearchDoAfterEvent : SimpleDoAfterEvent
{
    public CavitySearchDoAfterEvent(EntityUid user, Entity<CavitySearchableComponent> target)
    {

    }

    public override DoAfterEvent Clone() => this;
}
