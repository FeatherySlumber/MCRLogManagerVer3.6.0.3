using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace MCRLogManager
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorWindow : Window
    {
        readonly Graphinfo graphinfo = new Graphinfo();

        public ColorWindow(string str)
        {
            InitializeComponent();
            textblock1.Text = str;
            graphinfo = Global.Graph.First(t => t.Name == str);
            this.Yscale.Text = graphinfo.Y().ToString();
            Brush bcolor = graphinfo.Brush;
            this.beforeColor.Fill = bcolor;
            this.rSlid.Value = ((Color)bcolor.GetValue(SolidColorBrush.ColorProperty)).R;
            this.gSlid.Value = ((Color)bcolor.GetValue(SolidColorBrush.ColorProperty)).G;
            this.bSlid.Value = ((Color)bcolor.GetValue(SolidColorBrush.ColorProperty)).B;
        }

        private void SlideChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Color color = Color.FromRgb((byte)rSlid.Value, (byte)gSlid.Value, (byte)bSlid.Value);
            this.afterColor.Fill = new SolidColorBrush(color);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            graphinfo.Brush = this.afterColor.Fill;
            graphinfo.PathScaleSetY(double.Parse(this.Yscale.Text));
            this.Close();
        }

        private void TextPreviewTextInpt(object sender, TextCompositionEventArgs e)
        {
            //入力値が数値でない場合処理済みにする
            var regex = new Regex("[^0-9.]+");
            var text = e.Text;
            var result = regex.IsMatch(text);
            e.Handled = result;
        }

        private void TextPreviewExexuted(object sender, ExecutedRoutedEventArgs e)
        {
            //入力が貼り付けなら処理済みにする
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

    }

}
