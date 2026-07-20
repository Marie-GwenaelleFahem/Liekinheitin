using System.Windows.Controls;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class ColumnListView : UserControl
    {
        public event Action<int>? ColumnSelected;

        public ColumnListView()
        {
            InitializeComponent();
        }

        private void OnColumnSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ColumnListBox.SelectedItem is ColumnDisplayItem item)
                ColumnSelected?.Invoke(item.Index);

            ColumnListBox.SelectedItem = null; // permet de recliquer la même colonne plusieurs fois
        }
    }

    public sealed class ColumnDisplayItem
    {
        public int Index { get; init; }       // 0-based, utilisé en interne
        public override string ToString() => $"Colonne {Index + 1}"; // affiché 1-based
    }
}