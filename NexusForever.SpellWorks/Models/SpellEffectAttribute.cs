using NexusForever.Game.Static.Spell;

namespace NexusForever.SpellWorks.Models
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SpellEffectAttribute : Attribute
    {
        public SpellEffectType Type { get; }

        public SpellEffectAttribute(SpellEffectType type)
        {
            Type = type;
        }
    }
}
