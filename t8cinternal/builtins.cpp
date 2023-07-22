#include "builtins.h"
#include "offsets.h"
#include "detours.h"
#include <fstream>
#include <ctime>

std::unordered_map<int, void*> GSCBuiltins::CustomFunctions;
tScrVm_GetString GSCBuiltins::ScrVm_GetString;
tScrVm_GetInt GSCBuiltins::ScrVm_GetInt;
tScrVm_GetNumParam GSCBuiltins::ScrVm_GetNumParam;
tScrVm_AddInt GSCBuiltins::ScrVm_AddInt;
tScrVm_AddBool GSCBuiltins::ScrVm_AddBool;
tScrVm_AddUndefined GSCBuiltins::ScrVm_AddUndefined;

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
}

void GSCBuiltins::Init()
{
	GSCBuiltins::Generate();
	auto builtinFunction = (BuiltinFunctionDef*)OFF_IsProfileBuild;
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Installing %d builtins...", CustomFunctions.size());
#endif
	builtinFunction->max_args = 255;
	builtinFunction->type = 0;
	builtinFunction->actionFunc = GSCBuiltins::Exec;

	ScrVm_GetString = (tScrVm_GetString)OFF_ScrVm_GetString;
	ScrVm_GetInt = (tScrVm_GetInt)OFF_ScrVm_GetInt;
	ScrVm_GetNumParam = (tScrVm_GetNumParam)OFF_ScrVm_GetNumParam;
	ScrVm_AddInt = (tScrVm_AddInt)OFF_ScrVm_AddInt;
	ScrVm_AddBool = (tScrVm_AddBool)OFF_ScrVm_AddBool;
	ScrVm_AddUndefined = (tScrVm_AddUndefined)OFF_ScrVm_AddUndefined;
}

void GSCBuiltins::AddCustomFunction(const char* name, void* funcPtr)
{
	CustomFunctions[t8hash(name)] = funcPtr;
}

void GSCBuiltins::Exec(int scriptInst)
{
	auto numParams = ScrVm_GetNumParam(scriptInst);
	if (!numParams)
	{
		// default implementation
		ScrVm_AddBool(scriptInst, false); 
		return;
	}

	INT32 func = ScrVm_GetInt(scriptInst, 0);
#ifdef DETOUR_LOGGING
	nlog("called with %d parameters with %p", numParams, func); // TODO
#endif
	if (CustomFunctions.find(func) == CustomFunctions.end())
	{
		// unknown builtin
		nlog("unknown builtin %p", func);
		ScrVm_AddBool(scriptInst, false);
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
	nlog("%s", ScrVm_GetString(scriptInst, 1));
	ScrVm_AddUndefined(scriptInst);
}

void GSCBuiltins::GScr_detour(int scriptInst)
{
	ScriptDetours::DetoursEnabled[scriptInst] = true;
	ScrVm_AddUndefined(scriptInst);
}

void GSCBuiltins::GScr_relinkDetours(int scriptInst)
{
	ScriptDetours::LinkDetours(scriptInst);
	ScrVm_AddUndefined(scriptInst);
}

void GSCBuiltins::GScr_livesplit(int scriptInst)
{
	HANDLE livesplit = CreateFile("\\\\.\\pipe\\LiveSplit", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
	if (!livesplit)
	{
		return;
	}

	const char* message = ScrVm_GetString(scriptInst, 1);
	WriteFile(livesplit, message, strlen(message), nullptr, NULL);
	CloseHandle(livesplit);
	ScrVm_AddUndefined(scriptInst);
}


void GSCBuiltins::nlog(const char* str, ...)
{
	char buf[256];

	va_list ap;
	va_start(ap, str);
	vsprintf(buf, str, ap);
	va_end(ap);
	std::ofstream output{ "t8compiler.log", std::ios::app };

	output << buf << "\n";
	output.close();
	/*
	HWND notepad, edit;
	strcat_s(buf, 256, "\r\n");
	notepad = FindWindow(NULL, "Untitled - Notepad");
	if (!notepad)
	{
		notepad = FindWindow(NULL, "*Untitled - Notepad");
	}
	edit = FindWindowEx(notepad, NULL, "EDIT", NULL);
	SendMessage(edit, EM_REPLACESEL, TRUE, (LPARAM)buf);
	//*/
}