#include <windows.h>
#include <stdio.h>

#include "SharedMemoryMgr.h"

#define UNICODE

#ifdef _DEBUG
#define DEBUGCODE( code_fragment) {code_fragment}
#else
#define DEBUGCODE( code_fragment)
#endif

extern INT32 InitMemoryMgr(LPCTSTR SharedMemoryName, DWORD SharedMemorySize, PCOMMBUFFER *CommBufferIn);
extern INT32 JoinMemoryMgr(LPCTSTR SharedMemoryName, PCOMMBUFFER *CommBufferIn);
extern INT32 ReleaseMemoryMgr(PCOMMBUFFER *CommBufferIn);
extern INT32 PutBuffer(LPCSTR pEventBuffer, DWORD dwEventLength, DWORD dwTimeOut, PCOMMBUFFER *CommBufferIn);
extern INT32 GetBuffer(LPCSTR pEventBuffer, DWORD dwEventBufferLength, DWORD dwTimeOut, DWORD *pBytesRead, PCOMMBUFFER *CommBufferIn);

void InitEvent(PCOMMBUFFER CommBuffer);
void InitNewEvent(PCOMMBUFFER CommBuffer);
INT32 CopyEventToBuffer(DWORD iOffset, PWSPEVENT pWspEvent, LPCSTR pEventBuffer, DWORD dwEventLength, PCOMMBUFFER CommBuffer);
INT32 CopyEventFromBuffer(LPCSTR pEventBuffer, DWORD dwEventBufferLength, DWORD *pBytesRead, UINT64 *pEventNum, PCOMMBUFFER CommBuffer);
LPCSTR GetWspEvent(PWSPEVENT pWspEvent, PCOMMBUFFER CommBuffer, DWORD dwStartOffset);

#define MUTEX_NAME "Global\\WSP_MUTEX"
#define EVENT_NAME "Global\\WSP_EVENT"
#define CHILDEVENT_NAME "Global\\WSP_CHILDEVENT"
#define GLOBALPREPEND "Global\\"

#define SUCCESS 0
#define GENERALERRORCODE 5
#define TIMEOUT -1
#define OVERFLOW 9999
#define ENDOFDATA 3

#define READYTOREAD 0xFA
#define PREPARETOREAD 0xFB
#define NOTREADYTOREAD 0xFC
#define ALREADYREAD 0xFD


