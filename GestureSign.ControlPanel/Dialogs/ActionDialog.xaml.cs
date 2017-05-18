﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.ViewModel;
using MahApps.Metro.Controls;
using ManagedWinapi;
using ManagedWinapi.Hooks;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for ActionDialog.xaml
    /// </summary>
    public partial class ActionDialog : MetroWindow
    {
        #region Private Instance Fields

        private readonly IAction _sourceAction;
        private IApplication _sourceApplication;

        #endregion

        #region Public Instance Fields

        public IAction NewAction { get; private set; } = new GestureSign.Common.Applications.Action() { Commands = new List<GestureSign.Common.Applications.ICommand>() };

        #endregion

        #region Dependency Properties

        public IGesture CurrentGesture
        {
            get { return (IGesture)GetValue(CurrentGestureProperty); }
            set { SetValue(CurrentGestureProperty, value); }
        }

        public static readonly DependencyProperty CurrentGestureProperty =
            DependencyProperty.Register(nameof(CurrentGesture), typeof(IGesture), typeof(ActionDialog), new FrameworkPropertyMetadata(new Gesture()));

        #endregion

        #region Constructors

        protected ActionDialog()
        {
            InitializeComponent();
        }

        public ActionDialog(IAction sourceAction, IApplication sourceApplication) : this()
        {
            _sourceAction = sourceAction;
            _sourceApplication = sourceApplication;
        }

        #endregion

        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (MouseActionDescription.DescriptionDict.ContainsKey(AppConfig.DrawingButton))
                DrawingButtonTextBlock.Text = MouseActionDescription.DescriptionDict[AppConfig.DrawingButton] + "  +  ";
            if (AppConfig.DrawingButton == MouseActions.None)
                MouseHotKeyTextBlock.Text += LocalizationProvider.Instance.GetTextValue("ActionDialog.MouseGestureNotEnabled");

            SetValue(_sourceAction);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveAction())
            {
                DialogResult = true;
                Close();
            }
        }

        private void ConditionTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            EditConditionDialog editConditionDialog = new EditConditionDialog(ConditionTextBox.Text);
            if (editConditionDialog.ShowDialog().Value)
            {
                ConditionTextBox.Text = editConditionDialog.ConditionTextBox.Text;
            }
        }

        private void GestureButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            GestureDefinition gestureDialog = new GestureDefinition();
            var result = gestureDialog.ShowDialog();

            if (result != null && result.Value)
            {
                var existingAction = _sourceApplication.Actions.Find(a => a.GestureName == GestureManager.Instance.GestureName);
                if (existingAction != null)
                {
                    SetValue(existingAction);
                }
                else CurrentGesture = GestureManager.Instance.GetNewestGestureSample(GestureManager.Instance.GestureName);
            }
        }

        #endregion

        #region Private Instance Fields

        private void SetValue(IAction action)
        {
            if (action != null)
            {
                ActionNameTextBox.Text = action.Name;
                ConditionTextBox.Text = action.Condition;
                MouseActionComboBox.SelectedValue = action.MouseHotkey;
                ActivateWindowCheckBox.IsChecked = action.ActivateWindow;

                var gesture = GestureManager.Instance.GetNewestGestureSample(action.GestureName);
                if (gesture != null)
                    CurrentGesture = gesture;

                var hotkey = action.Hotkey;
                if (hotkey != null)
                    HotKeyTextBox.HotKey = new HotKey(KeyInterop.KeyFromVirtualKey(hotkey.KeyCode), (ModifierKeys)hotkey.ModifierKeys);
            }
        }

        private bool ShowErrorMessage(string title, string message)
        {
            MessageFlyoutText.Text = message;
            MessageFlyout.Header = title;
            MessageFlyout.IsOpen = true;
            return false;
        }

        private bool SaveAction()
        {
            try
            {
                var regex = new Regex("finger_[0-9]+_start_[XY]?|finger_[0-9]+_end_[XY]?|finger_[0-9]+_ID");
                var replaced = regex.Replace(ConditionTextBox.Text, "10");

                DataTable dataTable = new DataTable();
                dataTable.Compute(replaced, null);
            }
            catch (Exception exception)
            {
                return ShowErrorMessage(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ConditionError"), exception.Message);
            }
            // Move command to existing action
            var sameAction = _sourceApplication.Actions.Find(a => a.GestureName == CurrentGesture?.Name);
            if (sameAction != null)
                NewAction = sameAction;
            else
                _sourceApplication.AddAction(NewAction);

            // Store new values
            NewAction.Condition = string.IsNullOrWhiteSpace(ConditionTextBox.Text) ? null : ConditionTextBox.Text;
            NewAction.ActivateWindow = ActivateWindowCheckBox.IsChecked;
            NewAction.GestureName = CurrentGesture?.Name ?? string.Empty;
            NewAction.Name = ActionNameTextBox.Text.Trim();
            NewAction.MouseHotkey = (MouseActions?)MouseActionComboBox.SelectedValue ?? MouseActions.None;
            NewAction.Hotkey = HotKeyTextBox.HotKey != null
                ? new Hotkey()
                {
                    KeyCode = KeyInterop.VirtualKeyFromKey(HotKeyTextBox.HotKey.Key),
                    ModifierKeys = (int)HotKeyTextBox.HotKey.ModifierKeys
                }
                : null;
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();

            return true;
        }

        #endregion
    }
}