using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro.Async {
    public partial class Conductor<T> {
        /// <summary>
        /// An implementation of <see cref="IConductor"/> that holds on many items.
        /// </summary>
        public partial class Collection {
            /// <summary>
            /// An implementation of <see cref="IConductor"/> that holds on many items but only activates one at a time.
            /// </summary>
            public class OneActive : ConductorBaseWithActiveItem<T> {
                readonly BindableCollection<T> items = new BindableCollection<T>();

                /// <summary>
                /// Initializes a new instance of the <see cref="Conductor&lt;T&gt;.Collection.OneActive"/> class.
                /// </summary>
                public OneActive() {
                    items.CollectionChanged += (s, e) => {
                        switch(e.Action) {
                            case NotifyCollectionChangedAction.Add:
                                e.NewItems.OfType<IChild>().Apply(x => x.Parent = this);
                                break;
                            case NotifyCollectionChangedAction.Remove:
                                e.OldItems.OfType<IChild>().Apply(x => x.Parent = null);
                                break;
                            case NotifyCollectionChangedAction.Replace:
                                e.NewItems.OfType<IChild>().Apply(x => x.Parent = this);
                                e.OldItems.OfType<IChild>().Apply(x => x.Parent = null);
                                break;
                            case NotifyCollectionChangedAction.Reset:
                                items.OfType<IChild>().Apply(x => x.Parent = this);
                                break;
                        }
                    };
                }

                /// <summary>
                /// Gets the items that are currently being conducted.
                /// </summary>
                public IObservableCollection<T> Items {
                    get { return items; }
                }

                /// <summary>
                /// Gets the children.
                /// </summary>
                /// <returns>The collection of children.</returns>
                public override IEnumerable<T> GetChildren() {
                    return items;
                }

                /// <summary>
                /// Activates the specified item.
                /// </summary>
                /// <param name="item">The item to activate.</param>
                /// <param name="cancellationToken"></param>
                public override async Task ActivateItem(T item, CancellationToken cancellationToken = default(CancellationToken))
                {
                    if (item != null && item.Equals(ActiveItem))
                    {
                        if (IsActive)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await ScreenExtensions.TryActivate(item, cancellationToken);

                            cancellationToken.ThrowIfCancellationRequested();
                            OnActivationProcessed(item, true);
                        }

                        return;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    await ChangeActiveItem(item, false, cancellationToken);
                }

                /// <summary>
                /// Deactivates the specified item.
                /// </summary>
                /// <param name="item">The item to close.</param>
                /// <param name="close">Indicates whether or not to close the item after deactivating it.</param>
                /// <param name="cancellationToken"></param>
                public override async Task DeactivateItem(T item, bool close, CancellationToken cancellationToken = default(CancellationToken))
                {
                    if (item == null)
                    {
                        return;
                    }

                    if (!close)
                    {
                        await ScreenExtensions.TryDeactivate(item, false, cancellationToken);
                    }
                    else
                    {
                        var closeResult = await CloseStrategy.Execute(new[] { item });
                        if (closeResult.CanClose)
                        {
                            await CloseItemCore(item);
                        }
                    }
                }

                private async Task CloseItemCore(T item) {
                    if (item.Equals(ActiveItem))
                    {
                        var index = items.IndexOf(item);
                        var next = DetermineNextItemToActivate(items, index);

                        await ChangeActiveItem(next, true);
                    }
                    else
                    {
                        await ScreenExtensions.TryDeactivate(item, true);
                    }

                    items.Remove(item);
                }

                /// <summary>
                /// Determines the next item to activate based on the last active index.
                /// </summary>
                /// <param name="list">The list of possible active items.</param>
                /// <param name="lastIndex">The index of the last active item.</param>
                /// <returns>The next item to activate.</returns>
                /// <remarks>Called after an active item is closed.</remarks>
                protected virtual T DetermineNextItemToActivate(IList<T> list, int lastIndex) {
                    var toRemoveAt = lastIndex - 1;

                    if (toRemoveAt == -1 && list.Count > 1) {
                        return list[1];
                    }

                    if (toRemoveAt > -1 && toRemoveAt < list.Count - 1) {
                        return list[toRemoveAt];
                    }

                    return default(T);
                }

                /// <summary>
                /// Called to check whether or not this instance can close.
                /// </summary>
                public override async Task<bool> CanClose()
                {
                    var closeResult = await CloseStrategy.Execute(items.ToList());
                    if (!closeResult.CanClose && closeResult.Items.Any())
                    {
                        var closables = closeResult.Items;
                        if (closables.Contains(ActiveItem))
                        {
                            var list = items.ToList();
                            var next = ActiveItem;
                            do
                            {
                                var previous = next;
                                next = DetermineNextItemToActivate(list, list.IndexOf(previous));
                                list.Remove(previous);
                            } while (closables.Contains(next));

                            var previousActive = ActiveItem;
                            await ChangeActiveItem(next, true);
                            items.Remove(previousActive);

                            var stillToClose = closeResult.Items.ToList();
                            stillToClose.Remove(previousActive);
                            closables = stillToClose.ToArray();
                        }

                        var closeableItems = closeResult.Items.OfType<IDeactivate>().ToArray();
                        var closeableTasks = closeableItems.Select(d => d.Deactivate(true));
                        await Task.WhenAll(closeableTasks);
                        items.RemoveRange((IEnumerable<T>)closeableItems);
                    }
                    return closeResult.CanClose;
                }

                /// <summary>
                /// Called when activating.
                /// </summary>
                /// <param name="cancellationToken"></param>
                protected override async Task OnActivate(CancellationToken cancellationToken = default(CancellationToken))
                {
                    await ScreenExtensions.TryActivate(ActiveItem, cancellationToken);
                }

                /// <summary>
                /// Called when deactivating.
                /// </summary>
                /// <param name="close">Indicates whether this instance will be closed.</param>
                /// <param name="cancellationToken"></param>
                protected override async Task OnDeactivate(bool close, CancellationToken cancellationToken = default(CancellationToken)) {

                    if (close)
                    {
                        var deactivateables = items.OfType<IDeactivate>().Select(x => x.Deactivate(true, cancellationToken));
                        await Task.WhenAll(deactivateables);
                        items.Clear();
                    }
                    else
                    {
                        await ScreenExtensions.TryDeactivate(ActiveItem, false, cancellationToken);
                    }
                }

                /// <summary>
                /// Ensures that an item is ready to be activated.
                /// </summary>
                /// <param name="newItem">The item that is about to be activated.</param>
                /// <returns>The item to be activated.</returns>
                protected override T EnsureItem(T newItem) {
                    if (newItem == null) {
                        newItem = DetermineNextItemToActivate(items, ActiveItem != null ? items.IndexOf(ActiveItem) : 0);
                    }
                    else {
                        var index = items.IndexOf(newItem);

                        if (index == -1)
                            items.Add(newItem);
                        else newItem = items[index];
                    }

                    return base.EnsureItem(newItem);
                }
            }
        }
    }
}
