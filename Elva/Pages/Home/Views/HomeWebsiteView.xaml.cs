﻿using Elva.Common.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Elva.Pages.Home.Views
{
    /// <summary>
    /// Interaktionslogik für HomeItemViewer.xaml
    /// </summary>
    public partial class HomeWebsiteView : UserControl
    {
        private static int _columns = 1;
        private static int _gridWidth = 1;

        public HomeWebsiteView()
        {
            InitializeComponent();
        }

        private void UniformGridPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_gridWidth != (int)e.NewSize.Width)
            {
                _gridWidth = (int)e.NewSize.Width;
                int colums = _gridWidth / 350;
                _columns = colums < 1 ? 1 : colums;
            }
            ((UniformGridPanel)sender).Columns = _columns;
        }
        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_gridWidth != (int)e.NewSize.Width)
            {
                _gridWidth = (int)e.NewSize.Width;
                int colums = _gridWidth / 325;
                _columns = colums < 1 ? 1 : colums;
            }
            ((UniformGrid)sender).Columns = _columns;
        }
    }
}
