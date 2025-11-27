using System.Linq;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Strip.Components;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Strip;

public abstract class SharedCavitySearchableSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private   readonly INetManager _netManager = default!;

    public readonly float CavitySearchTimeSeconds = 4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CavitySearchableComponent, GetVerbsEvent<Verb>>(AddCavitySearchVerb);

        // DoAfters
        SubscribeLocalEvent<HandsComponent, CavitySearchDoAfterEvent>(OnCavitySearchDoAfterFinished);

        // Call OnStorageImplantStartup when a StorageImplantComponent starts up
        SubscribeLocalEvent<StorageImplantComponent, ComponentStartup>(OnStorageImplantStartup);
    }
    private void AddCavitySearchVerb(EntityUid uid, CavitySearchableComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract || args.Target == args.User)
            return;

        if (!IsWearingAppropriateGloves(args.User))
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("Cavity Search"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
            Act = () => PerformCavitySearch(args.User, args.Target, true),
        };

        args.Verbs.Add(verb);
    }

    private void PerformCavitySearch(EntityUid user, EntityUid target, bool openInCombat = false)
    {
        if (!openInCombat && TryComp<CombatModeComponent>(user, out var mode) && mode.IsInCombatMode)
            return;

        if (!HasComp<CavitySearchableComponent>(target))
            return;

        if (!IsWearingAppropriateGloves(user))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, CavitySearchTimeSeconds, new CavitySearchDoAfterEvent(),target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            Target = target,
            BlockDuplicate =  true,
            CancelDuplicate =  true,
            DuplicateCondition = DuplicateConditions.SameEvent | DuplicateConditions.SameTarget,
        };


        _doAfterSystem.TryStartDoAfter(doAfterArgs);

        _popupSystem.PopupClient(Loc.GetString("cavity-searchable-component-performing-cavity-search"), user, user);
        _popupSystem.PopupEntity(Loc.GetString("cavity-searchable-component-receiving-cavity-search",("user",user)), target, target);//TODO check if this works
    }

    private void OnCavitySearchDoAfterFinished(Entity<HandsComponent> entity, ref CavitySearchDoAfterEvent ev)
    {
        if (_netManager.IsClient)
            return;

        if (ev.Target != null)
            OpenStorageImplantUiFor(ev.User, ev.Target.Value);
    }

    private void OpenStorageImplantUiFor(EntityUid viewer, EntityUid target)
    {
        if (!TryGetStorageImplant(target, out var implantUid))
        {
            Logger.Info($"No storage implant found on {target}");
            return;
        }

        if (!TryComp<StorageComponent>(implantUid, out var storageComp))
        {
            Logger.Info($"StorageImplant {implantUid} has no StorageComponent");
            return;
        }

        if (!HasComp<ActorComponent>(viewer))
        {
            Logger.Info($"Viewer {viewer} has no ActorComponent; can't open UI");
            return;
        }

        _storage.OpenStorageUI(implantUid, viewer, storageComp);
    }

    private bool IsWearingAppropriateGloves(EntityUid user)
    {
        //any gloves with the CanPerformCavitySearch tag are accepted
        if (_inventorySystem.TryGetSlotEntity(user, "gloves", out var glovesUid))
            return _tagSystem.HasTag(glovesUid.Value, "CanPerformCavitySearch");

        return false;
    }

    private bool TryGetStorageImplant(EntityUid owner, out EntityUid implantUid)
    {
        implantUid = default;

        var enumerator = EntityQueryEnumerator<StorageImplantComponent>();

        while (enumerator.MoveNext(out var uid, out var _))
        {
            if (_containers.TryGetContainingContainer(uid, out var container)
                && container.Owner == owner)
            {
                implantUid = uid;
                return true;
            }
        }

        return false;
    }

    private void OnStorageImplantStartup(Entity<StorageImplantComponent> ent, ref ComponentStartup args)
    {
        var uiEnt = (ent.Owner, (UserInterfaceComponent?) null);

        if (!_uiSystem.TryGetInterfaceData(uiEnt, StorageComponent.StorageUiKey.Key, out var data))
            return;

        //storage implants don't have normal world coordinates so this is a workaround so they don't automatically fail rangecheck
        var newData = data;
        newData.InteractionRange = 0f;

        _uiSystem.SetUi(uiEnt, StorageComponent.StorageUiKey.Key, newData);
    }
}
