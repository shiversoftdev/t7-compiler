#pragma once
#include "framework.h"

class LazyLink
{
public:
	static void Init();
	static void VM_OP_GetLazyFunction(INT32 inst, INT64* fs_0, INT64 vmc, bool* terminate);
};