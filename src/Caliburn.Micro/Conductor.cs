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
        public override async Task ActivateItem(T item)
        {
            if (item != null && item.Equals(ActiveItem))
            {
                if (IsActive)
                {
                    await ScreenExtensions.TryActivate(item);
                    OnActivationProcessed(item, true);
                }
                return;
            }

            var closeResult = await CloseStrategy.Execute(new[] { ActiveItem });
            if (closeResult.CanClose)
            {
                await ChangeActiveItem(item, true);
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
        public override async Task DeactivateItem(T item, bool close) {
            if (item == null || !item.Equals(ActiveItem)) {
                return;
            }

            var closeResult = await CloseStrategy.Execute(new[] { ActiveItem });
            if (closeResult.CanClose)
            {
                await ChangeActiveItem(default(T), closeResult.CanClose);
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
        protected override async Task OnActivate() {
            await ScreenExtensions.TryActivate(ActiveItem);
        }

        /// <summary>
        /// Called when deactivating.
        /// </summary>
        /// <param name="close">Indicates whether this instance will be closed.</param>
        protected override async Task OnDeactivate(bool close) {
            await ScreenExtensions.TryDeactivate(ActiveItem, close);
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