using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Gangs;
using Content.Shared.GameTicking;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;

namespace Content.Server.GameTicking.Rules;

public sealed class GangRuleSystem : GameRuleSystem<GangRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GangSystem _gang = default!;
    [Dependency] private readonly SharedInmateSystem _inmate = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GangRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
        SubscribeLocalEvent<GangMemberRoleComponent, GetBriefingEvent>(OnGetBriefing);

        //run after GangSystem so that the gang is assigned before making a briefing
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned, null, [typeof(SharedInmateSystem),typeof(GangSystem)]);
    }

    //Antag selection can run BEFORE or AFTER the player spawns (late join) so both paths have to work
    private void AfterAntagSelected(Entity<GangRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var mob = args.EntityUid;
        if (!_mind.TryGetMind(mob, out var mindId, out var mindComp))
            return;
        _roleSystem.MindAddRole(mindId, "MindRoleGangMember");//triggers GangSystem.OnRoleAdded

        if (mindComp.OwnedEntity.HasValue)//if player is spawned
        {
            //LATE JOIN
            _antag.SendBriefing(mindComp.OwnedEntity.Value, MakeBriefing(mindComp.OwnedEntity.Value), null, null);
        }
        // else ROUND START JOIN
        // let OnPlayerSpawned handle Briefing
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Mob, out var mindId, out _))
            return;

        if (_roleSystem.MindHasRole<GangMemberRoleComponent>(mindId, out _))
        {
            //ROUND START JOIN
            // AfterAntagSelected has run and has set up gangmember (SetupGangMemberIfNeeded)
            // but didn't send Briefing because player had not spawned yet at that point
            // This means Gang Member is set up and we can send Briefing now
            _antag.SendBriefing(ev.Mob, MakeBriefing(ev.Mob), null, null);
        }
        //else LATE JOIN
        //let AfterAntagSelected handle the Briefing
    }

    private void OnGetBriefing(Entity<GangMemberRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
        args.Briefing = MakeBriefing(ent.Value);//TODO check if should be args.Append instead
    }

    private string MakeBriefing(EntityUid ent)
    {
        var briefing = "";
        if (
            _mind.TryGetMind(ent, out var mindId, out _)
            && _roleSystem.MindHasRole<GangMemberRoleComponent>(mindId, out var role)
            && TryComp<GangMemberRoleComponent>(role, out var gangMemberRole)
            )
        {
            if (gangMemberRole.Gang == null)
                return Loc.GetString("gangmember-role-greeting-no-gang-error"); // Or just return empty/generic

            var isShotCaller = _gang.IsShotCaller(ent);
            var rank = Loc.GetString(isShotCaller ? "gangs-the-leader" : "gangs-a-member");
            var gangName = Loc.GetString(gangMemberRole.Gang.Name);

            briefing += Loc.GetString("gangmember-role-greeting-intro",
                ("rank", rank),
                ("gangName", gangName)
            );

            briefing += " ";

            if (isShotCaller)
            {
                var car = _inmate.GetInmatesCar(ent);
                var carName = car != null ? Loc.GetString(car.Name) : "Unknown";

                briefing += Loc.GetString("gangmember-role-greeting-shotcaller",
                    ("car", carName)
                );
            }
            else
            {
                var shotCaller = _gang.GetShotCallerOfInmate(ent);
                if (shotCaller == null)
                    throw new Exception($"{ent} is a Gang Member, but belongs to a Car ({_inmate.GetInmatesCar(ent)?.ID}) without a Shot Caller. If there is no Shot Caller, he should be it!");
                var shotCallerName = MetaData(shotCaller.Value).EntityName;

                briefing += Loc.GetString("gangmember-role-greeting-member",
                    ("shotCallerName", shotCallerName),
                    ("gangName", gangName)
                );
            }

            //TODO if needs starting equipment briefing += "\n \n" + Loc.GetString("gangmember-role-greeting-equipment") + "\n";
        }
        else
        {
            Log.Warning("Cannot correctly make briefing for GangRule");
        }
        return briefing;
    }
}
