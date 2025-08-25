namespace Game.Core
{
    /// <summary>
    /// Interface for checking if input should be suppressed during perspective switches.
    /// </summary>
    public interface IInputSuppressor
    {
        /// <summary>Returns true if input is currently being suppressed.</summary>
        bool IsInputSuppressed { get; }
    }
}