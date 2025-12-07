using System.Globalization;
using Content.Server.Chat.Managers;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Server.Roles.Jobs;

/// <summary>
///     Handles the job data on mind entities.
/// </summary>
public sealed class JobSystem : SharedJobSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAddedEvent);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemovedEvent);
        // defer greetings until after spawn so any exclusive antag roles (f.e. GangMember) are present
        // and after GangSystem to ensure Gang assignment is finalized
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned, null,[typeof(Gangs.GangSystem), typeof(GameTicking.Rules.GangRuleSystem)]);
    }

    private void OnRoleAddedEvent(RoleAddedEvent args)
    {
        MindOnDoGreeting(args.MindId, args.Mind, args);

        if (args.RoleTypeUpdate)
            _roles.RoleUpdateMessage(args.Mind);
    }

    private void OnRoleRemovedEvent(RoleRemovedEvent args)
    {
        if (args.RoleTypeUpdate)
            _roles.RoleUpdateMessage(args.Mind);
    }

    private void MindOnDoGreeting(EntityUid mindId, MindComponent component, RoleAddedEvent args)
    {
        if (args.Silent)
            return;

        // skip if no mob yet. (OnPlayerSpawned will handle this)
        if (!component.OwnedEntity.HasValue)
            return;

        // defer by a short delay and re-check exclusivity. This avoids the round-start window
        // where PlayerSpawn completes before antag selection runs, which would otherwise cause
        // Inmate greetings to be sent to players who will become exclusive antags (Gang Members)
        Timer.Spawn(TimeSpan.FromSeconds(2), () => SendJobGreetingIfAllowed(mindId));
    }

    private void SendJobGreeting(EntityUid mindId, MindComponent component)
    {
        if (!MindTryGetJob(mindId, out var job))
            return;
        if (!_player.TryGetSessionById(component.UserId, out var session))
            return;

        _chat.DispatchServerMessage(session, Loc.GetString("job-greet-introduce-job-name",
            ("jobName", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job.LocalizedName))));

        if (job.RequireAdminNotify)
            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-important-disconnect-admin-notify"));

        _chat.DispatchServerMessage(session, Loc.GetString("job-greet-supervisors-warning", ("jobName", job.LocalizedName), ("supervisors", Loc.GetString(job.Supervisors))));
    }

    private void SendJobGreetingIfAllowed(EntityUid mindId)
    {
        // mind could become antag after RoleAdded so re-check here
        if (_roles.MindIsExclusiveAntagonist(mindId))
            return;

        if (!TryComp<MindComponent>(mindId, out var mind))
            return;

        if (!mind.OwnedEntity.HasValue)
            return;

        SendJobGreeting(mindId, mind);
    }

    // runs after gang assignment
    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Mob, out var mindId, out var mind))
            return;

        // defer by a short delay to allow antag selection/game rules to complete first
        // then re-check exclusivity before sending.
        Timer.Spawn(TimeSpan.FromSeconds(2), () => SendJobGreetingIfAllowed(mindId));
    }

    public void MindAddJob(EntityUid mindId, string jobPrototypeId)
    {
        if (MindHasJobWithId(mindId, jobPrototypeId))
            return;

        _roles.MindAddJobRole(mindId, null, false, jobPrototypeId);
    }
}
