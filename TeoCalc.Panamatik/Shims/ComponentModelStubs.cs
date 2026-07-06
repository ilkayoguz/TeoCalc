namespace System.ComponentModel;

public interface IContainer : IDisposable
{
}

public sealed class Container : IContainer
{
  public void Dispose()
  {
  }
}

public interface ISupportInitialize
{
  void BeginInit();

  void EndInit();
}

public sealed class ComponentResourceManager
{
  public ComponentResourceManager(Type type)
  {
  }

  public object? GetObject(string name) => null;
}
