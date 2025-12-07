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

    private static readonly Random Rnd = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GangRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
        SubscribeLocalEvent<GangMemberRoleComponent, GetBriefingEvent>(OnGetBriefing);

        //run after GangSystem so that the gang is assigned before making a briefing
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned, null, new[] {typeof(GangSystem)});
    }

    // two mutually exclusive timing paths:
    // 1) gangMember role assigned before/during spawn (round-start antag):
    // AfterAntagEntitySelected->_roleSystem.MindAddRole(... "MindRoleGangMember")->OnPlayerSpawned->SendBriefing
    // 2) role assigned after spawn (late join / late antag grant):
    // OnPlayerSpawned (no GangMember role so exits)->AfterAntagEntitySelected (adds role)->SendBriefing
    // so SendBriefing always gets called exactly once

    private void AfterAntagSelected(Entity<GangRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var mob = args.EntityUid;
        if (!_mind.TryGetMind(mob, out var mindId, out var mindComp))
            return;
        _roleSystem.MindAddRole(mindId, "MindRoleGangMember");

        // if the player is already spawned, send the briefing
        if (mindComp.OwnedEntity.HasValue)
        {
            // the role was just added, which triggers GangSystem.OnRoleAdded which assigns gang
            _antag.SendBriefing(mindComp.OwnedEntity.Value, MakeBriefing(mindComp.OwnedEntity.Value), null, null);
        }
        // otherwise OnPlayerSpawned will call SendBriefing when the player spawns
    }

    // GangSystem.OnPlayerSpawned runs first so gangs are assigned
    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Mob, out var mindId, out _))
            return;

        var hasGangRole = _roleSystem.MindHasRole<GangMemberRoleComponent>(mindId, out _);
        if (!hasGangRole)
            return;

        _antag.SendBriefing(ev.Mob, MakeBriefing(ev.Mob), null, null);
    }

    private void OnGetBriefing(Entity<GangMemberRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
        args.Append(MakeBriefing(ent.Value));
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

            var r = Rnd.Next(gangMemberRole.Gang.Nicknames.Count);
            var gangNickname = Loc.GetString(gangMemberRole.Gang.Nicknames.ElementAt(r));

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
                    ("car", carName),
                    ("gangNickname", gangNickname)
                );
            }
            else
            {
                var shotCaller = _gang.GetShotCallerOfInmate(ent);
                var shotCallerName = MetaData(shotCaller).EntityName;

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
