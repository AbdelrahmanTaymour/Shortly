namespace Shortly.Core.Common;


/// <summary>
///     Represents an optional value that can distinguish between a value
///     that was explicitly provided and a value that was not provided at all.
/// </summary>
/// <typeparam name="T">
///     The underlying value type.
/// </typeparam>
/// <remarks>
///     <para>
///         <see cref="Optional{T}" /> is designed for partial update (PATCH-style)
///         operations where it is necessary to distinguish between:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 A property that was not supplied in the request (<see cref="IsPresent" /> is <c>false</c>).
///             </description>
///         </item>
///         <item>
///             <description>
///                 A property that was supplied with a non-null value.
///             </description>
///         </item>
///         <item>
///             <description>
///                 A property that was supplied explicitly with a <c>null</c> value,
///                 indicating intent to clear or remove the existing value.
///             </description>
///         </item>
///     </list>
///     <para>
///         This type differs from nullable reference types (<c>T?</c>), which cannot
///         distinguish between omitted and explicitly null values.
///     </para>
/// </remarks>
public readonly struct Optional<T>(T? value)
{
    /// <summary>
    ///     Gets a value indicating whether the optional value was explicitly provided.
    /// </summary>
    /// <remarks>
    ///     When <c>false</c>, the value was not supplied and should not be modified.
    ///     When <c>true</c>, the value was supplied, even if the underlying
    ///     <see cref="Value" /> is <c>null</c>.
    /// </remarks>
    public bool IsPresent { get; } = true;

    /// <summary>
    ///     Gets the underlying value.
    /// </summary>
    /// <remarks>
    ///     This value is meaningful only when <see cref="IsPresent" /> is <c>true</c>.
    ///     The value may be <c>null</c> to explicitly indicate removal or clearing.
    /// </remarks>
    public T? Value { get; } = value;

    /// <summary>
    ///     Creates an <see cref="Optional{T}" /> instance representing
    ///     a value that was not provided.
    /// </summary>
    /// <returns>
    ///     An <see cref="Optional{T}" /> instance with <see cref="IsPresent" /> set to <c>false</c>.
    /// </returns>
    public static Optional<T> Missing()
    {
        return default;
    }
}