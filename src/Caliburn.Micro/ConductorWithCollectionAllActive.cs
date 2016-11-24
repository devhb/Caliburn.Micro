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
            /// An implementation of <see cref="IConductor"/> that holds on to many items which are all activated.
            /// </summary>
            public class AllActive : ConductorBase<T> {
                private readonly BindableCollection<T> items = new BindableCollection<T>();
                private readonly bool openPublicItems;

                /// <summary>
                /// Initializes a new instance of the <see cref="Conductor&lt;T&gt;.Collection.AllActive"/> class.
                /// </summary>
                /// <param name="openPublicItems">if set to <c>true</c> opens public items that are properties of this class.</param>
                public AllActive(bool openPublicItems)
                    : this() {
                    this.openPublicItems = openPublicItems;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="Conductor&lt;T&gt;.Collection.AllActive"/> class.
                /// </summary>
                public AllActive() {
                    items.CollectionChanged += (s, e) => {
                        switch (e.Action) {
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
                /// Called when activating.
                /// </summary>
                /// <param name="cancellationToken"></param>
                protected override async Task OnActivate(CancellationToken cancellationToken = default(CancellationToken))
                {
                    var activatables = items.OfType<IActivate>().Select(a => a.Activate(cancellationToken));
                    await Task.WhenAll(activatables);
                }

                /// <summary>
                /// Called when deactivating.
                /// </summary>
                /// <param name="close">Indicates whether this instance will be closed.</param>
                /// <param name="cancellationToken"></param>
                protected override async Task OnDeactivate(bool close, CancellationToken cancellationToken = default(CancellationToken))
                {
                    // remove items that are already closed
                    var closeables = items.OfType<IDeactivate>().ToArray();
                    var closeableResults = closeables.Select(c => c.Deactivate(true, cancellationToken));
                    await Task.WhenAll(closeableResults);

                    if (close)
                    {
                        items.Clear();    
                    }
                }

                /// <summary>
                /// Called to check whether or not this instance can close.
                /// </summary>
                public override async Task<bool> CanClose()
                {
                    var closeResult = await CloseStrategy.Execute(items.ToList());
                    
                    if (!closeResult.CanClose && closeResult.Items.Length > 0)
                    {
                        // remove items that are already closed
                        var closeables = closeResult.Items.OfType<IDeactivate>().ToArray();
                        var closeableResults = closeables.Select(c => c.Deactivate(true));
                        await Task.WhenAll(closeableResults);
                        items.RemoveRange(closeables.Cast<T>());
                    }
                    return closeResult.CanClose;
                }

                /// <summary>
                /// Called when initializing.
                /// </summary>
                /// <param name="cancellationToken"></param>
                protected override async Task OnInitialize(CancellationToken cancellationToken = default(CancellationToken))
                {
                    if (openPublicItems)
                    {
                        var activationItems = GetType().GetProperties()
                            .Where(x => x.Name != "Parent" && typeof(T).IsAssignableFrom(x.PropertyType))
                            .Select(x => x.GetValue(this, null))
                            .Cast<T>();

                        var activationTasks = activationItems.OfType<IActivate>().Select(a => a.Activate(cancellationToken));
                        await Task.WhenAll(activationTasks);
                    }
                }

                /// <summary>
                /// Activates the specified item.
                /// </summary>
                /// <param name="item">The item to activate.</param>
                /// <param name="cancellationToken"></param>
                public override async Task ActivateItem(T item, CancellationToken cancellationToken = default(CancellationToken)) {
                    if (item == null)
                    {
                        return;
                    }

                    item = EnsureItem(item);

                    if (IsActive)
                    {
                        await ScreenExtensions.TryActivate(item, cancellationToken);
                    }

                    OnActivationProcessed(item, true);
                }

                /// <summary>
                /// Deactivates the specified item.
                /// </summary>
                /// <param name="item">The item to close.</param>
                /// <param name="close">Indicates whether or not to close the item after deactivating it.</param>
                /// <param name="cancellationToken"></param>
                public override async Task DeactivateItem(T item, bool close, CancellationToken cancellationToken = default(CancellationToken))
                {
                    if (item == null) {
                        return;
                    }

                    if (close)
                    {
                        var closeResult = await CloseStrategy.Execute(new[] {item});
                        if (closeResult.CanClose)
                        {
                            await CloseItemCore(item, cancellationToken);
                        }
                    }
                    else
                    {
                        await ScreenExtensions.TryDeactivate(item, false, cancellationToken);
                    }
                }

                /// <summary>
                /// Gets the children.
                /// </summary>
                /// <returns>The collection of children.</returns>
                public override IEnumerable<T> GetChildren() {
                    return items;
                }

                private async Task CloseItemCore(T item, CancellationToken cancellationToken = default(CancellationToken)) {
                    await ScreenExtensions.TryDeactivate(item, true, cancellationToken);
                    items.Remove(item);
                }

                /// <summary>
                /// Ensures that an item is ready to be activated.
                /// </summary>
                /// <param name="newItem">The item that is about to be activated.</param>
                /// <returns>The item to be activated.</returns>
                protected override T EnsureItem(T newItem) {
                    var index = items.IndexOf(newItem);

                    if (index == -1) {
                        items.Add(newItem);
                    }
                    else {
                        newItem = items[index];
                    }

                    return base.EnsureItem(newItem);
                }
            }
        }
    }
}
