using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Gangs;

public sealed class SharedGangSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    static System.Random rnd = new();

    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent..
    }
    public void SortIntoGangs()
    {
        var enumerator = EntityQueryEnumerator<GangMemberRoleComponent>();
        var possibleGangs = _prototype.EnumeratePrototypes<GangPrototype>().ToList();

        //TODO temptest pick random shotcaller //TODO should be different per gang/car
        int r1 = rnd.Next(EntityQuery<ActorComponent>().Count());
        var shotcaller = EntityQuery<ActorComponent>().ElementAt(r1).PlayerSession.AttachedEntity;

        while (enumerator.MoveNext(out var uid, out var comp))
        {
            //TODO shouldn't be random, but be deduced from your car
            int r2 = rnd.Next(possibleGangs.Count());
            comp.Gang = possibleGangs.ElementAt(r2);
            comp.Shotcaller = shotcaller;//TODO should be the shotcaller of your gang
        }
    }

    public bool IsShotcaller(EntityUid ent)
    {
        if (
            _mind.TryGetMind(ent, out var mindId, out var mind)
            && _roleSystem.MindHasRole<GangMemberRoleComponent>(mindId, out var ent2)
            && TryComp<GangMemberRoleComponent>(ent2, out var gangMemberMindRole)
        )
        {
            var shotcaller = gangMemberMindRole.Shotcaller;
            return (shotcaller! == ent);
        }

        return false;
    }
}
