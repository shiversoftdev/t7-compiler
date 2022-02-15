#pragma once
#include "framework.h"

class Opcodes
{
public:
	static void Init();
	static void VM_OP_GetLazyFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_GetLocalFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
	static void VM_OP_NOP(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
};