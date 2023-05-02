#nullable enable
namespace ductwork;

public interface IPlug
{
}

/// <summary>
/// Data input plug for <see cref="Components.Component"/>. Used by <see cref="Executors.IExecutor"/> to connect data
/// flow in from a <see cref="OutputPlug"/> on another Component.
/// </summary>
public class InputPlug : IPlug
{
}


/// <summary>
/// Data output plug for <see cref="Components.Component"/>. Used by <see cref="Executors.IExecutor"/> to connect data
/// flow out from a Component to an <see cref="InputPlug"/> on another Component.
/// </summary>
public class OutputPlug : IPlug
{
}
