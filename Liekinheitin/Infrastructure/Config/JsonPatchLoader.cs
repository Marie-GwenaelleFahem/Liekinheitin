using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Liekinheitin.Infrastructure.Config;

/// <summary>
/// Implémentation concrète d'<see cref="IPatchLoader"/> : lit et écrit le fichier
/// patch.json sur le disque, au format établi avec l'équipe (champs en français :
/// "controleurs" et "plages").
/// </summary>
public class JsonPatchLoader : IPatchLoader
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <inheritdoc />
    public PatchData Load(string path)
    {
        string json = File.ReadAllText(path);
        var fichier = JsonSerializer.Deserialize<PatchFileDto>(json, Options)
                      ?? throw new InvalidDataException($"Fichier de patch invalide : {path}");

        var data = new PatchData
        {
            Controllers = fichier.Controleurs.Select(c => new Controller
            {
                Id = c.Id,
                IpAddress = c.Ip,
            }).ToList(),

            Ranges = fichier.Plages.Select(p => new PatchRange
            {
                EntityIdStart = p.EntiteDebut,
                EntityIdEnd = p.EntiteFin,
                ControllerId = p.Controleur,
                Universe = p.Univers,
                ChannelStart = p.CanalDepart,
                ChannelsPerEntity = p.CanauxParEntite,
            }).ToList(),
        };

        return data;
    }

    /// <inheritdoc />
    public void Save(string path, PatchData data)
    {
        var fichier = new PatchFileDto
        {
            Controleurs = data.Controllers.Select(c => new ControllerDto { Id = c.Id, Ip = c.IpAddress }).ToList(),
            Plages = data.Ranges.Select(r => new PatchRangeDto
            {
                EntiteDebut = r.EntityIdStart,
                EntiteFin = r.EntityIdEnd,
                Controleur = r.ControllerId,
                Univers = r.Universe,
                CanalDepart = r.ChannelStart,
                CanauxParEntite = r.ChannelsPerEntity,
            }).ToList(),
        };

        string json = JsonSerializer.Serialize(fichier, Options);
        File.WriteAllText(path, json);
    }

    // DTO internes reflétant exactement le format JSON du fichier patch.json (noms de champs
    // en français), séparés des entités Domain pour ne jamais faire fuiter un détail de
    // sérialisation dans le reste de l'application.

    private class PatchFileDto
    {
        [JsonPropertyName("controleurs")]
        public List<ControllerDto> Controleurs { get; set; } = new();

        [JsonPropertyName("plages")]
        public List<PatchRangeDto> Plages { get; set; } = new();
    }

    private class ControllerDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("ip")]
        public string Ip { get; set; } = string.Empty;
    }

    private class PatchRangeDto
    {
        [JsonPropertyName("entiteDebut")]
        public int EntiteDebut { get; set; }

        [JsonPropertyName("entiteFin")]
        public int EntiteFin { get; set; }

        [JsonPropertyName("controleur")]
        public string Controleur { get; set; } = string.Empty;

        [JsonPropertyName("univers")]
        public int Univers { get; set; }

        [JsonPropertyName("canalDepart")]
        public int CanalDepart { get; set; }

        [JsonPropertyName("canauxParEntite")]
        public int CanauxParEntite { get; set; }
    }
}