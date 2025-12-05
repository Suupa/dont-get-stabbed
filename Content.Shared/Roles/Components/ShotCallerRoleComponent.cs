using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// This is used to mark Shot Callers properly, as they get Minds.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShotCallerRoleComponent : BaseMindRoleComponent;//TODO not used yet
