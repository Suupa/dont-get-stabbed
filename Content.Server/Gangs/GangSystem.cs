using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Gangs;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs.Components;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Gangs;

public sealed class GangSystem : SharedGangSystem
{
    //TODO move some of this stuff to SharedGangSystem

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    //TODO technically system shouldn't have data, but this seems like the best way to do this
    //TODO see if it's possible to move this all to components
    private readonly Dictionary<string,GangPrototype> _carGangDict = new();
    private readonly Dictionary<string,EntityUid> _gangShotcallerDict = new();
    private readonly Dictionary<string, List<EntityUid>> _gangToMembersDict = new();

    private static readonly Random Rnd = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartAttemptEvent ev)
    {
        _carGangDict.Clear();
        _gangShotcallerDict.Clear();
        _gangToMembersDict.Clear();
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
            var mobNetUserId = _role.GetUserFromMindRole(mindRoleId);

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
        foreach (var (gang, members) in _gangToMembersDict)
        {
            //TODO shotcallers are picked at random now. There should probably be a playtime restriction of some type
            var r = Rnd.Next(members.Count);
            var shotCaller = members.ElementAt(r);

            if (!_mind.TryGetMind(shotCaller, out var mindId, out _))
                continue;

            if(!_role.MindHasRole<GangMemberRoleComponent>(mindId, out var mindComp))
                continue;

            mindComp.Value.Comp2.IsShotCaller = true;
            _gangShotcallerDict[gang] = shotCaller;
        }

    }

    public bool MindBelongsToShotCaller(EntityUid mindId)
    {
        return _role.MindHasRole<GangMemberRoleComponent>(mindId, out var role) && role.Value.Comp2.IsShotCaller;
    }

    public EntityUid GetShotCallerOfInmate(EntityUid inmate)
    {
        if (!TryComp<InmateComponent>(inmate, out var comp))
            throw new ArgumentException($"{inmate} is not an inmate.");

        var gang = _carGangDict[comp!.Car.ID];
        return GetShotCallerOfGang(gang.ID);
    }

    public EntityUid GetShotCallerOfGang(GangPrototype gang)
    {
        return _gangShotcallerDict[gang.ID];
    }

    public EntityUid GetShotCallerOfGang(string gangId)
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

    private List<GangPrototype> GetPossibleGangs()
    {
        return _prototype.EnumeratePrototypes<GangPrototype>().ToList();
    }
}
