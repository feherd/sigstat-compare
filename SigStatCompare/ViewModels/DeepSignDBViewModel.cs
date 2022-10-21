namespace SigStatCompare.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SigStat.Common;
using SigStat.Common.Helpers;
using SigStatCompare.Models;
using SigStatCompare.Models.Exporters;
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
    partial void OnSelectedDBCategoryChanged(DBCategory value)
    {
        datasetGenerator.DBs = value.DBs;
        UpdateStatistics();
    }

    public static readonly List<InputDeviceCategory> inputDeviceCategories = new()
    {
        new InputDeviceCategory(InputDevice.Finger.ToString(), new HashSet<InputDevice>{InputDevice.Finger}),
        new InputDeviceCategory(InputDevice.Stylus.ToString(), new HashSet<InputDevice>{InputDevice.Stylus})
    };

    [ObservableProperty]
    private InputDeviceCategory selectedInputDeviceCategory = inputDeviceCategories.First();
    partial void OnSelectedInputDeviceCategoryChanged(InputDeviceCategory value)
    {
        datasetGenerator.InputDevices = value.InputDevices;
        UpdateStatistics();
    }

    public static readonly List<SplitCategory> splitCategories = new()
    {
        new SplitCategory("All", Enum.GetValues<Split>().ToHashSet()),
        new SplitCategory(Split.Development.ToString(), new HashSet<Split>{Split.Development}),
        new SplitCategory(Split.Evaluation.ToString(), new HashSet<Split>{Split.Evaluation})
    };

    [ObservableProperty]
    private SplitCategory selectedSplitCategory = splitCategories.First();
    partial void OnSelectedSplitCategoryChanged(SplitCategory value)
    {
        datasetGenerator.Splits = value.Splits;
        UpdateStatistics();
    }

    private readonly StatisticsViewModel statisticsViewModel = new();
    public StatisticsViewModel StatisticsViewModel => statisticsViewModel;

    [ObservableProperty]
    public int seed = 0;

    private readonly DataSetParametersViewModel trainingSetParameters = new()
    {
        Name = "Training"
    };
    public DataSetParametersViewModel TrainingSetParameters => trainingSetParameters;

    private readonly DataSetParametersViewModel testSetParameters = new()
    {
        Name = "Test"
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
            await Task.Run(() =>
            {
                datasetGenerator.LoadSignatures(fileResult.FullPath, (signerCount, signatureCount) =>
                {
                    LoadedSigners = signerCount;
                    LoadedSignatures = signatureCount;
                });
            });
            datasetGenerator.DBs = selectedDBCategory.DBs;
            datasetGenerator.InputDevices = selectedInputDeviceCategory.InputDevices;
            datasetGenerator.Splits = selectedSplitCategory.Splits;
            UpdateStatistics();
        }
    });

    internal readonly CSVExporter csvExporter = new();
    internal readonly XLSXExporter xlsxExporter = new();


    [ObservableProperty]
    public double exportProgress = 0;

    [ObservableProperty]
    public TimeSpan remaining;

    public Command SaveCommand => new(async (exporter) =>
    {
        await Task.Run(() => datasetGenerator.Save(
            trainingSetParameters.DataSetParameters,
            testSetParameters.DataSetParameters,
            seed,
            exporter as IDataSetExporter,
            UpdateProgress));
    }, o => datasetGenerator.signers != null);

    private void UpdateProgress(ProgressHelper progressHelper)
    {
        ExportProgress = (double)progressHelper.Value / progressHelper.Maximum;
        Remaining = progressHelper.Remaining;
    }

    private void UpdateStatistics()
    {
        Statistics statistics = datasetGenerator.CalculateStatistics();

        StatisticsViewModel.SetStatistics(statistics);
    }

    public DeepSignDBViewModel(){
        var random = new Random();
        seed = random.Next();
    }
}
