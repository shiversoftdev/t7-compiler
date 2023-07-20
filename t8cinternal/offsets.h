#pragma once
#include "framework.h"

// dont mess with these because when I add new games, these macros will change

#define OFFSET(x) ((INT64)GetModuleHandle(NULL) + (INT64)x)

#define OFF_IsProfileBuild OFFSET(0x49611A0)
#define OFF_ScrVm_GetInt OFFSET(0x2773B50)
#define OFF_ScrVm_GetString OFFSET(0x2774840)
#define OFF_ScrVm_GetNumParam OFFSET(0x2774440)
#define OFF_ScrVm_AddInt OFFSET(0x276EB80)
#define OFF_ScrVm_AddBool OFFSET(0x276E760)
#define OFF_ScrVm_Opcodes OFFSET(0x4EED340)
#define OFF_Scr_GetFunction OFFSET(0x33AF840)
#define OFF_Scr_GetMethod OFFSET(0x33AFC20)
#define OFF_CScr_GetFunction OFFSET(0x1F13140)
#define OFF_CScr_GetMethod OFFSET(0x1F13650)
#define OFF_DB_FindXAssetHeader OFFSET(0x2EB75B0)
#define OFF_xAssetScriptParseTree OFFSET(0x912BBB0)
#define XASSETTYPE_SCRIPTPARSETREE 0x30
#define OFF_gObjFileInfo OFFSET(0x82efcd0)
#define OFF_gObjFileInfoCount OFFSET(0x82f76b0)
#define OFF_s_runningUILevel OFFSET(0x0000000008B50819)
#define OFF_Scr_GscObjLink OFFSET(0x2748E70)
