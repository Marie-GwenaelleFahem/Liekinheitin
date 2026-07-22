using System.Collections.Generic;
using Liekinheitin.Domain.Entities;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class FixtureManager
    {
        public StaticProjector Projector { get; } = new();
        public List<MovingHead> MovingHeads { get; } = new();

        public FixtureManager()
        {
            for (int i = 0; i < 4; i++)
                MovingHeads.Add(new MovingHead { EntityId = 2 + i, Index = i + 1 });
        }

        public List<Entity> BuildEntities()
        {
            var list = new List<Entity>
            {
                new Entity { Id = 1, Channels = new byte[] { Projector.R, Projector.G, Projector.B, Projector.W } }
            };

            foreach (var head in MovingHeads)
            {
                list.Add(new Entity
                {
                    Id = head.EntityId,
                    Channels = new byte[]
                    {
                        head.Pan, 0, head.Tilt, 0,
                        head.Speed, head.Dimming, head.Strobe,
                        head.R, head.G, head.B, head.W,
                        0, 0
                    }
                });
            }

            return list;
        }

        public void ApplyKeyframe(int entityId, FixtureKeyframe kf)
        {
            if (entityId == 1)
            {
                Projector.R = kf.R; Projector.G = kf.G; Projector.B = kf.B; Projector.W = kf.W;
                return;
            }

            var head = MovingHeads.Find(h => h.EntityId == entityId);
            if (head is null) return;

            head.Pan = kf.Pan; head.Tilt = kf.Tilt; head.Speed = kf.Speed;
            head.Dimming = kf.Dimming; head.Strobe = kf.Strobe;
            head.R = kf.R; head.G = kf.G; head.B = kf.B; head.W = kf.W;
        }
    }
}