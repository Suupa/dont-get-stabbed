using System.Globalization;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Gangs;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;

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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAddedEvent);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemovedEvent);
        // defer greetings until after spawn so any exclusive antag roles (f.e. gang members) are present
        // run strictly after GangSystem, GangRuleSystem and AntagSelectionSystem so assignment is finalized
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(
            OnPlayerSpawned,
            null,
            [typeof(Gangs.GangSystem), typeof(GameTicking.Rules.GangRuleSystem), typeof(AntagSelectionSystem)]);
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

    private void SendJobGreeting(EntityUid mindId, MindComponent component)
    {
        if (!MindTryGetJob(mindId, out var job))
            return;
        if (!_player.TryGetSessionById(component.UserId, out var session))
            return;

        // for exclusive antagonists (f.e. Gang Member) suppress the generic
        // "Your role is: {jobName}" chat line to avoid confusing/duplicate messages
        if (!_roles.MindIsExclusiveAntagonist(mindId))
        {
            //unique message for inmates
            if (job.ID == "Inmate" && component.OwnedEntity is { } mob)
            {
                var car = _inmate.GetInmatesCar(mob);
                if (car != null && component.OwnedEntity is { } mob2)
                {
                    var shotCaller = _gang.GetShotCallerOfInmate(mob2);
                    var shotCallerName = Exists(shotCaller) ? MetaData(shotCaller.Value).EntityName : "Unknown";

                    var msg = Loc.GetString("job-greet-inmate-introduce-job-name",
                        ("car", Loc.GetString(car.Name)),
                        ("shotCaller", shotCallerName)
                        );

                    _chat.DispatchServerMessageColored(session, msg);
                }
            }
            else
            {
                _chat.DispatchServerMessage(session, Loc.GetString("job-greet-introduce-job-name",
                    ("jobName", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job.LocalizedName))));
            }
        }

        if (job.RequireAdminNotify)
            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-important-disconnect-admin-notify"));


        if (job.ID == "Inmate")
        {
            //unique message for inmates
            _chat.DispatchServerMessageColored(session, Loc.GetString("job-greet-inmate-warning"));
        }
        else
        {
            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-supervisors-warning", ("jobName", job.LocalizedName), ("supervisors", Loc.GetString(job.Supervisors))));
        }
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
