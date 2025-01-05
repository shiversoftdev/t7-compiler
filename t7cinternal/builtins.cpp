#include "builtins.h"
#include "offsets.h"
#include "detours.h"

std::unordered_map<int, void*> GSCBuiltins::CustomFunctions;
tScrVm_GetString GSCBuiltins::ScrVm_GetString;
tScrVm_GetInt GSCBuiltins::ScrVm_GetInt;
tScrVar_AllocVariableInternal GSCBuiltins::ScrVar_AllocVariableInternal;
tScrVm_GetFunc GSCBuiltins::ScrVm_GetFunc;

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

	AddCustomFunction("patchbyte", GSCBuiltins::GScr_patchbyte);
	AddCustomFunction("setmempoolsize", GSCBuiltins::GScr_setmempool);
	AddCustomFunction("debugallocvariables", GSCBuiltins::GScr_debugallocvariables);
	AddCustomFunction("script_detour", GSCBuiltins::GScr_runtimedetour);

	// compiler::erasefunc(str_script, int_namespace, int_function);
	// Replaces a function in a given script with OP_END
	// str_script: script affected, ex: "scripts/my/script.gsc"
	// int_namespace: fnv hash of the namespace the function is in
	// int_function: fnv hash of the function to replace
	AddCustomFunction("erasefunc", GSCBuiltins::GScr_erasefunc);

	AddCustomFunction("abort", GSCBuiltins::GScr_abort);
	AddCustomFunction("catch_exit", GSCBuiltins::GScr_catch_exit);

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

	builtinFunction = (BuiltinFunctionDef*)OFF_BID_Scr_CastInt;
	builtinFunction->actionFunc = GSCBuiltins::Scr_CastInt_Wrapper;

	ScrVm_GetString = (tScrVm_GetString)OFF_ScrVm_GetString;
	ScrVm_GetInt = (tScrVm_GetInt)OFF_ScrVm_GetInt;
	ScrVar_AllocVariableInternal = (tScrVar_AllocVariableInternal)OFF_ScrVar_AllocVariableInternal;
	ScrVm_GetFunc = (tScrVm_GetFunc)OFF_ScrVm_GetFunc;
}

void GSCBuiltins::AddCustomFunction(const char* name, void* funcPtr)
{
	CustomFunctions[fnv1a(name)] = funcPtr;
}

