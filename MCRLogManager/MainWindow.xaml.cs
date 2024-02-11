using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;

namespace MCRLogManager
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new ViewModel { Xscale = 1, Yscale = 1, IsGraphBack = false, GridBack = Brushes.Tomato };
            this.DataContext = _viewModel;

        }

        private readonly ViewModel _viewModel;


        private void OpenFile(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();

            dialog.Filter = "CSVファイル|*.csv;*.CSV|テキスト文書|*.txt;*.text|All File(*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                Read(dialog.FileName);
                AdvancedSet.Visibility = Visibility.Visible;
                ReWrite();
                GraphWriteLBox();
                this.FileName.Text = dialog.FileName;
            }
        }

        private void LBoxButton(object sender, SelectionChangedEventArgs e)
        {
            GraphWriteLBox();
        }

        private void GraphWriteLBox()
        {
            var list = this.LBox;
            TransformGroup tfg = new TransformGroup();
            tfg.Children.Add(new TranslateTransform(-_viewModel.GraphOrigin * double.Parse(sXbox.Text), 0));

            //表示してるpathだけ変更。
            foreach(Graphinfo gi in list.SelectedItems)
            {
                gi.Path.RenderTransform = tfg;
            }
        }

        private void Reset(object sender, RoutedEventArgs e)
        {
            this.LBox.SelectedIndex = -1;
        }

        private void CanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.CheckBox.IsChecked == false)
            {
                return;
            }
            Canvas c = sender as Canvas;
            if (c.Children.Count <= 2)
            {
                // XaxisとYlineだけなのでファイルが開かれてないかおかしい
                return;
            }
            Point pt = e.GetPosition(c);
            var dataGrid = this.DataGridView;
            int x = (int)(pt.X / _viewModel.Xscale + _viewModel.GraphOrigin);

            if (Global.Graph[0].Data.Count <= x)
            {
                dataGrid.SelectedIndex = Global.Graph[0].Data.Count - 1;
            }
            else
            {
                dataGrid.SelectedIndex = x;
            }

            try
            {
                dataGrid.ScrollIntoView(this.DataGridView.SelectedItem);
            }
            catch
            {
                MessageBox.Show("多分ファイルが開かれていません。\nファイルを開いてから操作してください。", "ErrerDayo", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Yline.X1 = pt.X;
        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            (sender as Canvas).Cursor = Cursors.Cross;
            Point pt = e.GetPosition(sender as Canvas);
            int x = (int)(pt.X / _viewModel.Xscale) + _viewModel.GraphOrigin;
            int y = (int)((int)(Canvas.GetTop(this.Xaxis) - pt.Y) / _viewModel.Yscale);

            this.CanvasPoint.Text = "X=" + String.Format("{0,4}", x) + ",Y=" + String.Format("{0,4}", y);
        }

        //表のサイズを整える
        private void ViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.CanvasBox.Height = (int)(this.MainScrollViewer.ActualHeight) * 0.6;
            this.LineGraphCanvas.Height = (int)(this.MainScrollViewer.ActualHeight * 0.6 - this.ControlButtonBox.ActualHeight);
            this.DataGridView.Height = (int)(this.MainScrollViewer.ActualHeight) * 0.4;
        }

        private void DataGridView_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            YSetData();
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

        private void ScaleChange(object sender, RoutedEventArgs e)
        {
            Erase_Graph_Background();
            if(_viewModel.IsGraphBack == true) { 
                _viewModel.IsGraphBack = false;
            }else if(_viewModel.IsGraphBack == null)
            {
                NewCreate_Graph_Background(true);
            }

            YSetData();
            double xscale = double.Parse(sXbox.Text);
            double yscale = double.Parse(sYbox.Text);
            for (int i = 0; i < Global.Graph.Count; i++) Global.Graph[i].PathScaleSet( xscale, yscale);
            GraphWriteLBox();
        }

        //表の設定に欲しかったけど表示させたくない部分をCancel
        private void DataGridView_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "RowIndex" || e.PropertyName == "IsAnswering")
            {
                e.Cancel = true;
            }
        }


        //ファイルを開いてデータを読み取る
        void Read(string filename)
        {
            try
            {
                LineGraphCanvas.Children.RemoveRange(2, LineGraphCanvas.Children.Count); //XaxisとYlineを以外を消す
                int data_cnt = 0;
                List<string> hn = new List<string>();
                List<List<int>> da = new List<List<int>>();
                using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (TextReader reader = new StreamReader(fileStream))
                    {
                        string line;
                        Global.Graph.Clear();

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (Regex.IsMatch(line, "[a-zA-Z]"))
                            {
                                line = line.Replace(".", "\u2024");
                                hn.Clear();
                                hn.AddRange(line.Split(','));
                            }
                            else
                            {
                                List<int> dl = Regex.Matches(line, "[-]?[0-9]+")
                                                    .Cast<Match>()
                                                    .Select(m => int.Parse(m.Value))
                                                    .ToList();

                                if(dl.Count > 0) 
                                { 
                                    data_cnt += dl.Count;
                                    da.Add(dl);
                                }
                            }
                        }
                    }

                }

                int j = data_cnt / da.Count;
                StringBuilder removestr = new StringBuilder();
                for (int i = da.Count - 1; i >= 0; i--)
                {
                    //データ数が少ない列は関係ないものとして消去
                    //jはデータ数の平均値
                    if (da[i].Count < j)
                    {
                        removestr.Append(i);
                        removestr.Append("行目");
                        removestr.AppendLine();
                        da.RemoveAt(i);
                    }
                }
                if(removestr.Length > 0) { 
                    MessageBox.Show(removestr.ToString() + "辺りを読み飛ばしました。\n元ファイルを確認してください。", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                //ヘッダが余ったら最後から消す
                while (hn.Count > da[0].Count)
                {
                    hn.RemoveAt(hn.Count - 1);
                    MessageBox.Show("ヘッダが多すぎます。\n一部削除されました。", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                //ヘッダ情報が足りなかったらNoneで補う
                while (hn.Count < da[0].Count)
                {
                    hn.Add("None" + hn.Count.ToString());
                }

                //グラフ情報生成
                int par = (255 / hn.Count);
                for (int i = 0; i < hn.Count; i++)
                {
                    Graphinfo gi = new Graphinfo();
                    
                    //列名セット
                    if (hn[i].Length != 0)
                    {
                        gi.Name = hn[i];
                    }
                    else
                    {
                        if (hn.Count - 1 != i)
                        {
                            gi.Name = "None" + i.ToString();
                        }
                    }

                    gi.Path = new System.Windows.Shapes.Path()
                    {
                        Name = "path" + i,
                        StrokeThickness = 1,
                        IsHitTestVisible = false
                    };
                    Canvas.SetZIndex(gi.Path, 5);

                    //グラフカラーセット 重複を避ける
                    Byte chigh = (byte)(par * (hn.Count - i));
                    Byte clow = (byte)(par * (i / 3));
                    switch (i % 6)
                    {
                        case 0:
                            gi.SetColor(Color.FromRgb(chigh, clow, clow));
                            break;
                        case 1:
                            gi.SetColor(Color.FromRgb(clow, chigh, clow));
                            break;
                        case 2:
                            gi.SetColor(Color.FromRgb(clow, clow, chigh));
                            break;
                        case 3:
                            gi.SetColor(Color.FromRgb(chigh, chigh, clow));
                            break;
                        case 4:
                            gi.SetColor(Color.FromRgb(chigh, clow, chigh));
                            break;
                        case 5:
                            gi.SetColor(Color.FromRgb(clow, chigh, chigh));
                            break;
                    }

                    List<double> datalist = new List<double>();
                    for (int k = 0; k < da.Count; k++)
                    {
                        datalist.Add(da[k][i]);
                    }
                    gi.Data = datalist;

                    gi.PathScaleSet(double.Parse(sXbox.Text), double.Parse(sYbox.Text));

                    //Canvasの高さの半分に置くようバインド
                    Binding binding = new Binding() { ElementName = "LineGraphCanvas", Path = new PropertyPath("ActualHeight"), Converter = new Converter.HalfDoubleConverter(), Mode=BindingMode.OneWay };
                    gi.Path.SetBinding(Canvas.TopProperty, binding);
                    //Canvasに反映
                    LineGraphCanvas.Children.Add(gi.Path);


                    Global.Graph.Add(gi);
                }
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void YSetData()
        {
            int rowNum = this.DataGridView.Items.IndexOf(this.DataGridView.SelectedItem);

            Yline.X1 = (rowNum - _viewModel.GraphOrigin) * double.Parse(sXbox.Text);
        }

        private void ReWrite()
        {
            TableWrite();

            // this.LBox.ItemsSource = Global.graph;
            // this.SearchList.ItemsSource = Global.Graph;

            this.ResultList.SelectedIndex = -1;
            this.ResultList.ClearValue(ListBox.ItemsSourceProperty);
        }

        // 検索結果リストに含まれる行の色を変えて表を作る
        private void TableWrite()
        {
            List<int> rlist = ResultList.Items.Cast<int>().ToList();
            var datatable = new DataTable();

            for (int i = 0; i < Global.Graph.Count; i++)
            {
                datatable.Columns.Add(Global.Graph[i].Name);
            }
            datatable.Columns.Add("IsAnswering");
            datatable.Columns.Add("RowIndex");

            DataRow dataRow;
            bool answer = this.GraphBackCheck.IsChecked ?? false;

            for (int j = 0; j < Global.Graph[0].Data.Count; j++)
            {
                dataRow = datatable.NewRow();
                for (int i = 0; i < Global.Graph.Count; i++)
                {
                    dataRow[i] = Global.Graph[i].Data[j];
                }
                dataRow["IsAnswering"] = answer && rlist.Contains(j);
                dataRow["RowIndex"] = j;
                datatable.Rows.Add(dataRow);
            }

            this.DataGridView.DataContext = datatable;
        }

        private void LBoxRightMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            string str = (sender as TextBlock).Text;
            var window = new ColorWindow(str);
            window.ShowDialog();
        }

        private void CanvasLeftMove(object sender, RoutedEventArgs e)
        {
            if (this.LBox.SelectedIndex == -1) { return; }
            if (_viewModel.GraphOrigin <= 0) { return; }
            _viewModel.GraphOrigin--;
            CanvasMove();
        }

        private void CanvasRightMove(object sender, RoutedEventArgs e)
        {
            if (this.LBox.SelectedIndex == -1) { return; }
            if (_viewModel.GraphOrigin >= Global.Graph[0].Data.Count - 1) { return; }
            _viewModel.GraphOrigin++;
            CanvasMove();
        }

        private void CanvasStartMove(object sender, RoutedEventArgs e)
        {
            if (this.LBox.SelectedIndex == -1) { return; }

            _viewModel.GraphOrigin = 0;
            CanvasMove();
        }

        private void CanvasEndMove(object sender, RoutedEventArgs e)
        {
            if (this.LBox.SelectedIndex == -1) { return; }

            _viewModel.GraphOrigin = Global.Graph[0].Data.Count - 1;
            CanvasMove();
        }

        private void CanvasSelectMove(object sender, RoutedEventArgs e)
        {
            if (this.LBox.SelectedIndex == -1) { return; }
            if (this.DataGridView.SelectedIndex <= 0) { return; }
            _viewModel.GraphOrigin = this.DataGridView.SelectedIndex;
            CanvasMove();
        }

        private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.LBox.SelectedIndex == -1) { return; }
            _viewModel.GraphOrigin += e.Delta;
            if (_viewModel.GraphOrigin <= 0)
            {
                _viewModel.GraphOrigin = 0;
            }
            if (_viewModel.GraphOrigin >= Global.Graph[0].Data.Count - 1)
            {
                _viewModel.GraphOrigin = Global.Graph[0].Data.Count - 1;
            }
            CanvasMove();
        }

        private void CanvasMove()
        {
            if (_viewModel.IsGraphBack is bool b && b)
            {
                Erase_Graph_Background();
                _viewModel.IsGraphBack = false;
            }
            else
            {
                NewCreate_Graph_Background(false);
            }

            GraphWriteLBox();
            YSetData();
        }

        private void SearchClick(object sender, RoutedEventArgs e)
        {
            if(this.SearchList.SelectedIndex == -1 || this.ConditionValue == null || ConditionBox.SelectedIndex == -1) {
                MessageBox.Show("検索に失敗しました", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return; 
            }
            
            switch (this.ConditionBox.SelectedIndex)
            {
                case 0: //以上
                    this.ResultList.ItemsSource = Global.Graph[SearchList.SelectedIndex].Data.Select((p, i) => new { Content = p, Index = i })
                                                                                             .Where(x => x.Content >= int.Parse(this.ConditionValue.Text))
                                                                                             .Select(x => x.Index)
                                                                                             .ToList();
                    break;

                case 1: //以下
                    this.ResultList.ItemsSource = Global.Graph[SearchList.SelectedIndex].Data.Select((p, i) => new { Content = p, Index = i })
                                                                                             .Where(x => x.Content <= int.Parse(this.ConditionValue.Text))
                                                                                             .Select(x => x.Index)
                                                                                             .ToList();
                    break;
                
                case 2: //同値
                    this.ResultList.ItemsSource = Global.Graph[SearchList.SelectedIndex].Data.Select((p, i) => new { Content = p, Index = i })
                                                                                             .Where(x => x.Content == int.Parse(this.ConditionValue.Text))
                                                                                             .Select(x => x.Index)
                                                                                             .ToList();
                    break;
            }
        }

        // 検索結果の列に表を追従
        private void ResultList_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (ResultList.SelectedIndex == -1) return;
            this.DataGridView.SelectedIndex = int.Parse(this.ResultList.SelectedItem.ToString());
            this.DataGridView.ScrollIntoView(this.DataGridView.SelectedItem);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var window = new Search();
            window.ShowDialog();
            this.ResultList.ItemsSource = window.resultList;
            TableWrite();
            if (window.sdc.ChangeColor)
            {
                _viewModel.GridBack = window.sdc.GridBrush;
                Global.GraphBackColor = window.sdc.GraphBrush;
            }
        }

        private void Write_Graph_Background(object sender, RoutedEventArgs e)
        {
            Erase_Graph_Background();

            switch (_viewModel.IsGraphBack)
            {
                case true:
                    NewCreate_Graph_Background(false);
                    break;
                case null:
                    NewCreate_Graph_Background(false);
                    break;
                case false:
                    return;
            }
        }

        // 検索結果から背景作成 因数で画面外まで描画するか指定 たくさんRectangleあると重い、気にする場合false
        private void NewCreate_Graph_Background(bool w_offscreen)
        {
            Erase_Graph_Background();
            if (ResultList.Items.Count == 0) return;
            var rlist = ResultList.Items.Cast<int>().ToList();
            List<int[]> cbg = new List<int[]>();
            int[] bg = { rlist[0], 0 };

            for (int cnt = 1; cnt < rlist.Count; cnt++)
            {
                if (rlist[cnt] != rlist[cnt - 1] + 1)
                {
                    bg[1] = rlist[cnt - 1];
                    cbg.Add((int[])bg.Clone());
                    bg[0] = rlist[cnt];
                }
            }
            if (bg[1] != 0 || bg[0] != 0)
            {
                bg[1] = rlist[rlist.Count - 1];
                cbg.Add((int[])bg.Clone());
            }
            if (!w_offscreen) //falseなら画面外を削除
            {
                int visible_graph = (int)(((int)LineGraphCanvas.ActualWidth + 1) / double.Parse(sXbox.Text));
                cbg.RemoveAll(b => b[1] <= _viewModel.GraphOrigin || b[0] >= visible_graph + _viewModel.GraphOrigin);
            }

            for (int i = 0; i < cbg.Count; i++)
            {
                var rectangle = new Rectangle
                {
                    Width = (cbg[i][1] - cbg[i][0]) * double.Parse(sXbox.Text)
                };
                Binding binding = new Binding() { ElementName = "LineGraphCanvas", Path = new PropertyPath("ActualHeight"), Mode = BindingMode.OneWay };
                rectangle.SetBinding(Rectangle.HeightProperty, binding);
                rectangle.Fill = Global.GraphBackColor;
                rectangle.IsHitTestVisible = false;
                rectangle.Name = "rect" + i;
                if (rectangle.Width == 0)
                {
                    rectangle.Width = double.Parse(sXbox.Text) / 2;
                    Canvas.SetLeft(rectangle, (cbg[i][0] - _viewModel.GraphOrigin) * double.Parse(sXbox.Text) - double.Parse(sXbox.Text) / 4);
                }
                else
                {
                    Canvas.SetLeft(rectangle, (cbg[i][0] - _viewModel.GraphOrigin) * double.Parse(sXbox.Text));
                }
                TransformGroup tfg = new TransformGroup();
                tfg.Children.Add(new TranslateTransform(0, 0));
                rectangle.RenderTransform = tfg;
                Canvas.SetZIndex(rectangle, 1);
                LineGraphCanvas.Children.Add(rectangle);
                LineGraphCanvas.RegisterName(rectangle.Name, rectangle);
            }

        }

        private void Create_Grid_Background(object sender, RoutedEventArgs e)
        {
            // 表の背景
            TableWrite();

        }

        private void Erase_Graph_Background()
        {
            List<FrameworkElement> removelist = LineGraphCanvas.Children.OfType<FrameworkElement>().Where(f => f.Name.Contains("rect")).ToList();
            foreach (FrameworkElement ui in removelist)
            {
                LineGraphCanvas.UnregisterName(ui.Name);
                LineGraphCanvas.Children.Remove(ui);
            }
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        private void BackgroundInvoke()
        {
            Application.Current.Dispatcher.Invoke(
                new Action(() => { }), DispatcherPriority.Background, new object[] { });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private int _GraphOrigin;
        public int GraphOrigin
        {
            get
            {
                return _GraphOrigin;
            }
            set
            {
                _GraphOrigin = value;
                OnPropertyChanged("GraphOrigin");
            }
        }

        private double _Xscale;
        public double Xscale
        {
            get
            {
                return _Xscale;
            }
            set
            {
                _Xscale = value;
                OnPropertyChanged("Xscale");
            }
        }
        private double _Yscale;
        public double Yscale
        {
            get
            {
                return _Yscale;
            }
            set
            {
                _Yscale = value;
                OnPropertyChanged("Yscale");
            }
        }

        private SolidColorBrush _GridBack;
        public SolidColorBrush GridBack
        {
            get { return _GridBack; }
            set 
            {
                _GridBack = value;
                OnPropertyChanged("GridBack");
            }
        }

        private bool? _IsGraphBack;
        public bool? IsGraphBack
        {
            get { return _IsGraphBack; }
            set
            {
                _IsGraphBack = value;
                OnPropertyChanged("IsGraphBack");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    public class Graphinfo
    {
        public System.Windows.Shapes.Path Path { get => path; set => path = value; }
        public string Name { get => name; set => name = value; }

        private Brush _brush;
        public Brush Brush
        {
            get => _brush;
            set
            {
                _brush = value;
                Path.Stroke = _brush;
            }
        }

        public void SetColor(Color color)
        {
            Brush = new SolidColorBrush(color);
        }

        public List<double> Data { get; set; } = new List<double>();

        private double _x = 1;
        private double _y = 1;
        private System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
        private string name;

        public void PathScaleSet(double xscale, double yscale)
        {
            _x = xscale;
            _y = yscale;

             DataLine(xscale, yscale);
        }

        public void PathScaleSetX(double xscale)
        {
            _x = xscale;

             DataLine(xscale, _y);
        }

        public void PathScaleSetY(double yscale)
        {
            _y = yscale;

            DataLine(_x, yscale);
        }

        //dataからpathを作る
        //倍率を指定
        private void DataLine(double xscale, double yscale)
        {
            PathGeometry pg = new PathGeometry();
            try
            {
                PathFigureCollection pfcollection = new PathFigureCollection();
                Point oldpoint = new Point(0, -(Data[0] * yscale));
                Point newpoint = new Point();

                for (int i = 0; i < Data.Count; i++)
                {
                    newpoint.X = i * xscale;
                    newpoint.Y = -(Data[i] * yscale);

                    PathSegmentCollection psc = new PathSegmentCollection();
                    psc.Add(new LineSegment() { Point = newpoint });

                    PathFigure pf = new PathFigure()
                    {
                        StartPoint = oldpoint,
                        Segments = psc,
                    };

                    pfcollection.Add(pf);
                    oldpoint = newpoint;
                }

                pg.Figures = pfcollection;
            }
            catch (Exception e)
            {
                MessageBox.Show("線が描画できませんでした" + e.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            Path.Data = pg;
        }

        public double X()
        {
            return _x;
        }

        public double Y()
        {
            return _y;
        }
    }

    public class Global
    {
        public static readonly ObservableCollection<Graphinfo> Graph = new ObservableCollection<Graphinfo>();
        public static SolidColorBrush GraphBackColor = Brushes.Tomato;
    }

}
