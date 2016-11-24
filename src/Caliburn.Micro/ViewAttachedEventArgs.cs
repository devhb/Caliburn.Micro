using System;

namespace Caliburn.Micro.Async {
    /// <summary>
    /// The event args for the <see cref="IViewAware.ViewAttached"/> event.
    /// </summary>
    public class ViewAttachedEventArgs : EventArgs {
        /// <summary>
        /// The view.
        /// </summary>
        public object View;

        /// <summary>
        /// The context.
        /// </summary>
        public object Context;
    }
}
