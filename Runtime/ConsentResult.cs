using System;
using Cysharp.Threading.Tasks;

namespace RiseOn.Consent {
    /// <summary>How one run of the consent flow ended: <see cref="ConsentSucceeded"/> or <see cref="ConsentFailed"/>.</summary>
    public abstract class ConsentResult {
        private protected ConsentResult() { }
    }

    /// <summary>The flow is complete, with a fresh decision or, when UMP could not be reached, the stored one.</summary>
    public sealed class ConsentSucceeded : ConsentResult {
        public ConsentState State { get; }

        internal ConsentSucceeded(ConsentState state) {
            State = state;
        }
    }

    /// <summary>
    /// UMP failed and no decision from an earlier launch exists, so no ads may be requested.<br/>
    /// Until its owner retries or continues, the flow stays incomplete and everything awaiting <see cref="ConsentFlow.WaitUntilCompleteAsync"/> keeps waiting.
    /// </summary>
    public sealed class ConsentFailed : ConsentResult {
        private readonly Func<UniTask<ConsentResult>> retry;
        private readonly Func<UniTask>                continueWithoutDecision;
        private          bool                         resolved;

        public ConsentException Error { get; }

        internal ConsentFailed(ConsentException error, Func<UniTask<ConsentResult>> retry, Func<UniTask> continueWithoutDecision) {
            Error                        = error;
            this.retry                   = retry;
            this.continueWithoutDecision = continueWithoutDecision;
        }

        /// <summary>Runs the flow again; the result belongs to the caller and is not handed out by <see cref="ConsentFlow.PopResultAsync"/>.</summary>
        public UniTask<ConsentResult> RetryAsync() {
            Resolve();
            return retry();
        }

        /// <summary>
        /// Completes the flow without a decision: <see cref="ConsentState.CanRequestAds"/> stays false for this launch, and the next launch asks again.
        /// </summary>
        public UniTask ContinueAsync() {
            Resolve();
            return continueWithoutDecision();
        }

        private void Resolve() {
            if (resolved) throw new InvalidOperationException($"{nameof(ConsentFailed)}: this failure was already retried or continued");
            resolved = true;
        }
    }

    public enum ConsentErrorKind {
        /// <summary>No connection, or UMP's servers did not answer in time; trying again can help.</summary>
        Network
      , /// <summary>The AdMob app id or the Privacy & messaging setup is wrong; only the developer can fix it.</summary>
        Misconfiguration
      , /// <summary>UMP failed on its own; trying again may help.</summary>
        Internal
    }

    public sealed class ConsentException : Exception {
        public ConsentErrorKind Kind { get; }

        internal ConsentException(ConsentErrorKind kind, string message) : base(message) {
            Kind = kind;
        }
    }
}