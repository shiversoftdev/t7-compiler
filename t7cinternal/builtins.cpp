#include "builtins.h"
#include "offsets.h"
#include "detours.h"

std::unordered_map<int, void*> GSCBuiltins::CustomFunctions;
tScrVm_GetString GSCBuiltins::ScrVm_GetString;
tScrVm_GetInt GSCBuiltins::ScrVm_GetInt;

// add all custom builtins here
void GSCBuiltins::Generate()
{
	// Compiler related functions //

	// compiler::detour()
	// Link and execute detours included in loaded scripts.
	AddCustomFunction("detour", GSCBuiltins::GScr_detour);
	
	// compiler::relinkdetours()
	// Re-link any detours that did not get linked previously due to script load order, etc.
	AddCustomFunction("relinkdetours", GSCBuiltins::GScr_relinkDetours);

	// General purpose //
	
	// compiler::livesplit(str_split_name);
	// Send a split signal to livesplit through named pipe access.
	// <str_split_name>: Name of the split to send to livesplit
	AddCustomFunction("livesplit", GSCBuiltins::GScr_livesplit);

	// compiler::nprintln(str_message)
	// Prints a line of text to an open, untitled notepad window.
	// <str_message>: Text to print
	AddCustomFunction("nprintln", GSCBuiltins::GScr_nprintln);

	// compiler::erasefunc(str_script, int_namespace, int_function);
	// Replaces a function in a given script with OP_END
	// str_script: script affected, ex: "scripts/my/script.gsc"
	// int_namespace: fnv hash of the namespace the function is in
	// int_function: fnv hash of the function to replace
	AddCustomFunction("erasefunc", GSCBuiltins::GScr_erasefunc);

	//// compiler::setmempoolsize(int_size);
	//// Resizes the script memory pool. Note: default size is 10MB. Increasing this too much can cause performance issues and break the vm. Use this carefully.
	//// int_size: size in bytes to set the memory pool. Will get clamped to be at least the default size, and will always be a multiple of 0x40.
	//AddCustomFunction("setmempoolsize", GSCBuiltins::GScr_setmempool);

	AddCustomFunction("enableonlinematch", GSCBuiltins::GScr_enableonlinematch);
}

void GSCBuiltins::Init()
{
	GSCBuiltins::Generate();
	auto builtinFunction = (BuiltinFunctionDef*)OFF_IsProfileBuild;
	builtinFunction->max_args = 255;
	builtinFunction->actionFunc = GSCBuiltins::Exec;

	ScrVm_GetString = (tScrVm_GetString)OFF_ScrVm_GetString;
	ScrVm_GetInt = (tScrVm_GetInt)OFF_ScrVm_GetInt;
}

void GSCBuiltins::AddCustomFunction(const char* name, void* funcPtr)
{
	CustomFunctions[fnv1a(name)] = funcPtr;
}

void GSCBuiltins::Exec(int scriptInst)
{
	INT32 func = ScrVm_GetInt(scriptInst, 0);
	if (CustomFunctions.find(func) == CustomFunctions.end())
	{
		// unknown builtin
		nlog("unknown builtin %h", func);
		return;
	}
	reinterpret_cast<void(__fastcall*)(int)>(CustomFunctions[func])(scriptInst);
}

// START OF BUILTIN DEFINITIONS

/*
	prints a line to an open notepad window
	nprintln(whatToPrint);
*/
void GSCBuiltins::GScr_nprintln(int scriptInst)
{
	// note: we use 1 as our param index because custom builtin params start at 1. The first param (0) is always the name of the function called.
	// we also use %s to prevent a string format vulnerability!
	nlog("%s", ScrVm_GetString(0, 1));
}

void GSCBuiltins::GScr_detour(int scriptInst)
{
	if (scriptInst)
	{
		return;
	}
	ScriptDetours::DetoursEnabled = true;
}

void GSCBuiltins::GScr_relinkDetours(int scriptInst)
{
	if (scriptInst)
	{
		return;
	}
	ScriptDetours::LinkDetours();
}

