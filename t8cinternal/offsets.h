#pragma once
#include "framework.h"

// dont mess with these because when I add new games, these macros will change

#define OFFSET(x) ((INT64)GetModuleHandle(NULL) + (INT64)x)

#define OFF_IsProfileBuild OFFSET(0x49621A0)
#define OFF_ScrVm_GetInt OFFSET(0x2773C50)
#define OFF_ScrVm_GetString OFFSET(0x2774940)
#define OFF_ScrVm_GetNumParam OFFSET(0x2774540)
#define OFF_ScrVm_AddInt OFFSET(0x276EC80)
#define OFF_ScrVm_AddBool OFFSET(0x276E860)
#define OFF_ScrVm_Opcodes OFFSET(0x4EEE340)
#define OFF_Scr_GetFunction OFFSET(0x33AF940)
#define OFF_Scr_GetMethod OFFSET(0x33AFD20)
#define OFF_DB_FindXAssetHeader OFFSET(0x2EB76B0)
#define XASSETTYPE_SCRIPTPARSETREE 0x30
#define OFF_s_runningUILevel OFFSET(0x8B51819)
#define OFF_Scr_GscObjLink OFFSET(0x2748F70)