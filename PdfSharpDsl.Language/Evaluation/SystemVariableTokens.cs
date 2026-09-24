namespace PdfSharpDslCore.Evaluation
{
    /// <summary>
    /// Sentinel tokens for system variables whose value is only known at publish time
    /// (e.g. $PAGECOUNT, resolved once every page has been recorded).
    /// </summary>
    public static class SystemVariableTokens
    {
        public const string PageCountSentinel = "\u0001PAGECOUNT\u0001";
    }
}
