using System.Collections.ObjectModel;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class ColumnListViewModel
    {
        public ObservableCollection<Views.ColumnDisplayItem> Columns { get; }

        public ColumnListViewModel(int columnCount)
        {
            Columns = new ObservableCollection<Views.ColumnDisplayItem>(
                Enumerable.Range(0, columnCount).Select(i => new Views.ColumnDisplayItem { Index = i }));
        }
    }
}