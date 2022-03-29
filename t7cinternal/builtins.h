#pragma once
#include "framework.h"
#include <unordered_map>

struct alignas(8) BuiltinFunctionDef
{
	int canonId;
	int min_args;
	int max_args;
	void* actionFunc;
	int type;
};

constexpr uint32_t fnv_base_32 = 0x4B9ACE2F;

inline uint32_t fnv1a(const char* key) {

	const char* data = key;
	uint32_t hash = 0x4B9ACE2F;
	while(*data)
	{
		hash ^= tolower(*data);
		hash *= 0x1000193;
		data++;
	}
	hash *= 0x1000193; // bo3 wtf lol
	return hash;

}

typedef INT64(__fastcall* tScrVm_GetInt)(unsigned int inst, unsigned int index);
typedef char*(__fastcall* tScrVm_GetString)(unsigned int inst, unsigned int index);
#define PTR_sSessionModeState ((INT64)GetModuleHandleA(NULL) + (INT64)0x168EF7F4)

class GSCBuiltins
{
public:
	static void Init();
	static void AddCustomFunction(const char* name, void* funcPtr);
	static tScrVm_GetInt ScrVm_GetInt;
	static tScrVm_GetString ScrVm_GetString;

private:
	static void Exec(int scriptInst);
	static void Generate();
	static std::unordered_map<int, void*> CustomFunctions;

private:
	static void GScr_nprintln(int scriptInst);
	static void GScr_detour(int scriptInst);
	static void GScr_relinkDetours(int scriptInst);
	static void GScr_livesplit(int scriptInst);
	static void GScr_erasefunc(int scriptInst);
	static void GScr_setmempool(int scriptInst);
	static void GScr_enableonlinematch(int scriptInst);

public:
	static void nlog(const char* str, ...);
};