using Bunkum.Core;
using Bunkum.Protocols.Gemini;
using JetBrains.Annotations;

namespace Voyager.Extensions;

public static class RequestContextExtensions
{
    [Pure]
    public static bool IsGemini(this RequestContext context) => context.Protocol.Name == GeminiProtocolInformation.Gemini.Name;
}