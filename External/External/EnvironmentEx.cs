using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.PEStructures;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class EnvironmentEx
    {
        private static readonly PointerEx APISetMapAddress;
        static EnvironmentEx()
        {
            if (Environment.Is64BitProcess) APISetMapAddress = Marshal.PtrToStructure<Peb64>(RtlGetCurrentPeb()).APISetMap;
            else APISetMapAddress = Marshal.PtrToStructure<Peb32>(RtlGetCurrentPeb()).APISetMap;
        }
        #region methods
        public static string ResolveAPISet(string apiSetName)
        {
            var @namespace = Marshal.PtrToStructure<ApiSetNamespace>(APISetMapAddress);

            // Create a hash for the API set name, skipping the patch number and suffix
            var charactersToHash = apiSetName.Substring(0, apiSetName.LastIndexOf("-", StringComparison.Ordinal));
            var apiSetNameHash = charactersToHash.Aggregate(0, (currentHash, character) => currentHash * @namespace.HashFactor + char.ToLower(character));

            // Search the namespace for the corresponding hash entry
            var low = 0;
            var high = @namespace.Count - 1;
            while (low <= high)
            {
                var middle = (low + high) / 2;

                // Read the hash entry
                var hashEntryAddress = APISetMapAddress + @namespace.HashOffset + Unsafe.SizeOf<ApiSetHashEntry>() * middle;
                var hashEntry = Marshal.PtrToStructure<ApiSetHashEntry>(hashEntryAddress);
                if (apiSetNameHash == hashEntry.Hash)
                {
                    // Read the namespace entry
                    var namespaceEntryAddress = APISetMapAddress + @namespace.EntryOffset + Unsafe.SizeOf<ApiSetNamespaceEntry>() * hashEntry.Index;
                    var namespaceEntry = Marshal.PtrToStructure<ApiSetNamespaceEntry>(namespaceEntryAddress);

                    // Read the first value entry that the namespace entry maps to
                    var valueEntryAddress = APISetMapAddress + namespaceEntry.ValueOffset;
                    var valueEntry = Marshal.PtrToStructure<ApiSetValueEntry>(valueEntryAddress);

                    // Read the value entry name
                    var valueEntryNameAddress = APISetMapAddress + valueEntry.ValueOffset;
                    var valueEntryName = Marshal.PtrToStringUni(valueEntryNameAddress, valueEntry.ValueCount / sizeof(char));
                    return valueEntryName;
                }

                if ((uint)apiSetNameHash < (uint)hashEntry.Hash) high = middle - 1;
                else low = middle + 1;
            }
            return null;
        }
        #endregion
        #region pinvoke
        [DllImport("ntdll.dll")]
        public static extern int NtCreateThreadEx(out SafeWaitHandle threadHandle, AccessMask accessMask, PointerEx objectAttributes, PointerEx processHandle, PointerEx startAddress, PointerEx argument, ThreadCreationFlags flags, PointerEx zeroBits, PointerEx stackSize, PointerEx maximumStackSize, IntPtr attributeList);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(SafeProcessHandle processHandle, ProcessInformationType informationType, out byte information, int informationSize, out int returnLength);

        [DllImport("ntdll.dll")]
        public static extern PointerEx RtlGetCurrentPeb();

        [DllImport("ntdll.dll")]
        public static extern int RtlNtStatusToDosError(int status);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int WaitForSingleObject(SafeWaitHandle waitHandle, int milliseconds);
        #endregion
        #region typedef

        [Flags]
        public enum AccessMask
        {
            SpecificRightsAll = 0xFFFF,
            StandardRightsAll = 0x1F0000
        }
        [Flags]
        public enum ThreadCreationFlags
        {
            SkipThreadAttach = 0x2,
            HideFromDebugger = 0x4
        }
        public enum ProcessInformationType
        {
            BasicInformation = 0x0,
            Wow64Information = 0x1A
        }
        [StructLayout(LayoutKind.Explicit, Size = 28)]
        internal readonly struct ApiSetNamespace
        {
            [FieldOffset(0xC)]
            internal readonly int Count;

            [FieldOffset(0x10)]
            internal readonly int EntryOffset;

            [FieldOffset(0x14)]
            internal readonly int HashOffset;

            [FieldOffset(0x18)]
            internal readonly int HashFactor;
        }
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        internal readonly struct ApiSetHashEntry
        {
            [FieldOffset(0x0)]
            internal readonly int Hash;

            [FieldOffset(0x4)]
            internal readonly int Index;
        }
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        internal readonly struct ApiSetNamespaceEntry
        {
            [FieldOffset(0x10)]
            internal readonly int ValueOffset;
        }
        [StructLayout(LayoutKind.Explicit, Size = 20)]
        internal readonly struct ApiSetValueEntry
        {
            [FieldOffset(0xC)]
            internal readonly int ValueOffset;

            [FieldOffset(0x10)]
            internal readonly int ValueCount;
        }

        internal enum FreeType
        {
            Release = 0x8000
        }
        #endregion
    }
}
