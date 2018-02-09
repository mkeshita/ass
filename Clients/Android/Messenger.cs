using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace norsu.ass
{
    public sealed class Messenger
    {
        private static Messenger _defaultMessenger;

        public static Messenger Default => _defaultMessenger ?? (_defaultMessenger = new Messenger());

        private MessageToActionMap _messageToActionMap = new MessageToActionMap();


        [Conditional("DEBUGx")]
        private void VerifyParameterType(Messages message, Type parameterType)
        {
            Type prevRegisteredType = null;
            if (_messageToActionMap.TryGetParameterType(message, ref prevRegisteredType))
            {
                if (prevRegisteredType != null && parameterType != null)
                {
                    if (prevRegisteredType != parameterType) throw new InvalidOperationException("check");
                }
                else
                {
                    if (prevRegisteredType != parameterType) throw new InvalidOperationException("check");
                }
            }
        }

        private void AddListener(Messages message, Delegate callback, Type parameterType)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            VerifyParameterType(message, parameterType);
            _messageToActionMap.AddAction(message, callback.Target, callback.Method, parameterType);
        }

        public void AddListener<T>(Messages message, Action<T> callback)
        {
            AddListener(message, callback, typeof(T));
        }

        public void AddListener(Messages message, Action callback)
        {
            AddListener(message, callback, null);
        }

        public void Broadcast(Messages message, object parameter)
        {
            Type registeredParameter = null;
            if (_messageToActionMap.TryGetParameterType(message, ref registeredParameter))
                if (registeredParameter == null) throw new TargetParameterCountException("Cannot pass a parameter with message "+message+". Registered actions(s) expect no parameter.)");
            var actions = _messageToActionMap.GetActions(message);
            actions?.ForEach(a =>
            {
                try
                {
                    a.DynamicInvoke(parameter);
                }
                catch (Exception )
                {
                    //
                }
                
            });
        }

        public void Broadcast(Messages message)
        {
            Type regParamType = null;
            if (_messageToActionMap.TryGetParameterType(message, ref regParamType))
                if (regParamType != null) throw new TargetParameterCountException($"Must pass a parameter of type { regParamType.FullName } with this message. Registered action(s) expect it.");
            var actions = _messageToActionMap.GetActions(message);
            actions?.ForEach(a => a.DynamicInvoke());
        }

        private class MessageToActionMap
        {
            private readonly Dictionary<Messages, List<WeakAction>> _map = new Dictionary<Messages, List<WeakAction>>();

            internal void AddAction(Messages message, object target, MethodInfo method, Type actionType)
            {
                if (method == null) throw new ArgumentNullException(nameof(method));
                lock (_map)
                {
                    if (!_map.ContainsKey(message)) _map.Add(message, new List<WeakAction>());
                    _map[message].Add(new WeakAction(target, method, actionType));
                }
            }

            internal List<Delegate> GetActions(Messages message)
            {
                List<Delegate> actions;
                lock (_map)
                {
                    if (!_map.ContainsKey(message)) return null;
                    var weakActions = _map[message];
                    actions = new List<Delegate>(weakActions.Count);
                    for (var i = weakActions.Count - 1; i >= 0; i--)
                    {
                        var weakAction = weakActions[i];
                        if (weakAction == null) continue;
                        var action = weakAction.CreateAction();
                        if (action != null)
                        {
                            actions.Add(action);
                        }
                        else
                        {
                            weakActions.Remove(weakAction);
                        }
                    }

                    if (weakActions.Count == 0) _map.Remove(message);
                }

                actions.Reverse();
                return actions;
            }

            internal bool TryGetParameterType(Messages message, ref Type parameterType)
            {
                parameterType = null;
                List<WeakAction> weakActions;
                lock (_map)
                {
                    if (!_map.TryGetValue(message, out weakActions)) return false;
                    if (weakActions.Count == 0) return false;
                }
                parameterType = weakActions[0].ParameterType;
                return true;
            }
        }

        private class WeakAction
        {
            internal Type ParameterType { get; }
            private Type _delegateType;
            private MethodInfo _method;
            private WeakReference _targetReference;

            internal WeakAction(object target, MethodInfo method, Type parameterType)
            {
                _targetReference = target == null ? null : new WeakReference(target);
                _method = method;
                ParameterType = parameterType;
                _delegateType = parameterType == null
                    ? typeof(Action)
                    : typeof(Action<>).MakeGenericType(parameterType);
            }

            internal Delegate CreateAction()
            {
                if (_targetReference == null)
                {
                    return Delegate.CreateDelegate(_delegateType, _method);
                }
                try
                {
                    var target = _targetReference.Target;
                    if (target != null) return Delegate.CreateDelegate(_delegateType, target, _method);
                }
                catch (Exception)
                {
                    //ignored
                }
                return null;
            }
        }
    }
}
