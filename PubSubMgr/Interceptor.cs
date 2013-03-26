using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WebSolutionsPlatform.PubSubManager
{
    /// <summary>
    /// Defines the property types supported by the base event class.
    /// </summary>
    public enum InterceptDirection : int
    {
        /// <summary>
        /// Intercept all events subscribed to
        /// </summary>
        Subscribe = 0,

        /// <summary>
        /// Intercept all events published
        /// </summary>
        Publish = 1
    }

    /// <summary>
    /// Interceptor delegate used for intercepting events
    /// </summary>
    /// <param name="wspEvent">This is the Wsp event being intercepted</param>
    /// <param name="wspEventsOut">If this array has one or more events then they will be returned to the application instead of the original wspEvent</param>
    /// <returns>False to have Wsp stop all processing of the event; True to hand the event(s) to the application</returns>
    public delegate bool InterceptorDelegate(WspEvent wspEvent, out WspEvent[] wspEventsOut);
    
    /// <summary>
    /// This is a delegate which is returned during registration of an interceptor to allow events to be published by the
    /// interceptor or one of its components to bypass the interceptor logic.
    /// </summary>
    /// <param name="wspEvent">Event to be published</param>
    public delegate void OnNextPrivate(WspEvent wspEvent);

    /// <summary>
    /// Interceptor class to register an interceptor which will be called for Publish and Subscribe of events
    /// </summary>
    public class Interceptor
    {
        internal static Guid subscribeRegistrationGuid = Guid.Empty;
        internal static Guid publishRegistrationGuid = Guid.Empty;

        internal static InterceptorDelegate publishInterceptor = null;
        internal static InterceptorDelegate subscribeInterceptor = null;

        /// <summary>
        /// Method used to register an interceptor with Wsp. Only one interceptor is permitted.
        /// </summary>
        /// <param name="direction">Defines the direction the interceptor is registering for: publish or subscribe</param>
        /// <param name="interceptor">Delegate which Wsp will call for interception</param>
        /// <param name="onNextPrivate">Delegate to publish events which bypass interceptor logic</param>
        /// <returns>Guid needed to unregister</returns>
        public static Guid Register(InterceptDirection direction, InterceptorDelegate interceptor, out OnNextPrivate onNextPrivate)
        {
            WspEventPublish eventPublish = new WspEventPublish();

            onNextPrivate = eventPublish.OnNextPrivate;

            if (direction != InterceptDirection.Publish && direction != InterceptDirection.Subscribe)
            {
                onNextPrivate = null;

                return Guid.Empty;
            }

            if (direction == InterceptDirection.Subscribe)
            {
                if (subscribeRegistrationGuid == Guid.Empty)
                {
                    subscribeInterceptor = interceptor;
                    subscribeRegistrationGuid = Guid.NewGuid();

                    return subscribeRegistrationGuid;
                }
                else
                {
                    onNextPrivate = null;

                    return Guid.Empty;
                }
            }
            else
            {
                if (publishRegistrationGuid == Guid.Empty)
                {
                    publishInterceptor = interceptor;
                    publishRegistrationGuid = Guid.NewGuid();

                    return publishRegistrationGuid;
                }
                else
                {
                    onNextPrivate = null;

                    return Guid.Empty;
                }
            }
        }

        /// <summary>
        /// Method to unregister an interceptor
        /// </summary>
        /// <param name="direction">Defines if interceptor is for: publish or subscribe</param>
        /// <param name="registrationGuid">Goid which was returned from the register call</param>
        public static void UnRegister(InterceptDirection direction, Guid registrationGuid)
        {
            if (direction != InterceptDirection.Publish && direction != InterceptDirection.Subscribe)
            {
                return;
            }

            if (direction == InterceptDirection.Subscribe)
            {
                if (subscribeRegistrationGuid == registrationGuid)
                {
                    subscribeInterceptor = null;
                    subscribeRegistrationGuid = Guid.Empty;
                }
            }
            else
            {
                if (publishRegistrationGuid == registrationGuid)
                {
                    publishInterceptor = null;
                    publishRegistrationGuid = Guid.Empty;
                }
            }

            return;
        }
    }
}
