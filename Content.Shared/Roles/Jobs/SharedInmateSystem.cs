using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Gangs;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles.Jobs;

/// <summary>
///     Handles the data on Inmates.
/// </summary>
public sealed class SharedInmateSystem : EntitySystem
{
    static System.Random rnd = new();

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;

    private List<CarPrototype> _cars = [];

    public override void Initialize()
    {
        base.Initialize();
        _cars = _prototype.EnumeratePrototypes<CarPrototype>().ToList();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    public CarPrototype? GetInmatesCar(EntityUid inmate)
    {
        if (!_mind.TryGetMind(inmate, out var mindId, out _) || !_job.MindHasJobWithId(mindId, "Inmate"))
            return null;

        return !TryComp<InmateComponent>(inmate, out var comp) ? null : comp.Car;
    }

    public List<CarPrototype> GetAllCars()
    {
        return [.._cars];
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId != "Inmate")
            return;

        var comp = EnsureComp<InmateComponent>(ev.Mob);

        //TODO make car depended on race instead of random (later there should be exceptions, but start out with simpler version)
        var cars = GetAllCars();
        var r2 = rnd.Next(cars.Count);
        comp.Car = cars.ElementAt(r2);
    }
}
