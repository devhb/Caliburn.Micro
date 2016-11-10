using System.Threading.Tasks;

namespace Caliburn.Micro {
    using System;

    /// <summary>
    /// Denotes an instance which may prevent closing.
    /// </summary>
    public interface IGuardClose : IClose {
        /// <summary>
        /// Called to check whether or not this instance can close.
        /// </summary>
        Task<bool> CanClose();
    }
}
