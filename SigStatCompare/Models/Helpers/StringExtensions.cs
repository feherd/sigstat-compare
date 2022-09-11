namespace SVC2021;

public static class StringExtensions
{
    public static bool Between(this string s, string inclusiveLowerBound, string inclusiveUpperBound)
    {
        return
            string.Compare(s, inclusiveLowerBound) >= 0 &&
            string.Compare(s, inclusiveUpperBound) <= 0;
    }
}
