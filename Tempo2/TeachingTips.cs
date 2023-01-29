using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Tempo
{

    /// <summary>
    /// Helper class that manages which teaching tips to show
    /// </summary>
    static internal class TeachingTips
    {
        static TeachingTipIds _shownTips = TeachingTipIds.None;
        static bool _aTipHasBeenShown = false;

        const string _teachingTipsShownKey = "TeachingTipsShown";

        /// <summary>
        /// Save which tips have been shown
        /// </summary>
        static void SaveTipShown(TeachingTipIds id)
        {
            _shownTips |= id;

            ApplicationDataContainer settings = ApplicationData.Current.RoamingSettings;
            settings.Values[_teachingTipsShownKey] = (int)_shownTips;
        }

        static bool _settingsLoaded = false;
        /// <summary>
        /// Load which settings have been shown
        /// </summary>
        static void EnsureShownTipIdsLoaded()
        {
            if (_settingsLoaded)
            {
                return;
            }
            _settingsLoaded = true;

            ApplicationDataContainer settings = ApplicationData.Current.RoamingSettings;

            // For testing
            //settings.Values.Remove(_teachingTipsShownKey);

            if (settings.Values.TryGetValue(_teachingTipsShownKey, out var shownTips))
            {
                _shownTips = (TeachingTipIds)shownTips;
            }
            else
            {
                // Don't show any tips on the first run of the app
                _aTipHasBeenShown = true;
                SaveTipShown(_shownTips);
            }
        }

        /// <summary>
        /// Try showing a teaching tip. If it's not shown, the tip won't be created.
        /// Returns false if we shouldn't continue to even try anyore (we've already shown enough)
        /// Target is passed as a parameter so we can validate it's not collapsed first
        /// The optional `force` parameter forces it to show even if it's been shown before.
        /// </summary>
        internal static bool TryShow(
            TeachingTipIds id, 
            Panel parent, 
            FrameworkElement target, 
            Func<TeachingTip> action,
            bool force = false)
        {
            EnsureShownTipIdsLoaded();

            if (!force)
            {
                // Only show one tip per run of the app
                if (_aTipHasBeenShown)
                {
                    return false;
                }

                // If we've already shown this tip, don't show it again, but
                // return true indicating that the caller should try other tips
                if (_shownTips.HasFlag(id))
                {
                    return true;
                }


                // If the target is collapsed, try moving on to the next tip
                if (target.ActualWidth == 0)
                {
                    return true;
                }
            }

            // Show a tip
            DoShow(id, parent, target, action);
            _aTipHasBeenShown = true;

            // Return false to say don't try to show any more tips
            return false;
        }

        /// <summary>
        /// Show a teaching tip
        /// </summary>
        static async void DoShow(TeachingTipIds id, Panel parent, FrameworkElement target, Func<TeachingTip> action)
        {
            var tip = action();
            tip.Target = target;

            // The tip has to be in the tree or it can't find the XamlRoot
            parent.Children.Add(tip);
            tip.Closed += (_, __) => parent.Children.Remove(tip);

            // Set up the tip so that if the target is unloaded, such as when we navigate away the page, we close the tip
            var unloader = new TargetUnloader(tip);
            target.Unloaded += unloader.Target_Unloaded;

            // bugbug: without this delay, the tip opens, but won't close
            await Task.Delay(100);

            // Show the tip
            tip.IsOpen = true;

            // Remember that we showed this so we don't show it again
            SaveTipShown(id);
        }

        /// <summary>
        /// Helper to close a teaching tip when the target is unloaded
        /// </summary>
        class TargetUnloader
        {
            TeachingTip _tip;
            public TargetUnloader(TeachingTip tip) 
            {
                _tip = tip;
            }
            public void Target_Unloaded(object sender, RoutedEventArgs e)
            {
                if(_tip == null)
                {
                    // Shouldn't happen
                    return;
                }

                _tip.IsOpen = false;
                _tip = null; // Shouldn't be necessary but play it safe

                var target = sender as FrameworkElement;
                target.Unloaded -= Target_Unloaded;
            }
        }


    }  



    [Flags]
    internal enum TeachingTipIds
    {
        None = 0,
        CustomFiles = 1,
        Filters = 2,
        ApiScopeSwitcher = 4,
        CommandPrompt = 8,
        SearchSyntax = 16,
        CppProjection = 32
    }
}
