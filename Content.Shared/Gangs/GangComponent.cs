using Robust.Shared.GameStates;

namespace Content.Shared.Gangs;


[RegisterComponent, NetworkedComponent, Access(typeof(SharedGangSystem))]
public sealed partial class GangComponent : Component
{

}
