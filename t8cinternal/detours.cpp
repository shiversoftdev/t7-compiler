#include "framework.h"
#include "detours.h"
#include "offsets.h"
#include "builtins.h"

// Note: Some auto-exec scripts will not get detoured due to the way linking works in the game

struct ReadScriptDetour
{
	INT32 FixupName;
	INT32 ReplaceNamespace;
	INT32 ReplaceFunction;
	INT32 FixupOffset;
	INT32 FixupSize;
};

tScr_GetFunction ScriptDetours::Scr_GetFunction = NULL;
tScr_GetMethod ScriptDetours::Scr_GetMethod = NULL;
tScr_GetFunction ScriptDetours::CScr_GetFunction = NULL;
tScr_GetMethod ScriptDetours::CScr_GetMethod = NULL;
tDB_FindXAssetHeader ScriptDetours::DB_FindXAssetHeader = NULL;
tScr_GscObjLink ScriptDetours::Scr_GscObjLink = NULL;
char* ScriptDetours::GSC_OBJ[2] = { NULL, NULL };

std::vector<ScriptDetour*> ScriptDetours::RegisteredDetours[2];
std::unordered_map<INT64, ScriptDetour*> ScriptDetours::LinkedDetours[2];
std::unordered_map<INT64*, INT64> ScriptDetours::AppliedFixups[2];
tVM_Opcode ScriptDetours::VM_OP_GetFunction_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_GetAPIFunction_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_ScriptFunctionCall_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_ScriptMethodCall_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_ScriptThreadCall_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_ScriptMethodThreadCall_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_CallBuiltin_Old = NULL;
tVM_Opcode ScriptDetours::VM_OP_CallBuiltinMethod_Old = NULL;
bool ScriptDetours::DetoursLinked[2] = { false, false };
bool ScriptDetours::DetoursReset[2] = { true, true };
bool ScriptDetours::DetoursEnabled[2] = { false, false };
bool ScriptDetours::DetoursInitialized = false;

EXPORT void ResetDetours()
{
	if (!ScriptDetours::DetoursInitialized)
	{
		return;
	}
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Resetting detours...");
#endif
	for (int inst = 0; inst < 2; inst++)
	{
		ScriptDetours::ResetDetoursByInst(inst);
	}
}

void ScriptDetours::ResetDetoursByInst(int inst)
{
	if (!ScriptDetours::DetoursInitialized)
	{
		return;
	}
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Resetting detours (%s)...", inst ? "CLIENT" : "SERVER");
#endif
	for (auto it = AppliedFixups[inst].begin(); it != AppliedFixups[inst].end(); it++)
	{
		*it->first = it->second;
	}
	AppliedFixups[inst].clear();
	DetoursReset[inst] = true;
	DetoursLinked[inst] = false;
	DetoursEnabled[inst] = false;
}

EXPORT void RemoveDetours(INT32 inst)
{
	if (!ScriptDetours::DetoursInitialized)
	{
		return;
	}
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Removing detours (%s)...", inst ? "CLIENT" : "SERVER");
#endif
	for (auto it = ScriptDetours::RegisteredDetours[inst].begin(); it != ScriptDetours::RegisteredDetours[inst].end(); it++)
	{
		free(*it);
	}
	ScriptDetours::RegisteredDetours[inst].clear();
	ScriptDetours::DetoursLinked[inst] = false;
	ScriptDetours::ResetDetoursByInst(inst);
}

void ScriptDetours::RemoveAllDetours()
{
	if (!ScriptDetours::DetoursInitialized)
	{
		return;
	}
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Removing detours...");
#endif
	for (int inst = 0; inst < 2; inst++)
	{
		RemoveDetours(inst);
	}
}

