namespace SigStatCompare.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SigStat.Common;
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

public class DeepSignDBViewModel : INotifyPropertyChanged
{

    public event PropertyChangedEventHandler PropertyChanged;

    private readonly List<SplitCategory> splitCategories = new()
    {
        new SplitCategory("All", new HashSet<Split>{Split.Unkonwn, Split.Development, Split.Evaluation}),
        new SplitCategory("Unkonwn", new HashSet<Split>{Split.Unkonwn}),
        new SplitCategory("Development", new HashSet<Split>{Split.Development}),
        new SplitCategory("Evaluation", new HashSet<Split>{Split.Evaluation})
    };
    public List<SplitCategory> SplitCategories => splitCategories;

    private SplitCategory selectedSplitCategory;
    public SplitCategory SelectedSplitCategory
    {
        get { return selectedSplitCategory; }
        set
        {
            if (value != selectedSplitCategory)
            {
                selectedSplitCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MatchingSignatureCount));
            }
        }
    }



    private ObservableCollection<Signer> signers;
    public ObservableCollection<Signer> Signers
    {
        get { return signers; }
        set
        {
            if (value != signers)
            {
                signers = value;
                OnPropertyChanged();
            }
        }
    }

    private int signatureCount;
    public int SignatureCount
    {
        get { return signatureCount; }
        set
        {
            if (value != signatureCount)
            {
                signatureCount = value;
                OnPropertyChanged();
            }
        }
    }

    public int MatchingSignatureCount
    {
        get
        {
            if (signers == null) return 0;

            int count = 0;
            foreach (var signer in signers)
            {
                count += signer.Signatures.Count((signature) =>
                {
                    var svc2021Signature = signature as SVC2021.Entities.Svc2021Signature;
                    return selectedSplitCategory.Splits.Contains(svc2021Signature.Split);
                });
            }
            return count;
        }
    }

    private object filePickerLock = new object();
    private bool isFilePickerOpen = false;

    public Command LoadCommand => new(async () =>
    {
        lock (filePickerLock)
        {
            if (isFilePickerOpen) return;
            isFilePickerOpen = true;
        }

        var databaseDir = Environment.GetEnvironmentVariable("SigStatDB");

        FileResult fileResult = await FilePicker.PickAsync();

        if (fileResult is not null)
        {
            Console.WriteLine(fileResult.FullPath);

            var loader = new SVC2021.Svc2021Loader(fileResult.FullPath, false);
            Signers = await Task.Run(() => new ObservableCollection<Signer>(loader.EnumerateSigners().OrderBy(s => s.ID)));
            SignatureCount = await Task.Run(() =>
            {
                int count = 0;
                foreach (var signer in signers)
                {
                    count += signer.Signatures.Count;
                }
                return count;
            });
        }

        isFilePickerOpen = false;
    });

    public DeepSignDBViewModel()
    {

    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
