using System.Threading.Tasks;

namespace Caliburn.Micro {
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    public partial class Conductor<T> {
        /// <summary>
        /// An implementation of <see cref="IConductor"/> that holds on many items.
        /// </summary>
        public partial class Collection {
            /// <summary>
            /// An implementation of <see cref="IConductor"/> that holds on to many items which are all activated.
            /// </summary>
            public class AllActive : ConductorBase<T> {
                private readonly BindableCollection<T> m_items = new BindableCollection<T>();
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
                    m_items.CollectionChanged += (s, e) => {
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
                                m_items.OfType<IChild>().Apply(x => x.Parent = this);
                                break;
                        }
                    };
                }

                /// <summary>
                /// Gets the items that are currently being conducted.
                /// </summary>
                public IObservableCollection<T> MItems {
                    get { return m_items; }
                }

                /// <summary>
                /// Called when activating.
                /// </summary>
                protected override async Task OnActivate()
                {
                    var activatables = m_items.OfType<IActivate>().Select(a => a.Activate());
                    await Task.WhenAll(activatables);
                }

                /// <summary>
                /// Called when deactivating.
                /// </summary>
                /// <param name="close">Indicates whether this instance will be closed.</param>
                protected override async Task OnDeactivate(bool close)
                {
                    // remove items that are already closed
                    var closeables = m_items.OfType<IDeactivate>().ToArray();
                    var closeableResults = closeables.Select(c => c.Deactivate(true));
                    await Task.WhenAll(closeableResults);

                    if (close)
                    {
                        m_items.Clear();    
                    }
                }

                /// <summary>
                /// Called to check whether or not this instance can close.
                /// </summary>
                public override async Task<bool> CanClose()
                {
                    var closeResult = await CloseStrategy.Execute(m_items.ToList());
                    
                    if (!closeResult.CanClose && closeResult.Items.Length > 0)
                    {
                        // remove items that are already closed
                        var closeables = closeResult.Items.OfType<IDeactivate>().ToArray();
                        var closeableResults = closeables.Select(c => c.Deactivate(true));
                        await Task.WhenAll(closeableResults);
                        m_items.RemoveRange(closeables.Cast<T>());
                    }
                    return closeResult.CanClose;
                }

                /// <summary>
                /// Called when initializing.
                /// </summary>
                protected override async Task OnInitialize()
                {
                    if (openPublicItems)
                    {
                        var activationItems = GetType().GetProperties()
                            .Where(x => x.Name != "Parent" && typeof(T).IsAssignableFrom(x.PropertyType))
                            .Select(x => x.GetValue(this, null))
                            .Cast<T>();

                        var activationTasks = activationItems.OfType<IActivate>().Select(a => a.Activate());
                        await Task.WhenAll(activationTasks);
                    }
                }

                /// <summary>
                /// Activates the specified item.
                /// </summary>
                /// <param name="item">The item to activate.</param>
                public override async Task ActivateItem(T item) {
                    if (item == null)
                    {
                        return;
                    }

                    item = EnsureItem(item);

                    if (IsActive)
                    {
                        await ScreenExtensions.TryActivate(item);
                    }

                    OnActivationProcessed(item, true);
                }

                /// <summary>
                /// Deactivates the specified item.
                /// </summary>
                /// <param name="item">The item to close.</param>
                /// <param name="close">Indicates whether or not to close the item after deactivating it.</param>
                public override async Task DeactivateItem(T item, bool close)
                {
                    if (item == null) {
                        return;
                    }

                    if (close)
                    {
                        var closeResult = await CloseStrategy.Execute(new[] {item});
                        if (closeResult.CanClose)
                        {
                            await CloseItemCore(item);
                        }
                    }
                    else
                    {
                        await ScreenExtensions.TryDeactivate(item, false);
                    }
                }

                /// <summary>
                /// Gets the children.
                /// </summary>
                /// <returns>The collection of children.</returns>
                public override IEnumerable<T> GetChildren() {
                    return m_items;
                }

                private async Task CloseItemCore(T item) {
                    await ScreenExtensions.TryDeactivate(item, true);
                    m_items.Remove(item);
                }

                /// <summary>
                /// Ensures that an item is ready to be activated.
                /// </summary>
                /// <param name="newItem">The item that is about to be activated.</param>
                /// <returns>The item to be activated.</returns>
                protected override T EnsureItem(T newItem) {
                    var index = m_items.IndexOf(newItem);

                    if (index == -1) {
                        m_items.Add(newItem);
                    }
                    else {
                        newItem = m_items[index];
                    }

                    return base.EnsureItem(newItem);
                }
            }
        }
    }
}