EXPORT bool RegisterDetours(void* DetourData, int NumDetours, INT64 scriptOffset, INT32 inst)
{
	if (!ScriptDetours::DetoursInitialized)
	{
		return true;
	}
	RemoveDetours(inst);
	ScriptDetours::GSC_OBJ[inst] = (char*)scriptOffset;
	
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Registering %d detours in script %p (%s)...", NumDetours, scriptOffset, inst ? "CLIENT" : "SERVER");
#endif

	INT64 base = (INT64)DetourData;
	for (int i = 0; i < NumDetours; i++)
	{
		ReadScriptDetour* read_detour = (ReadScriptDetour*)(base + (i * 256));
		ScriptDetour* detour = new ScriptDetour();
		detour->hFixup = read_detour->FixupOffset + scriptOffset;
		detour->ReplaceFunction = read_detour->ReplaceFunction;
		detour->ReplaceNamespace = read_detour->ReplaceNamespace;
		detour->FixupSize = read_detour->FixupSize;
#ifdef DETOUR_LOGGING
		GSCBuiltins::nlog("Detour Parsed: {FixupName:%x, ReplaceNamespace:%x, ReplaceFunction:%x, FixupOffset:%x, FixupSize:%x} {FixupMin:%p, FixupMax:%p}", read_detour->FixupName, read_detour->ReplaceNamespace, read_detour->ReplaceFunction, read_detour->FixupOffset, read_detour->FixupSize, detour->hFixup, detour->hFixup + detour->FixupSize);
#endif
		detour->ReplaceScriptName = *(INT64*)((INT64)read_detour + sizeof(ReadScriptDetour));
		ScriptDetours::RegisteredDetours[inst].push_back(detour);
	}

	ScriptDetours::DetoursLinked[inst] = false;
	return true;
}

void ScriptDetours::InstallHooks()
{
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Installing hooks...");
#endif

	// initialize methods
	Scr_GetFunction = (tScr_GetFunction)OFF_Scr_GetFunction;
	Scr_GetMethod = (tScr_GetMethod)OFF_Scr_GetMethod;
	CScr_GetFunction = (tScr_GetFunction)OFF_CScr_GetFunction;
	CScr_GetMethod = (tScr_GetMethod)OFF_CScr_GetMethod;
	DB_FindXAssetHeader = (tDB_FindXAssetHeader)OFF_DB_FindXAssetHeader;
	Scr_GscObjLink = (tScr_GscObjLink)OFF_Scr_GscObjLink;

	// opcodes to hook:
	VTableReplace(0x5d8, VM_OP_GetFunction, &VM_OP_GetFunction_Old);
	VTableReplace(0x6f7, VM_OP_GetAPIFunction, &VM_OP_GetAPIFunction_Old);
	VTableReplace(0x75c, VM_OP_ScriptFunctionCall, &VM_OP_ScriptFunctionCall_Old);
	VTableReplace(0x7f2, VM_OP_ScriptMethodCall, &VM_OP_ScriptMethodCall_Old);
	VTableReplace(0x8cd, VM_OP_ScriptThreadCall, &VM_OP_ScriptThreadCall_Old);
	VTableReplace(0xa34, VM_OP_ScriptMethodThreadCall, &VM_OP_ScriptMethodThreadCall_Old);
	VTableReplace(0x00f, VM_OP_CallBuiltin, &VM_OP_CallBuiltin_Old);
	VTableReplace(0x010, VM_OP_CallBuiltinMethod, &VM_OP_CallBuiltinMethod_Old);
	// TODO all the 2 methods (figuring out what the fuck they do too...)

	DetoursInitialized = true;
}

INT64 ScriptDetours::GetFunction(INT32 inst, INT32 canonID, INT32* type, INT32* min_args, INT32* max_args) {
	return (inst ? CScr_GetFunction : Scr_GetFunction)(canonID, type, min_args, max_args);
}

INT64 ScriptDetours::GetMethod(INT32 inst, INT32 canonID, INT32* type, INT32* min_args, INT32* max_args) {
	return (inst ? CScr_GetMethod : Scr_GetMethod)(canonID, type, min_args, max_args);
}

