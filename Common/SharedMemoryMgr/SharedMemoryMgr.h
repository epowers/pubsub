
typedef struct _SHAREDMEMORY
{
	UINT32	iSharedMemSize;		// Size of the shared memory buffer
	DWORD   dwNextReadOffset;	// Offset of next event entry to read
	DWORD   dwNextWriteOffset;	// Offset of next event entry to write
	UINT64	iLastEventNumWritten;		// Sequential number assigned to each event
	DWORD	iEventBufferSize;	// Size of the event buffer
	BYTE	bEventBuffer;		// Buffer for storing events
} SHAREDMEMORY, *PSHAREDMEMORY;

#pragma pack(1)
typedef struct _WSPEVENT
{
	BYTE	bReadyToRead;		// 0xFA if event is ready to be read
	UINT32	iEventSize1;		// Size of the event
	UINT64	iEventNum;			// Number assigned to this event
	UINT32	iEventSize2;		// Size of the event. THIS AND THE PREVIOUS MUST MATCH
	BYTE	bEvent;				// Event data
} WSPEVENT, *PWSPEVENT;

typedef struct _COMMBUFFER
{
	HANDLE			ghMapFile;
	HANDLE			ghMutex;
	HANDLE			ghEvent;
	UINT64			iLastEventNumRead;
	DWORD			dwNextReadOffset;
	PSHAREDMEMORY	gpBuf;
	BYTE			bSharedMemoryOwner;
	WSPEVENT		gInitEvent;
} COMMBUFFER, *PCOMMBUFFER;

