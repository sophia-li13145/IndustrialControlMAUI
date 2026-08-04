using System.Collections;
using System.Collections.Specialized;

namespace IndustrialControlMAUI.Behaviors;

/// <summary>
/// Returns a collection view to its first item after its bound collection is
/// cleared and populated again, while leaving incremental additions untouched.
/// </summary>
public sealed class ScrollToTopOnRefreshBehavior : Behavior<CollectionView>
{
    private CollectionView? _collectionView;
    private INotifyCollectionChanged? _observableItems;
    private bool _isWaitingForRefreshedItems;

    protected override void OnAttachedTo(CollectionView bindable)
    {
        base.OnAttachedTo(bindable);
        _collectionView = bindable;
        bindable.PropertyChanged += OnCollectionViewPropertyChanged;
        SubscribeToItems(bindable.ItemsSource);
    }

    protected override void OnDetachingFrom(CollectionView bindable)
    {
        bindable.PropertyChanged -= OnCollectionViewPropertyChanged;
        UnsubscribeFromItems();
        _collectionView = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnCollectionViewPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == CollectionView.ItemsSourceProperty.PropertyName && sender is CollectionView collectionView)
        {
            SubscribeToItems(collectionView.ItemsSource);
            ScrollToTopWhenPopulated(collectionView.ItemsSource);
        }
    }

    private void SubscribeToItems(IEnumerable? itemsSource)
    {
        UnsubscribeFromItems();
        _observableItems = itemsSource as INotifyCollectionChanged;
        if (_observableItems is not null)
            _observableItems.CollectionChanged += OnItemsCollectionChanged;
    }

    private void UnsubscribeFromItems()
    {
        if (_observableItems is not null)
            _observableItems.CollectionChanged -= OnItemsCollectionChanged;

        _observableItems = null;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _isWaitingForRefreshedItems = true;
            return;
        }

        if (_isWaitingForRefreshedItems && e.Action == NotifyCollectionChangedAction.Add)
            ScrollToTopWhenPopulated(sender as IEnumerable);
    }

    private void ScrollToTopWhenPopulated(IEnumerable? items)
    {
        if (_collectionView is null || items is null)
            return;

        var enumerator = items.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
                return;
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        _isWaitingForRefreshedItems = false;
        _collectionView.Dispatcher.Dispatch(() =>
            _collectionView?.ScrollTo(0, position: ScrollToPosition.Start, animate: false));
    }
}
