﻿using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro {
    using System;

    /// <summary>
    /// A base implementation of <see cref = "IScreen" />.
    /// </summary>
    public class Screen : ViewAware, IScreen, IChild {
        static readonly ILog Log = LogManager.GetLog(typeof (Screen));

        bool isActive;
        bool isInitialized;
        object parent;
        string displayName;

        /// <summary>
        /// Creates an instance of the screen.
        /// </summary>
        public Screen() {
            displayName = GetType().FullName;
        }

        /// <summary>
        /// Gets or Sets the Parent <see cref = "IConductor" />
        /// </summary>
        public virtual object Parent {
            get { return parent; }
            set {
                parent = value;
                NotifyOfPropertyChange("Parent");
            }
        }

        /// <summary>
        /// Gets or Sets the Display Name
        /// </summary>
        public virtual string DisplayName {
            get { return displayName; }
            set {
                displayName = value;
                NotifyOfPropertyChange("DisplayName");
            }
        }

        /// <summary>
        /// Indicates whether or not this instance is currently active.
        /// Virtualized in order to help with document oriented view models.
        /// </summary>
        public virtual bool IsActive {
            get { return isActive; }
            private set {
                isActive = value;
                NotifyOfPropertyChange("IsActive");
            }
        }

        /// <summary>
        /// Indicates whether or not this instance is currently initialized.
        /// Virtualized in order to help with document oriented view models.
        /// </summary>
        public virtual bool IsInitialized {
            get { return isInitialized; }
            private set {
                isInitialized = value;
                NotifyOfPropertyChange("IsInitialized");
            }
        }

        /// <summary>
        /// Raised after activation occurs.
        /// </summary>
        public virtual event EventHandler<ActivationEventArgs> Activated = delegate { };

        /// <summary>
        /// Raised before deactivation.
        /// </summary>
        public virtual event EventHandler<DeactivationEventArgs> AttemptingDeactivation = delegate { };

        /// <summary>
        /// Raised after deactivation.
        /// </summary>
        public virtual event EventHandler<DeactivationEventArgs> Deactivated = delegate { };

        private struct ActivationState {
            internal readonly bool IsInitialized;
            internal readonly bool IsActive;

            public ActivationState(bool isInitialized, bool active) {
                IsInitialized = isInitialized;
                IsActive = active;
            }
        }

        //async Task IActivate.Activate(CancellationToken cancellationToken)
        //{
        //    var state = new ActivationState(isInitialized, isActive);
        //    var wasCanceled = false;
        //    var wasRaisedWhileInitilazation = false;
        //    try
        //    {

        //        if (IsActive)
        //        {
        //            return;
        //        }

        //        var initialized = false;
        //        var handler = Activated;
        //        // raise event to let the framework show the ui before we initializing
        //        if (!IsInitialized)
        //        {
        //            var handler1 = handler;
        //            await Execute.OnUIThreadAsync(() => handler1?.Invoke(this, new ActivationEventArgs { WasInitialized = false }));
        //            wasRaisedWhileInitilazation = true;
        //        }

        //        if (!IsInitialized)
        //        {
        //            IsInitialized = initialized = true;
        //            Log.Info("**** Initializing {0}.", this);
        //            await OnInitialize(cancellationToken);
        //        }

        //        IsActive = true;
        //        Log.Info("Activating {0}.", this);
        //        await OnActivate(cancellationToken);
        //        Log.Info("**** Activating finished {0}.", this);

        //        if (!wasRaisedWhileInitilazation)
        //        {
        //            handler = Activated;
        //            if (handler != null)
        //            {
        //                handler(this,
        //                    new ActivationEventArgs
        //                    {
        //                        WasInitialized = initialized
        //                    });
        //            }
        //        }


        //    }
        //    catch (OperationCanceledException)
        //    {
        //        wasCanceled = true;
        //        Log.Info("**** Activating canceled {0}.", this);

        //        throw;
        //    }
        //    finally
        //    {
        //        if (wasCanceled)
        //        {
        //            IsActive = state.IsActive;
        //            IsInitialized = state.IsInitialized;
        //        }
        //    }
        //}

        async Task IActivate.Activate(CancellationToken cancellationToken)
        {
            var wasCanceled = false;
            var state = new ActivationState(isInitialized, isActive);
            try
            {
                if (IsActive)
                {
                    return;
                }

                var initialized = false;
                
                if (!IsInitialized)
                {
                    IsInitialized = initialized = true;
                    await OnInitialize(cancellationToken);
                }
                
                IsActive = true;
                Log.Info("Activating {0}.", this);
                
                await OnActivate(cancellationToken);

                var handler = Activated;
                handler?.Invoke(this,
                    new ActivationEventArgs
                    {
                        WasInitialized = initialized
                    });
            }
            catch (OperationCanceledException)
            {
                wasCanceled = true;
                Log.Info($"Canceling Activate {this}");
                throw;
            }
            finally
            {
                if (wasCanceled)
                {
                    IsActive = state.IsActive;
                    IsInitialized = state.IsInitialized;
                }
            }
        }
    

        /// <summary>
        /// Called when initializing.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected virtual Task OnInitialize(CancellationToken cancellationToken = default(CancellationToken)) {
            return TaskExtensions.CompletedTask;
        }

        /// <summary>
        /// Called when activating.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected virtual Task OnActivate(CancellationToken cancellationToken = default(CancellationToken)) {
            return TaskExtensions.CompletedTask;
        }

        async Task IDeactivate.Deactivate(bool close, CancellationToken cancellationToken) {
            if (IsActive || (IsInitialized && close)) {
                var attemptingDeactivationHandler = AttemptingDeactivation;
                if (attemptingDeactivationHandler != null) {
                    attemptingDeactivationHandler(this, new DeactivationEventArgs
                    {
                        WasClosed = close
                    });
                }

                IsActive = false;
                Log.Info("Deactivating {0}.", this);
                await OnDeactivate(close, cancellationToken);

                var deactivatedHandler = Deactivated;
                if (deactivatedHandler != null) {
                    deactivatedHandler(this, new DeactivationEventArgs
                    {
                        WasClosed = close
                    });
                }

                if (close) {
                    Views.Clear();
                    Log.Info("Closed {0}.", this);
                }
            }
        }

        /// <summary>
        /// Called when deactivating.
        /// </summary>
        /// <param name = "close">Indicates whether this instance will be closed.</param>
        /// <param name="cancellationToken"></param>
        protected virtual Task OnDeactivate(bool close, CancellationToken cancellationToken = default(CancellationToken)) {
            return TaskExtensions.CompletedTask;
        }

        /// <summary>
        /// Called to check whether or not this instance can close.
        /// </summary>
        public virtual Task<bool> CanClose() {
            return TaskExtensions.TrueTask;
        }

        /// <summary>
        /// Tries to close this instance by asking its Parent to initiate shutdown or by asking its corresponding view to close.
        /// Also provides an opportunity to pass a dialog result to it's corresponding view.
        /// </summary>
        public virtual async Task<bool?> TryClose() {
            bool? result = null;
            await PlatformProvider.Current.GetViewCloseAction(this, Views.Values, result).OnUIThreadAsync();
            return result;
            //PlatformProvider.Current.GetViewCloseAction(this, Views.Values, dialogResult).OnUIThread();
        }
    }
}