extern INT32 InitMemoryMgr(LPCTSTR SharedMemoryNameIn, DWORD SharedMemorySize, PCOMMBUFFER *CommBufferIn)
{
	DWORD dwError;
	DWORD errco;
	PCOMMBUFFER CommBuffer;
	BOOL bFileExists;
	int iSize;
	LPCTSTR lpSharedMemoryName;
	SECURITY_DESCRIPTOR  sd;
	SECURITY_ATTRIBUTES sa = { sizeof sa, &sd, FALSE };

	DEBUGCODE( DebugOutputString("Entered InitMemoryMgr"); )

	DEBUGCODE( DebugOutputString("Initializing and Setting Security Descriptor"); )
	InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION);
	SetSecurityDescriptorDacl(&sd, TRUE, NULL, FALSE);

	DEBUGCODE( DebugOutputString("Creating lpSharedMemoryName"); )
	iSize = strlen(GLOBALPREPEND) + strlen(SharedMemoryNameIn) + 1;
	lpSharedMemoryName = malloc(iSize);
	strcpy_s((char*)lpSharedMemoryName, iSize, (char*)GLOBALPREPEND);
	strcat_s((char*)lpSharedMemoryName, iSize, (char*)SharedMemoryNameIn);
	DEBUGCODE( DebugOutputString("Done creating lpSharedMemoryName: "); )
	DEBUGCODE( DebugOutputString(lpSharedMemoryName); )
	DEBUGCODE( DebugOutputWithInteger("Size = %i", iSize); )

	bFileExists = FALSE;

	DEBUGCODE( DebugOutputString("Allocating memory to CommBuffer: "); )
	DEBUGCODE( DebugOutputWithInteger("Size = %i", sizeof(COMMBUFFER)); )
	CommBuffer = malloc(sizeof(COMMBUFFER));
	*CommBufferIn = CommBuffer;

	InitEvent(CommBuffer);

	CommBuffer->iLastEventNumRead = 0;
	CommBuffer->bSharedMemoryOwner = TRUE;
	CommBuffer->dwNextReadOffset = 0;

	DEBUGCODE( DebugOutputString("Creating mutex:"); )
	DEBUGCODE( DebugOutputString(MUTEX_NAME); )
	CommBuffer->ghMutex = CreateMutex(&sa, FALSE, MUTEX_NAME);

	errco = GetLastError();

	if (CommBuffer->ghMutex == NULL) 
	{
		if(errco == 0)
			errco = GENERALERRORCODE;

		DEBUGCODE( DebugOutputString("Freeing resources"); )
		free((char*)lpSharedMemoryName);
		free(CommBuffer);
		*CommBufferIn = NULL;
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", errco); )
		return errco;
	}

	DEBUGCODE( DebugOutputString("Creating event"); )
	DEBUGCODE( DebugOutputString(EVENT_NAME); )
    CommBuffer->ghEvent = CreateEvent(&sa, TRUE, 0, EVENT_NAME); 

	errco = GetLastError();

    if (CommBuffer->ghEvent == NULL) 
    { 
		if(errco == 0)
			errco = GENERALERRORCODE;

		DEBUGCODE( DebugOutputString("Freeing resources"); )
		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		free(CommBuffer);
		*CommBufferIn = NULL;
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", (int)errco); )
		return errco;
    }

	DEBUGCODE( DebugOutputString("Creating file mapping:"); )
	DEBUGCODE( DebugOutputString(lpSharedMemoryName); )
	DEBUGCODE( DebugOutputWithInteger("Size = %i", SharedMemorySize); )
	CommBuffer->ghMapFile = CreateFileMapping(
                 (HANDLE) INVALID_HANDLE_VALUE,		// use paging file
                 &sa,								// default security 
                 PAGE_READWRITE,					// read/write access
                 0,									// max. object size 
                 SharedMemorySize,					// buffer size 
                 (LPCTSTR) lpSharedMemoryName);		// name of mapping object

	errco = GetLastError();

	if(errco == ERROR_ALREADY_EXISTS)
	{
		DEBUGCODE( DebugOutputString("File mapping already exists"); )
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", errco); )
		bFileExists = TRUE;
	}
 
	if(CommBuffer->ghMapFile == NULL || CommBuffer->ghMapFile == INVALID_HANDLE_VALUE) 
	{ 
		if(errco == 0)
			errco = GENERALERRORCODE;

		DEBUGCODE( DebugOutputString("Freeing resources"); )
		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		CloseHandle(CommBuffer->ghEvent);
		free(CommBuffer);
		*CommBufferIn = NULL;
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", errco); )
		return errco;
	}

	DEBUGCODE( DebugOutputString("Mapping view of file"); )
	CommBuffer->gpBuf = (PSHAREDMEMORY) MapViewOfFile(
						CommBuffer->ghMapFile,	// handle to map object
						FILE_MAP_ALL_ACCESS,	// read/write permission
						0,                   
						0,                   
						0);

	errco = GetLastError();
 
   if (CommBuffer->gpBuf == NULL) 
   { 
		if(errco == 0)
			errco = GENERALERRORCODE;

		DEBUGCODE( DebugOutputString("Freeing resources"); )
		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		CloseHandle(CommBuffer->ghEvent);
		CloseHandle(CommBuffer->ghMapFile);
		free(CommBuffer);
		*CommBufferIn = NULL;
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", errco); )
		return errco;
   }

   if(bFileExists == FALSE)
   {
		DEBUGCODE( DebugOutputString("Setting gpBuf defaults"); )
		DEBUGCODE( DebugOutputWithInteger("iSharedMemSize = %i", SharedMemorySize); )
		DEBUGCODE( DebugOutputWithInteger("iEventBufferSize = %i", SharedMemorySize - 
			(DWORD)((BYTE *)&(CommBuffer->gpBuf->bEventBuffer) - (BYTE *)CommBuffer->gpBuf)); )
	   CommBuffer->gpBuf->dwNextReadOffset = 0;
	   CommBuffer->gpBuf->dwNextWriteOffset = 0;
	   CommBuffer->gpBuf->iLastEventNumWritten = 0;
	   CommBuffer->gpBuf->iSharedMemSize = SharedMemorySize;
	   CommBuffer->gpBuf->iEventBufferSize = SharedMemorySize - 
		   (DWORD)((BYTE *)&(CommBuffer->gpBuf->bEventBuffer) - (BYTE *)CommBuffer->gpBuf);

	   InitNewEvent(CommBuffer);
   }

   DEBUGCODE( DebugOutputString("Releasing mutex"); )
   ReleaseMutex(CommBuffer->ghMutex);

   DEBUGCODE( DebugOutputString("Freeing lpSharedMemoryName"); )
   free((char*)lpSharedMemoryName);

   DEBUGCODE( DebugOutputString("Exiting InitMemoryMgr succesfully"); )
   return SUCCESS;
}