EXPORT void AddCustomFunction(const char* name, void* funcPtr)
{
	GSCBuiltins::AddCustomFunction(name, funcPtr);
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

void Scr_Error(uint32_t inst, const char* error, uint8_t force_terminal)
{
	if (IS_WINSTORE)
	{
		((void(__fastcall*)(uint32_t, const char*))REBASE(NULL, 0x1392DF0))(inst, error); // Scr_SetErrorMessage
		*((uint8_t*)REBASE(NULL, 0x3F66B50) + 0x8A40llu * inst + 43) = force_terminal;
		((void(__fastcall*)(uint32_t))REBASE(NULL, 0x138E030))(inst); // Scr_ErrorInternal (__noreturn btw)
		return;
	}
	((void(__fastcall*)(uint32_t, const char*, uint32_t))REBASE(0x12EA430, NULL))(inst, error, force_terminal);
}

uint32_t Scr_GetType(uint32_t inst, uint32_t index)
{
	static char err_buff[256]{ 0 };

	signed int v2; // ebx
	uint64_t v3; // rax
	const char* v5; // rax

	v3 = 0x8A40llu * inst;
	if (index < *(uint32_t*)(REBASE(0x51A3840, 0x3F66B50) + v3 + 56))
		return *(uint32_t*)(*(uint64_t*)(REBASE(0x51A3840, 0x3F66B50) + v3 + 32) - 16llu * index + 8);

	sprintf_s(err_buff, "parameter %d does not exist", index + 1);
	Scr_Error(inst, err_buff, false);
	return 0;
}

void Scr_AddInt(int scriptInst, uint32_t val)
{
	if (IS_WINSTORE)
	{
		// note: this is SO WEIRD!!! they inlined Scr_AddInt but NOT IncInParam, whereas steam doesnt inline Scr_AddInt but DOES inline IncInParam... wtf??
		((void(__fastcall*)(uint32_t))REBASE(NULL, 0x1390370))(scriptInst); // IncInParam
		*((uint32_t*)(*(uint64_t*)REBASE(NULL, 0x3F66B70)) + 2) = 7;
		*(uint32_t*)(*(uint64_t*)REBASE(NULL, 0x3F66B70)) = val;
		return;
	}
	((void(__fastcall*)(int, __int32))REBASE(0x12E9870, NULL))(scriptInst, val); // Scr_AddInt
}

void GSCBuiltins::Scr_CastInt_Wrapper(int scriptInst)
{
	auto type = Scr_GetType(scriptInst, 0);

	if (type == 5) // hash
	{
		Scr_AddInt(scriptInst, (__int32)ScrVm_GetInt(scriptInst, 0));
		return;
	}

	((void(__fastcall*)(int))OFF_Scr_CastInt)(scriptInst); // scr_castint
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

// str_path, n_offset, char_value
void GSCBuiltins::GScr_patchbyte(int scriptInst)
{
	char* str_file = ScrVm_GetString(0, 1);
	int n_offset = ScrVm_GetInt(0, 2);
	int n_value = ScrVm_GetInt(0, 3);

	if (!str_file || !n_offset)
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

	*(BYTE*)(buffer + n_offset) = (BYTE)n_value;
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

#define MEM_SCRVAR_COUNT 130000
#define MEM_SCRVAR_CSC_COUNT 65000
#define MEM_SCRVAR_SPACE(inst) (sizeof(ScrVar_t) * (inst ? MEM_SCRVAR_CSC_COUNT : MEM_SCRVAR_COUNT))

char* newVarMemPool = NULL;
void GSCBuiltins::GScr_setmempool(int scriptInst)
{
	UINT64* llpScrVarMemPool = (UINT64*)((char*)OFF_ScrVarGlob + 128 + (scriptInst << 8));
	if (*llpScrVarMemPool == (UINT64)newVarMemPool)
	{
		return; // already allocated
	}

	int numBytes = ScrVm_GetInt(scriptInst, 1);

	if (numBytes % sizeof(ScrVar_t))
	{
		numBytes += sizeof(ScrVar_t) - (numBytes % sizeof(ScrVar_t));
	}

	if (numBytes < MEM_SCRVAR_SPACE(scriptInst))
	{
		numBytes = MEM_SCRVAR_SPACE(scriptInst);
	}

	void* oldPool = newVarMemPool;
	newVarMemPool = (char*)_aligned_malloc(numBytes, 128);
	if (newVarMemPool <= 0)
	{
		nlog("Failed to allocate memory! Pointer was null");
		return;
	}

	memset(newVarMemPool, 0, numBytes);
	memcpy(newVarMemPool, (void*)*llpScrVarMemPool, MEM_SCRVAR_SPACE(scriptInst));

	int newCount = (numBytes / sizeof(ScrVar_t));
	ScrVar_t* currentRef = (ScrVar_t*)(newVarMemPool);

	// ALOG("numbytes %X, sizeof scrvar %X, newcount %d, %p (%p) mempool, var.o (%x)", numBytes, sizeof(ScrVar_t), newCount, newVarMemPool, &currentRef[MEM_SCRVAR_COUNT - 1], offsetof(ScrVar_t, o));

	currentRef[(scriptInst ? MEM_SCRVAR_CSC_COUNT : MEM_SCRVAR_COUNT) - 1].value.type = VAR_FREE;
	currentRef[(scriptInst ? MEM_SCRVAR_CSC_COUNT : MEM_SCRVAR_COUNT) - 1].o.size = scriptInst ? MEM_SCRVAR_CSC_COUNT : MEM_SCRVAR_COUNT;

	for (int i = (scriptInst ? MEM_SCRVAR_CSC_COUNT : MEM_SCRVAR_COUNT); i < newCount; i++)
	{
		currentRef[i].value.type = VAR_FREE;
		currentRef[i].o.size = i + 1;
	}

	// clear last variable so the vm knows where to stop
	currentRef[newCount - 1].o.size = 0;

	*llpScrVarMemPool = (INT64)newVarMemPool;
}

void GSCBuiltins::GScr_debugallocvariables(int scriptInst)
{
	int numVariables = ScrVm_GetInt(scriptInst, 1);
	int varIndex = 0;

	ScrVar_t* variables = (ScrVar_t*)*(INT64*)((char*)OFF_ScrVarGlob + 128 + (scriptInst << 8));
	for (int i = 0; i < numVariables; i++)
	{
		varIndex = ScrVar_AllocVariableInternal(scriptInst, 1, 0, varIndex);
		variables[varIndex].value.type = VAR_UNDEFINED;
	}
}

void GSCBuiltins::GScr_runtimedetour(int scriptInst)
{
	char* str_file = ScrVm_GetString(scriptInst, 1);
	int n_namespace = ScrVm_GetInt(scriptInst, 2);
	int n_func = ScrVm_GetInt(scriptInst, 3);
	auto funcHandle = ScrVm_GetFunc(scriptInst, 4);

	if (!str_file || !n_namespace || !n_func || !funcHandle)
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

	char* fPos = buffer + currentExport->bytecodeOffset;
	ScriptDetours::RegisterRuntimeDetour((INT64)funcHandle, n_func, n_namespace, str_file, b_found ? fPos : NULL);
}

void GSCBuiltins::GScr_enableonlinematch(int scriptInst)
{
	*(int32_t*)PTR_sSessionModeState = (*(int32_t*)PTR_sSessionModeState & ~(1 << 14));
}

void GSCBuiltins::GScr_catch_exit(int scriptInst)
{
	if (IS_WINSTORE)
	{
		return; // this isnt supported because crt stuff got messed with
	}
	*(__int16*)GSCR_FASTEXIT = 0x6;
}

void GSCBuiltins::GScr_abort(int scriptInst)
{
	((void(__fastcall*)())REBASE(0, 0))();
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