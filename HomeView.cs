﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EchoBranch
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}