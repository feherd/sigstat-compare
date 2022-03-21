namespace SigStatCompare;

using SigStat.Common;
using SigStat.Common.Loaders;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

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
            SelectedSignature = selectedSigner?.Signatures[0];
        }
    }

    private Signature selectedSignature;

    public event PropertyChangedEventHandler PropertyChanged;

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
        }
    }

    public Command LoadCommand => new(async () =>
    {
        var databaseDir = Environment.GetEnvironmentVariable("SigStatDB");
        FileResult fileResult = await FilePicker.PickAsync();
        var ctor = SelectedDatasetLoader.GetConstructor(new[] { typeof(string), typeof(bool) });
        var loader = (IDataSetLoader)ctor.Invoke(new object[] { fileResult.FullPath, true });
        Signers = await Task.Run(() => new ObservableCollection<Signer>(loader.EnumerateSigners().OrderBy(s => s.ID)));
    });

    public MainViewModel()
    {
        DatasetLoaders = new ObservableCollection<Type>(
            typeof(Svc2004Loader).Assembly.GetTypes()
            .Where(t => t.GetInterface(typeof(IDataSetLoader).FullName) != null));

        SelectedDatasetLoader = typeof(Svc2004Loader);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
