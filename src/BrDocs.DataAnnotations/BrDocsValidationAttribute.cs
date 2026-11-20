using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BrDocs.DataAnnotations;

/// <summary>
/// Base class for the BrDocs validation attributes. It supplies a lazily resolved pt-BR message
/// from <see cref="ValidationMessages"/> and the standard DataAnnotations null contract.
/// </summary>
/// <remarks>
/// A <see langword="null"/> value is always valid so that presence is the concern of
/// <see cref="RequiredAttribute"/>, as in the .NET validation framework. An empty string is
/// <em>not</em> null and is therefore invalid. Non-string values are invalid. Every attribute may
/// have its message overridden with <see cref="ValidationAttribute.ErrorMessage"/> or globally
/// with <see cref="ValidationMessages.SetMessage"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
public abstract class BrDocsValidationAttribute : ValidationAttribute
{
    private readonly string _defaultMessageKey;

    private string? _messageKey;

    /// <summary>Creates an attribute whose default message comes from the given resource key.</summary>
    /// <param name="messageKey">A key in <see cref="ValidationMessages"/>.</param>
    protected BrDocsValidationAttribute(string messageKey)
        : base(() => ValidationMessages.Get(messageKey))
    {
        ArgumentException.ThrowIfNullOrEmpty(messageKey);
        _defaultMessageKey = messageKey;
    }

    /// <summary>The resource key currently used for the error message.</summary>
    protected string MessageKey
    {
        get => _messageKey ?? _defaultMessageKey;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _messageKey = value;
        }
    }

    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }
        return value is string text && IsValidText(text.AsSpan());
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        if (ErrorMessage is not null)
        {
            return base.FormatErrorMessage(name);
        }
        return string.Format(CultureInfo.CurrentCulture, ValidationMessages.Get(MessageKey), name);
    }

    /// <summary>Validates the significant text of a non-null string value.</summary>
    /// <param name="value">The value, formatted or unformatted.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    protected abstract bool IsValidText(ReadOnlySpan<char> value);
}
