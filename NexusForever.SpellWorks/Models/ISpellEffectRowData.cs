using NexusForever.GameTable.Model;

namespace NexusForever.SpellWorks.Models
{
    public interface ISpellEffectRowData
    {
        Spell4EffectsEntry Entry { get; set; }

        string Data00 { get; }
        bool Data00IsHyperlink { get; }
        string Data01 { get; }
        string Data02 { get; }
        string Data03 { get; }
        string Data04 { get; }
        string Data05 { get; }
        string Data06 { get; }
        string Data07 { get; }
        string Data08 { get; }
        string Data09 { get; }
    }
}