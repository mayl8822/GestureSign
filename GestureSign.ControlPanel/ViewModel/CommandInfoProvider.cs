﻿using GestureSign.Common.Applications;
using GestureSign.ControlPanel.Common;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GestureSign.ControlPanel.ViewModel
{
    public class CommandInfoProvider
    {
        private IApplication _currentApp;
        private Task _addCommandTask;
        private ListBox _listBox;
        private volatile int _commandTaskCount;
        public ObservableCollection<CommandInfo> CommandInfos { get; } = new ObservableCollection<CommandInfo>();

        public CommandInfoProvider()
        {

        }

        public void RefreshCommandInfos(IApplication source, ListBox listBox)
        {
            _listBox = listBox;
            if (_currentApp != null)
            {
                _currentApp.CollectionChanged -= CommandCollectionChanged;
            }
            _currentApp = source;
            source.CollectionChanged -= ActionCollectionChanged;
            source.CollectionChanged += ActionCollectionChanged;

            Action<object> refreshAction = (o) =>
            {
                Application.Current.Dispatcher.Invoke(ClearCommandInfo, DispatcherPriority.Loaded);
                try
                {
                    foreach (var currentAction in source.Actions.ToList())
                    {
                        if (currentAction.Commands == null) continue;

                        currentAction.CollectionChanged -= CommandCollectionChanged;
                        currentAction.CollectionChanged += CommandCollectionChanged;

                        foreach (var info in currentAction.Commands.Select(c => CommandInfo.FromCommand(c, currentAction)))
                        {
                            if (_commandTaskCount > 1)
                                return;
                            try
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    AddCommandInfo(info);
                                }, DispatcherPriority.Input);
                            }
                            catch { }
                        }
                    }
                }
                finally
                {
                    _commandTaskCount--;
                }
            };
            _addCommandTask = _addCommandTask?.ContinueWith(refreshAction) ?? Task.Factory.StartNew(refreshAction, null);
            _commandTaskCount++;
        }

        private void ClearCommandInfo()
        {
            foreach (var actionGroup in CommandInfos.GroupBy(ci => ci.Action))
            {
                actionGroup.Key.CollectionChanged -= CommandCollectionChanged;
            }
            CommandInfos.Clear();
        }

        private void CommandCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var action = (IAction)sender;
            if (!_currentApp.Actions.Contains(action)) return;
            if (e.OldItems != null)
            {
                foreach (var oldCommand in e.OldItems)
                {
                    var oldInfo = CommandInfos.FirstOrDefault(ci => ci.Command == oldCommand);
                    CommandInfos.Remove(oldInfo);
                }
            }
            if (e.NewItems != null)
            {
                foreach (var newCommand in e.NewItems)
                {
                    var newInfo = CommandInfo.FromCommand((ICommand)newCommand, action);
                    AddCommandInfo(newInfo);
                    _listBox.SelectedItems.Add(newInfo);
                }
                _listBox.Dispatcher.InvokeAsync(() => _listBox.ScrollIntoView(_listBox.SelectedItem), DispatcherPriority.Background);
            }
        }

        private void ActionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var app = (IApplication)sender;
            if (app != _currentApp) return;

            if (e.OldItems != null)
            {
                foreach (IAction oldAction in e.OldItems)
                {
                    oldAction.CollectionChanged -= CommandCollectionChanged;
                    var oldInfos = CommandInfos.Where(ci => ci.Action == oldAction).ToList();
                    foreach (var info in oldInfos)
                    {
                        CommandInfos.Remove(info);
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (IAction newAction in e.NewItems)
                {
                    newAction.CollectionChanged += CommandCollectionChanged;
                    foreach (ICommand newCommand in newAction.Commands)
                    {
                        var newInfo = CommandInfo.FromCommand(newCommand, newAction);
                        AddCommandInfo(newInfo);
                        _listBox.SelectedItems.Add(newInfo);
                    }
                }
                _listBox.Dispatcher.InvokeAsync(() => _listBox.ScrollIntoView(_listBox.SelectedItem), DispatcherPriority.Background);
            }
        }

        private void AddCommandInfo(CommandInfo commandInfo)
        {
            string features;
            int patternCount;
            GestureItem gi = null;
            if (commandInfo.Action?.GestureName != null && GestureItemProvider.GestureMap.TryGetValue(commandInfo.Action.GestureName, out gi))
            {
                features = gi.Features;
                patternCount = gi.PatternCount;
            }
            else
            {
                features = string.Empty;
                patternCount = 0;
            }
            commandInfo.GestureFeatures = features;
            commandInfo.PatternCount = patternCount;
            CommandInfos.Add(commandInfo);
        }
    }
}
