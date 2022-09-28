namespace SigStatCompare.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SigStat.Common;
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

public class InputDeviceCategory
{
    private string name;
    private HashSet<InputDevice> inputDevices;

    public string Name { get => name; set => name = value; }
    public HashSet<InputDevice> InputDevices { get => inputDevices; set => inputDevices = value; }

    public InputDeviceCategory(string name, HashSet<InputDevice> inputDevices)
    {
        this.name = name;
        this.inputDevices = inputDevices;
    }
}

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

    private readonly List<DBCategory> dbCategories = new()
    {
        new DBCategory("All", Enum.GetValues<DB>().ToHashSet()),
        new DBCategory(DB.Mcyt.ToString(), new HashSet<DB>{DB.Mcyt}),
        new DBCategory(DB.eBioSignDS1.ToString(), new HashSet<DB>{DB.eBioSignDS1}),
        new DBCategory(DB.eBioSignDS2.ToString(), new HashSet<DB>{DB.eBioSignDS2}),
        new DBCategory(DB.BiosecurID.ToString(), new HashSet<DB>{DB.BiosecurID}),
        new DBCategory(DB.BiosecureDS2.ToString(), new HashSet<DB>{DB.BiosecureDS2}),
        new DBCategory(DB.EvalDB.ToString(), new HashSet<DB>{DB.EvalDB})
    };
    public List<DBCategory> DBCategories => dbCategories;

    private DBCategory selectedDBCategory;
    public DBCategory SelectedDBCategory
    {
        get { return selectedDBCategory; }
        set
        {
            if (value != selectedDBCategory)
            {
                selectedDBCategory = value;
                OnPropertyChanged();
                UpdateStatistics();
            }
        }
    }

    private readonly List<InputDeviceCategory> inputDeviceCategories = new()
    {
        new InputDeviceCategory("All", Enum.GetValues<InputDevice>().ToHashSet()),
        new InputDeviceCategory(InputDevice.Unkown.ToString(), new HashSet<InputDevice>{InputDevice.Unkown}),
        new InputDeviceCategory(InputDevice.Finger.ToString(), new HashSet<InputDevice>{InputDevice.Finger}),
        new InputDeviceCategory(InputDevice.Stylus.ToString(), new HashSet<InputDevice>{InputDevice.Stylus})
    };
    public List<InputDeviceCategory> InputDeviceCategories => inputDeviceCategories;

    private InputDeviceCategory selectedInputDeviceCategory;
    public InputDeviceCategory SelectedInputDeviceCategory
    {
        get { return selectedInputDeviceCategory; }
        set
        {
            if (value != selectedInputDeviceCategory)
            {
                selectedInputDeviceCategory = value;
                OnPropertyChanged();
                UpdateStatistics();
            }
        }
    }

    private readonly List<SplitCategory> splitCategories = new()
    {
        new SplitCategory("All", Enum.GetValues<Split>().ToHashSet()),
        new SplitCategory(Split.Unkonwn.ToString(), new HashSet<Split>{Split.Unkonwn}),
        new SplitCategory(Split.Development.ToString(), new HashSet<Split>{Split.Development}),
        new SplitCategory(Split.Evaluation.ToString(), new HashSet<Split>{Split.Evaluation})
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
                UpdateStatistics();
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

    private int matchingSignerCount;
    public int MatchingSignerCount
    {
        get { return matchingSignerCount; }
        set
        {
            if (value != matchingSignerCount)
            {
                matchingSignerCount = value;
                OnPropertyChanged();
            }
        }
    }

    private int loadedSignatures;
    public int LoadedSignatures
    {
        get { return loadedSignatures; }
        set
        {
            if (value != loadedSignatures)
            {
                loadedSignatures = value;
                OnPropertyChanged();
            }
        }
    }

    private (int min, int max) matchingSignaturesPerSigner;
    public (int min, int max) MatchingSignaturesPerSigner
    {
        get => matchingSignaturesPerSigner;
        set
        {
            if (value != matchingSignaturesPerSigner)
            {
                matchingSignaturesPerSigner = value;
                OnPropertyChanged();
            }
        }
    }

    private (int min, int max) matchingGenuineSignaturesPerSigner;
    public (int min, int max) MatchingGenuineSignaturesPerSigner
    {
        get => matchingGenuineSignaturesPerSigner;
        set
        {
            if (value != matchingGenuineSignaturesPerSigner)
            {
                matchingGenuineSignaturesPerSigner = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaxGenuinePairCountPerSigner));
                OnPropertyChanged(nameof(MaxForgedPairCountPerSigner));
            }
        }
    }

    public int MaxGenuinePairCountPerSigner
    {
        get
        {
            int min = MatchingGenuineSignaturesPerSigner.min;
            return min * (min - 1) / 2;
        }
    }

    private (int min, int max) matchingForgedSignaturesPerSigner;
    public (int min, int max) MatchingForgedSignaturesPerSigner
    {
        get => matchingForgedSignaturesPerSigner;
        set
        {
            if (value != matchingForgedSignaturesPerSigner)
            {
                matchingForgedSignaturesPerSigner = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaxForgedPairCountPerSigner));
            }
        }
    }

    public int MaxForgedPairCountPerSigner
    {
        get
        {
            int genuineMin = MatchingGenuineSignaturesPerSigner.min;
            int forgedMin = MatchingForgedSignaturesPerSigner.min;
            return genuineMin * forgedMin / 2;
        }
    }

    private int loadedSigners;
    public int LoadedSigners
    {
        get => loadedSigners;
        set
        {
            if (value != loadedSigners)
            {
                loadedSigners = value;
                OnPropertyChanged();
            }
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

            var loader = new Svc2021Loader(fileResult.FullPath, false);
            Signers = await Task.Run(() =>
            {
                var signers = loader.EnumerateSigners()
                    .Select((signer, i) =>
                    {
                        LoadedSigners = i + 1;
                        LoadedSignatures += signer.Signatures.Count;
                        return signer;
                    })
                    .OrderBy(s => s.ID);
                return new ObservableCollection<Signer>(signers);
            });
            UpdateStatistics();
        }

        isFilePickerOpen = false;
    });

    private void Update(ref (int min, int max) signatureCount, int count)
    {
        if (count < signatureCount.min) signatureCount.min = count;
        if (count > signatureCount.max) signatureCount.max = count;
    }

    private void UpdateStatistics()
    {
        if (signers == null || signers.Count == 1) return;

        int signerCount = 0;
        (int min, int max) signatureCount = (int.MaxValue, int.MinValue);
        (int min, int max) genuineSignatureCount = (int.MaxValue, int.MinValue);
        (int min, int max) forgedSignatureCount = (int.MaxValue, int.MinValue);
        foreach (var signer in signers)
        {
            var signatures = signer.Signatures.Where((signature) =>
            {
                var svc2021Signature = signature as SVC2021.Entities.Svc2021Signature;
                return selectedDBCategory.DBs.Contains(svc2021Signature.DB)
                    && selectedInputDeviceCategory.InputDevices.Contains(svc2021Signature.InputDevice)
                    && selectedSplitCategory.Splits.Contains(svc2021Signature.Split);
            });

            int count = signatures.Count();
            int genuine = signatures.Count(signature => signature.Origin == Origin.Genuine);
            int forged = signatures.Count(signature => signature.Origin == Origin.Forged);

            if (count == 0) continue;

            signerCount++;

            Update(ref signatureCount, count);
            Update(ref genuineSignatureCount, genuine);
            Update(ref forgedSignatureCount, forged);
        }

        MatchingSignerCount = signerCount;
        MatchingSignaturesPerSigner = signatureCount != (int.MaxValue, int.MinValue) ? signatureCount : (0, 0);
        MatchingGenuineSignaturesPerSigner = genuineSignatureCount != (int.MaxValue, int.MinValue) ? genuineSignatureCount : (0, 0);
        MatchingForgedSignaturesPerSigner = forgedSignatureCount != (int.MaxValue, int.MinValue) ? forgedSignatureCount : (0, 0);
    }

    public DeepSignDBViewModel()
    {
        SelectedDBCategory = dbCategories.First();
        SelectedInputDeviceCategory = inputDeviceCategories.First();
        SelectedSplitCategory = splitCategories.First();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
