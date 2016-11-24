using System;
using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro.Async {
    /// <summary>
    /// Hosts extension methods for <see cref="IScreen"/> classes.
    /// </summary>
    public static class ScreenExtensions {
        /// <summary>
        /// Activates the item if it implements <see cref="IActivate"/>, otherwise does nothing.
        /// </summary>
        /// <param name="potentialActivatable">The potential activatable.</param>
        /// <param name="cancellationToken"></param>
        public static async Task TryActivate(object potentialActivatable, CancellationToken cancellationToken = default(CancellationToken)) {
            var activator = potentialActivatable as IActivate;
            if (activator == null) return;

            await activator.Activate(cancellationToken);
        }

        /// <summary>
        /// Deactivates the item if it implements <see cref="IDeactivate"/>, otherwise does nothing.
        /// </summary>
        /// <param name="potentialDeactivatable">The potential deactivatable.</param>
        /// <param name="close">Indicates whether or not to close the item after deactivating it.</param>
        /// <param name="cancellationToken"></param>
        public static Task TryDeactivate(object potentialDeactivatable, bool close, CancellationToken cancellationToken = default(CancellationToken)) {
            var deactivator = potentialDeactivatable as IDeactivate;
            if (deactivator == null) return TaskExtensions.CompletedTask;

            return deactivator.Deactivate(close, cancellationToken);
        }

        /// <summary>
        /// Closes the specified item.
        /// </summary>
        /// <param name="conductor">The conductor.</param>
        /// <param name="item">The item to close.</param>
        /// <param name="cancellationToken"></param>
        public static Task CloseItem(this IConductor conductor, object item, CancellationToken cancellationToken = default(CancellationToken)) {
            return conductor.DeactivateItem(item, true, cancellationToken);
        }

        /// <summary>
        /// Closes the specified item.
        /// </summary>
        /// <param name="conductor">The conductor.</param>
        /// <param name="item">The item to close.</param>
        public static Task CloseItem<T>(this ConductorBase<T> conductor, T item, CancellationToken cancellationToken = default(CancellationToken)) where T: class {
            return conductor.DeactivateItem(item, true, cancellationToken);
        }

        ///<summary>
        /// Activates a child whenever the specified parent is activated.
        ///</summary>
        ///<param name="child">The child to activate.</param>
        ///<param name="parent">The parent whose activation triggers the child's activation.</param>
        public static void ActivateWith(this IActivate child, IActivate parent) {
            var childReference = new WeakReference(child);
            EventHandler<ActivationEventArgs> handler = null;
            handler = async (s, e) => {
                var activatable = (IActivate) childReference.Target;
                if (activatable == null)
                    ((IActivate) s).Activated -= handler;
                else
                    await activatable.Activate();
            };
            parent.Activated += handler;
        }

        ///<summary>
        /// Deactivates a child whenever the specified parent is deactivated.
        ///</summary>
        ///<param name="child">The child to deactivate.</param>
        ///<param name="parent">The parent whose deactivation triggers the child's deactivation.</param>
        public static void DeactivateWith(this IDeactivate child, IDeactivate parent) {
            var childReference = new WeakReference(child);
            EventHandler<DeactivationEventArgs> handler = null;
            handler = async (s, e) => {
                var deactivatable = (IDeactivate) childReference.Target;
                if (deactivatable == null)
                    ((IDeactivate)s).Deactivated -= handler;
                else
                    await deactivatable.Deactivate(e.WasClosed);
            };
            parent.Deactivated += handler;
        }

        ///<summary>
        /// Activates and Deactivates a child whenever the specified parent is Activated or Deactivated.
        ///</summary>
        ///<param name="child">The child to activate/deactivate.</param>
        ///<param name="parent">The parent whose activation/deactivation triggers the child's activation/deactivation.</param>
        public static void ConductWith<TChild, TParent>(this TChild child, TParent parent) 
            where TChild : IActivate, IDeactivate
            where TParent : IActivate, IDeactivate
        {
            child.ActivateWith(parent);
            child.DeactivateWith(parent);
        }
    }
}
