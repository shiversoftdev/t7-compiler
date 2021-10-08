#include "LazyLink.h"
#include "offsets.h"
#include "detours.h"
#include "builtins.h"

void LazyLink::Init()
{
	// Change Opcode Handler 0x16 to VM_OP_GetLazyFunction
	*(INT64*)(0x16 * 8 + OFF_ScrVm_Opcodes) = (INT64)VM_OP_GetLazyFunction;
}

void LazyLink::VM_OP_GetLazyFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	INT64 base = (*fs_0 + 3) & 0xFFFFFFFFFFFFFFFCLL;
	INT32 Namespace = *(INT32*)base;
	INT32 Function = *(INT32*)(base + 4);
	char* script = (char*)(*fs_0 + (*(INT32*)(base + 8)));
	auto asset = ScriptDetours::FindScriptParsetree(script);

	if (!asset)
	{
		*(INT32*)(fs_0[1] + 0x18) = 0x0; // undefined
		fs_0[1] += 0x10; // change stack top
		return;
	}

	auto buffer = *(char**)(asset + 0x10);
	auto exportsOffset = *(INT32*)(buffer + 0x20);
	auto exports = (INT64)(exportsOffset + buffer);
	auto numExports = *(INT16*)(buffer + 0x3A);
	__t7export* currentExport = (__t7export*)exports;
	bool found = false;

	for (INT16 i = 0; i < numExports; i++, currentExport++)
	{
		if (currentExport->funcName != Function)
		{
			continue;
		}
		if (currentExport->funcNS != Namespace)
		{
			continue;
		}
		found = true;
		break;
	}

	if (!found)
	{
		*(INT32*)(fs_0[1] + 0x18) = 0x0; // undefined
		fs_0[1] += 0x10; // change stack top
		return;
	}

	*(INT32*)(fs_0[1] + 0x18) = 0xE; // assign the top variable's type
	*(INT64*)(fs_0[1] + 0x10) = (INT64)buffer + currentExport->bytecodeOffset; // assign the top variable's value
	fs_0[1] += 0x10; // change stack top
	*fs_0 = base + 0xC; // move past the data
}
