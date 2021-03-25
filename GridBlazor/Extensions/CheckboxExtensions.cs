namespace GridBlazor.Extensions
{
    public static class CheckboxExtensions
    {
        internal static string GetRowStringKeys<T>(this ICGrid grid, T item)
        {
            var keys = grid.GetPrimaryKeyValues(item);
            return string.Join('_', keys);
        }
    }
}