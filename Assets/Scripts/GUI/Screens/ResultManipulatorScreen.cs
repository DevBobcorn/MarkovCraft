#nullable enable
namespace MarkovCraft
{
    public abstract class ResultManipulatorScreen : BaseScreen
    {
        public abstract GenerationResult? GetResult();

        public string GetDefaultBaseName()
        {
            var result = GetResult();
            return result ? $"{result.ConfiguredModelName[0..^4]}_{result.GenerationSeed}" : "exported";
        }
    }
}