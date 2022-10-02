namespace SigStatCompare.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SigStat.Common;
using SigStatCompare.Models;
using SVC2021;

public partial class DeepSignDBViewModel : ObservableObject
{
    private readonly DatasetGenerator datasetGenerator = new DatasetGenerator();

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

    private readonly StatisticsViewModel statisticsViewModel = new();
    public StatisticsViewModel StatisticsViewModel => statisticsViewModel;

    private readonly DataSetParametersViewModel trainingSetParameters = new()
    {
        Name = "Training:"
    };
    public DataSetParametersViewModel TrainingSetParameters => trainingSetParameters;

    private readonly DataSetParametersViewModel testSetParameters = new()
    {
        Name = "Test:"
    };
    public DataSetParametersViewModel TestSetParameters => testSetParameters;

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

        lock (filePickerLock)
        {
            isFilePickerOpen = false;
        }

        if (fileResult is not null)
        {
            var enumerable = datasetGenerator.LoadSignatures(fileResult.FullPath);
            await Task.Run(() =>
            {
                foreach (var (signerCount, signatureCount) in enumerable)
                {
                    LoadedSigners = signerCount;
                    LoadedSignatures = signatureCount;
                }
            });
            UpdateStatistics();
        }
    });

    public Command SaveToCSV => new(() => {});

    public Command SaveToXLSX => new(() => {});

    private void UpdateStatistics()
    {
        Statistics statistics = datasetGenerator.CalculateStatistics(
            selectedDBCategory.DBs,
            selectedInputDeviceCategory.InputDevices,
            selectedSplitCategory.Splits
        );

        StatisticsViewModel.SetStatistics(statistics);
    }
}
