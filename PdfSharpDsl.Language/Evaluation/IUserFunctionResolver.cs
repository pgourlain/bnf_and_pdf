using System;
using System.Collections.Generic;

namespace PdfSharpDslCore.Evaluation
{
    /// <summary>Finds the functions a formula may call besides the registered ones (UDFs written in the script).</summary>
    internal interface IUserFunctionResolver
    {
        /// <summary>The function named <paramref name="upperCaseName"/>, or null when there is none.</summary>
        Func<object[], object>? Resolve(string upperCaseName);

        IEnumerable<string> Names { get; }
    }
}