INT64 ScriptDetours::FindScriptParsetree(INT64 name)
{
	SPTEntry* currentSpt = (SPTEntry*)*(INT64*)(OFF_xAssetScriptParseTree);
	INT32 sptCount = *(INT32*)OFFSET(0x912BBB0 + 0x14);
	for (int i = 0; i < sptCount; i++, currentSpt++)
	{
		if (!currentSpt->Name) continue;
		if (!currentSpt->Buffer) continue;
		if (!currentSpt->size) continue;
		if (currentSpt->Name != name) continue;
		return (INT64)currentSpt;
	}
	return 0;
}

INT64 ScriptDetours::FindLinkedScript(INT64 name, INT32 inst)
{
	INT32 linkedCount = ((INT32*)(OFF_gObjFileInfoCount))[inst];
	LinkedObjFileInfo* linkedObject = &(*(tobjFileInfo*)(OFF_gObjFileInfo))[inst][0];

	for (int i = 0; i < linkedCount; i++, linkedObject++)
	{
		if (!linkedObject->object) continue;
		INT64 objName = *(INT64*)(linkedObject->object + 0x10);
		if (objName != name) continue;
		return (INT64)linkedObject->object;
	}
	return 0;
}

void ScriptDetours::LinkDetours(INT32 inst)
{
	LinkedDetours[inst].clear();
	for (auto it = RegisteredDetours[inst].begin(); it != RegisteredDetours[inst].end(); it++)
	{
		auto detour = *it;
		if (detour->ReplaceScriptName) // not a builtin
		{
#ifdef DETOUR_LOGGING
			GSCBuiltins::nlog("Linking replacement %x<%p>::%x...", detour->ReplaceNamespace, detour->ReplaceScriptName, detour->ReplaceFunction);
#endif
			// locate the script to replace (using the linked scripts instead of the xassets to avoid issues with unlinked scripts)
			auto buffer = FindLinkedScript(detour->ReplaceScriptName, inst);
			if (!buffer)
			{
#ifdef DETOUR_LOGGING
				GSCBuiltins::nlog("Failed to locate %p...", detour->ReplaceScriptName);
#endif
				continue;
			}

#ifdef DETOUR_LOGGING
			GSCBuiltins::nlog("Located xAssetHeader...");
#endif
			// locate the target export to link
			auto exportsOffset = *(INT32*)(buffer + 0x30);
			auto exports = (INT64)(exportsOffset + buffer);
			auto numExports = *(INT16*)(buffer + 0x1E);
			__t8export* currentExport = (__t8export*)exports;
			for (INT16 i = 0; i < numExports; i++, currentExport++)
			{
				if (currentExport->funcName != detour->ReplaceFunction)
				{
					continue;
				}
				if (currentExport->funcNS != detour->ReplaceNamespace)
				{
					continue;
				}
#ifdef DETOUR_LOGGING
				GSCBuiltins::nlog("Found export at %p!", (INT64)buffer + currentExport->bytecodeOffset);
#endif
				LinkedDetours[inst][(INT64)buffer + currentExport->bytecodeOffset] = detour;
				break;
			}
		}
		else
		{
#ifdef DETOUR_LOGGING
			GSCBuiltins::nlog("Linking replacement for builtin %x...", detour->ReplaceFunction);
#endif
			INT32 discardType;
			INT32 discardMinParams;
			INT32 discardMaxParams;
			auto hReplace = GetFunction(inst, detour->ReplaceFunction, &discardType, &discardMinParams, &discardMaxParams);
			if (!hReplace)
			{
				hReplace = GetMethod(inst, detour->ReplaceFunction, &discardType, &discardMinParams, &discardMaxParams);
			}
			if (hReplace)
			{
#ifdef DETOUR_LOGGING
				GSCBuiltins::nlog("Found function definition at %p!", hReplace);
#endif
				LinkedDetours[inst][hReplace] = detour;
			}
		}
	}
	DetoursLinked[inst] = true;
}

