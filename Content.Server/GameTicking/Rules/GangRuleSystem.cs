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
        _gang.SortIntoGangs();//TODO find out where this needs to be called for normal antag selection

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
        //TODO does this only work when selecting antag as admin? If so, make sure it works on normal antag selection too

        var briefing = "";
        if (
            _mind.TryGetMind(ent, out var mindId, out var mind)
            && _roleSystem.MindHasRole<GangMemberRoleComponent>(mindId, out var ent2)
            && TryComp<GangMemberRoleComponent>(ent2, out var gangMemberMindRole)
            )
        {

            var isShotcaller = _gang.IsShotcaller(ent);
            var rank = Loc.GetString(isShotcaller ? "gangs-the-leader" : "gangs-a-member");
            var gangName = Loc.GetString(gangMemberMindRole.Gang?.Name!);

            var r = Rnd.Next((int) gangMemberMindRole.Gang?.Nicknames.Count!);
            var gangNickname = Loc.GetString(gangMemberMindRole.Gang?.Nicknames.ElementAt(r)!);

            briefing += Loc.GetString("gangmember-role-greeting-intro",
                ("rank", rank),
                ("gangName", gangName)
            );

            briefing += " ";

            if (isShotcaller)
            {
                var car = _inmate.GetInmatesCar(ent);

                briefing += Loc.GetString("gangmember-role-greeting-shotcaller",
                    ("car", Loc.GetString(car!.Name)),
                    ("gangNickname", gangNickname)
                );
            }
            else
            {
                var shotcallerName = MetaData(_gang.GetShotcallerOfInmate(ent)).EntityName;

                briefing += Loc.GetString("gangmember-role-greeting-member",
                    ("shotcallerName", shotcallerName),
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
