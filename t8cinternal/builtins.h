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

inline uint32_t t8hash(const char* key) {

	const char* data = key;
	uint32_t hash = 0x4B9ACE2F;

	while(*data)
	{
		char c = tolower(*data);
		hash = ((c + hash) ^ ((c + hash) << 10)) + (((c + hash) ^ ((c + hash) << 10)) >> 6);
		data++;
	}

	return 0x8001 * ((9 * hash) ^ ((9 * hash) >> 11));
}

typedef INT64(__fastcall* tScrVm_GetInt)(unsigned int inst, unsigned int index);
typedef char*(__fastcall* tScrVm_GetString)(unsigned int inst, unsigned int index);
typedef INT64(__fastcall* tScrVm_GetNumParam)(unsigned int inst);
typedef void(__fastcall* tScrVm_AddInt)(unsigned int inst, int64_t value);
typedef void(__fastcall* tScrVm_AddBool)(unsigned int inst, bool value);
typedef void(__fastcall* tScrVm_AddUndefined)(unsigned int inst);

class GSCBuiltins
{
public:
	static void Init();
	static void AddCustomFunction(const char* name, void* funcPtr);
	static tScrVm_GetInt ScrVm_GetInt;
	static tScrVm_GetString ScrVm_GetString;
	static tScrVm_GetNumParam ScrVm_GetNumParam;
	static tScrVm_AddInt ScrVm_AddInt;
	static tScrVm_AddBool ScrVm_AddBool;
	static tScrVm_AddUndefined ScrVm_AddUndefined;

private:
	static void Exec(int scriptInst);
	static void Generate();
	static std::unordered_map<int, void*> CustomFunctions;

private:
	static void GScr_nprintln(int scriptInst);
	static void GScr_detour(int scriptInst);
	static void GScr_relinkDetours(int scriptInst);
	static void GScr_livesplit(int scriptInst);
	static void GScr_fnprint(int scriptInst);
	static void GScr_fnprintln(int scriptInst);
	static void GScr_areAdvancedFeaturesSupported(int scriptInst);

public:
	static void nlog(const char* str, ...);
};