using System.Globalization;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Gangs;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles.Jobs;

/// <summary>
///     Handles the job data on mind entities.
/// </summary>
public sealed class JobSystem : SharedJobSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;//TODO probably shouldn't be shared
    [Dependency] private readonly SharedInmateSystem _inmate = default!;//TODO probably shouldn't be shared
    [Dependency] private readonly GangSystem _gang = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAddedEvent);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemovedEvent);
        // defer greetings until after spawn so any exclusive antag roles (f.e. gang members) are present
        // run strictly after GangSystem, GangRuleSystem and AntagSelectionSystem so assignment is finalized
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(
            OnSpawnComplete,
            null,
            [typeof(GangSystem), typeof(GameTicking.Rules.GangRuleSystem), typeof(AntagSelectionSystem)]);
    }

    private void OnRoleAddedEvent(RoleAddedEvent args)
    {
        if (args.RoleTypeUpdate)
            _roles.RoleUpdateMessage(args.Mind);
    }

    private void OnRoleRemovedEvent(RoleRemovedEvent args)
    {
        if (args.RoleTypeUpdate)
            _roles.RoleUpdateMessage(args.Mind);
    }

    private void SendJobGreeting(EntityUid mindId, MindComponent component, JobPrototype job)
    {
        if (!_player.TryGetSessionById(component.UserId, out var session))
            return;

        //unique message for inmates
        if (job.ID == "Inmate" && component.OwnedEntity is { } mob)
        {
            var car = _inmate.GetInmatesCar(mob);
            if (car != null)
            {

                var msg = Loc.GetString("job-greet-inmate-introduce-job-name",
                    ("car", Loc.GetString(car.Name))
                    );

                _chat.DispatchServerMessageColored(session, msg);
            }
            _chat.DispatchServerMessageColored(session, Loc.GetString("job-greet-inmate-warning"));
        }
        else
        {
            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-introduce-job-name",
                ("jobName", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job.LocalizedName))));

            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-supervisors-warning", ("jobName", job.LocalizedName), ("supervisors", Loc.GetString(job.Supervisors))));
        }

        if (job.RequireAdminNotify)
            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-important-disconnect-admin-notify"));
    }

    private void SendJobGreetingIfAllowed(EntityUid mindId)
    {
        //never job-brief job for Exclusive Antag
        if (_roles.MindIsExclusiveAntagonist(mindId))
            return;

        if (!TryComp<MindComponent>(mindId, out var mind))
            return;

        if (!mind.OwnedEntity.HasValue)
            return;

        if (!MindTryGetJob(mindId, out var job))
            return;

        SendJobGreeting(mindId, mind, job);
    }

    // runs after gang assignment
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Mob, out var mindId, out _))
            return;

        // Ordering ensures antag/gang assignment has completed. send immediately after re-check.
        SendJobGreetingIfAllowed(mindId);
    }

    public void MindAddJob(EntityUid mindId, string jobPrototypeId)
    {
        if (MindHasJobWithId(mindId, jobPrototypeId))
            return;

        _roles.MindAddJobRole(mindId, null, false, jobPrototypeId);
    }
}
