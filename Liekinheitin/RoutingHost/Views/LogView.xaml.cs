using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Liekinheitin.Infrastructure.Supervision;

namespace Liekinheitin.RoutingHost.Views
{
    /// <summary>
    /// Affiche en temps réel les messages journalisés dans <see cref="LogService.Instance"/> :
    /// se remplit au fil de l'eau (échecs d'envoi ArtNet, pertes de ping, etc.) sans qu'aucune
    /// autre vue n'ait besoin de savoir que cet écran existe.
    /// </summary>
    public partial class LogView : UserControl
    {
        private readonly ObservableCollection<LogEntry> _entries = new();

        public LogView()
        {
            InitializeComponent();

            LogList.ItemsSource = _entries;

            // Rejoue l'historique déjà journalisé avant l'ouverture de cet écran (par exemple
            // un échec d'envoi ArtNet survenu avant que LogView soit affichée).
            foreach (var entry in LogService.Instance.GetHistory())
                _entries.Add(entry);

            LogService.Instance.LogEntryAdded += OnLogEntryAdded;
            ScrollToEnd();
        }

        private void OnLogEntryAdded(LogEntry entry)
        {
            Dispatcher.Invoke(() =>
            {
                _entries.Add(entry);
                ScrollToEnd();
            });
        }

        private void ScrollToEnd()
        {
            if (_entries.Count > 0)
                LogList.ScrollIntoView(_entries[^1]);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) => _entries.Clear();
    }
}
