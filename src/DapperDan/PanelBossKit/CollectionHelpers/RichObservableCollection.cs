using global::Microsoft.Maui.Controls;

using CodeCrafty.DapperDan.PanelBossKit;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCrafty.DapperDan.PanelBossKit.CollectionHelpers
{
    public class RichObservableCollection<View> : ObservableCollection<Microsoft.Maui.Controls.View>
    {
        public RichObservableCollection()
        {
            this.CollectionChanged += SortedObservableCollection_CollectionChanged;
        }


        public RichObservableCollection(IEnumerable<Microsoft.Maui.Controls.View> collection) : base(collection)
        {
            this.CollectionChanged += SortedObservableCollection_CollectionChanged;
        }


        public RichObservableCollection(List<Microsoft.Maui.Controls.View> list) : base(list)
        {
            this.CollectionChanged += SortedObservableCollection_CollectionChanged;
        }


        //private Func<RichObservableCollection<View>, RichObservableCollection<View>> sortAndFilterDelegate;

        private ObservableCollection<View> sortedAndFilteredView = new ObservableCollection<View>();


        public Func<RichObservableCollection<View>, RichObservableCollection<View>> SortAndFilterDelegate
        {
            get;//=> sortAndFilterDelegate;
            set;//=> sortAndFilterDelegate = value;
        } = (RichObservableCollection<View> unsortedItems) =>
        {
            var retVal = from Microsoft.Maui.Controls.View iTM in unsortedItems
                         where iTM.IsEnabled
                         orderby PanelBoss.GetPanelPriority(iTM)
                         select iTM;


            var retValOutput = new RichObservableCollection<View>();

            foreach (var item in retVal)
            {
                retValOutput.Add(item);
            }

            return retValOutput;
        };
        //Don't lose these lines: magic linq link without calling any refresh:
        //public ObservableCollection<View> SortedAndFilteredView => sortAndFilterDelegate?.Invoke(this);
        public RichObservableCollection<View> SortedAndFilteredView => SortAndFilterDelegate.Invoke(this);


        private void SortedObservableCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Optionally debounce this call to optimize performance
        }
    }
}
