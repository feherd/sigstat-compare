using SigStat.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SVC2021.Entities;

/// <summary>
/// Strongly typed wrapper for representing SVC2021 Signatures
/// </summary>
public class Svc2021Signature : Signature
{
    public string FileName { get => GetFeature(Svc2021.FileName); set => SetFeature(Svc2021.FileName, value); }
    public DB DB { get => GetFeature(Svc2021.DB); set => SetFeature(Svc2021.DB, value); }
    public Split Split { get => GetFeature(Svc2021.Split); set => SetFeature(Svc2021.Split, value); }
    public InputDevice InputDevice { get => GetFeature(Svc2021.InputDevice); set => SetFeature(Svc2021.InputDevice, value); }

    public List<double> X { get => GetFeature(Features.X); set => SetFeature(Features.X, value); }
    public List<double> Y { get => GetFeature(Features.Y); set => SetFeature(Features.Y, value); }
    public List<double> Pressure { get => GetFeature(Features.Pressure); set => SetFeature(Features.Pressure, value); }
    public List<double> T { get => GetFeature(Features.T); set => SetFeature(Features.T, value); }

    public List<int> Svc_X { get => GetFeature(Svc2021.X); set => SetFeature(Svc2021.X, value); }
    public List<int> Svc_Y { get => GetFeature(Svc2021.Y); set => SetFeature(Svc2021.Y, value); }
    public List<double> Svc_Pressure { get => GetFeature(Svc2021.Pressure); set => SetFeature(Svc2021.Pressure, value); }
    public List<long> Svc_T { get => GetFeature(Svc2021.T); set => SetFeature(Svc2021.T, value); }

    public Image<Rgba32> Image { get => GetFeature(Features.Image);  set => SetFeature(Features.Image, value); }

    public bool IsPreprocessed { get => GetFeature(Svc2021.IsPreprocessed); set => SetFeature(Svc2021.IsPreprocessed, value); }

    public Svc2021Signature()
    {
        IsPreprocessed = false;
    }

}
