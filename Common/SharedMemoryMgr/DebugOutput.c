#include <windows.h>
#include <stdio.h>
#include <string.h>

#include "SharedMemoryMgr.h"

#define UNICODE

#ifdef _DEBUG

	extern void DebugOutputString(LPCTSTR lpszString)
	{
		OutputDebugString(lpszString);
	}

	extern void DebugOutputWithInteger(LPCTSTR lpszFormat, int iNumber)
	{
		int iBufSize = strlen(lpszFormat) * sizeof(lpszFormat[0]) + 20;
		char * pszMessage = (char *)malloc(iBufSize);
		ZeroMemory(pszMessage, iBufSize);
		sprintf_s(pszMessage, iBufSize, lpszFormat, iNumber);
		OutputDebugString(pszMessage);
		free(pszMessage);
	}

#endif