using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace IceCoffee.Common
{
    public class EventForwarder
    {
        public event Action<string, EventArgs>? EventRaised;

        public EventForwarder(object instance, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        {
            SubscribeEvents(instance.GetType(), instance, bindingFlags);
        }

        public EventForwarder(Type staticType, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        {
            SubscribeEvents(staticType, null, bindingFlags);
        }

        private void SubscribeEvents(Type type, object? instance, BindingFlags bindingFlags)
        {
            var events = type.GetEvents(bindingFlags);
            foreach (var evt in events)
            {
                var handler = CreateEventHandler(evt);
                evt.AddEventHandler(instance, handler);
            }
        }

        private void OnEventRaised(string eventName, EventArgs eventArgs)
        {
            EventRaised?.Invoke(eventName, eventArgs);
        }

        private Delegate CreateEventHandler(EventInfo eventInfo)
        {
            var handlerType = eventInfo.EventHandlerType!;
            var invokeMethod = handlerType.GetMethod("Invoke")!;
            var parameters = invokeMethod.GetParameters();

            if (parameters.Length != 2)
                throw new InvalidOperationException("The number of event delegate parameters is not 2");

            if (parameters[0].ParameterType != typeof(object))
                throw new InvalidOperationException("The first parameter is not of type object");

            var eventArgsType = parameters[1].ParameterType;
            if (typeof(EventArgs).IsAssignableFrom(eventArgsType) == false)
                throw new InvalidOperationException("The second argument is not an EventArgs or subclass thereof");

            // Since DynamicMethod is a static method, there is no this, and the instance needs to be bound through CreateDelegate
            // But here we can't directly access the instance, so we use the closure target to bind the instance
            var parameterTypes = new Type[] { typeof(EventForwarder), typeof(object), eventArgsType };

            string eventName = eventInfo.Name;

            // Define a dynamic method with the same signature as the event delegate and return void
            var dm = new DynamicMethod(
                $"DynamicHandler_{eventName}",
                typeof(void),
                parameterTypes,
                typeof(EventForwarder).Module,
                skipVisibility: true);

            var il = dm.GetILGenerator();

            // Load the closure instance: this (Parameter 0)
            il.Emit(OpCodes.Ldarg_0);

            // Loading event name string: eventName (Parameter 1)
            il.Emit(OpCodes.Ldstr, eventName);

            // Loading event parameter: eventArgs (Parameter 2)
            il.Emit(OpCodes.Ldarg_2);

            // Calling instance methods: OnEventRaised(object, EventArgs)
#if NETCOREAPP
            var onEventRaisedMethod = typeof(EventForwarder)
                .GetMethod(nameof(OnEventRaised), BindingFlags.NonPublic | BindingFlags.Instance, types: new Type[] { typeof(string), typeof(EventArgs) })!;
#else
            var onEventRaisedMethod = typeof(EventForwarder)
                .GetMethod(nameof(OnEventRaised), BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(EventArgs) }, null)!;
#endif
            il.Emit(OpCodes.Callvirt, onEventRaisedMethod);

            il.Emit(OpCodes.Ret);

            // Create a delegate and bind the instance to a closure
            var del = dm.CreateDelegate(handlerType, this);
            return del;
        }
    }
}
