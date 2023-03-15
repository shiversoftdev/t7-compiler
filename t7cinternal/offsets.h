#pragma once
#include "framework.h"
#include <winnt.h>

// dont mess with these because when I add new games, these macros will change

#define OFFSET(x) (*(__int64*)((char*)(NtCurrentTeb()->ProcessEnvironmentBlock) + 0x10) + (__int64)(x))
#define REBASE(x) (*(__int64*)((char*)(NtCurrentTeb()->ProcessEnvironmentBlock) + 0x10) + (__int64)(x))

#define OFF_IsProfileBuild REBASE(0x32D7D70)
#define OFF_ScrVm_GetInt REBASE(0x12EB7F0)
#define OFF_ScrVm_GetString REBASE(0x12EBAA0)
#define OFF_ScrVm_Opcodes REBASE(0x32E6350)
#define OFF_ScrVm_Opcodes2 REBASE(0x3306350)
#define OFF_Scr_GetFunction REBASE(0x1AF7820)
#define OFF_Scr_GetMethod REBASE(0x1AF79B0)
#define OFF_DB_FindXAssetHeader REBASE(0x1420ED0)
#define XASSETTYPE_SCRIPTPARSETREE 0x36u
#define OFF_s_runningUILevel REBASE(0x168ED91E)
#define OFF_Scr_GscObjLink REBASE(0x12CC300)
#define OFF_ScrVar_AllocVariableInternal REBASE(0x12D9A60)
#define OFF_ScrVm_GetFunc REBASE(0x12EB730)
#define PTR_sSessionModeState REBASE(0x168ED7F4)
#define GSCR_FASTEXIT REBASE(0x2C53903)