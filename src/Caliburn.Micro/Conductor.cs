using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An implementation of <see cref="IConductor"/> that holds on to and activates only one item at a time.
    /// </summary>
    public partial class Conductor<T> : ConductorBaseWithActiveItem<T> where T: class {
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
                    await ScreenExtensions.TryActivate(item, cancellationToken);
                    OnActivationProcessed(item, true);
                }
                return;
            }

            var closeResult = await CloseStrategy.Execute(new[] { ActiveItem });
            if (closeResult.CanClose)
            {
                await ChangeActiveItem(item, true, cancellationToken);
            }
            else
            {
                OnActivationProcessed(item, false);
            }
        }

        /// <summary>
        /// Deactivates the specified item.
        /// </summary>
        /// <param name="item">The item to close.</param>
        /// <param name="close">Indicates whether or not to close the item after deactivating it.</param>
        /// <param name="cancellationToken"></param>
        public override async Task DeactivateItem(T item, bool close, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (item == null || !item.Equals(ActiveItem)) {
                return;
            }

            var closeResult = await CloseStrategy.Execute(new[] { ActiveItem });
            if (closeResult.CanClose)
            {
                await ChangeActiveItem(default(T), closeResult.CanClose, cancellationToken);
            }
        }

        /// <summary>
        /// Called to check whether or not this instance can close.
        /// </summary>
        public override async Task<bool> CanClose()
        {
            var closeResult = await CloseStrategy.Execute(new[] { ActiveItem });
            return closeResult.CanClose;
        }

        /// <summary>
        /// Called when activating.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected override Task OnActivate(CancellationToken cancellationToken = new CancellationToken()) {
            return ScreenExtensions.TryActivate(ActiveItem, cancellationToken);
        }

        /// <summary>
        /// Called when deactivating.
        /// </summary>
        /// <param name="close">Indicates whether this instance will be closed.</param>
        /// <param name="cancellationToken"></param>
        protected override  Task OnDeactivate(bool close, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ScreenExtensions.TryDeactivate(ActiveItem, close, cancellationToken);
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <returns>The collection of children.</returns>
        public override IEnumerable<T> GetChildren() {
            return new[] { ActiveItem };
        }
    }
}