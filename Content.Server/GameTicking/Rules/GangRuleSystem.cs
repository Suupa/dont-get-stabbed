using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Roles.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class GangRuleSystem : GameRuleSystem<GangRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GangRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<GangMemberRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // Greeting upon gang member activation
    private void AfterAntagSelected(Entity<GangRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        //TODO select gang at random

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
        //TODO temptest
        var gangName = "Aryan Brotherhood";
        var shotcaller = "Bobby Bigbrains";

        var briefing = Loc.GetString("gangmember-role-greeting-human",("gangName",gangName),("shotcaller",shotcaller));
        //TODO if needs starting equipment briefing += "\n \n" + Loc.GetString("gangmember-role-greeting-equipment") + "\n";

        return briefing;
    }
}
