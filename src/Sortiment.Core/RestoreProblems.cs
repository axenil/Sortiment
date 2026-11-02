namespace Sortiment.Core;

/// <summary>
/// Collects the problems found in stored state passed to a <c>Restore</c> method that has no matching checks in
/// <c>Create</c>, so that every problem is reported in one exception (ADR 0011).
/// </summary>
internal sealed class RestoreProblems
{
    private readonly List<string> _problems = [];

    public void Add(string problem) => _problems.Add(problem);

    /// <param name="subject">What is being restored, for example "Family 't_shirts'".</param>
    /// <param name="parameterName">The parameter that holds the invalid state.</param>
    /// <exception cref="ArgumentException">At least one problem was added.</exception>
    public void ThrowIfAny(string subject, string parameterName)
    {
        if (_problems.Count > 0)
        {
            throw new ArgumentException(
                $"{subject} cannot be restored. {string.Join(" ", _problems)}",
                parameterName);
        }
    }
}
