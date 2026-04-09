namespace WinUtil.ViewModels;

public sealed class ProcessGroupViewModel
{
    public ProcessGroupViewModel(string name, IReadOnlyList<int> pids)
    {
        Name = name;
        Pids = pids;
        PidsDisplay = $"[{string.Join(", ", pids)}]";
    }

    public string Name { get; }
    public string PidsDisplay { get; }
    public IReadOnlyList<int> Pids { get; }
}
