namespace SigStatCompare.ViewModels;
using SVC2021;

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
