using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using WinUtil.Native;

namespace WinUtil.Services;

public static class PortEnumerator
{
    private const uint ErrorInsufficientBuffer = 122;

    public static List<PortEntry> GetOpenPorts()
    {
        var list = new List<PortEntry>();
        AddTcpRows(list, IpHelperApi.AfInet);
        AddTcpRows(list, IpHelperApi.AfInet6);
        AddUdpRows(list, IpHelperApi.AfInet);
        AddUdpRows(list, IpHelperApi.AfInet6);

        return list
            .OrderBy(p => p.AppName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Protocol)
            .ThenBy(p => p.Port)
            .ToList();
    }

    private static void AddTcpRows(List<PortEntry> list, int addressFamily)
    {
        int size = 0;
        var needSize = IpHelperApi.GetExtendedTcpTable(IntPtr.Zero, ref size, true, addressFamily, IpHelperApi.TcpTableOwnerPidAll, 0);
        if (needSize != 0 && needSize != ErrorInsufficientBuffer)
            return;
        if (size <= 0)
            return;

        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var result = IpHelperApi.GetExtendedTcpTable(buffer, ref size, true, addressFamily, IpHelperApi.TcpTableOwnerPidAll, 0);
            if (result != 0)
                return;

            var numEntries = Marshal.ReadInt32(buffer);
            var offset = sizeof(uint);

            if (addressFamily == IpHelperApi.AfInet)
            {
                const int rowSize = 24;
                for (var i = 0; i < numEntries; i++)
                {
                    var rowPtr = IntPtr.Add(buffer, offset + i * rowSize);
                    var row = Marshal.PtrToStructure<IpHelperApi.MibTcpRowOwnerPid>(rowPtr);
                    var port = IpHelperApi.NetworkDwordToPort(row.LocalPort);
                    var addr = IpHelperApi.IPv4FromNetworkOrder(row.LocalAddr);
                    list.Add(CreateEntry("TCP", port, row.OwningPid, addr.ToString()));
                }
            }
            else
            {
                const int rowSize = 56;
                for (var i = 0; i < numEntries; i++)
                {
                    var ptr = IntPtr.Add(buffer, offset + i * rowSize);
                    var row = Marshal.PtrToStructure<IpHelperApi.MibTcp6RowOwnerPid>(ptr);
                    var port = IpHelperApi.NetworkDwordToPort(row.LocalPort);
                    var bytes = row.UcLocalAddr ?? Array.Empty<byte>();
                    if (bytes.Length != 16)
                        continue;
                    var scope = row.LocalScopeId;
                    var addr = new IPAddress(bytes, scope);
                    list.Add(CreateEntry("TCP", port, row.OwningPid, addr.ToString()));
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void AddUdpRows(List<PortEntry> list, int addressFamily)
    {
        int size = 0;
        var needSize = IpHelperApi.GetExtendedUdpTable(IntPtr.Zero, ref size, true, addressFamily, IpHelperApi.UdpTableOwnerPid, 0);
        if (needSize != 0 && needSize != ErrorInsufficientBuffer)
            return;
        if (size <= 0)
            return;

        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var result = IpHelperApi.GetExtendedUdpTable(buffer, ref size, true, addressFamily, IpHelperApi.UdpTableOwnerPid, 0);
            if (result != 0)
                return;

            var numEntries = Marshal.ReadInt32(buffer);
            var offset = sizeof(uint);

            if (addressFamily == IpHelperApi.AfInet)
            {
                const int rowSize = 12;
                for (var i = 0; i < numEntries; i++)
                {
                    var rowPtr = IntPtr.Add(buffer, offset + i * rowSize);
                    var row = Marshal.PtrToStructure<IpHelperApi.MibUdpRowOwnerPid>(rowPtr);
                    var port = IpHelperApi.NetworkDwordToPort(row.LocalPort);
                    var addr = IpHelperApi.IPv4FromNetworkOrder(row.LocalAddr);
                    list.Add(CreateEntry("UDP", port, row.OwningPid, addr.ToString()));
                }
            }
            else
            {
                const int rowSize = 28;
                for (var i = 0; i < numEntries; i++)
                {
                    var ptr = IntPtr.Add(buffer, offset + i * rowSize);
                    var row = Marshal.PtrToStructure<IpHelperApi.MibUdp6RowOwnerPid>(ptr);
                    var port = IpHelperApi.NetworkDwordToPort(row.LocalPort);
                    var bytes = row.UcLocalAddr ?? Array.Empty<byte>();
                    if (bytes.Length != 16)
                        continue;
                    var scope = row.LocalScopeId;
                    var addr = new IPAddress(bytes, scope);
                    list.Add(CreateEntry("UDP", port, row.OwningPid, addr.ToString()));
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static PortEntry CreateEntry(string protocol, ushort port, uint pid, string localAddress)
    {
        var app = ResolveProcessName(pid);
        return new PortEntry(app, (int)pid, port, protocol, localAddress);
    }

    private static string ResolveProcessName(uint pid)
    {
        if (pid == 0)
            return "(system)";

        try
        {
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch
        {
            return "(unknown)";
        }
    }
}

public sealed record PortEntry(string AppName, int Pid, ushort Port, string Protocol, string LocalAddress);
