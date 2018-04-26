﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FluentAssertions;
using Foundary.Extensions;
using FoundaryMediaPlayer.Configuration;
using FoundaryMediaPlayer.Events;
using Prism.Events;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace FoundaryMediaPlayer.Input
{
    /// <summary>
    /// The input binding manager.
    /// </summary>
    public static class InputBindingManager
    {
        private static bool bInitialized { get; set; }

        /// <summary>
        /// The application event aggregator.
        /// </summary>
        private static IEventAggregator EventAggregator { get; set; }

        /// <summary>
        /// The application store.
        /// </summary>
        private static Store Store { get; set; }

        /// <summary>
        /// Registered key bindings.
        /// </summary>
        private static KeyBindingCollection Bindings { get; } = new KeyBindingCollection();

        /// <summary>
        /// Initializes the input binding manager.
        /// </summary>
        public static void Initialize(IEventAggregator eventAggregator, Store store)
        {
            if (!bInitialized)
            {
                lock (Bindings)
                {
                    if (!bInitialized)
                    {
                        eventAggregator.Should().NotBeNull();

                        EventAggregator = eventAggregator;
                        Store = store;

                        var bindingsToLoad = LoadBindingsFromStore(store);
                        foreach (var bindingSet in bindingsToLoad)
                        {
                            foreach (var binding in bindingSet.Value)
                            {
                                AddBinding(bindingSet.Key, binding);
                            }
                        }

                        bInitialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// Monitors the specified window for key presses.
        /// </summary>
        /// <param name="window"></param>
        public static void Monitor(Window window)
        {
            window.KeyDown += Window_KeyDown;
        }

        /// <summary>
        /// Unmonitors the specified window for key presses.
        /// </summary>
        /// <param name="window"></param>
        public static void Unmonitor(Window window)
        {
            window.KeyDown -= Window_KeyDown;
        }

        private static void Window_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = OnKeyBindingPressed(sender, new KeyBindingEventArgs(e.SystemKey, Keyboard.Modifiers));
        }
        
        private static bool OnKeyBindingPressed(object sender, KeyBindingEventArgs e)
        {
            var bindingEvent = Bindings.FindKey(e.KeyBinding);
            switch (bindingEvent)
            {
                case EKeybindableEvent.ToggleFullscreen:
                    EventAggregator.GetEvent<ToggleFullScreenKeyBindingEvent>().Publish(new ToggleFullScreenKeyBindingEvent(sender as Window));
                    return true;
                case EKeybindableEvent.IncreaseVolume:
                    EventAggregator.GetEvent<ToggleFullScreenKeyBindingEvent>().Publish(new ToggleFullScreenKeyBindingEvent(sender as Window));
                    return true;
                case EKeybindableEvent.DecreaseVolume:
                    EventAggregator.GetEvent<ToggleFullScreenKeyBindingEvent>().Publish(new ToggleFullScreenKeyBindingEvent(sender as Window));
                    return true;
                case EKeybindableEvent.ToggleVolume:
                    EventAggregator.GetEvent<ToggleFullScreenKeyBindingEvent>().Publish(new ToggleFullScreenKeyBindingEvent(sender as Window));
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Loads the key bindings from the store.
        /// </summary>
        /// <param name="store"></param>
        /// <remarks>This is called autmatically by the constructor.</remarks>
        private static KeyBindingCollection LoadBindingsFromStore(Store store)
        {
            var result = new KeyBindingCollection(KeyBindingCollection.DefaultBindings);

            EventAggregator.GetEvent<KeyBindingsLoadingEvent>().Publish(new KeyBindingsLoadingEvent());

            var logOutputElements = result.LoadBindingsFromStore(store);

            EventAggregator.GetEvent<KeyBindingsLoadedEvent>().Publish(new KeyBindingsLoadedEvent
            {
                Data = logOutputElements
            });

            return result;
        }

        /// <summary>
        /// Returns whether the specified key binding is already registered.
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        internal static bool IsBindingRegistered(MergedInputGesture binding)
        {
            return Bindings.IsBindingRegistered(binding);
        }

        /// <summary>
        /// Returns whether 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        /// <returns></returns>
        public static bool IsBindingRegistered(Key key, ModifierKeys modifiers)
        {
            return IsBindingRegistered(new MergedInputGesture(key, modifiers));
        }

        /// <summary>
        /// Returns the key binding associated with the bindable event.
        /// </summary>
        /// <param name="bindableEvent"></param>
        /// <returns></returns>
        internal static IReadOnlyCollection<MergedInputGesture> GetKeyBindings(EKeybindableEvent bindableEvent)
        {
            return Bindings.GetKeyBindings(bindableEvent);
        }


        /// <summary>
        /// Adds a custom binding and saves it to the store.
        /// </summary>
        /// <param name="bindableEvent"></param>
        /// <param name="binding"></param>
        /// <returns>Whether the binding was successfully added.</returns>
        internal static bool AddBinding(EKeybindableEvent bindableEvent, MergedInputGesture binding)
        {
            lock (Bindings)
            {
                try
                {
                    //TODO: Will this cause problems on load? Should we add an AddOrReplace method?
                    if (Bindings.IsBindingRegistered(binding))
                    {
                        throw new InvalidOperationException($"The binding [{binding.Key} , {binding.Modifiers}] is already registered.");
                    }

                    Bindings.Add(bindableEvent, binding);

                    if (Store != null)
                    {
                        if (Store.Interface.Bindings.TryGetValue(bindableEvent, out List<MergedInputGesture> value))
                        {
                            value.AddUnique(binding);
                        }
                        else
                        {
                            Store.Interface.Bindings[bindableEvent] = new List<MergedInputGesture> { binding };
                        }
                    }

                    return true;
                }
                catch (Exception)
                {
                    // At some point we want to do something with this exception, but as far as
                    // we are concerned any error is failure to bind.
                    // In the future, perhaps bind e to an out parameter?
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes a binding and removes it from the store.
        /// </summary>
        /// <param name="binding"></param>
        internal static bool RemoveBinding(MergedInputGesture binding)
        {
            var defaultBindings = KeyBindingCollection.DefaultBindings.Values.SelectMany(b => b);

            bool bCanOverride = true;
            foreach (var defaultBinding in defaultBindings)
            {
                if (defaultBinding.Equals(binding))
                {
                    bCanOverride = defaultBinding.bCanOverride;
                    break;
                }
            }

            if (!bCanOverride)
            {
                return false;
            }

            lock (Bindings)
            {
                try
                {
                    var bindings = Bindings.Values.SelectMany(b => b).Where(b => b.Equals(binding)).ToArray();
                    if (bindings.Length == 0)
                    {
                        // No such binding.
                        return true;
                    }

                    foreach (var b in bindings)
                    {
                        Bindings.Remove(b);

                        if (Store != null)
                        {
                            var storeBindings = Store.Interface.Bindings.Values;
                            foreach (var sb in storeBindings)
                            {
                                sb.Remove(b);
                            }
                        }
                    }

                    return true;
                }
                catch (Exception)
                {
                    // At some point we want to do something with this exception, but as far as
                    // we are concerned any error is failure to bind.
                    // In the future, perhaps bind e to an out parameter?
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes all bindings associated with an event and removes them from the store.
        /// </summary>
        /// <param name="bindableEvent"></param>
        public static bool ClearBindings(EKeybindableEvent bindableEvent)
        {
            bool bResult = true;

            var bindings = Bindings.GetKeyBindings(bindableEvent);
            foreach (var binding in bindings)
            {
                bResult &= RemoveBinding(binding);
            }

            return bResult;
        }

        /// <summary>
        /// Sets the bindings specified by <paramref name="bindableEvent"/> to only
        /// contain a single <paramref name="binding"/>.
        /// </summary>
        /// <param name="bindableEvent"></param>
        /// <param name="binding"></param>
        internal static bool SetBinding(EKeybindableEvent bindableEvent, MergedInputGesture binding)
        {
            //TODO: Setting the binding exclusively does not remove the default binding on next load.
            return ClearBindings(bindableEvent) && AddBinding(bindableEvent, binding);
        }

        /// <summary>
        /// Sets the bindings specified by <paramref name="bindableEvent"/> to only
        /// contain a single binding with the <paramref name="key"/> and <paramref name="modifiers"/>.
        /// </summary>
        /// <param name="bindableEvent"></param>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        public static bool SetBinding(EKeybindableEvent bindableEvent, Key key, ModifierKeys modifiers)
        {
            return SetBinding(bindableEvent, new MergedInputGesture(key, modifiers));
        }

        /// <summary>
        /// Sets the bindings for the specified event to the specified bindings.
        /// </summary>
        /// <param name="bindableEvent"></param>
        /// <param name="bindings"></param>
        internal static bool SetBindings(EKeybindableEvent bindableEvent, HashSet<MergedInputGesture> bindings)
        {
            bool bResult = ClearBindings(bindableEvent);
            foreach (var binding in bindings)
            {
                bResult &= AddBinding(bindableEvent, binding);
            }

            return bResult;
        }

        /// <exception cref="InvalidOperationException"></exception>
        internal static bool UpdateBinding(EKeybindableEvent bindableEvent, MergedInputGesture oldBinding, MergedInputGesture newBinding)
        {
            oldBinding.Should().NotBeNull();
            newBinding = newBinding ?? new MergedInputGesture(Key.None);

            var existingBindings = GetKeyBindings(bindableEvent);
            if (!existingBindings.Contains(oldBinding))
            {
                throw new InvalidOperationException($"{bindableEvent} does not have the specified old binding. Cannot update.");
            }

            return RemoveBinding(oldBinding) && AddBinding(bindableEvent, newBinding);
        }
    }
}
