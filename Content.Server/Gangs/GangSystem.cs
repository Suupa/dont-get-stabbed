using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Gangs;
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs.Components;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Gangs;

public sealed class GangSystem : SharedGangSystem
{
    //TODO move some of this stuff to SharedGangSystem

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private static readonly Random Rnd = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartAttemptEvent ev)
    {

    }

    public void SortIntoGangs()
    {
        var enumerator = EntityQueryEnumerator<GangMemberRoleComponent>();

        //assign gangs to gangmembers
        while (enumerator.MoveNext(out var mindRoleId, out var gangMemberComp))
        {
            var mob = _role.GetMobFromMindRole(mindRoleId);

            if (mob == null)
                continue;

            if (!TryComp<InmateComponent>(mob, out var inmateComp))
                continue;

            gangMemberComp.Gang = GetGangByCar(inmateComp.Car);
            //explicitly marks that component on that entity as "changed." This tells the server's networking system that the state of GangMemberRoleComponent for the entity mindRoleId has been updated and needs to be replicated (sent) to all relevant clients.
            Dirty(mindRoleId, gangMemberComp);
        }

        //pick Shot Callers
        foreach (var gang in GetPossibleGangs())
        {
            //TODO shotcallers are picked at random now. There should probably be a playtime restriction of some type
            var members = GetMembersOfGang(gang);
            if (members.Count <= 0)
                continue;
            var r = Rnd.Next(members.Count);
            MakeShotCaller(members.ElementAt(r));
        }

    }

    public bool IsShotCaller(EntityUid mob)
    {
        if (!_mind.TryGetMind(mob, out var mindId, out _))
            return false;

        return _role.MindHasRole<ShotCallerRoleComponent>(mindId, out _);
    }

    public EntityUid GetShotCallerOfInmate(EntityUid inmate)
    {
        if (!TryComp<InmateComponent>(inmate, out var comp))
            throw new ArgumentException($"{inmate} is not an inmate.");

        var shotCaller = GetShotCallerOfCar(comp.Car);
        if(shotCaller == null)
            throw new Exception($"car {comp.Car.ID} has no Shot Caller");
        return  shotCaller.Value;
    }

    public EntityUid? GetShotCallerOfGang(GangPrototype gang)
    {
        return GetShotCallerOfGang(gang.ID);
    }

    public EntityUid? GetShotCallerOfGang(string gangId)
    {
        using var shotCallersEnum = EntityQueryEnumerator<ShotCallerRoleComponent>();
        while(shotCallersEnum.MoveNext(out var shotCallerRoleId, out _))
        {
            // get the Mind entity (Parent of the Role) directly.
            var mindId = Transform(shotCallerRoleId).ParentUid;

            if (!_role.MindHasRole<GangMemberRoleComponent>(mindId, out var gangRole))
                continue;

            if (gangRole.Value.Comp2.Gang?.ID == gangId)
            {
                // mind.OwnedEntity = mob
                if (TryComp<MindComponent>(mindId, out var mind))
                    return mind.OwnedEntity;
            }
        }

        return null;
    }

    public EntityUid? GetShotCallerOfCar(CarPrototype car)
    {
        return GetShotCallerOfGang(GetGangByCar(car));
    }

    public GangPrototype GetGangByCar(CarPrototype car)
    {
        return GetGangByCarId(car.ID);
    }

    public GangPrototype GetGangByCarId(string carId)
    {
        var gang = GetPossibleGangs().FirstOrDefault(g => g.Car == carId);
        if (gang == null)
            throw new Exception($"car {carId} has no Gang");
        return gang;
    }

    public bool MakeShotCaller(EntityUid mob)
    {
        if (!_mind.TryGetMind(mob, out var mindId, out _))
            return false;

        if(!_role.MindHasRole<GangMemberRoleComponent>(mindId, out var mindComp))
            return false;

        if (mindComp.Value.Comp2.Gang == null)
            return false;

        //remove previous Shot Caller of this gang
        var currentShotCaller = GetShotCallerOfGang(mindComp.Value.Comp2.Gang);
        if(currentShotCaller != null)
            RevokeShotCallerStatus(currentShotCaller.Value);

        _role.MindAddRole(mindId, "MindRoleShotCaller");
        return true;
    }

    public bool RevokeShotCallerStatus(EntityUid mob)
    {
        return _mind.TryGetMind(mob, out var mindId, out _) && _role.MindRemoveRole<ShotCallerRoleComponent>(mindId);
    }

    public List<EntityUid> GetMembersOfGang(GangPrototype gang)
    {
        using var gangMemberEnum = EntityQueryEnumerator<GangMemberRoleComponent>();
        var members = new List<EntityUid>();
        while(gangMemberEnum.MoveNext(out var mindRoleId, out var _))
        {
            if (!TryComp<GangMemberRoleComponent>(mindRoleId, out var gangMemberRole))
                continue;

            var member = _role.GetMobFromMindRole(mindRoleId);
            if (gangMemberRole.Gang?.ID == gang.ID && member != null)
                members.Add(member.Value);
        }

        return members;
    }

    private List<GangPrototype> GetPossibleGangs()
    {
        return _prototype.EnumeratePrototypes<GangPrototype>().ToList();
    }
}
