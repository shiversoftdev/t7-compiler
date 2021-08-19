# External
Utility library for external cheats

In development, not in a working state.

This library aims to produce a utility to speed up workflow and provide additional functionality commonly needed for developing external cheats.\
It includes the following:\
  ProcessEx: An extended Process class supporting high level memory management, dll injection, thread management, and module management, along with aslr offsetting built in.\
    - Includes module lookups by name\
    - Includes Read/Write value, read/write array, read/write struct, and read/write string.\
    - Memory pattern searching\
    - Pre-imported commonly used kernel32 functions for process manipulation (along with types)\
    - Support for x64 and x86 with the same codebase (but different dlls, of course)\
  PointerEx: An extended IntPtr class supporting intrinsic casting to most value types, size_t math for respective platforms, and more.\
  ProcessModuleEx: An extension of ProcessModule, allowing base offsetting, and more.\
  ProcessThreadEx: An extension of ProcessThread, allowing suspend/resume operations, thread hijacking, and more.\
  Additional Utilities:\
    - Byte array to struct and back (extension methods)\
    - string.Bytes() -- null terminated byte array\
    - ByteArray.String() -- converts a byte array to a string (with an optional start index)\
    - string/char HexByte() -- converts a string/char into a byte if it is hex formatted\
    - char.IsHex() -- returns true if the char is within a hex digit range\
    - IntPtr.Add() extension method\
    - IntPtr.Subtract() extension method\
    \
And much, much more is planned (hopefully avoiding bloat).\
