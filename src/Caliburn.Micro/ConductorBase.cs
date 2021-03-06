﻿using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro {
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// A base class for various implementations of <see cref="IConductor"/>.
    /// </summary>
    /// <typeparam name="T">The type that is being conducted.</typeparam>
    public abstract class ConductorBase<T> : Screen, IConductor, IParent<T> where T : class {
        private ICloseStrategy<T> closeStrategy;

        /// <summary>
        /// Gets or sets the close strategy.
        /// </summary>
        /// <value>The close strategy.</value>
        public ICloseStrategy<T> CloseStrategy {
            get { return closeStrategy ?? (closeStrategy = new DefaultCloseStrategy<T>()); }
            set { closeStrategy = value; }
        }

        Task IConductor.ActivateItem(object item, CancellationToken cancellationToken) {
            return ActivateItem((T) item, cancellationToken);
        }

        Task IConductor.DeactivateItem(object item, bool close, CancellationToken cancellationToken) {
            return DeactivateItem((T) item, close, cancellationToken);
        }

        IEnumerable IParent.GetChildren() {
            return GetChildren();
        }

        /// <summary>
        /// Occurs when an activation request is processed.
        /// </summary>
        public virtual event EventHandler<ActivationProcessedEventArgs> ActivationProcessed = delegate { };

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <returns>The collection of children.</returns>
        public abstract IEnumerable<T> GetChildren();

        /// <summary>
        /// Activates the specified item.
        /// </summary>
        /// <param name="item">The item to activate.</param>
        /// <param name="cancellationToken"></param>
        public abstract Task ActivateItem(T item, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deactivates the specified item.
        /// </summary>
        /// <param name="item">The item to close.</param>
        /// <param name="close">Indicates whether or not to close the item after deactivating it.</param>
        /// <param name="cancellationToken"></param>
        public abstract Task DeactivateItem(T item, bool close, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Called by a subclass when an activation needs processing.
        /// </summary>
        /// <param name="item">The item on which activation was attempted.</param>
        /// <param name="success">if set to <c>true</c> activation was successful.</param>
        protected virtual void OnActivationProcessed(T item, bool success) {
            if (item == null) {
                return;
            }

            var handler = ActivationProcessed;
            if (handler != null) {
                handler(this, new ActivationProcessedEventArgs
                {
                    Item = item,
                    Success = success
                });
            }
        }

        /// <summary>
        /// Ensures that an item is ready to be activated.
        /// </summary>
        /// <param name="newItem">The item that is about to be activated.</param>
        /// <returns>The item to be activated.</returns>
        protected virtual T EnsureItem(T newItem) {
            var node = newItem as IChild;
            if (node != null && node.Parent != this)
                node.Parent = this;

            return newItem;
        }
    }
}