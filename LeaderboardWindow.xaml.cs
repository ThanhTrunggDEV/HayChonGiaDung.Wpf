using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HayChonGiaDung.Wpf
{
    /// <summary>
    /// Interaction logic for LeaderboardWindow.xaml
    /// </summary>
    public partial class LeaderboardWindow : Window
    {
        private class Row
        {
            public int Rank { get; set; }
            public string Name { get; set; } = "";
            public int Prize { get; set; }
        }

        public LeaderboardWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var top = LeaderboardService.Top(200);
            var rows = new List<Row>();
            for (int i = 0; i < top.Count; i++)
                rows.Add(new Row { Rank = i + 1, Name = top[i].Name, Prize = top[i].Prize });

            Grid.ItemsSource = rows;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
