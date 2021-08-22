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
        #region DSTRINGS
        /// <summary>
        /// Module '{0}' does not contain export '{1}'
        /// </summary>
        internal static readonly long DSTR_MODULE_EXPORT_NOT_FOUND = 0;
        /// <summary>
        /// OS Major Version must be >= 10
        /// </summary>
        internal static readonly long DSTR_OS_VERSION_TOO_OLD = 1;
        /// <summary>
        /// Failed to read imported DLL name.
        /// </summary>
        internal static readonly long DSTR_MODULE_NAME_INVALID = 2;
        /// <summary>
        /// {0}, unable to find the specified file.
        /// </summary>
        internal static readonly long DSTR_MODULE_FILE_NOT_FOUND = 3;
        /// <summary>
        /// Failed to call DllMain -> DLL_PROCESS_ATTACH
        /// </summary>
        internal static readonly long DSTR_DINVOKE_MAIN_FAILED = 4;
        /// <summary>
        /// modulePath cannot be null.
        /// </summary>
        internal static readonly long DSTR_DINVOKE_MOD_CANNOT_BE_NULL = 5;
        /// <summary>
        /// The module architecture does not match the process architecture.
        /// </summary>
        internal static readonly long DSTR_MOD_ARCHITECTURE_WRONG = 6;
        /// <summary>
        /// Failed to parse module exports.
        /// </summary>
        internal static readonly long DSTR_MOD_EXPORTS_BAD = 7;
        /// <summary>
        /// {0}, export not found.
        /// </summary>
        internal static readonly long DSTR_EXPORT_NOT_FOUND = 8;
        /// <summary>
        /// Failed to write to memory.
        /// </summary>
        internal static readonly long DSTR_FAILED_MEMORY_WRITE = 9;
        /// <summary>
        /// Memory access violation.
        /// </summary>
        internal static readonly long DSTR_MEM_ACCESS_VIOLATION = 10;
        /// <summary>
        /// api dll was not resolved ({0})
        /// </summary>
        internal static readonly long DSTR_API_DLL_UNRESOLVED = 11;
        /// <summary>
        /// Unknown section flag, {0}
        /// </summary>
        internal static readonly long DSTR_UNK_SEC_FLAG = 12;
        /// <summary>
        /// Unable to execute a 64 bit function without RAXStor
        /// </summary>
        internal static readonly long DSTR_RAXSTOR_MISSING = 13;
        /// <summary>
        /// Access is denied.
        /// </summary>
        internal static readonly long DSTR_ACCESS_DENIED = 14;
        /// <summary>
        /// The specified address range is already committed.
        /// </summary>
        internal static readonly long DSTR_ALREADY_COMMITTED = 15;
        /// <summary>
        /// Your system is low on virtual memory.
        /// </summary>
        internal static readonly long DSTR_LOW_ON_VMEM = 16;
        /// <summary>
        /// The specified address range conflicts with the address space.
        /// </summary>
        internal static readonly long DSTR_CONFLICTING_ADDRESS = 17;
        /// <summary>
        /// Insufficient system resources exist to complete the API call.
        /// </summary>
        internal static readonly long DSTR_INSUFFICIENT_RESOURCES = 18;
        /// <summary>
        /// Invalid handle
        /// </summary>
        internal static readonly long DSTR_INVALID_HANDLE = 19;
        /// <summary>
        /// The specified page protection was not valid.
        /// </summary>
        internal static readonly long DSTR_INVALID_PAGE_PROTECT = 20;
        /// <summary>
        /// Object type mismatch
        /// </summary>
        internal static readonly long DSTR_OBJECT_TYPE_MISMATCH = 21;
        /// <summary>
        /// An attempt was made to duplicate an object handle into or out of an exiting process.
        /// </summary>
        internal static readonly long DSTR_PROC_EXITING = 22;
        /// <summary>
        /// Failed get procedure address, {0}
        /// </summary>
        internal static readonly long DSTR_PROC_ADDRESS_LOOKUP_FAILED = 23;
        /// <summary>
        /// Invalid ProcessInfoClass {0}
        /// </summary>
        internal static readonly long DSTR_INVALID_PROCINFOCLASS = 24;
        /// <summary>
        /// Failed to change memory protection, {0}
        /// </summary>
        internal static readonly long DSTR_FAILED_MEMPROTECT = 25;
        /// <summary>
        /// Destination buffer size is too small
        /// </summary>
        internal static readonly long DSTR_BUFFER_TOO_SMALL = 26;
        /// <summary>
        /// Cannot cast data of length {0} to a pointer of size {1}
        /// </summary>
        internal static readonly long DSTR_PTR_CAST_FAIL = 27;
        /// <summary>
        /// Target process cannot be null
        /// </summary>
        internal static readonly long DSTR_TARG_PROC_NULL = 28;
        /// <summary>
        /// Tried to read from a memory region when a handle to the desired process doesn't exist
        /// </summary>
        internal static readonly long DSTR_READ_MISSING_HANDLE = 29;
        /// <summary>
        /// Type {0} is not a valid value type
        /// </summary>
        internal static readonly long DSTR_INVALID_VALUETYPE = 30;
        /// <summary>
        /// Tried to write to a memory region when a handle to the desired process doesn't exist
        /// </summary>
        internal static readonly long DSTR_WRITE_MISSING_HANDLE = 31;
        /// <summary>
        /// Failed to read data of size {0} from address 0x{1}
        /// </summary>
        internal static readonly long DSTR_FAILED_READFROM = 32;
        /// <summary>
        /// Failed to write {0} bytes to region 0x{1}
        /// </summary>
        internal static readonly long DSTR_FAILED_WRITETO = 33;
        /// <summary>
        /// Cannot inject a dll to a process which has exited
        /// </summary>
        internal static readonly long DSTR_INJECT_DEAD_PROC = 34;
        /// <summary>
        /// Cannot inject an empty dll
        /// </summary>
        internal static readonly long DSTR_INJECT_EMPTY_DLL = 35;
        /// <summary>
        /// Failed to load an essential module at '{0}'
        /// </summary>
        internal static readonly long DSTR_FAILED_LOAD_MODULE = 36;
        /// <summary>
        /// Cannot cast type [{0}] to a serializable type for RPC returns
        /// </summary>
        internal static readonly long DSTR_CAST_SERIALIZE_FAILED = 37;
        /// <summary>
        /// Cannot invoke rpc of type {0} because the rpc type has not been initialized.
        /// </summary>
        internal static readonly long DSTR_RPC_INITIALIZED = 38;
        /// <summary>
        /// Process exited unexpectedly...
        /// </summary>
        internal static readonly long DSTR_PROC_EXITED = 39;
        /// <summary>
        /// Unable to find a thread which can be hijacked.
        /// </summary>
        internal static readonly long DSTR_FIND_THREAD_HIJACK = 40;
        /// <summary>
        /// Unable to open target thread for RPC...
        /// </summary>
        internal static readonly long DSTR_OPEN_THREAD_FAILED = 41;
        /// <summary>
        /// Unable to get a thread context for the target process RPC.
        /// </summary>
        internal static readonly long DSTR_THREAD_CTX_FAILED = 42;
        /// <summary>
        /// Function call timed out
        /// </summary>
        internal static readonly long DSTR_RPC_TIMEOUT = 43;
        /// <summary>
        /// Tried to allocate a memory region when a handle to the desired process doesn't exist
        /// </summary>
        internal static readonly long DSTR_ALLOC_NO_HANDLE = 44;
        /// <summary>
        /// Tried to create a thread in a process which has no open handle!
        /// </summary>
        internal static readonly long DSTR_THREAD_NO_HANDLE = 45;
        /// <summary>
        /// Calltype {0} not implemented
        /// </summary>
        internal static readonly long DSTR_CALLTYPE_NOT_IMPLEMENTED = 46;
        /// <summary>
        /// Cannot cast type [{0}] to a serializable type for RPC. If this was an array, convert it to a byte array first.
        /// </summary>
        internal static readonly long DSTR_SERIALIZE_TYPE_INVALID = 47;
        /// <summary>
        /// Unhandled argument type for serialization: {0}
        /// </summary>
        internal static readonly long DSTR_UNHANDLED_ARG_RPC = 48;
        #endregion
        private static readonly PointerEx APISetMapAddress;
        static EnvironmentEx()
        {
            if (Environment.Is64BitProcess) APISetMapAddress = Marshal.PtrToStructure<Peb64>(RtlGetCurrentPeb()).APISetMap;
            else APISetMapAddress = Marshal.PtrToStructure<Peb32>(RtlGetCurrentPeb()).APISetMap;
            #region DSTRINGS
            #if DEBUG
            DSTR_MODULE_EXPORT_NOT_FOUND = CreateDebugString("Module '{0}' does not contain export '{1}'");
            DSTR_OS_VERSION_TOO_OLD = CreateDebugString("OS Major Version must be >= 10");
            DSTR_MODULE_NAME_INVALID = CreateDebugString("Failed to read imported DLL name.");
            DSTR_MODULE_FILE_NOT_FOUND = CreateDebugString("{0}, unable to find the specified file.");
            DSTR_DINVOKE_MAIN_FAILED = CreateDebugString("Failed to call DllMain -> DLL_PROCESS_ATTACH");
            DSTR_DINVOKE_MOD_CANNOT_BE_NULL = CreateDebugString("modulePath cannot be null.");
            DSTR_MOD_ARCHITECTURE_WRONG = CreateDebugString("The module architecture does not match the process architecture.");
            DSTR_MOD_EXPORTS_BAD = CreateDebugString("Failed to parse module exports.");
            DSTR_EXPORT_NOT_FOUND = CreateDebugString("{0}, export not found.");
            DSTR_FAILED_MEMORY_WRITE = CreateDebugString("Failed to write to memory.");
            DSTR_MEM_ACCESS_VIOLATION = CreateDebugString("Memory access violation.");
            DSTR_API_DLL_UNRESOLVED = CreateDebugString("api dll was not resolved ({0})");
            DSTR_UNK_SEC_FLAG = CreateDebugString("Unknown section flag, {0}");
            DSTR_RAXSTOR_MISSING = CreateDebugString("Unable to execute a 64 bit function without RAXStor");
            DSTR_ACCESS_DENIED = CreateDebugString("Access is denied.");
            DSTR_ALREADY_COMMITTED = CreateDebugString("The specified address range is already committed.");
            DSTR_LOW_ON_VMEM = CreateDebugString("Your system is low on virtual memory.");
            DSTR_CONFLICTING_ADDRESS = CreateDebugString("The specified address range conflicts with the address space.");
            DSTR_INSUFFICIENT_RESOURCES = CreateDebugString("Insufficient system resources exist to complete the API call.");
            DSTR_INVALID_HANDLE = CreateDebugString("Invalid handle");
            DSTR_INVALID_PAGE_PROTECT = CreateDebugString("The specified page protection was not valid.");
            DSTR_OBJECT_TYPE_MISMATCH = CreateDebugString("Object type mismatch");
            DSTR_PROC_EXITING = CreateDebugString("An attempt was made to duplicate an object handle into or out of an exiting process.");
            DSTR_PROC_ADDRESS_LOOKUP_FAILED = CreateDebugString("Failed get procedure address, {0}");
            DSTR_INVALID_PROCINFOCLASS = CreateDebugString("Invalid ProcessInfoClass {0}");
            DSTR_FAILED_MEMPROTECT = CreateDebugString("Failed to change memory protection, {0}");
            DSTR_BUFFER_TOO_SMALL = CreateDebugString("Destination buffer size is too small");
            DSTR_PTR_CAST_FAIL = CreateDebugString("Cannot cast data of length {0} to a pointer of size {1}");
            DSTR_TARG_PROC_NULL = CreateDebugString("Target process cannot be null");
            DSTR_READ_MISSING_HANDLE = CreateDebugString("Tried to read from a memory region when a handle to the desired process doesn't exist");
            DSTR_INVALID_VALUETYPE = CreateDebugString("Type {0} is not a valid value type");
            DSTR_WRITE_MISSING_HANDLE = CreateDebugString("Tried to write to a memory region when a handle to the desired process doesn't exist");
            DSTR_FAILED_READFROM = CreateDebugString("Failed to read data of size {0} from address 0x{1}");
            DSTR_FAILED_WRITETO = CreateDebugString("Failed to write {0} bytes to region 0x{1}");
            DSTR_INJECT_DEAD_PROC = CreateDebugString("Cannot inject a dll to a process which has exited");
            DSTR_INJECT_EMPTY_DLL = CreateDebugString("DSTR_INJECT_EMPTY_DLL");
            DSTR_FAILED_LOAD_MODULE = CreateDebugString("Failed to load an essential module at '{0}'");
            DSTR_CAST_SERIALIZE_FAILED = CreateDebugString("Cannot cast type [{0}] to a serializable type for RPC returns");
            DSTR_RPC_INITIALIZED = CreateDebugString("Cannot invoke rpc of type {0} because the rpc type has not been initialized.");
            DSTR_PROC_EXITED = CreateDebugString("Process exited unexpectedly...");
            DSTR_FIND_THREAD_HIJACK = CreateDebugString("Unable to find a thread which can be hijacked.");
            DSTR_OPEN_THREAD_FAILED = CreateDebugString("Unable to open target thread for RPC...");
            DSTR_THREAD_CTX_FAILED = CreateDebugString("Unable to get a thread context for the target process RPC.");
            DSTR_RPC_TIMEOUT = CreateDebugString("Function call timed out");
            DSTR_ALLOC_NO_HANDLE = CreateDebugString("Tried to allocate a memory region when a handle to the desired process doesn't exist");
            DSTR_THREAD_NO_HANDLE = CreateDebugString("Tried to create a thread in a process which has no open handle!");
            DSTR_CALLTYPE_NOT_IMPLEMENTED = CreateDebugString("Calltype {0} not implemented");
            DSTR_SERIALIZE_TYPE_INVALID = CreateDebugString("Cannot cast type [{0}] to a serializable type for RPC. If this was an array, convert it to a byte array first.");
            DSTR_UNHANDLED_ARG_RPC = CreateDebugString("Unhandled argument type for serialization: {0}");
            #endif
            #endregion
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

        /// <summary>
        /// A debug only dictionary for strings which should not be populated in a release build
        /// </summary>
        private static readonly Dictionary<long, string> DebugStrings = new Dictionary<long, string>();

        #if DEBUG
        public delegate void DebugMessageListener(string message);
        public static DebugMessageListener DebugLogListenCB;
        private static long __dstring_index = 0;

        /// <summary>
        /// Log a message to the debug logging system for external. Only active in debug builds. Assign a listener delegate to DebugLogListenCB to subscribe to message text, or logs will not be emitted.
        /// </summary>
        /// <param name="message"></param>
        public static void DLog(string message)
        {
            message = $"[{DateTime.Now.ToLongDateString()} - {DateTime.Now.ToLongTimeString()}] " + message + "\n";
            DebugLogListenCB?.Invoke(message);
        }

        /// <summary>
        /// Add a debug string to the internal registry for queries. Only exists in a debug environment
        /// </summary>
        /// <param name="debugStringText"></param>
        /// <returns></returns>
        public static long CreateDebugString(string debugStringText)
        {
            DebugStrings[__dstring_index++] = debugStringText;
            return __dstring_index - 1;
        }
        #endif

        /// <summary>
        /// Query the debug strings table for some text. Release builds will use an error code
        /// </summary>
        /// <param name="textID"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string DSTR(long textID, params object[] formatArgs)
        {
        #if DEBUG
            if(DebugStrings.TryGetValue(textID, out string retV))
            {
                return string.Format(retV, formatArgs);
            }
        #endif
            return textID.ToString();
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

        public enum FreeType
        {
            Release = 0x8000
        }
        #endregion
    }
}
