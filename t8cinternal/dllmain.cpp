// dllmain.cpp : Defines the entry point for the DLL application.
#include "framework.h"
#include "builtins.h"
#include "detours.h"
#include "LazyLink.h"
#include <mutex>

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}


EXPORT void T8Dll_BuiltinsInit()
{
    static std::once_flag flag;
    std::call_once(flag, GSCBuiltins::Init);
}

EXPORT void T8Dll_DetoursInit()
{
    static std::once_flag flag;
    std::call_once(flag, ScriptDetours::InstallHooks);
}

EXPORT void T8Dll_LazyLinkInit()
{
    static std::once_flag flag;
    std::call_once(flag, LazyLink::Init);
}
