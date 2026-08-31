using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ArcaeaCoverMaker.Config;
using Microsoft.Win32;

namespace ArcaeaCoverMaker
{
    /// <summary>
    /// InfoEditor.xaml 的交互逻辑
    /// </summary>
    public partial class InfoEditor : Window
    {
        public InfoEditor()
        {
            InitializeComponent();
            Closing += InfoEditor_Closing;
        }

        private const string FONT_FILTER = "Font Files|*.ttf;*.otf|All Files|*.*";

        private CoverMakerConfig Config => MainWindow.Config;

        public void InfoEditor_Closing(object? sender, CancelEventArgs e)
        {
            //e.Cancel = true;
        }

        public void Init()
        {
            DataContext = Config;
        }

        private void BrowseTitleFont(object sender, RoutedEventArgs e)
        {
            string filePath = SelectFontFile();
            if (!string.IsNullOrEmpty(filePath))
            {
                Config.TitleFontFilePath = filePath;
                UpdateConfigAndReload();
            }
        }

        private void BrowseArtistFont(object sender, RoutedEventArgs e)
        {
            string filePath = SelectFontFile();
            if (!string.IsNullOrEmpty(filePath))
            {
                Config.ArtistFontFilePath = filePath;
                UpdateConfigAndReload();
            }
        }

        private void BrowseDifficultyFont(object sender, RoutedEventArgs e)
        {
            string filePath = SelectFontFile();
            if (!string.IsNullOrEmpty(filePath))
            {
                Config.DifficultyFontFilePath = filePath;
                UpdateConfigAndReload();
            }
        }

        private string SelectFontFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = FONT_FILTER,
                Title = "Select Font File"
            };

            bool? dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == true)
            {
                return openFileDialog.FileName;
            }

            return string.Empty;
        }

        public void UpdateConfigAndReload()
        {
            MainWindow.Instance.SaveAndReload();
            DataContext = Config;
        }

        private void UpdateConfigButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateConfigAndReload();
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {

        }
    }
}
