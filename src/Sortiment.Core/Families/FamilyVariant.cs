using System.Collections.ObjectModel;
using Sortiment.Core.Attributes;

namespace Sortiment.Core.Families;

/// <summary>
/// One way the products of a family vary, for example t-shirts by color and size (ADR 0015). The axes tell a model's
/// variants apart, and the variant attributes are set per variant. Every other attribute of the family is a model
/// attribute, shared by all variants of a product model.
/// </summary>
/// <remarks>
/// The family variant refers to attributes by ID. Use a <see cref="VariantSchema"/> to work with its attributes
/// together with their definitions.
/// </remarks>
public sealed class FamilyVariant
{
    /// <summary>
    /// The maximum number of axes.
    /// </summary>
    public const int MaxAxes = 5;

    private readonly List<AttributeId> _axes;
    private readonly List<AttributeId> _variantAttributes;

    private FamilyVariant(
        FamilyVariantId id,
        FamilyVariantCode code,
        FamilyId familyId,
        List<AttributeId> axes,
        List<AttributeId> variantAttributes)
    {
        Id = id;
        Code = code;
        FamilyId = familyId;
        _axes = axes;
        _variantAttributes = variantAttributes;
        Axes = _axes.AsReadOnly();
        VariantAttributes = _variantAttributes.AsReadOnly();
    }

    /// <summary>
    /// Gets the identity of the family variant.
    /// </summary>
    public FamilyVariantId Id { get; }

    /// <summary>
    /// Gets the code that imports, feeds and the API use to refer to the family variant.
    /// </summary>
    public FamilyVariantCode Code { get; }

    /// <summary>
    /// Gets the identity of the family whose products vary this way.
    /// </summary>
    public FamilyId FamilyId { get; }

    /// <summary>
    /// Gets the axes, in order. They cannot change after the family variant is created.
    /// </summary>
    public ReadOnlyCollection<AttributeId> Axes { get; }

    /// <summary>
    /// Gets the attributes that are set per variant: the axes first, then the others in the order they were added.
    /// </summary>
    public ReadOnlyCollection<AttributeId> VariantAttributes { get; }

    /// <summary>
    /// Creates a new family variant.
    /// </summary>
    /// <param name="code">The family variant's code.</param>
    /// <param name="schema">The schema of the family whose products vary this way.</param>
    /// <param name="axes">The codes of the axes, in order.</param>
    /// <param name="variantAttributes">
    /// The codes of the other attributes that are set per variant. Axes and repeated codes are ignored.
    /// </param>
    /// <returns>
    /// The new family variant, or the errors <c>family_variant.axis_required</c>,
    /// <c>family_variant.too_many_axes</c>, <c>family_variant.duplicate_axis</c>,
    /// <c>family_variant.attribute_not_in_family</c>, <c>family_variant.invalid_axis_type</c> and
    /// <c>family_variant.axis_not_global</c>.
    /// </returns>
    public static Result<FamilyVariant> Create(
        FamilyVariantCode code,
        FamilySchema schema,
        IEnumerable<AttributeCode> axes,
        IEnumerable<AttributeCode> variantAttributes)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(variantAttributes);

        var errors = new List<ResultError>();
        var axisIds = ResolveAxes(code, schema, axes.ToList(), errors);
        var attributeIds = new List<AttributeId>(axisIds);
        foreach (var attribute in variantAttributes)
        {
            ArgumentNullException.ThrowIfNull(attribute, nameof(variantAttributes));
            var definition = schema.FindAttribute(attribute);
            if (definition is null)
            {
                errors.Add(FamilyVariantErrors.AttributeNotInFamily(code, schema.Family.Code, attribute));
            }
            else if (!attributeIds.Contains(definition.Id))
            {
                attributeIds.Add(definition.Id);
            }
        }

