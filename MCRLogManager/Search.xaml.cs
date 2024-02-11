using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

namespace MCRLogManager
{
    /// <summary>
    /// Search.xaml の相互作用ロジック
    /// </summary>
    public partial class Search : Window
    {
        public Search()
        {
            InitializeComponent();
            searches.Clear();
            sdc = new SearchDataContext { GraphBrush = Brushes.Black, GridBrush = Brushes.Black };
            this.DataContext = sdc;
            searches.Add(new SearchInfo() { GroupID = 0 });
            SearchSetBox.DataContext = searches;
            SearchSetBox.Items.SortDescriptions.Add(new SortDescription("GroupID", ListSortDirection.Ascending));
        }

        private readonly ObservableCollection<SearchInfo> searches = new ObservableCollection<SearchInfo>();
        public List<int> resultList = new List<int>();
        public SearchDataContext sdc;

        private void AND_Add(object sender, RoutedEventArgs e)
        {
            if (SearchSetBox.SelectedIndex == -1) return;
            SearchInfo ss = (SearchInfo)SearchSetBox.SelectedItem;
            SearchInfo si = new SearchInfo() { GroupID = ss.GroupID };
            searches.Add(si);
        }

        private void OR_Add(object sender, RoutedEventArgs e)
        {
            if (SearchSetBox.SelectedIndex == -1) return;
            var id = from p in searches select p.GroupID;
            SearchInfo si = new SearchInfo() { GroupID = id.Max() + 1 };
            searches.Add(si);
        }

        private void Delete(object sender, RoutedEventArgs e)
        {
            if (SearchSetBox.SelectedIndex == -1) return;
            SearchInfo select_info = (SearchInfo)SearchSetBox.SelectedItem;
            searches.Remove(select_info);
        }

        private void End_Research(object sender, RoutedEventArgs e)
        {
            var gi = from p in searches select p.TargetList;
            var cv = from p in searches select p.ConditionValue;
            if (gi.Contains(null)) return;

            resultList.Clear();
            List<int> result = new List<int>();
            List<IGrouping<int,SearchInfo>> list = searches.GroupBy(x => x.GroupID).ToList();
            foreach(IGrouping<int, SearchInfo> group in list)
            {
                List<SearchInfo> inlist = group.ToList();

                //AND処理
                List<int> temp = inlist[0].Search();
                foreach(SearchInfo info in inlist)
                {
                    temp = temp.Intersect(info.Search()).ToList();
                }
                //OR処理
                result = result.Union(temp).ToList();
                //result = (result.Count == 0) ? temp : result.Union(temp).ToList();
            }

            resultList = result;
            this.Close();
        }

        private void GraphColorChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Color color = Color.FromRgb((byte)rSlid1.Value, (byte)gSlid1.Value, (byte)bSlid1.Value);
            sdc.GraphBrush = new SolidColorBrush(color);
        }

        private void GridColorChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Color color = Color.FromRgb((byte)rSlid2.Value, (byte)gSlid2.Value, (byte)bSlid2.Value);
            sdc.GridBrush = new SolidColorBrush(color);
        }

    }


    public class SearchInfo
    {
        public Graphinfo TargetList { get; set; }

        private int _operation;
        public int Operation
        {
            get { return _operation; }
            set
            {
                if (value >= 0 && value < 3)
                {
                    _operation = value;
                }
            }
        }

        public double ConditionValue { get; set; }

        public List<int> Search()
        {
            List<int> resultIndexList = new List<int>();

            switch (_operation)
            {
                case 0: //以上
                    resultIndexList = TargetList.Data.Select((p, i) => new { Content = p, Index = i })
                                                    .Where(x => x.Content >= ConditionValue)
                                                    .Select(x => x.Index)
                                                    .ToList();
                    break;

                case 1: //以下
                    resultIndexList = TargetList.Data.Select((p, i) => new { Content = p, Index = i })
                                                    .Where(x => x.Content <= ConditionValue)
                                                    .Select(x => x.Index)
                                                    .ToList();
                    break;

                case 2: //同値
                    resultIndexList = TargetList.Data.Select((p, i) => new { Content = p, Index = i })
                                                    .Where(x => x.Content == ConditionValue)
                                                    .Select(x => x.Index)
                                                    .ToList();
                    break;
            }
            return resultIndexList;
        }

        private int _GroupID;
        public int GroupID
        {
            get { return _GroupID; }
            set 
            { 
                _GroupID = value;
                GroupMod = (_GroupID % 2 == 0);
            }
        }

        //グループを色で表現するためのバインド用
        public bool GroupMod { get; private set; }

        public SearchInfo()
        {
            ConditionValue = 0;
        }
    }

    public class SearchDataContext : INotifyPropertyChanged
    {
        private void BackgroundInvoke()
        {
            Application.Current.Dispatcher.Invoke(
                new Action(() => { }), DispatcherPriority.Background, new object[] { });
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private SolidColorBrush _GridBrush;
        public SolidColorBrush GridBrush
        {
            get
            {
                return _GridBrush;
            }
            set
            {
                _GridBrush = value;
                OnPropertyChanged("GridBrush");
            }
        }
        private SolidColorBrush _GraphBrush;
        public SolidColorBrush GraphBrush
        {
            get
            {
                return _GraphBrush;
            }
            set
            {
                _GraphBrush = value;
                OnPropertyChanged("GraphBrush");
            }
        }
        private bool _ChangeColor;
        public bool ChangeColor
        {
            get { return _ChangeColor; }
            set { _ChangeColor = value; }
        }

        protected void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
