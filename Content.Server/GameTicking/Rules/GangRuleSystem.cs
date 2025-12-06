using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Gangs;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;

namespace Content.Server.GameTicking.Rules;

public sealed class GangRuleSystem : GameRuleSystem<GangRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GangSystem _gang = default!;
    [Dependency] private readonly SharedInmateSystem _inmate = default!;//TODO check if this should be moved to server
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private static readonly Random Rnd = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GangRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<GangMemberRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // Greeting upon gang member activation
    private void AfterAntagSelected(Entity<GangRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        //TODO find out where this needs to be called (this currently runs every time a new player is selected to be an antag. This should obviously be linked to roundstart instead (or something like that)
        _gang.SortIntoGangs();

        var ent = args.EntityUid;
        _antag.SendBriefing(ent, MakeBriefing(ent), null, null);
    }

    // Character screen briefing
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

            var isShotCaller = _gang.IsShotCaller(ent);
            var rank = Loc.GetString(isShotCaller ? "gangs-the-leader" : "gangs-a-member");
            var gangName = Loc.GetString(gangMemberRole.Gang?.Name!);

            var r = Rnd.Next((int) gangMemberRole.Gang?.Nicknames.Count!);
            var gangNickname = Loc.GetString(gangMemberRole.Gang?.Nicknames.ElementAt(r)!);

            briefing += Loc.GetString("gangmember-role-greeting-intro",
                ("rank", rank),
                ("gangName", gangName)
            );

            briefing += " ";

            if (isShotCaller)
            {
                var car = _inmate.GetInmatesCar(ent);

                briefing += Loc.GetString("gangmember-role-greeting-shotcaller",
                    ("car", Loc.GetString(car!.Name)),
                    ("gangNickname", gangNickname)
                );
            }
            else
            {
                var shotCallerName = MetaData(_gang.GetShotCallerOfInmate(ent)).EntityName;

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