extern INT32 JoinMemoryMgr(LPCTSTR SharedMemoryNameIn, PCOMMBUFFER *CommBufferIn)
{
	int rc;
	PCOMMBUFFER CommBuffer;
	int iSize;
	LPCTSTR lpSharedMemoryName;
	DWORD errco;

	DEBUGCODE( DebugOutputString("Entered JoinMemoryMgr"); )

	DEBUGCODE( DebugOutputString("Creating lpSharedMemoryName"); )
	iSize = strlen(GLOBALPREPEND) + strlen(SharedMemoryNameIn) + 1;
	lpSharedMemoryName = malloc(iSize);
	strcpy_s((char*)lpSharedMemoryName, iSize, GLOBALPREPEND);
	strcat_s((char*)lpSharedMemoryName, iSize, SharedMemoryNameIn);
	DEBUGCODE( DebugOutputString("Done creating lpSharedMemoryName: "); )
	DEBUGCODE( DebugOutputString(lpSharedMemoryName); )
	DEBUGCODE( DebugOutputWithInteger("Size = %i", iSize); )

	DEBUGCODE( DebugOutputString("Allocating memory to CommBuffer: "); )
	DEBUGCODE( DebugOutputWithInteger("Size = %i", sizeof(COMMBUFFER)); )
	CommBuffer = malloc(sizeof(COMMBUFFER));
	*CommBufferIn = CommBuffer;

	InitEvent(CommBuffer);

	CommBuffer->iLastEventNumRead = 0;
	CommBuffer->bSharedMemoryOwner = FALSE;

	DEBUGCODE( DebugOutputString("Opening mutex:"); )
	DEBUGCODE( DebugOutputString(MUTEX_NAME); )
	CommBuffer->ghMutex = OpenMutex(MUTEX_ALL_ACCESS, TRUE, MUTEX_NAME);

	errco = GetLastError();

	if (CommBuffer->ghMutex == NULL) 
	{
		if(errco == 0)
			errco = GENERALERRORCODE;

		DEBUGCODE( DebugOutputString("Freeing resources"); )
		free((char*)lpSharedMemoryName);
		free(CommBuffer);
		*CommBufferIn = NULL;
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", errco); )
		return errco;
	}

	DEBUGCODE( DebugOutputString("Opening event"); )
	DEBUGCODE( DebugOutputString(EVENT_NAME); )
    CommBuffer->ghEvent = OpenEvent(EVENT_ALL_ACCESS, TRUE, EVENT_NAME); 

	errco = GetLastError();

    if (CommBuffer->ghEvent == NULL) 
    { 
		if(errco == 0)
			errco = GENERALERRORCODE;

		DEBUGCODE( DebugOutputString("Freeing resources"); )
		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		free(CommBuffer);
		*CommBufferIn = NULL;
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", errco); )
		return errco;
    }

	DEBUGCODE( DebugOutputString("Opening file mapping:"); )
	DEBUGCODE( DebugOutputString(lpSharedMemoryName); )
   CommBuffer->ghMapFile = OpenFileMapping(
                 FILE_MAP_ALL_ACCESS,				// desired access
                 TRUE,								// inherit handle 
                 lpSharedMemoryName);				// name of mapping object

	errco = GetLastError();

   if(CommBuffer->ghMapFile == NULL) 
   { 
		if(errco == 0)
			errco = GENERALERRORCODE;

		DEBUGCODE( DebugOutputString("Freeing resources"); )
		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		CloseHandle(CommBuffer->ghEvent);
		CloseHandle(CommBuffer->ghMapFile);
		free(CommBuffer);
		*CommBufferIn = NULL;
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", errco); )
		return errco;
   }

   DEBUGCODE( DebugOutputString("Mapping view of file"); )
   CommBuffer->gpBuf = (PSHAREDMEMORY) MapViewOfFile(
						CommBuffer->ghMapFile,				// handle to map object
                        FILE_MAP_ALL_ACCESS,	// read/write permission
                        0,                   
                        0,                   
                        0);           

	errco = GetLastError();
 
   if (CommBuffer->gpBuf == NULL) 
   { 
		if(errco == 0)
			errco = GENERALERRORCODE;

		DEBUGCODE( DebugOutputString("Freeing resources"); )
		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		CloseHandle(CommBuffer->ghEvent);
		CloseHandle(CommBuffer->ghMapFile);
		free(CommBuffer);
		*CommBufferIn = NULL;
		DEBUGCODE( DebugOutputWithInteger("Error code = %i", errco); )
		return errco;
   }

   CommBuffer->dwNextReadOffset = CommBuffer->gpBuf->dwNextReadOffset;

	DEBUGCODE( DebugOutputString("Freeing lpSharedMemoryName"); )
   free((char*)lpSharedMemoryName);

	DEBUGCODE( DebugOutputString("Exiting JoinMemoryMgr successfully"); )
   return SUCCESS;
}

