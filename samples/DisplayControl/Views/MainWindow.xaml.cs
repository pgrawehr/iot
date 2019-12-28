using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            this.AttachDevTools();
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

                m_viewModel.PropertyChanged += AnyPropertyChanged;
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

        private void AnyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.ListBoxElements))
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    m_listBox.BeginInit();
                    var elems = new List<SensorValueSource>(ViewModel.ListBoxElements);
                    ViewModel.ListBoxElements.Clear();
                    foreach(var elem in elems)
                    {
                        ViewModel.ListBoxElements.Add(elem);
                    }
                    m_listBox.EndInit();
                });
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
