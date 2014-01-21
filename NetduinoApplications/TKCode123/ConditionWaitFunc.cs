using System;

namespace TKCode123
{
    /// <summary>
    /// Represents a callback that is invoked periodically until a condition is met or the waiting is to be aborted.
    /// </summary>
    /// <param name="accumulated">Accumulated time since first invocation.</param>
    /// <returns>True if condition should be checked again or <c>False</c> if wait should be aborted.</returns>
    public delegate bool ConditionWaitFunc(TimeSpan accumulated);
}
