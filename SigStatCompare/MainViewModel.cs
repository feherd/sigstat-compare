namespace SigStatCompare;

using SigStat.Common;
using SigStat.Common.Loaders;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

enum SelectionState
{
    None,
    SelectingFirstSignature,
    SelectingSecondSignature
}

public class MainViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Type> datasetLoaders;
    public ObservableCollection<Type> DatasetLoaders
    {
        get { return datasetLoaders; }
        set
        {
            if (value != datasetLoaders)
            {
                datasetLoaders = value;
                OnPropertyChanged();
            }
        }
    }

    private Type selectedDatasetLoader;
    public Type SelectedDatasetLoader
    {
        get { return selectedDatasetLoader; }
        set
        {
            if (value != selectedDatasetLoader)
            {
                selectedDatasetLoader = value;
                OnPropertyChanged();
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

    private Signer selectedSigner;
    public Signer SelectedSigner
    {
        get { return selectedSigner; }
        set
        {
            if (value != selectedSigner)
            {
                selectedSigner = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private Signature selectedSignature;

    public Signature SelectedSignature
    {
        get { return selectedSignature; }
        set
        {
            if (value != selectedSignature)
            {
                selectedSignature = value;
                OnPropertyChanged();
            }

            if (SelectionState == SelectionState.SelectingFirstSignature)
            {
                FirstSelectedSignature = SelectedSignature;
                SelectionState = SelectionState.None;
            }

            if (SelectionState == SelectionState.SelectingSecondSignature)
            {
                SecondSelectedSignature = SelectedSignature;
                SelectionState = SelectionState.None;
            }
        }
    }

    private Signature firstSelectedSignature;

    public Signature FirstSelectedSignature
    {
        get { return firstSelectedSignature; }
        set
        {
            if (value != firstSelectedSignature)
            {
                firstSelectedSignature = value;
                OnPropertyChanged();
            }
        }
    }

    private Signature secondSelectedSignature;

    public Signature SecondSelectedSignature
    {
        get { return secondSelectedSignature; }
        set
        {
            if (value != secondSelectedSignature)
            {
                secondSelectedSignature = value;
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
            var ctor = SelectedDatasetLoader.GetConstructor(new[] { typeof(string), typeof(bool) });
            var loader = (IDataSetLoader)ctor.Invoke(new object[] { fileResult.FullPath, true });
            Signers = await Task.Run(() => new ObservableCollection<Signer>(loader.EnumerateSigners().OrderBy(s => s.ID)));
        }

        isFilePickerOpen = false;
    });

    private ObservableCollection<FeatureDescriptor<List<double>>> dtwFeatures;
    public ObservableCollection<FeatureDescriptor<List<double>>> DtwFeatures
    {
        get { return dtwFeatures; }
        set
        {
            if (value != dtwFeatures)
            {
                dtwFeatures = value;
                OnPropertyChanged();
            }
        }
    }

    private FeatureDescriptor<List<double>> selectedDtwFeature;
    public FeatureDescriptor<List<double>> SelectedDtwFeature
    {
        get { return selectedDtwFeature; }
        set
        {
            if (value != selectedDtwFeature)
            {
                selectedDtwFeature = value;
                OnPropertyChanged();
            }
        }
    }

    private SelectionState selectionState = SelectionState.None;

    internal SelectionState SelectionState
    {
        get => selectionState;
        set
        {
            selectionState = value;
            OnPropertyChanged();

            SelectFirstSignatureCommand.ChangeCanExecute();
            SelectSecondSignatureCommand.ChangeCanExecute();
        }
    }

    public Command SelectFirstSignatureCommand => new(
        execute: () =>
        {
            SelectionState = SelectionState.SelectingFirstSignature;
            FirstSelectedSignature = null;
        },
        canExecute: () => SelectionState != SelectionState.SelectingFirstSignature
    );

    public Command SelectSecondSignatureCommand => new(
        execute: () =>
        {
            SelectionState = SelectionState.SelectingSecondSignature;
            SecondSelectedSignature = null;
        },
        canExecute: () => SelectionState != SelectionState.SelectingSecondSignature
    );


    public MainViewModel()
    {
        DatasetLoaders = new ObservableCollection<Type>(
            typeof(Svc2004Loader).Assembly.GetTypes()
            .Where(t => t.GetInterface(typeof(IDataSetLoader).FullName) != null));

        SelectedDatasetLoader = typeof(Svc2004Loader);

        DtwFeatures = new ObservableCollection<FeatureDescriptor<List<double>>>{
            Features.X,
            Features.Y
        };

        SelectedDtwFeature = Features.X;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
