using Content.Shared.Gangs;
using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

[RegisterComponent, NetworkedComponent] // NetworkedComponent if you need it visible client-side
public sealed partial class GangMemberRoleComponent : BaseMindRoleComponent
{
    [DataField]
    public GangPrototype? Gang  { get; set; }

}
