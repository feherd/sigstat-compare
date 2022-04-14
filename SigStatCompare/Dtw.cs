using SigStat.Common;

namespace SigStatCompare;

public class Dtw<P>
{
    readonly P[] s1;
    readonly P[] s2;
    readonly int n;
    readonly int m;

    private readonly Func<P, P, double> distance;
    readonly double[,] dtw;

    public Dtw(IEnumerable<P> sequence1, IEnumerable<P> sequence2, Func<P, P, double> distance)
    {
        s1 = (new[] { default(P) }).Concat(sequence1).ToArray();
        s2 = (new[] { default(P) }).Concat(sequence2).ToArray();

        n = s1.Length - 1;
        m = s2.Length - 1;

        this.distance = distance;

        dtw = new double[n + 1, m + 1];

        Run();
    }

    private double Run()
    {
        dtw.SetValues(double.PositiveInfinity);
        dtw[0, 0] = 0;
        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
            {
                var cost = distance(s1[i], s2[j]);
                dtw[i, j] = cost + Min(
                    dtw[i - 1, j],
                    dtw[i, j - 1],
                    dtw[i - 1, j - 1]
                );
            }
        return dtw[n, m];
    }

    public IEnumerable<(int, int)> GetPath()
    {
        var pairs = new List<(int, int)>();

        int i = n, j = m;
        while (i > 1 && j > 1)
        {
            var a = dtw[i - 1, j - 1];
            var b = dtw[i - 1, j];
            var c = dtw[i, j - 1];

            if (a < b && a < c)
                pairs.Add((--i - 1, --j - 1));
            else if (b < c)
                pairs.Add((--i - 1, j - 1));
            else
                pairs.Add((i - 1, --j - 1));
        }

        return pairs;
    }

    private static double Min(double d1, double d2, double d3)
    {
        double d12 = d1 > d2 ? d2 : d1;
        return d12 > d3 ? d3 : d12;
    }
}
