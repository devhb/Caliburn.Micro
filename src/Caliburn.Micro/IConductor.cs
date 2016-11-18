using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro {
    using System;

    /// <summary>
    /// Denotes an instance which conducts other objects by managing an ActiveItem and maintaining a strict lifecycle.
    /// </summary>
    /// <remarks>Conducted instances can optin to the lifecycle by impelenting any of the follosing <see cref="IActivate"/>, <see cref="IDeactivate"/>, <see cref="IGuardClose"/>.</remarks>
    public interface IConductor : IParent, INotifyPropertyChangedEx {
        /// <summary>
        /// Activates the specified item.
        /// </summary>
        /// <param name="item">The item to activate.</param>
        /// <param name="cancellationToken"></param>
        Task ActivateItem(object item, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deactivates the specified item.
        /// </summary>
        /// <param name="item">The item to close.</param>
        /// <param name="close">Indicates whether or not to close the item after deactivating it.</param>
        /// <param name="cancellationToken"></param>
        Task DeactivateItem(object item, bool close, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Occurs when an activation request is processed.
        /// </summary>
        event EventHandler<ActivationProcessedEventArgs> ActivationProcessed;
    }

    /// <summary>
    /// An <see cref="IConductor"/> that also implements <see cref="IHaveActiveItem"/>.
    /// </summary>
    public interface IConductActiveItem : IConductor, IHaveActiveItem { }
}
