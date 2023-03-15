#pragma once
#include "framework.h"
#include <winnt.h>
#include <unordered_map>

struct alignas(8) BuiltinFunctionDef
{
	int canonId;
	int min_args;
	int max_args;
	void* actionFunc;
	int type;
};

typedef INT64(__fastcall* tScrVm_GetInt)(unsigned int inst, unsigned int index);
typedef char* (__fastcall* tScrVm_GetString)(unsigned int inst, unsigned int index);
typedef INT32(__fastcall* tScrVar_AllocVariableInternal)(unsigned int inst, unsigned int nameType, __int64 a3, unsigned int a4);
typedef INT64(__fastcall* tScrVm_GetFunc)(unsigned int inst, unsigned int index);

class GSCBuiltins
{
public:
	static void Init();
	static void AddCustomFunction(const char* name, void* funcPtr);
	static tScrVm_GetInt ScrVm_GetInt;
	static tScrVm_GetString ScrVm_GetString;
	static tScrVm_GetFunc ScrVm_GetFunc;
	static tScrVar_AllocVariableInternal ScrVar_AllocVariableInternal;

private:
	static void Exec(int scriptInst);
	static void Generate();
	static std::unordered_map<int, void*> CustomFunctions;

private:
	static void GScr_nprintln(int scriptInst);
	static void GScr_detour(int scriptInst);
	static void GScr_relinkDetours(int scriptInst);
	static void GScr_livesplit(int scriptInst);
	static void GScr_patchbyte(int scriptInst);
	static void GScr_erasefunc(int scriptInst);
	static void GScr_setmempool(int scriptInst);
	static void GScr_debugallocvariables(int scriptInst);
	static void GScr_runtimedetour(int scriptInst);
	static void GScr_catch_exit(int scriptInst);
	static void GScr_abort(int scriptInst);
	static void GScr_enableonlinematch(int scriptInst);

public:
	static void nlog(const char* str, ...);
};

typedef uint32_t ScrVarIndex_t;
typedef uint32_t ScrString_t;
typedef uint64_t ScrVarNameIndex_t;
typedef uint32_t ScrVarCannonicalName_t;

enum ScrVarType_t
{
	VAR_UNDEFINED = 0,
	VAR_POINTER = 1,
	VAR_STRING = 2,
	VAR_ISTRING = 3,
	VAR_VECTOR = 4,
	VAR_HASH = 5,
	VAR_FLOAT = 6,
	VAR_INTEGER = 7,
	VAR_UINT64 = 8,
	VAR_UINTPTR = 9,
	VAR_ENTITYOFFSET = 10,
	VAR_CODEPOS = 11,
	VAR_PRECODEPOS = 12,
	VAR_APIFUNCTION = 13,
	VAR_FUNCTION = 14,
	VAR_STACK = 15,
	VAR_ANIMATION = 16,
	VAR_THREAD = 17,
	VAR_NOTIFYTHREAD = 18,
	VAR_TIMETHREAD = 19,
	VAR_CHILDTHREAD = 20,
	VAR_CLASS = 21,
	VAR_STRUCT = 22,
	VAR_REMOVEDENTITY = 23,
	VAR_ENTITY = 24,
	VAR_ARRAY = 25,
	VAR_REMOVEDTHREAD = 26,
	VAR_FREE = 27,
	VAR_THREADLIST = 28,
	VAR_ENTLIST = 29,
	VAR_COUNT = 30
};

struct ScrVarChildPair_t
{
	ScrVarIndex_t firstChild;
	ScrVarIndex_t lastChild;
};

struct ScrVarStackBuffer_t
{
	BYTE* pos;
	BYTE* creationPos;
	unsigned __int16 size;
	unsigned __int16 bufLen;
	ScrVarIndex_t threadId;
	BYTE buf[1];
};

union ScrVarValueUnion_t
{
	int64_t intValue;
	int32_t hashValue;
	uintptr_t uintptrValue;
	float floatValue;
	ScrString_t stringValue;
	const float* vectorValue;
	BYTE* codePosValue;
	ScrVarIndex_t pointerValue;
	ScrVarStackBuffer_t* stackValue;
	ScrVarChildPair_t childPair;
};

__declspec(align(8)) struct ScrVarValue_t
{
	ScrVarValueUnion_t u;
	ScrVarType_t type;
	uint32_t pad;
};

struct ScrVarRuntimeInfo_t
{
	unsigned __int32 nameType : 3;
	unsigned __int32 flags : 5;
	unsigned __int32 refCount : 24;
};

union EntRefUnion
{
	uint64_t val;
};

__declspec(align(8)) union ScrVarObjectInfo_t
{
	uint64_t object_o;
	unsigned int size;
	EntRefUnion entRefUnion;
	ScrVarIndex_t nextEntId;
	ScrVarIndex_t self;
	ScrVarIndex_t free;
};

struct ScrVarEntityInfo_t
{
	unsigned __int16 classNum;
	unsigned __int16 clientNum;
};

union ScrVarObjectW_t
{
	uint32_t object_w;
	ScrVarEntityInfo_t varEntityInfo;
	ScrVarIndex_t stackId;
};

struct ScrVar_t
{
	ScrVarValue_t value; // 0 (size 10)
	ScrVarRuntimeInfo_t info; // 10 (size 4)
	ScrVarObjectInfo_t o; // 18 (size 8)
	ScrVarObjectW_t w; // 20 (size 4)
	ScrVarNameIndex_t nameIndex; // 28 (size 8)
	ScrVarIndex_t nextSibling; // 30 (size 4)
	ScrVarIndex_t prevSibling; // 34 (size 4)
	ScrVarIndex_t parentId; // 38 (size 4)
	ScrVarIndex_t nameSearchHashList; // 3C (size 4)
};