extern INT32 ReleaseMemoryMgr(PCOMMBUFFER *CommBufferIn)
{
	BOOL rc1;
	BOOL rc2;
	PCOMMBUFFER CommBuffer = *CommBufferIn;

	if(CommBuffer == NULL)
	{
		return SUCCESS;
	}

	DEBUGCODE( DebugOutputString("Entered ReleaseMemoryMgr"); )

	DEBUGCODE( DebugOutputString("Releasing Mutex: CommBuffer->ghMutex"); )
	ReleaseMutex(CommBuffer->ghMutex);
	DEBUGCODE( DebugOutputString("Closing Handle: CommBuffer->ghEvent"); )
	CloseHandle(CommBuffer->ghEvent);

	DEBUGCODE( DebugOutputString("Unmapping view of file: CommBuffer->gpBuf"); )
	rc1 = UnmapViewOfFile(CommBuffer->gpBuf);

	DEBUGCODE( DebugOutputString("Closing Handle: CommBuffer->ghMapFile"); )
	rc2 = CloseHandle(CommBuffer->ghMapFile);

	DEBUGCODE( DebugOutputString("Freeing Memory: CommBuffer"); )
	free(CommBuffer);
	*CommBufferIn = NULL;

	if(rc1 == FALSE || rc2 == FALSE)
	{
		DEBUGCODE( DebugOutputString("Exiting ReleaseMemoryMgr with error: Could not either unmap file or close handle"); )
		return GENERALERRORCODE;
	}

	DEBUGCODE( DebugOutputString("Exiting ReleaseMemoryMgr successfully"); )
	return SUCCESS;
}

extern DWORD GetQueueSize(PCOMMBUFFER *CommBufferIn)
{
	PCOMMBUFFER CommBuffer = *CommBufferIn;

	if(CommBuffer == NULL)
	{
		return 0;
	}

	return CommBuffer->gpBuf->iEventBufferSize;
}

extern INT32 PutBuffer(LPCSTR pEventBuffer, DWORD dwEventLength, DWORD dwTimeOut, PCOMMBUFFER *CommBufferIn)
{
	DWORD dwWaitResult;
	WSPEVENT wspEvent;
	INT32 rc;
	PCOMMBUFFER CommBuffer = *CommBufferIn;

	DEBUGCODE( DebugOutputString("Entered PutBuffer"); )

	DEBUGCODE( DebugOutputString("Waiting for Mutex: CommBuffer->ghMutex"); )
	DEBUGCODE( DebugOutputWithInteger("Timeout = %i", dwTimeOut); )
    dwWaitResult = WaitForSingleObject(CommBuffer->ghMutex, dwTimeOut);
 
    if(dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED) 
    {
		DEBUGCODE( DebugOutputString("Preparing to read event"); )
		wspEvent.bReadyToRead = PREPARETOREAD;
		wspEvent.iEventNum = CommBuffer->gpBuf->iLastEventNumWritten + 1;
		DEBUGCODE( DebugOutputWithInteger("Event Number = %i", (int) wspEvent.iEventNum); )
		wspEvent.iEventSize1 = sizeof(WSPEVENT) - 1 + dwEventLength;
		DEBUGCODE( DebugOutputWithInteger("Event Size 1 = %i", wspEvent.iEventSize1); )
		wspEvent.iEventSize2 = wspEvent.iEventSize1;
		DEBUGCODE( DebugOutputWithInteger("Event Size 2 = %i", wspEvent.iEventSize2); )

		DEBUGCODE( DebugOutputString("Copying event to buffer"); )
		DEBUGCODE( DebugOutputWithInteger("Write offset = %i", CommBuffer->gpBuf->dwNextWriteOffset); )
		DEBUGCODE( DebugOutputWithInteger("Event length = %i", dwEventLength); )
		rc = CopyEventToBuffer(CommBuffer->gpBuf->dwNextWriteOffset, &wspEvent, 
								pEventBuffer, dwEventLength, CommBuffer);

		if(rc == SUCCESS)
		{
			DEBUGCODE( DebugOutputString("Successfully copied event to buffer"); )
			CommBuffer->gpBuf->dwNextWriteOffset = (CommBuffer->gpBuf->dwNextWriteOffset + 
				wspEvent.iEventSize1) % CommBuffer->gpBuf->iEventBufferSize;
			DEBUGCODE( DebugOutputWithInteger("Next write offset = %i", CommBuffer->gpBuf->dwNextWriteOffset); )

			DEBUGCODE( DebugOutputString("Incrementing last event number written by one"); )
			CommBuffer->gpBuf->iLastEventNumWritten++;

			DEBUGCODE( DebugOutputString("Releasing mutex: CommBuffer->ghMutex"); )
			ReleaseMutex(CommBuffer->ghMutex);

			DEBUGCODE( DebugOutputString("Pulsing event: CommBuffer->ghEvent"); )
			PulseEvent(CommBuffer->ghEvent);

			DEBUGCODE( DebugOutputString("Exiting PutBuffer successfully"); )
			return SUCCESS;
		}

		DEBUGCODE( DebugOutputString("Releasing mutex: CommBuffer->ghMutex"); )
		ReleaseMutex(CommBuffer->ghMutex);

		DEBUGCODE( DebugOutputString("Exiting PutBuffer with error: Could not copy event to buffer"); )
		return GENERALERRORCODE;
	}
	else
	{
		DEBUGCODE( DebugOutputString("Exiting PutBuffer due to timeout: Did not copy event to buffer"); )
		return TIMEOUT;
	}
}

