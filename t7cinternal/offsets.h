#pragma once
#include "framework.h"
#include <winnt.h>

// dont mess with these because when I add new games, these macros will change

#define OFF_IsProfileBuild REBASE(0x32D7D70, 0x31154A0)
#define OFF_ScrVm_GetInt REBASE(0x12EB7F0, 0x1391240)
#define OFF_ScrVm_GetString REBASE(0x12EBAA0, 0x12EBAA0)
#define OFF_ScrVm_Opcodes REBASE(0x32E6350, 0x2EB38B0)
#define OFF_ScrVm_Opcodes2 REBASE(0x3306350, 0x2E838B0)
#define OFF_Scr_GetFunction REBASE(0x1AF7820, NULL)
#define OFF_GetMethod REBASE(NULL, 0x136CD40)
#define OFF_Scr_GetMethod REBASE(0x1AF79B0, NULL)
#define OFF_DB_FindXAssetHeader REBASE(0x1420ED0, 0x14DC380)
#define XASSETTYPE_SCRIPTPARSETREE 0x36u
#define OFF_s_runningUILevel REBASE(0x168ED91E, 0x148FD0EF)
#define OFF_Scr_GscObjLink REBASE(0x12CC300, 0x1370AC0)
#define OFF_ScrVar_AllocVariableInternal REBASE(0x12D9A60, 0x137F050)
#define OFF_ScrVm_GetFunc REBASE(0x12EB730, 0x1392030)
#define PTR_sSessionModeState REBASE(0x168ED7F4, 0x18AE65C4)
#define GSCR_FASTEXIT REBASE(0x2C53903, NULL)
#define OFF_BID_Scr_CastInt REBASE(0x32D71A0, 0x31148E0)
#define OFF_Scr_CastInt REBASE(0x162E60, 0x1F1EF0)
#define OFF_ScrVarGlob REBASE(0x51A3500, 0x3F66900)

#define OFF_VM_OP_GetAPIFunction REBASE(0x12D0890, 0x1374E90)
#define OFF_VM_OP_GetFunction REBASE(0x12D0A30, 0x1374E50)
#define OFF_VM_OP_ScriptFunctionCall REBASE(0x12CEE80, 0x1372F80)
#define OFF_VM_OP_ScriptMethodCall REBASE(0x12CF1D0, 0x1373320)
#define OFF_VM_OP_ScriptThreadCall REBASE(0x12CFB10, 0x13735F0)
#define OFF_VM_OP_ScriptMethodThreadCall REBASE(0x12CF570, 0x1373A20)
#define OFF_VM_OP_CallBuiltin REBASE(0x12CE460, 0x1372CB0)
#define OFF_VM_OP_CallBuiltinMethod REBASE(0x12CE3A0, 0x1372D20)