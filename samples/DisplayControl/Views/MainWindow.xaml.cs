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
                    Close();
                };

                PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "ClientSize")
                    {
                        m_viewModel.Size = ClientSize;
                    }
                };

                m_viewModel.Size = ClientSize;
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
