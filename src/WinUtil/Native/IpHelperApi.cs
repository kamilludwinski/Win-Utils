using System.Net;
using System.Runtime.InteropServices;

namespace WinUtil.Native;

internal static class IpHelperApi
{
    public const int AfInet = 2;
    public const int AfInet6 = 23;

    public const uint TcpTableOwnerPidAll = 5;
    public const uint UdpTableOwnerPid = 1;

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwSize,
        bool bOrder,
        int ulAf,
        uint tableClass,
        uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int dwSize,
        bool bOrder,
        int ulAf,
        uint tableClass,
        uint reserved);

    [StructLayout(LayoutKind.Sequential)]
    public struct MibTcpRowOwnerPid
    {
        public uint State;
        public uint LocalAddr;
        public uint LocalPort;
        public uint RemoteAddr;
        public uint RemotePort;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MibTcp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] UcLocalAddr;

        public uint LocalScopeId;
        public uint LocalPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] UcRemoteAddr;

        public uint RemoteScopeId;
        public uint RemotePort;
        public uint State;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MibUdpRowOwnerPid
    {
        public uint LocalAddr;
        public uint LocalPort;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MibUdp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] UcLocalAddr;

        public uint LocalScopeId;
        public uint LocalPort;
        public uint OwningPid;
    }

    public static ushort NetworkDwordToPort(uint dwPort)
    {
        return (ushort)(((dwPort >> 8) & 0xFF) | ((dwPort & 0xFF) << 8));
    }

    public static IPAddress IPv4FromNetworkOrder(uint dwLocalAddr)
    {
        var host = (uint)IPAddress.NetworkToHostOrder(unchecked((int)dwLocalAddr));
        return new IPAddress(host);
    }
}
