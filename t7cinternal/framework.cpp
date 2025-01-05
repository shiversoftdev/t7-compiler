#include "framework.h"

#pragma section(".offsets",read,write)
// this is the result of severe brain damage
// this is a function used as MSELECT(steam_offset, msstore_offset)
// mov rax, 0x1122334455667788 (replaced with game base address)
// add rax, rcx (rcx replaced with rdx if its msstore)
// ret
__declspec(allocate(".offsets")) const char MSELECT[] = 
{ 
	0x48, 0xB8, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, 
	0x48, 0x01, 0xC8, // c8 -> d0 for rcx -> rdx
	0xC3 
};

#pragma optimize("",off)
bool is_tls_initialized = false;
void NTAPI tls_callback(PVOID DllHandle, DWORD dwReason, PVOID)
{
	if (is_tls_initialized)
	{
		return;
	}
	is_tls_initialized = true;

	VirtualProtect((LPVOID)MSELECT, sizeof(MSELECT), PAGE_EXECUTE_READWRITE, &dwReason);
	auto base_address_game = *(uint64_t*)((uint64_t)(NtCurrentTeb()->ProcessEnvironmentBlock) + 0x10);
	*(uint64_t*)(MSELECT + 2) = base_address_game;

	if (*(uint32_t*)(base_address_game + 0x3C) == 0x1A0) // if we are windows store exe
	{
		*(uint8_t*)(MSELECT + 0xC) = 0xd0; // use rdx
	}
}
#pragma optimize("",on)

#pragma comment (linker, "/INCLUDE:_tls_used")
#pragma comment (linker, "/INCLUDE:tls_callback_func") 
#pragma const_seg(".CRT$XLF")
EXTERN_C const
PIMAGE_TLS_CALLBACK tls_callback_func = tls_callback;
#pragma const_seg()

void chgmem(__int64 addy, __int32 size, void* copy)
{
	DWORD oldprotect;
	VirtualProtect((void*)addy, size, PAGE_EXECUTE_READWRITE, &oldprotect);
	memcpy((void*)addy, copy, size);
	VirtualProtect((void*)addy, size, oldprotect, &oldprotect);
}