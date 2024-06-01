using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DisplayControl.ViewModels;
using ReactiveUI;

namespace DisplayControl.Views
{
    public class MainWindow : Window, IViewFor<MainWindowViewModel>
    {
        private MainWindowViewModel m_viewModel;
        private ListBox m_listBox;
        private bool m_closingConfirmed = false;

        public MainWindow()
        {
            InitializeComponent();
            m_listBox = this.FindControl<ListBox>("ListElements");
        }

        public MainWindowViewModel ViewModel
        {
            get
            {
                return m_viewModel;
            }

            set
            {
                m_viewModel = value;

                // Subscribe to Cancel, and close the Window when it happens
                m_viewModel.DoClose += () =>
                {
                    m_closingConfirmed = true;
                    Close();
                };

                ClientSize = new Size(900, 400);
                PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "ClientSize")
                    {
                        OnSizeChanged();
                    }
                };

                m_viewModel.SetSize(ClientSize, 100);
            }
        }

        object IViewFor.ViewModel
        {
            get
            {
                return ViewModel;
            }
            set
            {
                ViewModel = (MainWindowViewModel)value;
            }
        }

        private void OnSizeChanged()
        {
            var statusBar = this.FindControl<TextBlock>("StatusBar");
            // ClientSize seems to be the whole window, and Bounds is not set up properly initially.
            // So we just need to guess about the size of the title bar and the menu
            double height = statusBar == null ? 20 : statusBar.Height;
            m_viewModel.SetSize(ClientSize, (int)Math.Ceiling(height + (2 * 50)));
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!m_closingConfirmed)
            {
                e.Cancel = true;
                return;
            }
            base.OnClosing(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
