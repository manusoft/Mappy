namespace Mappy;

/// <summary>
/// Represents an error raised while building or executing a mapping.
/// </summary>
public sealed class MappingException : InvalidOperationException
{
    /// <summary>
    /// Gets the source type of the mapping that caused the exception, if available.
    /// </summary>
    public Type? SourceType { get; }

    /// <summary>
    /// Gets the destination type of the mapping that caused the exception, if available.
    /// </summary>
    public Type? DestinationType { get; }

    /// <summary>
    /// Gets the name of the member that caused the exception, if available.
    /// </summary>
    public string? MemberName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingException"/> class with a specified error message, 
    /// source type, destination type, member name, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="sourceType"></param>
    /// <param name="destinationType"></param>
    /// <param name="memberName"></param>
    /// <param name="innerException"></param>
    public MappingException(string message, Type? sourceType = null, Type? destinationType = null,
        string? memberName = null, Exception? innerException = null)
        : base(message, innerException)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
        MemberName = memberName;
    }
}
