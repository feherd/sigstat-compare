namespace SigStatCompare.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SigStat.Common;
using SVC2021;

public partial class DeepSignDBViewModel : ObservableObject
{
    [ObservableProperty]
    private int loadedSignatures;

    [ObservableProperty]
    private int loadedSigners;

    [ObservableProperty]
    private ObservableCollection<Signer> signers;

    public static readonly List<DBCategory> dbCategories = new()
    {
        new DBCategory("All", Enum.GetValues<DB>().ToHashSet()),
        new DBCategory(DB.Mcyt.ToString(), new HashSet<DB>{DB.Mcyt}),
        new DBCategory(DB.eBioSignDS1.ToString(), new HashSet<DB>{DB.eBioSignDS1}),
        new DBCategory(DB.eBioSignDS2.ToString(), new HashSet<DB>{DB.eBioSignDS2}),
        new DBCategory(DB.BiosecurID.ToString(), new HashSet<DB>{DB.BiosecurID}),
        new DBCategory(DB.BiosecureDS2.ToString(), new HashSet<DB>{DB.BiosecureDS2}),
        new DBCategory(DB.EvalDB.ToString(), new HashSet<DB>{DB.EvalDB})
    };

    [ObservableProperty]
    private DBCategory selectedDBCategory = dbCategories.First();
    partial void OnSelectedDBCategoryChanged(DBCategory _) => UpdateStatistics();

    public static readonly List<InputDeviceCategory> inputDeviceCategories = new()
    {
        new InputDeviceCategory("All", Enum.GetValues<InputDevice>().ToHashSet()),
        new InputDeviceCategory(InputDevice.Unkown.ToString(), new HashSet<InputDevice>{InputDevice.Unkown}),
        new InputDeviceCategory(InputDevice.Finger.ToString(), new HashSet<InputDevice>{InputDevice.Finger}),
        new InputDeviceCategory(InputDevice.Stylus.ToString(), new HashSet<InputDevice>{InputDevice.Stylus})
    };

    [ObservableProperty]
    private InputDeviceCategory selectedInputDeviceCategory = inputDeviceCategories.First();
    partial void OnSelectedInputDeviceCategoryChanged(InputDeviceCategory _) => UpdateStatistics();

    public static readonly List<SplitCategory> splitCategories = new()
    {
        new SplitCategory("All", Enum.GetValues<Split>().ToHashSet()),
        new SplitCategory(Split.Unkonwn.ToString(), new HashSet<Split>{Split.Unkonwn}),
        new SplitCategory(Split.Development.ToString(), new HashSet<Split>{Split.Development}),
        new SplitCategory(Split.Evaluation.ToString(), new HashSet<Split>{Split.Evaluation})
    };

    [ObservableProperty]
    private SplitCategory selectedSplitCategory = splitCategories.First();
    partial void OnSelectedSplitCategoryChanged(SplitCategory _) => UpdateStatistics();

    private readonly StatisticsViewModel statistics = new();
    public StatisticsViewModel Statistics => statistics;

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

        Statistics.SignerCount = signerCount;
        Statistics.SignatureCountPerSigner = signatureCount != (int.MaxValue, int.MinValue) ? signatureCount : (0, 0);
        Statistics.GenuineSignatureCountPerSigner = genuineSignatureCount != (int.MaxValue, int.MinValue) ? genuineSignatureCount : (0, 0);
        Statistics.ForgedSignatureCountPerSigner = forgedSignatureCount != (int.MaxValue, int.MinValue) ? forgedSignatureCount : (0, 0);
    }
}
