namespace BionicAthlete.Training.Test.Collections;

using System.Collections.Specialized;
using BionicCode.Utilities.Net;

public sealed class ReadOnlyObservableHashSetTests
{
    [Fact]
    public void AsReadOnlyReflectsUnderlyingSetState()
    {
        ObservableHashSet<string> set = new(["alpha", "beta"]);

        ReadOnlyObservableHashSet<string> readOnlySet = set.AsReadOnly();

        Assert.Equal(2, readOnlySet.Count);
        Assert.True(readOnlySet.Contains("alpha"));
        Assert.True(readOnlySet.SetEquals(["alpha", "beta"]));
        Assert.True(readOnlySet.TryGetValue("beta", out string? actualValue));
        Assert.Equal("beta", actualValue);
        Assert.Same(set.Comparer, readOnlySet.Comparer);
    }

    [Fact]
    public void UnderlyingAddRaisesCollectionChangedOnReadOnlyWrapper()
    {
        ObservableHashSet<int> set = [];
        ReadOnlyObservableHashSet<int> readOnlySet = set.AsReadOnly();
        NotifyCollectionChangedEventArgs? eventArgs = null;

        readOnlySet.CollectionChanged += (_, args) => eventArgs = args;

        Assert.True(set.Add(42));

        Assert.NotNull(eventArgs);
        Assert.Equal(NotifyCollectionChangedAction.Add, eventArgs.Action);
        Assert.NotNull(eventArgs.NewItems);
        Assert.Equal(42, Assert.IsType<int>(eventArgs.NewItems[0]));
    }

    [Fact]
    public void UnderlyingMutationsRaisePropertyChangedOnReadOnlyWrapper()
    {
        ObservableHashSet<int> set = [];
        ReadOnlyObservableHashSet<int> readOnlySet = set.AsReadOnly();
        List<string?> changedProperties = [];

        readOnlySet.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        Assert.True(set.Add(1));
        _ = set.EnsureCapacity(8);

        Assert.Contains(nameof(ReadOnlyObservableHashSet<int>.Count), changedProperties);
        Assert.Contains(nameof(ReadOnlyObservableHashSet<int>.Capacity), changedProperties);
    }
}