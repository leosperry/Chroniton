namespace Chroniton
{
    /// <summary>
    /// A factory for getting the singleton instance of the Singularity
    /// Useful when using dependency injection.
    /// </summary>
    public interface ISingularityFactory
    {
        ISingularity GetSingularity();
    }

    public class SingularityFactory : ISingularityFactory
    {
        public ISingularity GetSingularity()
        {
            return Singularity.Instance;
        }
    }
}
