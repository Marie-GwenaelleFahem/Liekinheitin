using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using System.Linq;

namespace Liekinheitin.Application.Services
{
    public class RoutingEngine
    {
        private readonly PatchService _patchService;
        private readonly IPacketSender _packetSender;

        // Snapshot cumulé de tous les canaux actuellement connus. Nécessaire depuis que
        // CreativeTool peut n'envoyer que les entités modifiées (delta) : sans ce cumul,
        // BuildFrames "oublierait" les LED non touchées par la frame courante et les
        // éteindrait par erreur à chaque appel.
        private readonly Dictionary<int, byte[]> _currentChannelsByEntityId = new();

        public RoutingEngine(PatchService patchService, IPacketSender packetSender)
        {
            _patchService = patchService;
            _packetSender = packetSender;
        }

        public void Start(IStateSource stateSource)
        {
            stateSource.StateReceived += OnStateReceived;
        }

        public void OnStateReceived(State state)
        {
            foreach (var frame in BuildFrames(state))
            {
                _packetSender.Send(frame);
            }
        }

        public List<DmxFrame> BuildFrames(State state)
        {
            // Fusionne le state reçu (complet ou partiel) dans le snapshot cumulé.
            var touchedControllerUniverses = new HashSet<(string ControllerId, int Universe)>();

            foreach (var entity in state.Entities)
            {
                _currentChannelsByEntityId[entity.Id] = entity.Channels;

                var range = _patchService.FindAddress(entity.Id);
                if (range is null) continue;
                touchedControllerUniverses.Add((range.ControllerId, range.Universe));
            }

            var buffers = new Dictionary<(string ControllerId, int Universe), byte[]>();

            foreach (var (controllerId, universe) in touchedControllerUniverses)
            {
                var buffer = new byte[512];

                // Reconstruit le buffer complet de cet univers à partir du snapshot cumulé
                // (pas seulement des entités de la frame courante), pour ne pas éteindre
                // les LED de cet univers qui n'ont pas changé cette fois-ci.
                foreach (var range in _patchService.Ranges.Where(r => r.ControllerId == controllerId && r.Universe == universe))
                {
                    for (int id = range.EntityIdStart; id <= range.EntityIdEnd; id++)
                    {
                        if (!_currentChannelsByEntityId.TryGetValue(id, out var channels)) continue;

                        int positionDansLaPlage = id - range.EntityIdStart;
                        int canalDeDepart = (range.ChannelStart - 1) + positionDansLaPlage * range.ChannelsPerEntity;

                        for (int i = 0; i < channels.Length; i++)
                        {
                            int canal = canalDeDepart + i;
                            if (canal >= 0 && canal < 512)
                                buffer[canal] = channels[i];
                        }
                    }
                }

                buffers[(controllerId, universe)] = buffer;
            }

            var frames = new List<DmxFrame>();
            foreach (var ((controllerId, universe), data) in buffers)
            {
                var controller = _patchService.Controllers.FirstOrDefault(c => c.Id == controllerId);
                if (controller is null) continue;

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