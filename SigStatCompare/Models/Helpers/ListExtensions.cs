namespace SigStatCompare.Models.Helpers;

static class ListExtensions {
    
    public static IEnumerable<T> RandomOrder<T>(this IList<T> list, Random random) {
        var listCopy = new List<T>(list);

        while (listCopy.Count > 0)
        {
            int index = random.Next(listCopy.Count);
            T item = listCopy[index];
            listCopy.RemoveAt(index);

            yield return item;
        }
    }

}
