using System.Diagnostics;

namespace WinUtil.Services;

public static class ProcessEnumerator
{
    public static List<ProcessGroup> GroupProcesses(IEnumerable<string> excludedNames)
    {
        var excluded = new HashSet<string>(excludedNames, StringComparer.OrdinalIgnoreCase);
        var groups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                var name = proc.ProcessName;
                if (excluded.Contains(name))
                    continue;

                if (!groups.TryGetValue(name, out var list))
                {
                    list = [];
                    groups[name] = list;
                }

                list.Add(proc.Id);
            }
            catch
            {
                // Access denied or process exited
            }
            finally
            {
                proc.Dispose();
            }
        }

        foreach (var list in groups.Values)
            list.Sort();

        return groups
            .Select(kv => new ProcessGroup(kv.Key, kv.Value))
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public sealed record ProcessGroup(string Name, IReadOnlyList<int> Pids);
