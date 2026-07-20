using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.Domain.Entities;

namespace Liekinheitin.CreativeTool.Application
{
    public static class CanvasStateBuilder
    {
        /// <summary>Construit l'état complet (toutes les LED). Utilisé pour la resynchro périodique.</summary>
        public static List<Entity> Build(PixelCanvas canvas, WallLayout layout)
        {
            var entities = new List<Entity>();

            for (int c = 0; c < layout.Columns; c++)
                for (int r = 0; r < layout.Rows; r++)
                {
                    int id = layout.GetEntityId(c, r);
                    if (id == -1) continue;

                    var color = canvas.GetPixel(c, r);
                    entities.Add(new Entity { Id = id, Channels = new byte[] { color.R, color.G, color.B } });
                }

            return entities;
        }

        /// <summary>Ne construit que les entités dont le pixel a changé depuis le dernier appel.</summary>
        public static List<Entity> BuildDelta(PixelCanvas canvas, WallLayout layout)
        {
            var dirty = canvas.ConsumeDirty();
            var entities = new List<Entity>(dirty.Count);

            foreach (var (col, row) in dirty)
            {
                int id = layout.GetEntityId(col, row);
                if (id == -1) continue;

                var color = canvas.GetPixel(col, row);
                entities.Add(new Entity { Id = id, Channels = new byte[] { color.R, color.G, color.B } });
            }

            return entities;
        }
    }
}