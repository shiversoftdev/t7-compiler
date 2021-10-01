#pragma once
#include "framework.h"

// dont mess with these because when I add new games, these macros will change

#define OFFSET(x) ((INT64)GetModuleHandle(NULL) + (INT64)x)

#define OFF_IsProfileBuild OFFSET(0x32D9D70)
#define OFF_ScrVm_GetInt OFFSET(0x12EB7F0)
#define OFF_ScrVm_GetString OFFSET(0x12EBAA0)
#define OFF_ScrVm_Opcodes OFFSET(0x32E8350)
#define OFF_Scr_GetFunction OFFSET(0x1AF7820)
#define OFF_Scr_GetMethod OFFSET(0x1AF79B0)
#define OFF_DB_FindXAssetHeader OFFSET(0x1420ED0)
#define XASSETTYPE_SCRIPTPARSETREE 0x36u
#define OFF_s_runningUILevel OFFSET(0x168EF91E)
#define OFF_Scr_GscObjLink OFFSET(0x12CC300)