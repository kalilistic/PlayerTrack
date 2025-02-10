using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;

namespace PlayerTrack.Extensions;

/// <summary>
/// Dalamud IChatGui extensions.
/// </summary>
public static class ChatGuiExtensions
{
    /// <summary>
    /// Print message with plugin name to notice channel.
    /// </summary>
    /// <param name="value">chat gui service.</param>
    /// <param name="payloads">list of payloads.</param>
    public static void PluginPrintNotice(this IChatGui value, IEnumerable<Payload> payloads)
    {
        value.Print(new XivChatEntry
        {
            Message = BuildSeString(Plugin.PluginInterface.InternalName, payloads),
            Type = XivChatType.Notice,
        });
    }

    private static SeString BuildSeString(string? pluginName, IEnumerable<Payload> payloads)
    {
        var builder = new SeStringBuilder();
        builder.AddUiForeground(548);
        builder.AddText($"[{pluginName}] ");
        builder.Append(payloads);
        builder.AddUiForegroundOff();

        return builder.BuiltString;
    }
}
