﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Application = System.Windows.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace novideo_srgb
{
    public partial class MainWindow
    {
        private readonly MainViewModel _viewModel;

        private ContextMenu _contextMenu;

        private bool _closeFromContextMenu;

        ScreenPowerMgmt _screenPowerMgmt = new ScreenPowerMgmt();

        public MainWindow()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("Already running!");
                Close();
                return;
            }

            InitializeComponent();
            _viewModel = (MainViewModel)DataContext;
            SystemEvents.DisplaySettingsChanged += _viewModel.OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged += _viewModel.OnPowerModeChanged;

            _screenPowerMgmt.Register(new WindowInteropHelper(this).EnsureHandle());
            _screenPowerMgmt.ScreenPower += _viewModel.OnScreenPower;
            var args = Environment.GetCommandLineArgs().ToList();
            args.RemoveAt(0);

            if (args.Contains("-minimize"))
            {
                Minize();
            }

            InitializeTrayIcon();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }

            base.OnStateChanged(e);
        }

        private void Minize()
        {
            WindowState = WindowState.Minimized;
            Hide();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs o)
        {
            var window = new AboutWindow
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Windows.Cast<Window>().Any(x => x is AdvancedWindow)) return;
            var monitor = ((FrameworkElement)sender).DataContext as MonitorData;
            var window = new AdvancedWindow(monitor)
            {
                Owner = this
            };

            void CloseWindow(object o, EventArgs e2) => window.Close();

            SystemEvents.DisplaySettingsChanged += CloseWindow;
            if (window.ShowDialog() == false) return;
            SystemEvents.DisplaySettingsChanged -= CloseWindow;

            if (window.ChangedCalibration)
            {
                _viewModel.SaveConfig();
                monitor?.ReapplyClamp();
            }

            if (window.ChangedDither)
            {
                monitor?.ApplyDither(window.DitherState.SelectedIndex, Math.Max(window.DitherBits.SelectedIndex, 0),
                    Math.Max(window.DitherMode.SelectedIndex, 0));
            }
        }

        private void ReapplyButton_Click(object sender, RoutedEventArgs e)
        {
            ReapplyMonitorSettings();
        }

        private void InitializeTrayIcon()
        {
            var notifyIcon = new NotifyIcon
            {
                Text = "Novideo sRGB",
                Icon = Properties.Resources.icon,
                Visible = true
            };

            notifyIcon.MouseDoubleClick +=
                delegate
                {
                    Show();
                    WindowState = WindowState.Normal;
                };

            _contextMenu = new ContextMenu();

            _contextMenu.Popup += delegate { UpdateContextMenu(); };

            notifyIcon.ContextMenu = _contextMenu;

            Closed += delegate { notifyIcon.Dispose(); };
        }

        private void UpdateContextMenu()
        {
            _contextMenu.MenuItems.Clear();

            foreach (var monitor in _viewModel.Monitors)
            {
                var item = new MenuItem();
                _contextMenu.MenuItems.Add(item);
                item.Text = monitor.Name;
                item.Checked = monitor.Clamped;
                item.Enabled = monitor.CanClamp;
                item.Click += (sender, args) => monitor.Clamped = !monitor.Clamped;
            }

            _contextMenu.MenuItems.Add("-");

            var reapplyItem = new MenuItem();
            _contextMenu.MenuItems.Add(reapplyItem);
            reapplyItem.Text = "Reapply";
            reapplyItem.Click += delegate { ReapplyMonitorSettings(); };

            var exitItem = new MenuItem();
            _contextMenu.MenuItems.Add(exitItem);
            exitItem.Text = "Exit";
            exitItem.Click += delegate { _closeFromContextMenu = true;  Close(); };
        }

        private void ReapplyMonitorSettings()
        {
            foreach (var monitor in _viewModel.Monitors)
            {
                monitor.ReapplyClamp();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdateMonitors();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !_closeFromContextMenu;
            if (e.Cancel) {
                Minize();
            }
        }
    }
}