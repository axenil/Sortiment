using System.Collections.ObjectModel;
using Sortiment.Core.Attributes;

namespace Sortiment.Core.Products;

/// <summary>
/// How complete a product is for one locale in one channel.
/// </summary>
public sealed class LocaleCompleteness
{
    internal LocaleCompleteness(Locale locale, int requiredCount, IList<AttributeCode> missingAttributes)
    {
        Locale = locale;
        RequiredCount = requiredCount;
        MissingAttributes = new ReadOnlyCollection<AttributeCode>(missingAttributes);
    }

    /// <summary>
    /// Gets the locale.
    /// </summary>
    public Locale Locale { get; }

    /// <summary>
    /// Gets the number of attributes the channel requires.
    /// </summary>
    public int RequiredCount { get; }

    /// <summary>
    /// Gets the number of required attributes that have a value.
    /// </summary>
    public int FilledCount => RequiredCount - MissingAttributes.Count;

    /// <summary>
    /// Gets the required attributes that have no value, in the family's attribute order.
    /// </summary>
    public ReadOnlyCollection<AttributeCode> MissingAttributes { get; }

    /// <summary>
    /// Gets a value indicating whether every required attribute has a value.
    /// </summary>
    public bool IsComplete => MissingAttributes.Count == 0;

    /// <summary>
    /// Gets the share of required attributes that have a value, as a whole percentage rounded down so that only a
    /// complete product reaches 100. A channel that requires nothing gives 100.
    /// </summary>
    public int Percentage => RequiredCount == 0 ? 100 : FilledCount * 100 / RequiredCount;
}
