namespace SigStatCompare.ViewModels;
using SVC2021;

public class DBCategory
{
    private string name;
    private HashSet<DB> dbs;

    public string Name { get => name; set => name = value; }
    public HashSet<DB> DBs { get => dbs; set => dbs = value; }

    public DBCategory(string name, HashSet<DB> dbs)
    {
        this.name = name;
        this.dbs = dbs;
    }
}
