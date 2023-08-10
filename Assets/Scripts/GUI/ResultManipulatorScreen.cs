#nullable enable
namespace MarkovCraft
{
    public abstract class ResultManipulatorScreen : BaseScreen
    {
        public abstract GenerationResult? GetResult();

        public string GetDefaultBaseName()
        {
            var result = GetResult();
            if (result != null)
            {
                return $"{result.ConfiguredModelName[0..^4]}_{result.GenerationSeed}";
            }

            return "exported";
        }
    }
}