        return errors.Count == 0
            ? Result.Success(new FamilyVariant(FamilyVariantId.New(), code, schema.Family.Id, axisIds, attributeIds))
            : Result.Failure<FamilyVariant>(errors);
    }

    /// <summary>
    /// Restores a family variant from stored state (ADR 0011).
    /// </summary>
    /// <param name="id">The family variant's ID.</param>
    /// <param name="code">The family variant's code.</param>
    /// <param name="familyId">The identity of its family.</param>
    /// <param name="axes">The IDs of the axes, in order.</param>
    /// <param name="variantAttributes">The IDs of the variant attributes, as in <see cref="VariantAttributes"/>.</param>
    /// <returns>The family variant.</returns>
    /// <exception cref="ArgumentException">
    /// There are no axes or more than <see cref="MaxAxes"/>, an attribute is listed more than once, or an axis is not
    /// among the variant attributes.
    /// </exception>
    public static FamilyVariant Restore(
        FamilyVariantId id,
        FamilyVariantCode code,
        FamilyId familyId,
        IEnumerable<AttributeId> axes,
        IEnumerable<AttributeId> variantAttributes)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(variantAttributes);

        var axisIds = axes.ToList();
        var attributeIds = variantAttributes.ToList();
        var problems = new RestoreProblems();
        if (axisIds.Count is 0 or > MaxAxes)
        {
            problems.Add($"It has {axisIds.Count} axes. It must have 1 to {MaxAxes}.");
        }

        foreach (var repeated in Repeated(axisIds))
        {
            problems.Add($"Attribute {repeated} is an axis more than once.");
        }

        foreach (var repeated in Repeated(attributeIds))
        {
            problems.Add($"Attribute {repeated} is a variant attribute more than once.");
        }

        foreach (var axis in axisIds.Where(axis => !attributeIds.Contains(axis)))
        {
            problems.Add($"Axis {axis} is not a variant attribute.");
        }

        problems.ThrowIfAny($"Family variant '{code}'", nameof(axes));
        return new FamilyVariant(id, code, familyId, axisIds, attributeIds);
    }

    /// <summary>
    /// Gets a value indicating whether an attribute is an axis.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <returns><see langword="true"/> if the attribute is an axis.</returns>
    public bool IsAxis(AttributeId attribute) => _axes.Contains(attribute);

    /// <summary>
    /// Gets a value indicating whether an attribute is set per variant. Axes always are.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <returns><see langword="true"/> if the attribute is a variant attribute.</returns>
    public bool IsVariantAttribute(AttributeId attribute) => _variantAttributes.Contains(attribute);

    /// <summary>
    /// Makes an attribute of the family a variant attribute. Values of the attribute that are already set on product
    /// models must then be moved to their variants by the caller (ADR 0015). Adding an attribute that is already a
    /// variant attribute has no effect.
    /// </summary>
    /// <param name="schema">The schema of the family variant's family.</param>
    /// <param name="attribute">The attribute's code.</param>
    /// <returns>Success, or the error <c>family_variant.attribute_not_in_family</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="schema"/> is not the schema of this family variant's family.</exception>
    public Result AddVariantAttribute(FamilySchema schema, AttributeCode attribute)
    {
        EnsureOwnFamily(schema);
        ArgumentNullException.ThrowIfNull(attribute);

        var definition = schema.FindAttribute(attribute);
        if (definition is null)
        {
            return Result.Failure(FamilyVariantErrors.AttributeNotInFamily(Code, schema.Family.Code, attribute));
        }

        if (!IsVariantAttribute(definition.Id))
        {
            _variantAttributes.Add(definition.Id);
        }

        return Result.Success();
    }

    /// <summary>
    /// Makes a variant attribute a model attribute again. Values of the attribute that are already set on variants
    /// must then be moved to their product models by the caller (ADR 0015). Removing an attribute that is not a
    /// variant attribute has no effect.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <returns>Success, or the error <c>family_variant.axis_cannot_be_removed</c> if the attribute is an axis.</returns>
    public Result RemoveVariantAttribute(AttributeId attribute)
    {
        if (IsAxis(attribute))
        {
            return Result.Failure(FamilyVariantErrors.AxisCannotBeRemoved(Code, attribute));
        }

        _variantAttributes.Remove(attribute);
        return Result.Success();
    }

    private static List<AttributeId> ResolveAxes(
        FamilyVariantCode code,
        FamilySchema schema,
        List<AttributeCode> axes,
        List<ResultError> errors)
    {
        if (axes.Count == 0)
        {
            errors.Add(FamilyVariantErrors.AxisRequired(code));
        }
        else if (axes.Count > MaxAxes)
        {
            errors.Add(FamilyVariantErrors.TooManyAxes(code, axes.Count));
        }

        var resolved = new List<AttributeId>();
        foreach (var axis in axes)
        {
            ArgumentNullException.ThrowIfNull(axis, nameof(axes));
            var definition = schema.FindAttribute(axis);
            if (definition is null)
            {
                errors.Add(FamilyVariantErrors.AttributeNotInFamily(code, schema.Family.Code, axis));
                continue;
            }

            if (resolved.Contains(definition.Id))
            {
                errors.Add(FamilyVariantErrors.DuplicateAxis(code, axis));
                continue;
            }

            errors.AddRange(AxisErrors(code, definition));
            resolved.Add(definition.Id);
        }

        return resolved;
    }

    private static IEnumerable<ResultError> AxisErrors(FamilyVariantCode code, AttributeDefinition axis)
    {
        if (axis.Type is not (AttributeType.SingleSelect or AttributeType.Boolean))
        {
            yield return FamilyVariantErrors.InvalidAxisType(code, axis);
        }

        if (axis.IsLocalizable || axis.IsScopable)
        {
            yield return FamilyVariantErrors.AxisNotGlobal(code, axis.Code);
        }
    }

    private static IEnumerable<AttributeId> Repeated(IEnumerable<AttributeId> attributes) =>
        attributes.GroupBy(attribute => attribute).Where(group => group.Count() > 1).Select(group => group.Key);

    private void EnsureOwnFamily(FamilySchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (schema.Family.Id != FamilyId)
        {
            throw new ArgumentException(
                "The schema must be the schema of the family variant's own family.",
                nameof(schema));
        }
    }
}
