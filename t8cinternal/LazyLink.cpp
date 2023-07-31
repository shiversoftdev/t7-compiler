#include "LazyLink.h"
#include "offsets.h"
#include "detours.h"
#include "builtins.h"

void LazyLink::Init()
{
#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Installing Lazy link");
#endif
	// Change Opcode Handler 0x16 to VM_OP_GetLazyFunction
	*(INT64*)(0x16 * 8 + OFF_ScrVm_Opcodes) = (INT64)VM_OP_GetLazyFunction;
}

void LazyLink::VM_OP_GetLazyFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate)
{
	INT64 base = (*fs_0 + 3) & 0xFFFFFFFFFFFFFFFCLL;
	INT32 nsp = *(INT32*)base;
	INT32 func = *(INT32*)(base + 4);
	INT64 script = *(INT64*)(base + 8);

#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Lazy loading %x::%x <%p>...", nsp, func, script);
#endif
	// (field_0) move past the data
	fs_0[0] = base + 0x10;
	// (top) go to next field
	fs_0[1] += 0x10;

	auto buffer = ScriptDetours::FindLinkedScript(script, inst);

	if (!buffer)
	{
#ifdef DETOUR_LOGGING
		GSCBuiltins::nlog("Failed to locate %p...", script);
#endif
		*(INT32*)(fs_0[1] + 8) = 0x0; // undefined
		return;
	}

	auto exportsOffset = *(INT32*)(buffer + 0x30);
	auto exports = (INT64)(exportsOffset + buffer);
	auto numExports = *(INT16*)(buffer + 0x1E);
	__t8export* currentExport = (__t8export*)exports;

	INT64 link = 0;

	for (INT16 i = 0; i < numExports; i++, currentExport++)
	{
		if (currentExport->funcName != func)
		{
			continue;
		}
		if (currentExport->funcNS != nsp)
		{
			continue;
		}
		link = (INT64)buffer + currentExport->bytecodeOffset;
	}

#ifdef DETOUR_LOGGING
	GSCBuiltins::nlog("Output export: %p!", link);
#endif
	if (!link)
	{
		*(INT32*)(fs_0[1] + 8) = 0x0; // undefined
		return;
	}

	// assign the top variable's to the ptr
	*(INT64*)(fs_0[1]) = link;
	*(INT32*)(fs_0[1] + 8) = 0xC; // SCRIPT_FUNCTION(0xC)
}
