#nullable enable
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.UnitTesting;
using Content.IntegrationTests.Tests.Localization;

namespace Content.IntegrationTests.Tests.Gangs;

[TestFixture]
public sealed class GangTests
{
    private TestPair _pair;
    private RobustIntegrationTest.ServerIntegrationInstance _server;
    private RobustIntegrationTest.ClientIntegrationInstance _client;

    private IServerEntityManager _entity;
    private IPlayerManager _player;

    private MindSystem _mind;
    private RoleSystem _role;
    private JobSystem _job;
    private AntagSelectionSystem _antag;

    private ICommonSession _playerSession;

    private EntityUid _ent;
    private EntityUid _mindId;
    private MindComponent _mindComp = null!;


    [OneTimeSetUp]
    public async Task Setup()
    {
        //TODO cleanup
        //await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });

        //we can't use DummyTicker because it would skip real round start and job assignment pipeline etc..
        _pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });
        _server = _pair.Server;
        _client = _pair.Client;

        _entity = _server.ResolveDependency<IServerEntityManager>();
        _player = _server.ResolveDependency<IPlayerManager>();

        _mind = _entity.EntitySysManager.GetEntitySystem<MindSystem>();
        _role = _entity.EntitySysManager.GetEntitySystem<RoleSystem>();
        _job = _entity.EntitySysManager.GetEntitySystem<JobSystem>();
        _antag = _entity.EntitySysManager.GetEntitySystem<AntagSelectionSystem>();

        _playerSession = _player.Sessions.Single();
    }

    [Test]
    public async Task InmateThenMadeGangMember_ShowsOnlyGangMemberBriefing()
    {
        //make Inmate and Assert no Briefing
        await _server.WaitAssertion(() =>
        {
            // spawn a body and attach a mind for the client
            _ent = _entity.SpawnEntity(null, new MapCoordinates());
            _entity.EnsureComponent<MindContainerComponent>(_ent);

            _mindId = _mind.CreateMind(_playerSession.UserId, "Test Inmate");
            _mindComp = _entity.GetComponent<MindComponent>(_mindId);

            _mind.TransferTo(_mindId, _ent);

            // Give the Inmate job
            _job.MindAddJob(_mindId, "Inmate");

            // ensure Inmate job actually assigned
            Assert.Multiple(() =>
            {
                Assert.That(_job.MindHasJobWithId(_mindId, "Inmate"), Is.True);
                Assert.That(_job.MindTryGetJob(_mindId, out var jobProto), Is.True);
                Assert.That(jobProto!.ID, Is.EqualTo("Inmate"));
                Assert.That(_entity.TryGetComponent<InmateComponent>(_ent, out _), Is.True);
            });

            // Verify there is currently no role briefing (Inmate should not add a mind-role briefing)
            var preBrief = _role.MindGetBriefing(_mindId);
            Assert.That(string.IsNullOrEmpty(preBrief));
        });

        // Now, make the player a Gang Member (late-join style since they're already spawned)
        await _server.WaitAssertion(() =>
        {
            _antag.ForceMakeAntag<GangMemberRoleComponent>(_playerSession, "GangRule");
        });

        // Allow systems to process events/briefing
        await _pair.RunTicksSync(5);

        await _server.WaitAssertion(() =>
        {
            // Gang briefing should now appear in the mind briefing UI once
            var postBrief = _role.MindGetBriefing(_mindId);

            Assert.That(string.IsNullOrEmpty(postBrief), Is.False, "Expected gang member briefing to be present");

            var regex = LocalizationTestHelper.GetRegex_AllVarsWildCards([
                new LocalizationTestHelper.LocBlock("gangmember-role-greeting-intro", "rank", "gangName"),
                new LocalizationTestHelper.LocBlock("gangmember-role-greeting-shotcaller", "car"),
            ]);

            var matches = regex.Matches(postBrief!);

            Assert.That(matches.Count,Is.EqualTo(1),$"Gang member briefing should only appear once, but appeared {matches.Count} times.");
        });

        await _pair.CleanReturnAsync();
    }
}
