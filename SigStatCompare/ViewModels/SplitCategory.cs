namespace SigStatCompare.ViewModels;
using SVC2021;

public class SplitCategory
{
    private readonly string name;
    private readonly HashSet<Split> splits;

    public string Name => name;
    public HashSet<Split> Splits => splits;

    public SplitCategory(string name, HashSet<Split> splits)
    {
        this.name = name;
        this.splits = splits;
    }
}
