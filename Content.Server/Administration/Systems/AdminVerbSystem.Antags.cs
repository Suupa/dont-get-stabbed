using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Server.Clothing.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Roles.Components;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly OutfitSystem _outfit = default!;
    [Dependency] private readonly JobSystem _job = default!;

    private static readonly EntProtoId DefaultThiefRule = "Thief"; //TODO temptest
    private static readonly EntProtoId DefaultGangRule = "GangRule";

    // All antag verbs have names so invokeverb works.
    private void AddAntagVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (!HasComp<MindContainerComponent>(args.Target) || !TryComp<ActorComponent>(args.Target, out var targetActor))
            return;

        var targetPlayer = targetActor.PlayerSession;

        //TODO temptest remove
        var thiefName = Loc.GetString("admin-verb-text-make-thief");
        Verb thief = new()
        {
            Text = thiefName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Hands/Gloves/Color/black.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ThiefRuleComponent>(targetPlayer, DefaultThiefRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", thiefName, Loc.GetString("admin-verb-make-thief")),
        };
        args.Verbs.Add(thief);

        if (_mindSystem.TryGetMind(player, out var mindId, out var mind) && _job.MindHasJobWithId(mindId, "Inmate"))
        {
            var gangMemberName = Loc.GetString("admin-verb-text-make-gang-member");
            Verb gangMember = new()
            {
                Text = gangMemberName,
                Category = VerbCategory.Antag,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "DeathSquad"),
                Act = () =>
                {
                    _antag.ForceMakeAntag<GangMemberRoleComponent>(targetPlayer, DefaultGangRule);
                },
                Impact = LogImpact.High,
                Message = string.Join(": ", gangMemberName, Loc.GetString("admin-verb-make-gang-member")),
            };
            args.Verbs.Add(gangMember);
        }

        //add additional verbs here
    }
}