extern INT32 GetBuffer(LPCSTR pEventBuffer, DWORD dwEventBufferLength, DWORD dwTimeOut, 
						   DWORD *pBytesRead, PCOMMBUFFER *CommBufferIn)
{
	DWORD dwWaitResult;
	INT32 rc;
	BYTE *pEventStartLocation;
	UINT64 iEventNum;
	PCOMMBUFFER CommBuffer = *CommBufferIn;

	DEBUGCODE( DebugOutputString("Entered GetBuffer"); )

	DEBUGCODE( DebugOutputString("Calculating event start location"); )
	pEventStartLocation = &(CommBuffer->gpBuf->bEventBuffer) + CommBuffer->dwNextReadOffset;

	if(CommBuffer->gpBuf->iLastEventNumWritten > CommBuffer->iLastEventNumRead)
	{
		DEBUGCODE( DebugOutputString("There are events to be read"); )
		if(*pEventStartLocation == READYTOREAD || *pEventStartLocation == ALREADYREAD)
		{
			DEBUGCODE( DebugOutputString("Attempting to copy event from buffer"); )
			rc = CopyEventFromBuffer(pEventBuffer, dwEventBufferLength, pBytesRead, &iEventNum, CommBuffer);

			if(rc == SUCCESS)
			{
				DEBUGCODE( DebugOutputString("Successfully copied event from buffer"); )

				DEBUGCODE( DebugOutputWithInteger("Setting last event read to %i", (int)iEventNum); )
				CommBuffer->iLastEventNumRead = iEventNum;

				if(CommBuffer->bSharedMemoryOwner == TRUE)
				{
					DEBUGCODE( DebugOutputString("Thread is shared memory owner"); )

					DEBUGCODE( DebugOutputWithInteger("Setting next read offset to %i", CommBuffer->dwNextReadOffset); )
					CommBuffer->gpBuf->dwNextReadOffset = CommBuffer->dwNextReadOffset;

					DEBUGCODE( DebugOutputString("Setting event start location as ALREADYREAD"); )
					*pEventStartLocation = ALREADYREAD;
				}

				DEBUGCODE( DebugOutputString("Exiting GetBuffer successfully"); )
				return SUCCESS;
			}
			else
			{
				DEBUGCODE( DebugOutputString("Error: Could not copy event from buffer"); )
				DEBUGCODE( DebugOutputWithInteger("Error code = %i", rc); )
				if(rc != ENDOFDATA)
				{
					DEBUGCODE( DebugOutputString("Exiting GetBuffer with error"); )
					return rc;
				}
			}
		}
		else
		{
			DEBUGCODE( DebugOutputWithInteger("Setting next read offset to %i", CommBuffer->dwNextReadOffset); )
			CommBuffer->dwNextReadOffset = CommBuffer->gpBuf->dwNextReadOffset;
		}
	}

	DEBUGCODE( DebugOutputString("Waiting for Event: CommBuffer->ghEvent"); )
	DEBUGCODE( DebugOutputWithInteger("Timeout = %i", dwTimeOut); )
    dwWaitResult = WaitForSingleObject(CommBuffer->ghEvent, dwTimeOut);
 
    if(dwWaitResult == WAIT_OBJECT_0) 
    {
		if(CommBuffer->gpBuf->iLastEventNumWritten > CommBuffer->iLastEventNumRead &&
			(*pEventStartLocation == READYTOREAD || *pEventStartLocation == ALREADYREAD))
		{
			DEBUGCODE( DebugOutputString("Attempting to copy event from buffer"); )
			DEBUGCODE( DebugOutputWithInteger("Event number = %i", (int)iEventNum); )
			rc = CopyEventFromBuffer(pEventBuffer, dwEventBufferLength, pBytesRead, &iEventNum, CommBuffer);

			if(rc == SUCCESS)
			{
				DEBUGCODE( DebugOutputString("Successfully copied event from buffer"); )

				DEBUGCODE( DebugOutputWithInteger("Setting last event read to %i", (int)iEventNum); )
				CommBuffer->iLastEventNumRead = iEventNum;

				if(CommBuffer->bSharedMemoryOwner == TRUE)
				{
					DEBUGCODE( DebugOutputString("Thread is shared memory owner"); )

					DEBUGCODE( DebugOutputWithInteger("Setting next read offset to %i", CommBuffer->dwNextReadOffset); )
					CommBuffer->gpBuf->dwNextReadOffset = CommBuffer->dwNextReadOffset;

					DEBUGCODE( DebugOutputString("Setting event start location as ALREADYREAD"); )
					*pEventStartLocation = ALREADYREAD;
				}

				DEBUGCODE( DebugOutputString("Exiting GetBuffer successfully"); )
				return SUCCESS;
			}
			else
			{
				DEBUGCODE( DebugOutputString("Error: Could not copy event from buffer"); )
				DEBUGCODE( DebugOutputWithInteger("Error code = %i", rc); )
				if(rc != ENDOFDATA)
				{
					DEBUGCODE( DebugOutputString("Exiting GetBuffer with error"); )
					return rc;
				}
			}
		}
	}

	iEventNum = 0;
	*pBytesRead = 0;

	DEBUGCODE( DebugOutputString("Exiting GetBuffer with timeout error"); )
	return TIMEOUT;
}

