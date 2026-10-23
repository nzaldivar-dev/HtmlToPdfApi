/// <summary>
/// Problem type URIs (RFC 9457 section 3.1.1). They are identifiers, not necessarily dereferenceable URLs.
/// Statuses without a specific meaning keep the framework's default RFC 9110 type.
/// </summary>
public static class ProblemTypes
{
    public const string InvalidRequest = "urn:htmltopdfapi:problem:invalid-request";
    public const string MalformedBody = "urn:htmltopdfapi:problem:malformed-body";
    public const string ValidationError = "urn:htmltopdfapi:problem:validation-error";
}
