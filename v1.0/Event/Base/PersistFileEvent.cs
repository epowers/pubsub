using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.Security.Cryptography;

namespace Microsoft.WebSolutionsPlatform.Event
{
    public enum PersistFileState : byte
    {
        /// <summary>
        /// File was opened
        /// </summary>
        Open = 0,
        /// <summary>
        /// File was closed
        /// </summary>
        Close = 1,
        /// <summary>
        /// File was copied to destination's temp directory
        /// </summary>
        Copy = 2,
        /// <summary>
        /// File was moved from destination's temp directory to destination directory
        /// </summary>
		Move = 3,
        /// <summary>
        /// File copy failed
        /// </summary>
        CopyFailed = 4,
        /// <summary>
        /// File move failed
        /// </summary>
        MoveFailed = 5
    }
    
    /// <summary>
    /// This class is used to tract the state of the files for persisted events.
    /// </summary>
    public class PersistFileEvent : Event
    {
        private PersistFileState fileState;
        /// <summary>
        /// File's state
        /// </summary>
        public PersistFileState FileState
        {
            get
            {
                return fileState;
            }
            set
            {
                fileState = value;
            }
        }

        private string fileNameBase;
        /// <summary>
        /// Base name of the file
        /// </summary>
        public string FileNameBase
        {
            get
            {
                return fileNameBase;
            }
            set
            {
                fileNameBase = value;
            }
        }

        private string fileName;
        /// <summary>
        /// Fully qualified name of the file
        /// </summary>
        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }

        private Guid persistEventType;
        /// <summary>
        /// Event type which is persisted in file
        /// </summary>
        public Guid PersistEventType
        {
            get
            {
                return persistEventType;
            }
            set
            {
                persistEventType = value;
            }
        }

        private long fileSize;
        /// <summary>
        /// Size in bytes of the file
        /// </summary>
        public long FileSize
        {
            get
            {
                return fileSize;
            }
            set
            {
                fileSize = value;
            }
        }

        private long settingMaxFileSize;
        /// <summary>
        /// Config setting for maximum size of the file
        /// </summary>
        public long SettingMaxFileSize
        {
            get
            {
                return settingMaxFileSize;
            }
            set
            {
                settingMaxFileSize = value;
            }
        }

        private int settingMaxCopyInterval;
        /// <summary>
        /// Config setting for maximum file copy interval in seconds
        /// </summary>
        public int SettingMaxCopyInterval
        {
            get
            {
                return settingMaxCopyInterval;
            }
            set
            {
                settingMaxCopyInterval = value;
            }
        }

        private bool settingLocalOnly;
        /// <summary>
        /// Config setting for if the persistence subscription is for all events or only local events
        /// </summary>
        public bool SettingLocalOnly
        {
            get
            {
                return settingLocalOnly;
            }
            set
            {
                settingLocalOnly = value;
            }
        }

        private string settingFieldTerminator;
        /// <summary>
        /// Config setting for the field terminator
        /// </summary>
        public string SettingFieldTerminator
        {
            get
            {
                return settingFieldTerminator;
            }
            set
            {
                settingFieldTerminator = value;
            }
        }

        private string settingRowTerminator;
        /// <summary>
        /// Config setting for the row terminator
        /// </summary>
        public string SettingRowTerminator
        {
            get
            {
                return settingRowTerminator;
            }
            set
            {
                settingRowTerminator = value;
            }
        }

        /// <summary>
        /// Base constructor to create a new persist file event
        /// </summary>
        public PersistFileEvent()
            :
            base()
        {
            EventType = new Guid(@"62D531DE-5F76-43a4-B0B8-1DF325B6787E");
        }

        /// <summary>
        /// Base constructor to create a new persist file event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event data</param>
        public PersistFileEvent(byte[] serializationData)
            :
            base(serializationData)
        {
            EventType = new Guid(@"62D531DE-5F76-43a4-B0B8-1DF325B6787E");
        }

        /// <summary>
        /// Used for event serialization.
        /// </summary>
        /// <param name="data">SerializationData object passed to store serialized object</param>
        public override void GetObjectData(SerializationData data)
        {
            data.AddElement(@"FileState", (byte)fileState);
            data.AddElement(@"FileName", fileNameBase);
            data.AddElement(@"FileDirectory", fileName);
            data.AddElement(@"PersistEventType", persistEventType);
            data.AddElement(@"FileSize", fileSize);
            data.AddElement(@"SettingMaxFileSize", settingMaxFileSize);
            data.AddElement(@"SettingMaxCopyInterval", settingMaxCopyInterval);
            data.AddElement(@"SettingLocalOnly", settingLocalOnly);
            data.AddElement(@"SettingFieldTerminator", settingFieldTerminator);
            data.AddElement(@"SettingRowTerminator", settingRowTerminator);
        }
    }
}
