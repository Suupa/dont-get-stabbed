using System.Linq;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Gangs;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs.Components;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Gangs;

public sealed class GangSystem : SharedGangSystem
{
    //TODO move some of this stuff to SharedGangSystem

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    //TODO technically system shouldn't have data, but this seems like the best way to do this
    private readonly Dictionary<string,GangPrototype> _carGangDict = new();
    private readonly Dictionary<string,EntityUid> _gangShotcallerDict = new();
    private readonly Dictionary<string, List<EntityUid>> _gangToMembersDict = new();

    static System.Random rnd = new();

    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent..
    }
    public void SortIntoGangs()
    {
        var enumerator = EntityQueryEnumerator<GangMemberRoleComponent>();
        var possibleGangs = GetPossibleGangs();

        //fill _carGangDict
        foreach(var gang in possibleGangs)
        {
            _carGangDict.TryAdd(gang.Car, gang);
        }


        //assign gangs to gangmembers
        while (enumerator.MoveNext(out var mindRoleId, out var gangMemberComp))
        {
            var mobNetUserId = GetMobFromMindRole(mindRoleId);
            if (!_players.TryGetSessionById(mobNetUserId, out var session))
                continue;

            var mob = session.AttachedEntity!.Value;

            if (!TryComp<InmateComponent>(session.AttachedEntity, out var inmateComp))
                continue;



            gangMemberComp.Gang = GetGangByCar(inmateComp.Car);
            _gangToMembersDict.TryAdd(gangMemberComp.Gang.ID, []);
            _gangToMembersDict[gangMemberComp.Gang.ID].Add(mob);
        }

        //fill _gangShotcallerDict
        foreach (var pair in _gangToMembersDict)
        {
            //TODO shotcallers are picked at random now. There should probably be a playtime restriction of some type
            var members = pair.Value;
            var r = rnd.Next(members.Count);
            _gangShotcallerDict[pair.Key] = members.ElementAt(r);
        }

    }

    private NetUserId? GetMobFromMindRole(EntityUid mindRoleId)
    {
        if (!_containers.TryGetContainingContainer((mindRoleId, null, null), out var container))
            return null;

        return _mind.GetUserFromMind(container.Owner);
    }

    private List<GangPrototype> GetPossibleGangs()
    {
        return _prototype.EnumeratePrototypes<GangPrototype>().ToList();
    }

    public bool IsShotcaller(EntityUid ent)
    {
        return ent == GetShotcallerOfInmate(ent);
    }

    public EntityUid GetShotcallerOfInmate(EntityUid inmate)
    {
        TryComp<InmateComponent>(inmate, out var comp);

        var gang = _carGangDict[comp!.Car.ID];
        return GetShotcallerOfGang(gang.ID);
    }

    public EntityUid GetShotcallerOfGang(GangPrototype gang)
    {
        return _gangShotcallerDict[gang.ID];
    }

    public EntityUid GetShotcallerOfGang(string gangId)
    {
        return _gangShotcallerDict[gangId];
    }

    public GangPrototype GetGangByCar(CarPrototype car)
    {
        return _carGangDict[car.ID];
    }

    public GangPrototype GetGangByCarId(string carId)
    {
        return _carGangDict[carId];
    }
}
