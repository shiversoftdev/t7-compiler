#include "builtins.h"
#include "offsets.h"
#include "detours.h"

std::unordered_map<int, void*> GSCBuiltins::CustomFunctions;
tScrVm_GetString GSCBuiltins::ScrVm_GetString;
tScrVm_GetInt GSCBuiltins::ScrVm_GetInt;

// add all custom builtins here
void GSCBuiltins::Generate()
{
	// make sure your builtins are lowercase
	AddCustomFunction("nprintln", GSCBuiltins::GScr_nprintln);
	AddCustomFunction("detour", GSCBuiltins::GScr_detour);
	AddCustomFunction("relinkdetours", GSCBuiltins::GScr_relinkDetours);
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