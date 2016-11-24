using System;
using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro.Async {
    /// <summary>
    /// A base class for various implementations of <see cref="IConductor"/> that maintain an active item.
    /// </summary>
    /// <typeparam name="T">The type that is being conducted.</typeparam>
    public abstract class ConductorBaseWithActiveItem<T> : ConductorBase<T>, IConductActiveItem where T: class {
        T activeItem;

        /// <summary>
        /// The currently active item.
        /// </summary>
        public T ActiveItem {
            get { return activeItem; }
            set { ActivateItem(value); }
        }

        /// <summary>
        /// The currently active item.
        /// </summary>
        /// <value></value>
        object IHaveActiveItem.ActiveItem {
            get { return ActiveItem; }
            set { ActiveItem = (T)value; }
        }
        
        /// <summary>
        /// Changes the active item.
        /// </summary>
        /// <param name="newItem">The new item to activate.</param>
        /// <param name="closePrevious">Indicates whether or not to close the previous active item.</param>
        /// <param name="cancellationToken"></param>
        protected virtual async Task ChangeActiveItem(T newItem, bool closePrevious, CancellationToken cancellationToken = default(CancellationToken)) {
            T previousActiveItem = activeItem;
            try {
                await ScreenExtensions.TryDeactivate(activeItem, closePrevious, cancellationToken);

                newItem = EnsureItem(newItem);

                activeItem = newItem;

                // this notification about ActiveItem causes the ViewModelBinder do it's work so the view is displayed
                // before OnInitialize/OnActivate are called
                NotifyOfPropertyChange("ActiveItem");

                if (IsActive)
                {
                    await ScreenExtensions.TryActivate(newItem, cancellationToken);
                }

                OnActivationProcessed(activeItem, true);
            }
            catch (OperationCanceledException) {
                Log.Info($"Canceling ChangeActiveItem {this}");
                activeItem = previousActiveItem;
                throw;
            }
        }

        static readonly ILog Log = LogManager.GetLog(typeof(ConductorBaseWithActiveItem<>));
    }
}