void InitEvent(PCOMMBUFFER CommBuffer)
{
	CommBuffer->gInitEvent.bReadyToRead = NOTREADYTOREAD;
	CommBuffer->gInitEvent.iEventNum = 0;
	CommBuffer->gInitEvent.iEventSize1 = sizeof(WSPEVENT) - 1;
	CommBuffer->gInitEvent.iEventSize2 = sizeof(WSPEVENT) - 1;
	CommBuffer->gInitEvent.bEvent = 0;
}

void InitNewEvent(PCOMMBUFFER CommBuffer)
{
	WSPEVENT newEvent;

	memcpy_s(&newEvent, sizeof(WSPEVENT), &(CommBuffer->gInitEvent), sizeof(WSPEVENT));

	newEvent.iEventNum = CommBuffer->gpBuf->iLastEventNumWritten;
	CommBuffer->gpBuf->iLastEventNumWritten++;

	CopyEventToBuffer(0, &newEvent, NULL, sizeof(WSPEVENT), CommBuffer);
}

INT32 CopyEventToBuffer(DWORD iOffset, PWSPEVENT pWspEvent, LPCSTR pEventBuffer, DWORD dwEventLength, 
						PCOMMBUFFER CommBuffer)
{
	WSPEVENT wspEvent;
	BYTE *pStart;
	BYTE *pNext;
	DWORD dwSegmentLength;
	DWORD dwEventHeaderSize;

	dwEventHeaderSize = sizeof(WSPEVENT) - 1;

	// Event is larger than event buffer size
	if((dwEventLength + dwEventHeaderSize) >= CommBuffer->gpBuf->iEventBufferSize)
	{
		return GENERALERRORCODE;
	}

	// Event will wrap on buffer
	if((iOffset + dwEventLength + dwEventHeaderSize) >= CommBuffer->gpBuf->iEventBufferSize)
	{
		// Write will overtake next read
		if(CommBuffer->gpBuf->dwNextReadOffset > iOffset)
		{
			return GENERALERRORCODE;
		}
		else
		{
			if(iOffset == CommBuffer->gpBuf->dwNextReadOffset)
			{
				if(*((BYTE *) (&(CommBuffer->gpBuf->bEventBuffer) + iOffset)) == READYTOREAD)
				{
					GetWspEvent(&wspEvent, CommBuffer, iOffset);

					if(wspEvent.iEventSize1 == wspEvent.iEventSize2)
					{
						return GENERALERRORCODE;
					}
				}
			}
			else
			{
				// Write will overtake next read
				if((iOffset + dwEventLength + dwEventHeaderSize - CommBuffer->gpBuf->iEventBufferSize) > 
					CommBuffer->gpBuf->dwNextReadOffset)
				{
					return GENERALERRORCODE;
				}
				else
				{
					if((iOffset + dwEventLength + dwEventHeaderSize - CommBuffer->gpBuf->iEventBufferSize) == 
						CommBuffer->gpBuf->dwNextReadOffset)
					{
						if(*((BYTE *) (&(CommBuffer->gpBuf->bEventBuffer) + CommBuffer->gpBuf->dwNextReadOffset)) == READYTOREAD)
						{
							GetWspEvent(&wspEvent, CommBuffer, CommBuffer->gpBuf->dwNextReadOffset);

							if(wspEvent.iEventSize1 == wspEvent.iEventSize2)
							{
								return GENERALERRORCODE;
							}
						}
					}
				}
			}
		}

		pStart = ((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + iOffset;
		dwSegmentLength = CommBuffer->gpBuf->iEventBufferSize - iOffset;

		// Event header will fit in remaining buffer space
		if(dwSegmentLength > dwEventHeaderSize)
		{
			memcpy_s(pStart, dwEventHeaderSize, pWspEvent, dwEventHeaderSize);

			pNext = pStart + dwEventHeaderSize;
			dwSegmentLength = dwSegmentLength - dwEventHeaderSize;

			if(pEventBuffer != NULL)
			{
				// Copy first part of event to end of buffer
				memcpy_s(pNext, dwSegmentLength, pEventBuffer, dwSegmentLength);

				// Copy last part of event to beginning of buffer
				memcpy_s(&(CommBuffer->gpBuf->bEventBuffer), dwEventLength - dwSegmentLength, 
					((BYTE *) pEventBuffer) + dwSegmentLength, dwEventLength - dwSegmentLength);
			}

			if(*pStart == PREPARETOREAD)
			{
				*pStart = READYTOREAD;
			}
		}
		else
		{
			// Event header will NOT fit in remaining buffer space
			if(dwSegmentLength < dwEventHeaderSize)
			{
				// Copy first part of event header to end of buffer
				memcpy_s(pStart, dwSegmentLength, pWspEvent, dwSegmentLength);

				// Copy second part of event header to beginning of buffer
				memcpy_s(&(CommBuffer->gpBuf->bEventBuffer), dwEventHeaderSize - dwSegmentLength,
					((BYTE *) pWspEvent) + dwSegmentLength, dwEventHeaderSize - dwSegmentLength);

				pNext = ((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwEventHeaderSize - dwSegmentLength;

				if(pEventBuffer != NULL)
				{
					// Copy last part of event
					memcpy_s(pNext, dwEventLength, pEventBuffer, dwEventLength);
				}

				if(*pStart == PREPARETOREAD)
				{
					*pStart = READYTOREAD;
				}
			}
			else
			{
				// Event header fits exactly in remaining buffer space
				memcpy_s(pStart, dwEventHeaderSize, pWspEvent, dwEventHeaderSize);

				if(pEventBuffer != NULL)
				{
					// Copy last part of event at beginning of buffer
					memcpy_s(&(CommBuffer->gpBuf->bEventBuffer), dwEventLength, pEventBuffer, dwEventLength);
				}

				if(*pStart == PREPARETOREAD)
				{
					*pStart = READYTOREAD;
				}
			}
		}
	}
	// Event fits in remainder of buffer
	else
	{
		pStart = &(CommBuffer->gpBuf->bEventBuffer) + iOffset;

		// Write offset is BEFORE next read offset
		if(iOffset < CommBuffer->gpBuf->dwNextReadOffset)
		{
			// Write will overtake next read
			if((iOffset + dwEventLength + dwEventHeaderSize + 1) > CommBuffer->gpBuf->dwNextReadOffset)
			{
				return GENERALERRORCODE;
			}
		}

		// Write offset is EQUAL to next read offset and write will overtake next read
		if((iOffset == CommBuffer->gpBuf->dwNextReadOffset) &&
			(*((BYTE *) (&(CommBuffer->gpBuf->bEventBuffer) + iOffset)) == READYTOREAD ))
		{
			GetWspEvent(&wspEvent, CommBuffer, iOffset);

			if(wspEvent.iEventSize1 == wspEvent.iEventSize2)
			{
				return GENERALERRORCODE;
			}
		}

		// Copy event header
		memcpy_s(pStart, dwEventHeaderSize, pWspEvent, dwEventHeaderSize);

		if(pEventBuffer != NULL)
		{
			// Copy event
			memcpy_s(((BYTE *) pStart) + dwEventHeaderSize, dwEventLength, pEventBuffer, dwEventLength);
		}

		if(*pStart == PREPARETOREAD)
		{
			*pStart = READYTOREAD;
		}
	}

   return SUCCESS;
}

INT32 CopyEventFromBuffer(LPCSTR pEventBuffer, DWORD dwEventBufferLength, DWORD *pBytesRead, UINT64 *pEventNum, 
						  PCOMMBUFFER CommBuffer)
{
	WSPEVENT wspEvent;
	DWORD dwNewOffset;
	LPCSTR pStart;
	DWORD dwSegmentLength;
	DWORD dwEventLength;
	DWORD dwEventHeaderSize;

	dwEventHeaderSize = sizeof(WSPEVENT) - 1;

	*pBytesRead = 0;
	*pEventNum = 0;

	if(dwEventHeaderSize > dwEventBufferLength)
	{
		return GENERALERRORCODE;
	}

	pStart = GetWspEvent(&wspEvent, CommBuffer, CommBuffer->dwNextReadOffset);

	if(wspEvent.iEventSize1 != wspEvent.iEventSize2)
	{
		return ENDOFDATA;
	}

	dwEventLength = wspEvent.iEventSize1 - dwEventHeaderSize;

	*pBytesRead = dwEventLength;
	*pEventNum = wspEvent.iEventNum;

	if(dwEventLength > dwEventBufferLength)
	{
		return OVERFLOW;
	}

	// Event wraps buffer
	if( ((BYTE *) pStart - (BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwEventLength > 
		CommBuffer->gpBuf->iEventBufferSize)
	{
		dwSegmentLength = CommBuffer->gpBuf->iEventBufferSize - 
			((BYTE *) pStart - (BYTE *) &(CommBuffer->gpBuf->bEventBuffer));

		// Copy first part of event
		memcpy_s((void *)pEventBuffer, (size_t) dwSegmentLength, pStart, (size_t) dwSegmentLength);

		// Wrap and copy last part of event
		memcpy_s((void *)((BYTE *) pEventBuffer + dwSegmentLength), (size_t) (dwEventLength - dwSegmentLength), 
			&(CommBuffer->gpBuf->bEventBuffer), (size_t) (dwEventLength - dwSegmentLength));

		dwNewOffset = dwEventLength - dwSegmentLength;
	}
	else
	{
		// Copy event
		memcpy_s((void *)pEventBuffer, (size_t) dwEventLength, pStart, (size_t) dwEventLength);

		dwNewOffset = ((BYTE *) pStart - (BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwEventLength;
	}

	CommBuffer->dwNextReadOffset = dwNewOffset;

	if(CommBuffer->dwNextReadOffset == CommBuffer->gpBuf->iEventBufferSize)
	{
		CommBuffer->dwNextReadOffset = 0;
	}

	return SUCCESS;
}

LPCSTR GetWspEvent(PWSPEVENT pWspEvent, PCOMMBUFFER CommBuffer, DWORD dwStartOffset)
{
	LPCSTR pStart;
	DWORD dwSegmentLength;
	DWORD dwEventHeaderSize;

	dwEventHeaderSize = sizeof(WSPEVENT) - 1;

	// Event header wraps the buffer
	if((dwStartOffset + dwEventHeaderSize) >= CommBuffer->gpBuf->iEventBufferSize)
	{
		dwSegmentLength = CommBuffer->gpBuf->iEventBufferSize - dwStartOffset;

		// Copy first part of event header
		memcpy_s(pWspEvent, dwSegmentLength, ((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwStartOffset, dwSegmentLength);

		// Wrap and copy last part of event header
		memcpy_s(((BYTE *) pWspEvent) + dwSegmentLength, dwEventHeaderSize - dwSegmentLength, 
			&(CommBuffer->gpBuf->bEventBuffer), dwEventHeaderSize - dwSegmentLength);

		// Starting location for event
		pStart = ((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwEventHeaderSize - dwSegmentLength;
	}
	else
	{
		// Copy event header
		memcpy_s(pWspEvent, dwEventHeaderSize, 
			((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwStartOffset, dwEventHeaderSize);

		pStart = ((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwStartOffset + dwEventHeaderSize;
	}

	return pStart;
}
