using Bunkum.Core;
using Bunkum.Protocols.Gopher.Responses;
using Bunkum.Protocols.Gopher.Responses.Items;

namespace Voyager.Extensions;

public static class GohpermapListExtensions
{
    /// <summary>
    /// Helper function to add a heading, differing between the protocol
    /// </summary>
    public static void AddHeading(this IList<GophermapItem> list, RequestContext context, string text, int level)
    {
        if (level is > 3 or < 0)
            throw new ArgumentException($"{nameof(level)} must be between 1-3!", nameof(level));

        list.Add(context.IsGemini() ? new GophermapMessage($"{new string('#', level)} {text}") : new GophermapMessage(text));
    }
}