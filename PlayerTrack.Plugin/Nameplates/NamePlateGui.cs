using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.DrunkenToad.Core;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PlayerTrack.Nameplates;

/// <summary>
/// Class used to modify the data used when rendering nameplates.
/// </summary>
internal sealed class NamePlateGui : INamePlateGui
{
    /// <summary>
    /// The index for the number array used by the NamePlate addon.
    /// </summary>
    public const int NumberArrayIndex = 5;

    /// <summary>
    /// The index for the string array used by the NamePlate addon.
    /// </summary>
    public const int StringArrayIndex = 4;

    /// <summary>
    /// The index for of the FullUpdate entry in the NamePlate number array.
    /// </summary>
    internal const int NumberArrayFullUpdateIndex = 4;

    /// <summary>
    /// An empty null-terminated string pointer allocated in unmanaged memory, used to tag removed fields.
    /// </summary>
    internal static readonly nint EmptyStringPointer = CreateEmptyStringPointer();

    private NamePlateUpdateContext? context;

    private NamePlateUpdateHandler[] updateHandlers = [];

    public NamePlateGui()
    {
        DalamudContext.AddonLifecycleHandler.RegisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
    }

    public void Dispose()
    {
        DalamudContext.AddonLifecycleHandler.UnregisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
    }
    
    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnNamePlateUpdate;

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnDataUpdate;

    /// <inheritdoc/>
    public unsafe void RequestRedraw()
    {
        var addon = DalamudContext.GameGuiHandler.GetAddonByName("NamePlate");
        if (addon != 0)
        {
            var raptureAtkModule = RaptureAtkModule.Instance();
            if (raptureAtkModule == null)
            {
                return;
            }

            ((AddonNamePlate*)addon)->DoFullUpdate = 1;
            var namePlateNumberArrayData = raptureAtkModule->AtkArrayDataHolder.NumberArrays[NumberArrayIndex];
            namePlateNumberArrayData->SetValue(NumberArrayFullUpdateIndex, 1);
        }
    }

    /// <summary>
    /// Strips the surrounding quotes from a free company tag. If the quotes are not present in the expected location,
    /// no modifications will be made.
    /// </summary>
    /// <param name="text">A quoted free company tag.</param>
    /// <returns>A span containing the free company tag without its surrounding quote characters.</returns>
    internal static ReadOnlySpan<byte> StripFreeCompanyTagQuotes(ReadOnlySpan<byte> text)
    {
        if (text.Length > 4 && text.StartsWith(" «"u8) && text.EndsWith("»"u8))
        {
            return text[3..^2];
        }

        return text;
    }

    /// <summary>
    /// Strips the surrounding quotes from a title. If the quotes are not present in the expected location, no
    /// modifications will be made.
    /// </summary>
    /// <param name="text">A quoted title.</param>
    /// <returns>A span containing the title without its surrounding quote characters.</returns>
    internal static ReadOnlySpan<byte> StripTitleQuotes(ReadOnlySpan<byte> text)
    {
        if (text.Length > 5 && text.StartsWith("《"u8) && text.EndsWith("》"u8))
        {
            return text[3..^3];
        }

        return text;
    }

    private static nint CreateEmptyStringPointer()
    {
        var pointer = Marshal.AllocHGlobal(1);
        Marshal.WriteByte(pointer, 0, 0);
        return pointer;
    }

    private void CreateHandlers(NamePlateUpdateContext createdContext)
    {
        var handlers = new List<NamePlateUpdateHandler>();
        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++)
        {
            handlers.Add(new NamePlateUpdateHandler(createdContext, i));
        }

        this.updateHandlers = handlers.ToArray();
    }

    private void OnPreRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (this.OnDataUpdate == null && this.OnNamePlateUpdate == null)
        {
            return;
        }

        var reqArgs = (AddonRequestedUpdateArgs)args;
        if (this.context == null)
        {
            this.context = new NamePlateUpdateContext(DalamudContext.ObjectCollection, reqArgs);
            this.CreateHandlers(this.context);
        }
        else
        {
            this.context.ResetState(reqArgs);
        }

        var activeNamePlateCount = this.context.ActiveNamePlateCount;
        if (activeNamePlateCount == 0)
            return;

        var activeHandlers = this.updateHandlers[..activeNamePlateCount];

        if (this.context.IsFullUpdate)
        {
            foreach (var handler in activeHandlers)
            {
                handler.ResetState();
            }

            this.OnDataUpdate?.Invoke(this.context, activeHandlers);
            this.OnNamePlateUpdate?.Invoke(this.context, activeHandlers);
            if (this.context.HasParts)
                this.ApplyBuilders(activeHandlers);
        }
        else
        {
            var udpatedHandlers = new List<NamePlateUpdateHandler>(activeNamePlateCount);
            foreach (var handler in activeHandlers)
            {
                handler.ResetState();
                if (handler.IsUpdating)
                    udpatedHandlers.Add(handler);
            }

            if (this.OnDataUpdate is not null)
            {
                this.OnDataUpdate?.Invoke(this.context, activeHandlers);
                this.OnNamePlateUpdate?.Invoke(this.context, udpatedHandlers);
                if (this.context.HasParts)
                    this.ApplyBuilders(activeHandlers);
            }
            else if (udpatedHandlers.Count != 0)
            {
                var changedHandlersSpan = udpatedHandlers.ToArray().AsSpan();
                this.OnNamePlateUpdate?.Invoke(this.context, udpatedHandlers);
                if (this.context.HasParts)
                    this.ApplyBuilders(changedHandlersSpan);
            }
        }
    }

    private void ApplyBuilders(Span<NamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            if (handler.PartsContainer is { } container)
            {
                container.ApplyBuilders(handler);
            }
        }
    }
}