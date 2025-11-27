using Content.Shared.GameTicking;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;

namespace Content.Server.Strip;

public sealed class CavitySearchableSystem : SharedCavitySearchableSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        var mob = ev.Mob;
        var jobId = ev.JobId;

        if (jobId == "Inmate")
        {
            EnsureComp<CavitySearchableComponent>(mob);
        }
    }
}
