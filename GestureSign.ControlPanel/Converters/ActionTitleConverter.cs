using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using MahApps.Metro.Controls;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(IAction), typeof(string))]
    public class ActionTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var action = (IAction)value;
            if (action == null) return null;

            var actionName = string.IsNullOrWhiteSpace(action.Name) ? LocalizationProvider.Instance.GetTextValue("Action.NewAction") : action.Name;

            if (action.ContinuousGesture != null)
            {
                actionName += "\n" + LocalizationProvider.Instance.GetTextValue("ActionDialog.Continuous") + ": " +
                    string.Format(LocalizationProvider.Instance.GetTextValue("Action.Fingers"), action.ContinuousGesture.ContactCount) + " " + LocalizationProvider.Instance.GetTextValue("Action." + action.ContinuousGesture.Gesture);
            }
            if (action.Hotkey != null)
            {
                actionName += "\n" + LocalizationProvider.Instance.GetTextValue("ActionDialog.KeyboardHotKey") + ": " +
                    new HotKey(KeyInterop.KeyFromVirtualKey(action.Hotkey.KeyCode), (ModifierKeys)action.Hotkey.ModifierKeys).ToString() + "  ";
            }
            if (action.MouseHotkey != ManagedWinapi.Hooks.MouseActions.None && AppConfig.DrawingButton != ManagedWinapi.Hooks.MouseActions.None)
            {
                if (action.Hotkey == null)
                {
                    actionName += "\n";
                }
                actionName += LocalizationProvider.Instance.GetTextValue("ActionDialog.MouseHotKey") + ": " +
                    ViewModel.MouseActionDescription.DescriptionDict[AppConfig.DrawingButton] + " + " + ViewModel.MouseActionDescription.DescriptionDict[action.MouseHotkey];
            }

            if (!string.IsNullOrWhiteSpace(action.Condition))
                actionName += " [Cond]";

            return actionName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}