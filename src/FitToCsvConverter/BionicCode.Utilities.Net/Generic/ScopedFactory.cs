namespace BionicCode.Utilities.Net;

using System;

internal class ScopedFactory<TCreate> : Factory<TCreate>, IDisposableAdvanced
{
    public bool IsDisposed { get; private set; }

    public new FactoryMode FactoryMode => FactoryMode.Scoped;

    protected override TCreate CreateInstance() => Factory.CreateInstanceBase();

    protected override TCreate CreateInstance(params object[] args) => Factory.CreateInstanceBase(args);

    private Factory<TCreate> Factory { get; }

    public ScopedFactory(Factory<TCreate> factory) : base(FactoryMode.Scoped)
    {
        Factory = factory;
        Factory.IsScoped = true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                Factory.IsScoped = false;
                if (SharedProductInstance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            IsDisposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~FactoryScope()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
