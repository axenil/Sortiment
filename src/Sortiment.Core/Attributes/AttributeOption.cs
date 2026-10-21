namespace Sortiment.Core.Attributes;

/// <summary>
/// One of the values a <see cref="AttributeType.SingleSelect"/> or <see cref="AttributeType.MultiSelect"/>
/// attribute can take, for example <c>red</c> for the attribute <c>color</c>.
/// Options are created through <see cref="AttributeDefinition.AddOption"/>.
/// </summary>
public sealed class AttributeOption
{
    internal AttributeOption(AttributeOptionId id, AttributeOptionCode code)
    {
        Id = id;
        Code = code;
    }

    /// <summary>
    /// Gets the identity of the option.
    /// </summary>
    public AttributeOptionId Id { get; }

    /// <summary>
    /// Gets the code of the option, unique within its attribute.
    /// </summary>
    public AttributeOptionCode Code { get; }
}
