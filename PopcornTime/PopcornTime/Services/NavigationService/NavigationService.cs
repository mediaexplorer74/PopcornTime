﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Audiotica.Windows.Services.NavigationService;
using PopcornTime.Extensions;
using PopcornTime.Services.Interfaces;
using PopcornTime.Utilities.Interfaces;
using PopcornTime.Views;
using NavigationEventArgs = Windows.UI.Xaml.Navigation.NavigationEventArgs;

namespace PopcornTime.Services.NavigationService
{
    internal class NavigationService : INavigationService
    {
        private const string EmptyNavigation = "1,0";
        private const string SettingsPrefix = "NavService_";
        private const string SettingsNavigationState = SettingsPrefix + "NavigationState";
        private const string SettingsSessions = SettingsPrefix + "NavigationSessions";
        private readonly SystemNavigationManager _currentView;
        private readonly NavigationFacade _frame;
        private readonly IInsightsService _insightsService;
        private readonly ISettingsUtility _settingsUtility;
        private bool _keepOnBackstack = true;

        private Dictionary<string, Dictionary<string, object>> _sessions =
            new Dictionary<string, Dictionary<string, object>>();

        public NavigationService(Frame frame, ISettingsUtility settingsUtility, IInsightsService insightsService)
        {
            _settingsUtility = settingsUtility;
            _insightsService = insightsService;
            _frame = new NavigationFacade(frame);
            _frame.Navigating += (s, e) => NavigatedFrom(false);

            _currentView = SystemNavigationManager.GetForCurrentView();
        }

        public Type DefaultPage => typeof (MoviesPage);
        public bool CanGoBack => _frame.CanGoBack;
        public bool CanGoForward => _frame.CanGoForward;
        public Type CurrentPageType => _frame.CurrentPageType;
        public string CurrentPageParam => _frame.CurrentPageParam;

        public void NavigatedTo(NavigationMode mode, string parameter)
        {
            _currentView.AppViewBackButtonVisibility = _frame.BackStackDepth > 0
                ? AppViewBackButtonVisibility.Visible
                : AppViewBackButtonVisibility.Collapsed;

            if (!_keepOnBackstack)
            {
                if (mode == NavigationMode.New)
                    _frame.BackStack.RemoveAt(_frame.BackStackDepth - 1);
                _keepOnBackstack = true;
            }

            _frame.CurrentPageParam = parameter;
            _frame.CurrentPageType = _frame.Content.GetType();

            var page = _frame.Content as FrameworkElement;
            var dataContext = page?.DataContext as INavigatable;

            if (dataContext != null)
            {
                _keepOnBackstack = dataContext.KeepOnBackstack;
                var deserializedParameter = parameter.TryDeserializeJsonWithTypeInfo();
                _insightsService.TrackPageView(CurrentPageType.Name,
                    dataContext.SimplifiedParameter(deserializedParameter));

                dataContext.PageKey = CurrentPageType + parameter;

                if (mode == NavigationMode.New)
                {
                    if (_sessions.ContainsKey(dataContext.PageKey))
                        _sessions[dataContext.PageKey] = new Dictionary<string, object>();
                    else
                        _sessions.Add(dataContext.PageKey, new Dictionary<string, object>());
                }
                dataContext.OnNavigatedTo(deserializedParameter, mode, _sessions[dataContext.PageKey]);
            }
        }

        public bool Navigate(Type page, object parameter = null)
        {
            parameter = parameter.SerializeToJsonWithTypeInfo();
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            if (page == CurrentPageType
                && (string) parameter == CurrentPageParam)
                return false;
            return _frame.Navigate(page, parameter);
        }

        public void RestoreSavedNavigation()
        {
            var state = _settingsUtility.Read(SettingsNavigationState, string.Empty);
            _sessions = _settingsUtility.Read<Dictionary<string, Dictionary<string, object>>>(SettingsSessions, null) ??
                        new Dictionary<string, Dictionary<string, object>>();

            if (string.IsNullOrEmpty(state))
                Navigate(DefaultPage);
            else
            {
                _frame.SetNavigationState(state);
                _settingsUtility.Remove(SettingsNavigationState);
                _settingsUtility.Remove(SettingsSessions);
            }
        }

        public void GoBack()
        {
            if (_frame.CanGoBack) _frame.GoBack();
        }

        public void GoForward()
        {
            _frame.GoForward();
        }

        public void ClearHistory()
        {
            foreach (var keyValuePair in _sessions.Where(p => !p.Key.EndsWith("0")).ToList())
            {
                _sessions.Remove(keyValuePair.Key);
            }
            _frame.SetNavigationState(EmptyNavigation);
        }

        public void Suspending()
        {
            NavigatedFrom(true);

            var state = _frame.GetNavigationState();
            _settingsUtility.Write(SettingsNavigationState, state);
            _settingsUtility.Write(SettingsSessions, _sessions);
        }

        public void Show(SettingsFlyout flyout, object parameter = null)
        {
            if (flyout == null)
                throw new ArgumentNullException(nameof(flyout));
            var dataContext = flyout.DataContext as INavigatable;
            dataContext?.OnNavigatedTo(parameter, NavigationMode.New, null);
            flyout.Show();
        }

        private void NavigatedFrom(bool suspending)
        {
            var page = _frame.Content as FrameworkElement;
            var dataContext = page?.DataContext as INavigatable;
            if (dataContext == null) return;

            Dictionary<string, object> state;
            if (_sessions.TryGetValue(dataContext.PageKey, out state))
                dataContext.OnSaveState(suspending, state);
            dataContext.OnNavigatedFrom();
        }
    }

    public class NavigatablePage : Page
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!DesignMode.DesignModeEnabled)
                App.Current.NavigationService.NavigatedTo(e.NavigationMode, e.Parameter?.ToString());
        }
    }
}