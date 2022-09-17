namespace SigStatCompare.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SigStat.Common;

public class DeepSignDBViewModel : INotifyPropertyChanged
{

    public event PropertyChangedEventHandler PropertyChanged;

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