void ScriptDetours::VTableReplace(INT32 original_code, tVM_Opcode ReplaceFunc, tVM_Opcode* OutOld)
{
	INT64 handler_table = OFF_ScrVm_Opcodes;
	INT64 stub_final = *(INT64*)(handler_table + original_code * 0x8);
	*OutOld = (tVM_Opcode)stub_final;
	for (int i = 0; i < 0x4000; i++)
	{
		if (*(INT64*)(handler_table + (i * 8)) == stub_final)
		{
			*(INT64*)(handler_table + (i * 8)) = (INT64)ReplaceFunc;
		}
	}
}

void ScriptDetours::VM_OP_GetFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0);
	VM_OP_GetFunction_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_GetAPIFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0);
	VM_OP_GetAPIFunction_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_ScriptFunctionCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0, 1);
	VM_OP_ScriptFunctionCall_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_ScriptMethodCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0, 1);
	VM_OP_ScriptMethodCall_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_ScriptThreadCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0, 1);
	VM_OP_ScriptThreadCall_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_ScriptMethodThreadCall(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	CheckDetour(inst, fs_0, 1);
	VM_OP_ScriptMethodThreadCall_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_CallBuiltin(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	if (CheckDetour(inst, fs_0, 1))
	{
		// spoof opcode to ScriptFunctionCall (because we are no longer calling a builtin)
		*(INT16*)(*fs_0 - 2) = 0x75c;
		VM_OP_ScriptFunctionCall_Old(inst, fs_0, vmc, terminate);
		return;
	}
	VM_OP_CallBuiltin_Old(inst, fs_0, vmc, terminate);
}

void ScriptDetours::VM_OP_CallBuiltinMethod(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	if (CheckDetour(inst, fs_0, 1))
	{
		// spoof opcode to ScriptMethodCall (because we are no longer calling a builtin)
		*(INT16*)(*fs_0 - 2) = 0x7f2;
		VM_OP_ScriptMethodCall_Old(inst, fs_0, vmc, terminate);
		return;
	}
	VM_OP_CallBuiltinMethod_Old(inst, fs_0, vmc, terminate);
}

bool ScriptDetours::CheckDetour(INT32 inst, INT64* fs_0, INT32 offset)
{
	if (!DetoursEnabled[inst])
	{
		return false;
	}
	// detours are not supported in UI level
	if (*(BYTE*)(OFF_s_runningUILevel))
	{
		if (!ScriptDetours::DetoursReset[inst])
		{
			ResetDetours();
		}
		return false;
	}
	bool fixupApplied = false;
	if (!DetoursLinked[inst])
	{
		LinkDetours(inst);
	}
	INT64 ptrval = *(INT64*)((*fs_0 + 7 + offset) & 0xFFFFFFFFFFFFFFF8);
	if (LinkedDetours[inst].find(ptrval) != LinkedDetours[inst].end() && LinkedDetours[inst][ptrval]->hFixup)
	{
		INT64 fs_pos = *fs_0;
		// if pointer is below fixup or above it, the pointer is not within the detour and thus can be fixed up
		if (LinkedDetours[inst][ptrval]->hFixup > fs_pos || ((LinkedDetours[inst][ptrval]->hFixup + LinkedDetours[inst][ptrval]->FixupSize) <= fs_pos))
		{
#ifdef DETOUR_LOGGING
			GSCBuiltins::nlog("Replaced call at %p to fixup %p! Opcode: %x", (INT64)((*fs_0 + 7 + offset) & 0xFFFFFFFFFFFFFFF8), LinkedDetours[inst][ptrval]->hFixup, *(INT16*)(*fs_0 - 2));
#endif
			AppliedFixups[inst][(INT64*)((*fs_0 + 7 + offset) & 0xFFFFFFFFFFFFFFF8)] = ptrval;
			*(INT64*)((*fs_0 + 7 + offset) & 0xFFFFFFFFFFFFFFF8) = LinkedDetours[inst][ptrval]->hFixup;
			DetoursReset[inst] = false;
			fixupApplied = true;
		}
	}
	return fixupApplied;
}