void GSCBuiltins::GScr_livesplit(int scriptInst)
{
	if (scriptInst)
	{
		return;
	}

	HANDLE livesplit = CreateFile("\\\\.\\pipe\\LiveSplit", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
	if (!livesplit)
	{
		return;
	}

	const char* message = ScrVm_GetString(0, 1);
	WriteFile(livesplit, message, strlen(message), nullptr, NULL);
	CloseHandle(livesplit);
}

// str_file, int_namespace, int_func
void GSCBuiltins::GScr_erasefunc(int scriptInst)
{
	char* str_file = ScrVm_GetString(0, 1);
	int n_namespace = ScrVm_GetInt(0, 2);
	int n_func = ScrVm_GetInt(0, 3);

	if (!str_file || !n_namespace || !n_func)
	{
		return; // bad inputs
	}

	auto asset = ScriptDetours::FindScriptParsetree(str_file);
	if (!asset)
	{
		return; // couldn't find asset, quit
	}
	auto buffer = *(char**)(asset + 0x10);

	if (!buffer)
	{
		return; // buffer doesnt exist
	}

	auto exportsOffset = *(INT32*)(buffer + 0x20);
	auto exports = (INT64)(exportsOffset + buffer);
	auto numExports = *(INT16*)(buffer + 0x3A);
	__t7export* currentExport = (__t7export*)exports;
	bool b_found = false;
	for (INT16 i = 0; i < numExports; i++, currentExport++)
	{
		if (currentExport->funcName != n_func)
		{
			continue;
		}
		if (currentExport->funcNS != n_namespace)
		{
			continue;
		}
		b_found = true;
		break;
	}

	if (!b_found)
	{
		return; // couldnt find the function
	}

	INT32 target = currentExport->bytecodeOffset;
	char* fPos = buffer + currentExport->bytecodeOffset;

	b_found = false;
	currentExport = (__t7export*)exports;
	__t7export* lowest = NULL;
	for (INT16 i = 0; i < numExports; i++, currentExport++)
	{
		if (currentExport->bytecodeOffset <= target)
		{
			continue;
		}
		if (!lowest || (currentExport->bytecodeOffset < lowest->bytecodeOffset))
		{
			lowest = currentExport;
			b_found = true;
		}
	}

	// dont erase prologue

	auto code = *(UINT16*)fPos;

	if (code == 0xD || code == 0x200D) // CheckClearParams
	{
		fPos += 2;
	}
	else
	{
		fPos += 2;
		BYTE numParams = *(BYTE*)fPos;
		fPos += 2;
		for (BYTE i = 0; i < numParams; i++)
		{
			fPos = (char*)((INT64)fPos + 3 & 0xFFFFFFFFFFFFFFFCLL) + 4;
			fPos += 1; // type
		}
		if ((INT64)fPos & 1)
		{
			fPos++;
		}
	}

	char* fStart = fPos;
	char* fEnd = b_found ? (lowest->bytecodeOffset + buffer) : (fStart + 2); // cant erase entire functions if we dont know the end

	while (fStart < fEnd)
	{
		*(UINT16*)fStart = 0x10; // OP_END
		fStart += 2;
	}


}

// type_free = 27 (0x1B)
// (void*)(51A5500 + 128 * (inst << 8)) = malloc
// memset to 0
// vars are 0x40, stock has 130,000 of them
// copy the original memory
// change the pointer to the malloc'd memory
// change final one to not be null
// initialize all new variables
// change final entry in list to have 0 for index (+0x18) and type (0x8)??

#define OFF_ScrVarGlob OFFSET(0x51A5500)
#define MEM_SCRVAR_COUNT 130000
#define MEM_SCRVAR_SPACE (0x40 * MEM_SCRVAR_COUNT)

char* newVarMemPool = NULL;
void GSCBuiltins::GScr_setmempool(int scriptInst)
{
	UINT64* llpScrVarMemPool = (UINT64*)((char*)OFF_ScrVarGlob + 128 + (scriptInst << 8));
	int numBytes = ScrVm_GetInt(0, 1);

	if (numBytes % 0x40)
	{
		numBytes += 0x40 - (numBytes % 0x40);
	}

	if (numBytes < MEM_SCRVAR_SPACE)
	{
		numBytes = MEM_SCRVAR_SPACE;
	}

	void* oldPool = newVarMemPool;
	newVarMemPool = (char*)_aligned_malloc(numBytes, 128);

	if (newVarMemPool <= 0)
	{
		return;
	}

	memset(newVarMemPool, 0, numBytes);
	memcpy(newVarMemPool, (void*)*llpScrVarMemPool, MEM_SCRVAR_SPACE);

	char* lastRef = (char*)(newVarMemPool + (0x40 * (MEM_SCRVAR_COUNT - 1)));
	*(DWORD*)(lastRef + 0x8) = 27; // type
	*(DWORD*)(lastRef + 0x18) = MEM_SCRVAR_COUNT - 1; // index

	int newCount = (numBytes / 0x40);
	char* currentRef = 0;
	for (int i = MEM_SCRVAR_COUNT; i < newCount; i++)
	{
		currentRef = (newVarMemPool + (0x40 * i));
		*(DWORD*)(currentRef + 0x8) = 27; // type
		*(DWORD*)(currentRef + 0x18) = i; // index
	}

	// clear last variable so the vm knows where to stop
	currentRef = (newVarMemPool + (0x40 * (newCount - 1)));
	*(DWORD*)(currentRef + 0x8) = 0; // type
	*(DWORD*)(currentRef + 0x18) = 0; // index

	*llpScrVarMemPool = (INT64)newVarMemPool;

	if (oldPool)
	{
		free(oldPool);
	}
}

void GSCBuiltins::GScr_enableonlinematch(int scriptInst)
{
	*(int32_t*)PTR_sSessionModeState = (*(int32_t*)PTR_sSessionModeState & ~(1 << 14));
}

void GSCBuiltins::nlog(const char* str, ...)
{
	va_list ap;
	HWND notepad, edit;
	char buf[256];

	va_start(ap, str);
	vsprintf(buf, str, ap);
	va_end(ap);
	strcat_s(buf, 256, "\r\n");
	notepad = FindWindow(NULL, "Untitled - Notepad");
	if (!notepad)
	{
		notepad = FindWindow(NULL, "*Untitled - Notepad");
	}
	edit = FindWindowEx(notepad, NULL, "EDIT", NULL);
	SendMessage(edit, EM_REPLACESEL, TRUE, (LPARAM)buf);
}