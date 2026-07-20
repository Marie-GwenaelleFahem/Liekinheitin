using System.Windows;

namespace Liekinheitin.RoutingHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Transmet les instances partagées (construites une seule fois dans App.xaml.cs)
        /// à la vue de visualisation du patch. Appelée depuis App.xaml.cs avant Show(),
        /// pas besoin d'attendre l'événement Loaded : le UserControl existe déjà dans
        /// l'arbre visuel dès qu'InitializeComponent() de la fenêtre a construit son XAML.
        /// </summary>
        public void InitializePatchVisualization(
            Application.Services.PatchService patchService,
            Application.Interfaces.IPacketSender packetSender,
            Application.Services.RoutingEngine routingEngine)
        {
            PatchView.Initialize(patchService, packetSender, routingEngine);
        }
    }
}