namespace SigStatCompare.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

public partial class DeepSignDBViewModel : ObservableObject
{
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

    [ObservableProperty]
    private DBCategory selectedDBCategory;
    partial void OnSelectedDBCategoryChanged(DBCategory _) => UpdateStatistics();

    private readonly List<InputDeviceCategory> inputDeviceCategories = new()
    {
        new InputDeviceCategory("All", Enum.GetValues<InputDevice>().ToHashSet()),
        new InputDeviceCategory(InputDevice.Unkown.ToString(), new HashSet<InputDevice>{InputDevice.Unkown}),
        new InputDeviceCategory(InputDevice.Finger.ToString(), new HashSet<InputDevice>{InputDevice.Finger}),
        new InputDeviceCategory(InputDevice.Stylus.ToString(), new HashSet<InputDevice>{InputDevice.Stylus})
    };
    public List<InputDeviceCategory> InputDeviceCategories => inputDeviceCategories;

    [ObservableProperty]
    private InputDeviceCategory selectedInputDeviceCategory;
    partial void OnSelectedInputDeviceCategoryChanged(InputDeviceCategory _) => UpdateStatistics();

    private readonly List<SplitCategory> splitCategories = new()
    {
        new SplitCategory("All", Enum.GetValues<Split>().ToHashSet()),
        new SplitCategory(Split.Unkonwn.ToString(), new HashSet<Split>{Split.Unkonwn}),
        new SplitCategory(Split.Development.ToString(), new HashSet<Split>{Split.Development}),
        new SplitCategory(Split.Evaluation.ToString(), new HashSet<Split>{Split.Evaluation})
    };
    public List<SplitCategory> SplitCategories => splitCategories;

    [ObservableProperty]
    private SplitCategory selectedSplitCategory;
    partial void OnSelectedSplitCategoryChanged(SplitCategory _) => UpdateStatistics();

    [ObservableProperty]
    private ObservableCollection<Signer> signers;

    [ObservableProperty]
    private int matchingSignerCount;

    [ObservableProperty]
    private int loadedSignatures;

    [ObservableProperty]
    private (int min, int max) matchingSignaturesPerSigner;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxGenuinePairCountPerSigner))]
    [NotifyPropertyChangedFor(nameof(MaxForgedPairCountPerSigner))]
    private (int min, int max) matchingGenuineSignaturesPerSigner;

    public int MaxGenuinePairCountPerSigner
    {
        get
        {
            int min = MatchingGenuineSignaturesPerSigner.min;
            return min * (min - 1) / 2;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxForgedPairCountPerSigner))]
    private (int min, int max) matchingForgedSignaturesPerSigner;

    public int MaxForgedPairCountPerSigner
    {
        get
        {
            int genuineMin = MatchingGenuineSignaturesPerSigner.min;
            int forgedMin = MatchingForgedSignaturesPerSigner.min;
            return genuineMin * forgedMin / 2;
        }
    }

    [ObservableProperty]
    private int loadedSigners;

    private readonly object filePickerLock = new();
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

    private static void Update(ref (int min, int max) signatureCount, int count)
    {
        if (count < signatureCount.min) signatureCount.min = count;
        if (count > signatureCount.max) signatureCount.max = count;
    }

    private void UpdateStatistics()
    {
        if (Signers == null || Signers.Count == 1) return;

        int signerCount = 0;
        (int min, int max) signatureCount = (int.MaxValue, int.MinValue);
        (int min, int max) genuineSignatureCount = (int.MaxValue, int.MinValue);
        (int min, int max) forgedSignatureCount = (int.MaxValue, int.MinValue);
        foreach (var signer in Signers)
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
        SelectedDBCategory = DBCategories.First();
        SelectedInputDeviceCategory = InputDeviceCategories.First();
        SelectedSplitCategory = SplitCategories.First();
    }
}
