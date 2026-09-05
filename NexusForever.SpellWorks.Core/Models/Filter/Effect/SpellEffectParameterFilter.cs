using NexusForever.Game.Static.Spell;

namespace NexusForever.SpellWorks.Core.Models.Filter.Effect
{
    /// <summary>
    /// Matches an effect row carrying a parameter of a given type.
    /// </summary>
    /// <remarks>
    /// The parameters are two parallel arrays - a type per slot and a value per slot - so a constraint on the
    /// type alone asks "does this effect have one of these at all", and adding a threshold asks it of the
    /// slot that carries it. Only slots of the requested type are compared, which is the whole point: the
    /// value in another slot means something else entirely.
    /// </remarks>
    public class SpellEffectParameterFilter : IModelFilter<ISpellEffectModel>
    {
        public SpellEffectParameterType ParameterType { get; set; }

        /// <summary>Threshold on the matching slot's value. Null constrains the type only.</summary>
        public double? Value { get; set; }

        /// <summary>Whether <see cref="Value"/> is a ceiling rather than a floor.</summary>
        public bool AtMost { get; set; }

        public bool Filter(ISpellEffectModel model)
        {
            SpellEffectParameterType[] types = model.Entry?.ParameterType;
            if (types == null)
                return false;

            float[] values = model.Entry.ParameterValue;

            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] != ParameterType)
                    continue;

                if (Value is not { } threshold)
                    return true;

                if (values == null || i >= values.Length)
                    continue;

                if (AtMost ? values[i] <= threshold : values[i] >= threshold)
                    return true;
            }

            return false;
        }
    }
}
