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
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private bool current;

        public SettingsWindow()
        {
            InitializeComponent();
            SettingsService.Load();               // đọc file settings
            current = SoundManager.SoundOn;
            RbOn.IsChecked = current;
            RbOff.IsChecked = !current;
        }

        private void RbOn_Checked(object sender, RoutedEventArgs e) => current = true;
        private void RbOff_Checked(object sender, RoutedEventArgs e) => current = false;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.Save(current);        // lưu + áp dụng
            DialogResult = true;
            Close();
        }
    }
}
