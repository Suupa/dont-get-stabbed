using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.GameTicking;
using Content.Shared.Gangs;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Roles.Jobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Gangs;

public sealed class GangSystem : SharedGangSystem
{
    //TODO move some of this stuff to SharedGangSystem

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private static readonly Random Rnd = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStarted);
        // runs after SharedInmateSystem to ensure Car is assigned first
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned, null, new [] { typeof(SharedInmateSystem) });
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
    }

    private void OnRoundStarted(RoundStartAttemptEvent ev)
    {

    }

    // handle cases where role is added after spawn (f.e. late join antag selection)
    private void OnRoleAdded(RoleAddedEvent args)
    {
        // role just added. Check if there's a mob ready to assign gang
        if (!TryComp<MindComponent>(args.MindId, out var mind) || mind.OwnedEntity == null)
            return; // no mob yet (OnPlayerSpawned will handle it when they spawn)

        if (!_role.MindHasRole<GangMemberRoleComponent>(args.MindId, out var roleEnt))
            return;

        var mob = mind.OwnedEntity.Value;
        AssignGang(mob, roleEnt.Value.Owner, roleEnt.Value.Comp2);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Mob, out var mindId, out _))
            return;

        // if mob doesn't have GangMember role yet, do nothing.
        if (!_role.MindHasRole<GangMemberRoleComponent>(mindId, out var roleEnt))
            return;

        // Otherwise assign gang (if not already done)
        AssignGang(ev.Mob, roleEnt.Value.Owner, roleEnt.Value.Comp2);
    }

    private void AssignGang(EntityUid mob, EntityUid roleUid, GangMemberRoleComponent gangMemberComp)
    {
        // skip if already assigned
        if (gangMemberComp.Gang != null)
            return;

        if (!TryComp<InmateComponent>(mob, out var inmateComp))
            return;

        gangMemberComp.Gang = GetGangByCar(inmateComp.Car);
        Dirty(roleUid, gangMemberComp);

        // ensure a Shot Caller exists for this gang
        if (gangMemberComp.Gang != null)
        {
            if (GetShotCallerOfGang(gangMemberComp.Gang.ID) == null)
            {
                MakeShotCaller(mob);
            }
        }
    }

    public int GetMemberCount(string gangId)
    {
        return GetMembersOfGang(gangId).Count;
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
            var mindId = Transform(shotCallerRoleId).ParentUid;

            if (!_role.MindHasRole<GangMemberRoleComponent>(mindId, out var gangRole))
                continue;

            if (gangRole.Value.Comp2.Gang?.ID == gangId)
            {
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

    public List<EntityUid> GetMembersOfGang(string gangId)
    {
        using var gangMemberEnum = EntityQueryEnumerator<GangMemberRoleComponent>();
        var members = new List<EntityUid>();
        while(gangMemberEnum.MoveNext(out var mindRoleId, out var _))
        {
            if (!TryComp<GangMemberRoleComponent>(mindRoleId, out var gangMemberRole))
                continue;

            var member = _role.GetMobFromMindRole(mindRoleId);
            if (gangMemberRole.Gang?.ID == gangId && member != null)
                members.Add(member.Value);
        }

        return members;
    }

    private List<GangPrototype> GetPossibleGangs()
    {
        return _prototype.EnumeratePrototypes<GangPrototype>().ToList();
    }
}
