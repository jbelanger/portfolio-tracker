public static class EnumerableExtensions
{
    public static decimal WeightedAverage<T>(this IEnumerable<T> source, Func<T, decimal> valueSelector, Func<T, decimal> weightSelector)
    {
        var weightedValueSum = source.Sum(x => valueSelector(x) * weightSelector(x));
        var weightSum = source.Sum(weightSelector);
        return weightSum == 0 ? 0 : weightedValueSum / weightSum;
    }
}
