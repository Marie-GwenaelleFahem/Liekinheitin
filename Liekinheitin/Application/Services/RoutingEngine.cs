using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Liekinheitin.Application.Services
{

    /// <summary>
    /// Le cœur du routage : transforme un <see cref="State"/> reçu en trames DMX prêtes à
    /// envoyer aux contrôleurs.
    /// </summary>
    /// <remarks>
    /// <see cref="Start"/> s'abonne à l'événement <see cref="IStateSource.StateReceived"/> de la
    /// source fournie. Chaque fois qu'un <see cref="State"/> arrive, <see cref="OnStateReceived"/>
    /// appelle <see cref="BuildFrames"/> : pour chaque <see cref="Entity"/> du State, elle
    /// demande à <see cref="PatchService.FindAddress"/> la <see cref="PatchRange"/>
    /// correspondante, calcule le canal exact à partir de cette plage, regroupe les valeurs de
    /// toutes les entités par (contrôleur, univers), puis construit une <see cref="DmxFrame"/>
    /// par univers concerné. Elle transmet ensuite chaque trame à
    /// <see cref="IPacketSender.Send"/>, sans jamais savoir s'il s'agit réellement d'ArtNet ou
    /// d'un autre protocole.
    /// </remarks>
    public class RoutingEngine
    {
        private readonly PatchService _patchService;
        private readonly IPacketSender _packetSender;

        public RoutingEngine(PatchService patchService, IPacketSender packetSender)
        {
            _patchService = patchService;
            _packetSender = packetSender;
        }

        /// <summary>S'abonne à la source d'état fournie pour démarrer le routage en continu.</summary>
        /// <param name="stateSource">La source qui signalera l'arrivée de chaque nouvel état.</param>
        public void Start(IStateSource stateSource)
        {
            stateSource.StateReceived += OnStateReceived;
        }

        /// <summary>Appelée à chaque State reçu : construit les trames et les envoie.</summary>
        /// <param name="state">L'état reçu à router.</param>
        public void OnStateReceived(State state)
        {
            foreach (var frame in BuildFrames(state))
            {
                _packetSender.Send(frame);
            }
        }

        /// <summary>
        /// Calcule, pour chaque entité du State, son adresse DMX exacte, et regroupe le résultat
        /// en une trame par (contrôleur, univers) concerné.
        /// </summary>
        /// <param name="state">L'état à traduire en trames DMX.</param>
        /// <returns>La liste des trames prêtes à être envoyées.</returns>
        public List<DmxFrame> BuildFrames(State state)
        {
            // Un buffer de 512 canaux par (contrôleur, univers) réellement concerné.
            var buffers = new Dictionary<(string ControllerId, int Universe), byte[]>();

            foreach (var entity in state.Entities)
            {
                var range = _patchService.FindAddress(entity.Id);
                if (range is null)
                {
                    continue; // Entité inconnue du patch : ignorée plutôt que de faire planter le routage.
                }

                var key = (range.ControllerId, range.Universe);
                if (!buffers.TryGetValue(key, out var buffer))
                {
                    buffer = new byte[512];
                    buffers[key] = buffer;
                }

                int positionDansLaPlage = entity.Id - range.EntityIdStart;
                int canalDeDepart = (range.ChannelStart - 1) + positionDansLaPlage * range.ChannelsPerEntity;

                for (int i = 0; i < entity.Channels.Length; i++)
                {
                    int canal = canalDeDepart + i;
                    if (canal >= 0 && canal < 512)
                    {
                        buffer[canal] = entity.Channels[i];
                    }
                }
            }

            var frames = new List<DmxFrame>();
            foreach (var ((controllerId, universe), data) in buffers)
            {
                var controller = _patchService.Controllers.FirstOrDefault(c => c.Id == controllerId);
                if (controller is null)
                {
                    continue; // Contrôleur référencé par le patch mais absent de la liste : ignoré.
                }

                frames.Add(new DmxFrame
                {
                    TargetIp = controller.IpAddress,
                    Universe = universe,
                    Data = data,
                });
            }

            return frames;
        }

    }
}
