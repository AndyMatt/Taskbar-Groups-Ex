using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TaskbarGroupsEx.Classes;

namespace TaskbarGroupsEx.Handlers
{
    internal class lnkFileHandler
    {
        #region BlockReader
        private class BlockReader<T>
        {
            public dynamic? ReadBlock(ByteReader byteReader)
            {
                return ReadBlock(byteReader, byteReader.scan_uint());
            }

            public static dynamic? ReadBlock(ByteReader byteReader, uint byteLength)
            {
                return ReadBlock(byteReader.read_bytes(byteLength));
            }

            public static dynamic? ReadBlock(byte[] bytes)
            {
                T? block = default(T);
                IntPtr _Pointer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, _Pointer, bytes.Length);
                object? _Obj = Marshal.PtrToStructure(_Pointer, typeof(T));
                if (_Obj != null)
                {
                    block = (T)_Obj;
                }
                return block;
            }
        }
        #endregion

        #region EnumeratorDefs
        public enum DataFlags : uint
        {
            HasTargetIDList = 0x00000001, //The LNK file contains a link target identifier
            HasLinkInfo = 0x00000002, //The LNK file contains location information
            HasName = 0x00000004, //The LNK file contains a description data string
            HasRelativePath = 0x00000008, //The LNK file contains a relative path data string
            HasWorkingDir = 0x00000010, //The LNK file contains a working directory data string
            HasArguments = 0x00000020, //The LNK file contains a command line arguments data string
            HasIconLocation = 0x00000040, //The LNK file contains a custom icon location
            IsUnicode = 0x00000080, //The data strings in the LNK file are stored in Unicode (UTF-16 little-endian) instead of ASCII
            ForceNoLinkInfo = 0x00000100, //The location information is ignored
            HasExpString = 0x00000200, //The LNK file contains environment variables location data block
            RunInSeparateProcess = 0x00000400, //A 16-bit target application is run in a separate virtual machine.
            Unknown_1 = 0x00000800, //(Reserved)
            HasDarwinID = 0x00001000, //The LNK file contains a Darwin (Mac OS-X) properties data block
            RunAsUser = 0x00002000, //The target application is run as a different user.
            HasExpIcon = 0x00004000, //The LNK file contains an icon location data block
            NoPidlAlias = 0x00008000, //The file system location is represented in the shell namespace when the path to an item is parsed into the link target identifiers. Contains a known folder location data block?
            Unknown_2 = 0x00010000, //(Reserved)
                                    //Windows Vista and later?
            RunWithShimLayer = 0x00020000, //The target application is run with the shim layer. The LNK file contains shim layer properties data block.
            ForceNoLinkTrack = 0x00040000, //The LNK does not contain a distributed link tracking data block
            EnableTargetMetadata = 0x00080000, //The LNK file contains a metadata property store data block
            DisableLinkPathTracking = 0x00100000, //The environment variables location block should be ignored
            DisableKnownFolderTracking = 0x00200000, //Unknown
            DisableKnownFolderAlias = 0x00400000, //Unknown
            AllowLinkToLink = 0x00800000, //Unknown
            UnaliasOnSave = 0x01000000, //Unknown
            PreferEnvironmentPath = 0x02000000, //Unknown
            KeepLocalIDListForUNCTarget = 0x04000000, //Unknown
        }

        public enum FileAttributeFlags : uint
        {
            FILE_ATTRIBUTE_READONLY = 0x00000001, //Is read-only
            FILE_ATTRIBUTE_HIDDEN = 0x00000002, //Is hidden
            FILE_ATTRIBUTE_SYSTEM = 0x00000004, //Is a system file or directory
            RESERVED = 0x00000008, //Reserved, not used by the LNK format (Is a volume label)
            FILE_ATTRIBUTE_DIRECTORY = 0x00000010, //Is a directory
            FILE_ATTRIBUTE_ARCHIVE = 0x00000020, //Should be archived
            FILE_ATTRIBUTE_DEVICE = 0x00000040, //Reserved, not used by the LNK format (Is a device)
            FILE_ATTRIBUTE_NORMAL = 0x00000080, //Is normal (None of the other flags should be set)
            FILE_ATTRIBUTE_TEMPORARY = 0x00000100, //Is temporary
            FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200, //Is a sparse file
            FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400, //Is a reparse point or symbolic link
            FILE_ATTRIBUTE_COMPRESSED = 0x00000800, //Is compressed
            FILE_ATTRIBUTE_OFFLINE = 0x00001000, //Is offline (The data of the file is stored on an offline storage.)
            FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000, //Do not index content (The content of the file or directory should not be indexed by the indexing service.)
            FILE_ATTRIBUTE_ENCRYPTED = 0x00004000, //Is encrypted
            UNKNOWN = 0x00008000, //(seen on Windows 95 FAT)
            FILE_ATTRIBUTE_VIRTUAL = 0x00010000, //Currently reserved for future use, not used by the LNK format (Is virtual)
        }

        public enum ShowWindowFlags : uint
        {
            SW_HIDE = 0, //Hides the window and activates another window.
            SW_SHOWNORMAL = 1, //Activates and displays the window. The window is restored to its original size and position if the window is minimized or maximized.
            SW_SHOWMINIMIZED = 2, //Activates and minimizes the window.
            SW_SHOWMAXIMIZE = 3, //Activates and maximizes the window.
            SW_SHOWNOACTIVATE = 4, //Display the window in its most recent position and size without activating it.
            SW_SHOW = 5, //Activates the window and displays it in its current size and position.
            SW_MINIMIZE = 6, //Minimizes the window and activates the next top-level windows (in order of depth (Z order))
            SW_SHOWMINNOACTIVE = 7, //Display the window as minimized without activating it.
            SW_SHOWNA = 8, //Display the window in its current size and position without activating it.
            SW_RESTORE = 9, //Activates and displays the window. The window is restored to its original size and position if the window is minimized or maximized.
            SW_SHOWDEFAULT = 10, //Set the show state based on the ShowWindow values specified during the creation of the process.
            SW_FORCEMINIMIZE = 11, //Minimizes a window, even if the thread that owns the window is not responding.
            SW_NORMALNA = 0xcc//Undocumented according to wine project.
        }

        enum HotyKeyHigh : byte
        {
            HOTKEYF_SHIFT = 0x1,
            HOTKEYF_CONTROL = 0x2,
            HOTKEYF_ALT = 0x4,
        }
        #endregion

        #region ShellStructures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ShellLinkHeader
        {
            public uint mSize;
            public Guid mClassIdentifier;
            public DataFlags mDataFlags;
            public FileAttributeFlags mFileAttributeFlags;
            public System.Runtime.InteropServices.ComTypes.FILETIME mCreationDateTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME mLastAccessDateTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME mLastModifiedDateTime;
            public uint mFileSize;
            public uint mIconIndexValue;
            public uint mShowWindow;
            public ushort mHotKey; //Low Byte = Key; High Byte Modifier (HotyKeyHigh)
            ushort RESERVED1;
            UInt64 RESERVED2;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ShellLinkInfoHeader
        {
            public uint mSize;
            public uint mHeaderSize;
            public uint mLocationFlags;
            public uint mOffsetVolumeInfo;
            public uint mOffsetLocalPath;
            public uint mOffsetNetworkShareInfo;
            public uint mOffsetCommonPath;
            public uint mOffsetLocalPathW;
            public uint mOffsetCommonPathW;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ShellVolumeInfo
        {
            public uint mSize;
            public uint mDriveType;
            public uint mDriveSN;
            public uint mOffsetLabel;
            public uint mOffsetLabelW;
            public string mVolumeLabel;
            public string mVolumeLabelW;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ShellLinkInfo
        {
            public ShellLinkInfoHeader mHeader;
            public ShellVolumeInfo mVolumeInfo;
            public string mLocalPath;
            public string mNetworkCommonPath;
            public string mLocalPathW;
            public string mNetworkCommonPathW;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LocationBlockPathW
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            private readonly string mString;
            public override string ToString() { return mString; }
            public static bool operator ==(LocationBlockPathW s1, string s2) { return s1.mString == s2;  }
            public static bool operator !=(LocationBlockPathW s1, string s2) { return s1.mString != s2; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct EnvironmentVariableLocationBlock
        {
            public uint mSize;
            public uint mSigniture;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string mPath;
            public LocationBlockPathW mPathW;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DarwinPropertyBlock
        {
            public uint mSize;
            public uint mSigniture;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string mPath;
            public LocationBlockPathW mPathW;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct IconLocationDataBlock
        {
            public uint mSize;
            public uint mSigniture;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string mPath;
            public LocationBlockPathW mPathW;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ConsolePropertyBlock
        {
            public uint mSize;
            public uint mSigniture;
            public ushort mColorFlags;
            public ushort mPopupFillAttributes;
            public ushort mScreenWidthBufferSize;
            public ushort mScreenHeightBufferSize;
            public ushort mWindowWidth;
            public ushort mWindowHeight;
            public ushort mWindowOriginX;
            public ushort mWindowOriginY;
            uint mUnknown_1;
            uint mUnknown_2;
            uint mFontSize;
            uint mFontFamily;
            uint mFontWeight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            string mFaceName;
            uint mCursorSize;
            uint mFullScreen;
            uint mInsertMode;
            uint mAutomaticPositioning;
            uint mHistoryBufferSize;
            uint mHistoryBufferNumber;
            uint mDuplicatesAllowedInHistory;
            UInt128 mColorTable1;
            UInt128 mColorTable2;
            UInt128 mColorTable3;
            UInt128 mColorTable4;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DistrubutedLinkTracker
        {
            public uint mSize;
            public uint mSigniture;
            public uint mLinkTrackerSize;
            public uint mVersion;
            Guid mMachineID;
            Guid mDriodVolumeID;
            Guid mDriodFileID;
            Guid mBirthDriodVolumeID;
            Guid mBirthDriodFileID;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct CodePageBlock
        {
            public uint mSize;
            public uint mSigniture;
            public uint mCodePage;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SpecialFolderLocationBlock
        {
            public uint mSize;
            public uint mSigniture;
            public uint mSpecialFolderID;
            public uint mFirstChildSegment;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ShimLayerPropertyBlock
        {
            public uint mSize;
            public uint mSigniture;
            public string mName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct KnownFolderLocationBlock
        {
            public uint mSize;
            public uint mSigniture;
            public Guid mKnownFolder;
            public uint mFirstChildSegmentOffset;
        }
        #endregion

        #region ApplicationShellItem
        internal class ApplicationShellItem
        {
            public string? ID;
            public string? ItemDisplayName;
            public string? PackageFamilyName;
            public string? PackageFullName;
            public string? PackageInstallPath;
            public string? LongDisplayName;
            public string? SmallLogo; //SmallLogoPath(Icon);
            public string? TileSmall; //Square70x70LogoPath;
            public string? TileLarge; //Square310x310LogoPath;

            private static Guid GuidApplicationShellItem = new Guid("4234d49b-0245-4df3-b780-3893943456e1");
            private static Guid GuidApplicationShellPropertySets = new Guid("9f4c2855-9f79-4b39-a8d0-e1d42de1d5f3");
            private static Guid GuidCommonOpenProperties = new Guid("b725f130-47ef-101a-a5f1-02608c9eebac");
            private static Guid GuidTileProperties = new Guid("86d40b4d-9069-443c-819a-2a54090dccec");

            private uint GetProperty(ByteReader byteReader, ref object? property)
            {
                uint _size = byteReader.scan_uint();
                using (ByteReader propSetReader = new ByteReader(byteReader.read_bytes(_size)))
                {
                    propSetReader.read_uint();//skipSize
                    uint propertyType = propSetReader.read_uint();
                    propSetReader.read_byte(); //buffer
                    if(propSetReader.read_uint() == 0x1f)
                    {
                        property = propSetReader.read_LPWSTR();
                    }
                    return propertyType;
                }
            }

            private void ReadPropertySets(ByteReader byteReader)
            {
                if (byteReader.findGuid(GuidApplicationShellPropertySets) == uint.MaxValue)
                    return;

                ByteReader propSetReader = new ByteReader(byteReader);

                propSetReader.jump2guid(GuidApplicationShellPropertySets);
                while (propSetReader.scan_uint() != 0)
                {
                    object? _property = null;
                    uint propertyType = GetProperty(propSetReader, ref _property);

                    switch (propertyType)
                    {
                        case 5: ID = (string?)_property; break; //ID
                        case 15: PackageInstallPath = (string?)_property; break; //PackageInstallPath
                        case 17: PackageFamilyName = (string?)_property; break; //PackageFamilyName
                        case 21: PackageFullName = (string?)_property; break; //PackageFullName
                    }
                }
            }

            private void ReadCommonOpenProperties(ByteReader byteReader)
            {
                if (byteReader.findGuid(GuidCommonOpenProperties) == uint.MaxValue)
                    return;

                ByteReader propSetReader = new ByteReader(byteReader);
                propSetReader.jump2guid(GuidCommonOpenProperties);
                while (propSetReader.scan_uint() != 0)
                {
                    object? _property = null;
                    uint propertyType = GetProperty(propSetReader, ref _property);
                    if (propertyType == 10) //ItemDisplayName
                    {
                        ItemDisplayName = (string?)_property;
                    }
                }
            }

            private void ReadTileProperties(ByteReader byteReader)
            {
                if (byteReader.findGuid(GuidTileProperties) == uint.MaxValue)
                    return;

                ByteReader propSetReader = new ByteReader(byteReader);
                propSetReader.jump2guid(GuidTileProperties);
                while (propSetReader.scan_uint() != 0)
                {
                    object? _property = null;
                    uint propertyType = GetProperty(propSetReader, ref _property);
                    switch (propertyType)
                    {
                        case 2: SmallLogo = (string?)_property; break; //SmallLogoPath
                        case 11: LongDisplayName = (string?)_property; break; //LongDisplayName
                        case 19: TileLarge = (string?)_property; break; //Square310x310LogoPath
                        case 20: TileSmall = (string?)_property; break; //Square70x70LogoPath
                    }
                }
            }

            private ApplicationShellItem(ByteReader byteReader)
            {
                ReadPropertySets(byteReader);
                ReadCommonOpenProperties(byteReader);
                ReadTileProperties(byteReader);
            }

            public static ApplicationShellItem? GetApplicationShellItem(ByteReader byteReader)
            {
                uint AppShellLocation = byteReader.findGuid(GuidApplicationShellItem);
                if (AppShellLocation == uint.MaxValue)
                    return null;

                return new ApplicationShellItem(byteReader);
            }
        }
        #endregion
        
        private static Guid GuidLnkFile = new Guid("00021401-0000-0000-C000-000000000046");
        private const int GUID_SIZE = 16;

        private static Dictionary<uint, Type> BlockLookup = new Dictionary<uint, Type>()
        {
            {0xa0000001, typeof(EnvironmentVariableLocationBlock) },
            {0xa0000002, typeof(ConsolePropertyBlock) },
            {0xa0000003, typeof(DistrubutedLinkTracker) },
            {0xa0000004, typeof(CodePageBlock) },
            {0xa0000005, typeof(SpecialFolderLocationBlock) },
            {0xa0000006, typeof(DarwinPropertyBlock) },
            {0xa0000007, typeof(IconLocationDataBlock) },
            {0xa0000008, typeof(ShimLayerPropertyBlock) },
            {0xa000000b, typeof(KnownFolderLocationBlock) },
        };

        private static Dictionary<DataFlags, string> DataStringsFlags = new Dictionary<DataFlags, string>()
        {
            {  DataFlags.HasName, "Description" },
            {  DataFlags.HasRelativePath, "RelativePath" },
            {  DataFlags.HasWorkingDir, "WorkingDirectory" },
            {  DataFlags.HasArguments, "Arguments" },
            {  DataFlags.HasIconLocation, "IconLocation" },
        };

        ShellLinkHeader mShellLinkHeader;
        ShellLinkInfo? mShellLinkInfo;
        Dictionary<string, object> mDataStrings;
        Dictionary<string, object> mDataBlocks;
        ApplicationShellItem? mApplicationShellItem;

        public lnkFileHandler(MemoryStream dropData)
        {
            mDataStrings = new Dictionary<string, object>();
            mDataBlocks = new Dictionary<string, object>();

            byte[] bs = dropData.ToArray();
            ByteReader byteReader = new ByteReader(bs);

            byte[] shellHeaderBytes = byteReader.read_bytes(byteReader.scan_uint());

            mShellLinkHeader = BlockReader<ShellLinkHeader>.ReadBlock(shellHeaderBytes);


            if (mShellLinkHeader.mDataFlags == (mShellLinkHeader.mDataFlags | DataFlags.HasTargetIDList))
            {
                ReadShellItems(byteReader);
            }

            if (mShellLinkHeader.mDataFlags == (mShellLinkHeader.mDataFlags | DataFlags.HasLinkInfo))
            {
                mShellLinkInfo = ReadShellLinkInfo(byteReader);
            }

            mDataStrings = ProcessStringDataFlags(mShellLinkHeader.mDataFlags, byteReader);

            while (!byteReader.End() && byteReader.scan_uint() != 0)
            {
                uint blockSize = byteReader.scan_uint();
                ByteReader blockbyte = new ByteReader(byteReader.read_bytes(blockSize));
                uint blockID = blockbyte.scan_uint(4);

                if (blockID == 0xa0000009)
                {
                    ReadPropertyStoreDataBlock(blockbyte);
                }
                else if (blockID == 0xa000000c)
                {
                    ReadShellItems(blockbyte);
                }
                else if (BlockLookup.ContainsKey(blockID))
                {
                    Type blockReaderClass = typeof(BlockReader<>);
                    Type constructedClass = blockReaderClass.MakeGenericType(BlockLookup[blockID]);
                    dynamic? blockReader = Activator.CreateInstance(constructedClass);
                    if (blockReader != null)
                    {
                        dynamic? block = blockReader.ReadBlock(blockbyte);
                        if(block is not null)
                        {
                            mDataBlocks.Add(BlockLookup[blockID].Name, block);
                        }
                    }
                }
            }
        }

        public static bool Islnk(MemoryStream memoryStream)
        {
            ReadOnlySpan<byte> guidBytes = new ReadOnlySpan<byte>(memoryStream.ToArray(), sizeof(uint), GUID_SIZE);
            Guid guid = new Guid(guidBytes);
            if (guid == GuidLnkFile)
                return true;

            return false;
        }

        public ShortcutType GetShortcutType()
        {
            if (mShellLinkInfo == null && mApplicationShellItem != null)
                return ShortcutType.UWP;

            string targetCmd = GetTargetCommand();

            if (Path.GetExtension(targetCmd) == ".exe")
                return ShortcutType.Application;

            if (File.Exists(targetCmd))
                return ShortcutType.File;

            if (Directory.Exists(targetCmd))
                return ShortcutType.Directory;

            return ShortcutType.Unknown;

        }

        public bool isUWP()
        {
            if (mShellLinkInfo == null && mApplicationShellItem != null)
                return true;

            return false;
        }

        public bool isExe()
        {
            if(Path.GetExtension(GetTargetCommand()) == ".exe")
                return true;

            return false;
        }

        public bool isPath()
        {
            if (!isExe())
            {
                if(!File.Exists(GetTargetCommand()))
                { 
                    return Directory.Exists(GetTargetCommand());
                }
            }
            return false;
        }

        public bool isFile()
        {
            if (!isExe())
            {
                return File.Exists(GetTargetCommand());
            }
            return false;
        }

        public string GetTargetCommand()
        {
            if (mApplicationShellItem != null)
            {
                if (mApplicationShellItem.PackageFamilyName != null)
                { return mApplicationShellItem.PackageFamilyName; }
            }
            if (mShellLinkInfo != null)
            {
                ShellLinkInfo _shellLinkInfo = (ShellLinkInfo)mShellLinkInfo;
                return _shellLinkInfo.mLocalPathW != "" ? _shellLinkInfo.mLocalPathW : _shellLinkInfo.mLocalPath;
            }
            return "";
        }

        public string GetIconPath()
        {
            //ShellLink.GetIconPath(file);

            if (mApplicationShellItem != null)
            {
                if (mApplicationShellItem.SmallLogo != null)
                    return mApplicationShellItem.SmallLogo;
            }
            if (mDataBlocks.ContainsKey("IconLocationDataBlock"))
            {
                IconLocationDataBlock iconLocationBlock = (IconLocationDataBlock)mDataBlocks["IconLocationDataBlock"];
                return iconLocationBlock.mPathW == "" ? iconLocationBlock.mPathW.ToString() : iconLocationBlock.mPath;
            }
            return GetTargetCommand();
        }

        Dictionary<string, object> ProcessStringDataFlags(DataFlags mDataFlags, ByteReader byteReader)
        {
            Dictionary<string, object> mData = new Dictionary<string, object>();

            foreach (KeyValuePair<DataFlags, String> flag in DataStringsFlags)
            {
                if (mDataFlags == (mDataFlags | flag.Key))
                {
                    ushort StrLen = byteReader.read_ushort();
                    string StrW = byteReader.read_UnicodeString((int)StrLen);
                    mData.Add(flag.Value, StrW);
                }
            }

            return mData;
        }

        void ReadShellItems(ByteReader byteReader)
        {
            ushort IDListSize = byteReader.read_ushort();
            byte[] IDListBytes = byteReader.read_bytes(IDListSize);
            ByteReader IDByteReader = new ByteReader(IDListBytes);

            mApplicationShellItem = ApplicationShellItem.GetApplicationShellItem(IDByteReader);
        }

        ShellLinkInfoHeader ReadShellLinkInfoHeader(ByteReader byteReader)
        {
            uint mHeaderSize = byteReader.scan_uint(4);
            ShellLinkInfoHeader shellInforHeader = BlockReader<ShellLinkInfoHeader>.ReadBlock(byteReader, mHeaderSize);

            if (mHeaderSize <= 28)
            {
                shellInforHeader.mOffsetCommonPathW = shellInforHeader.mOffsetLocalPathW = 0;
            }
            return shellInforHeader;
        }

        ShellLinkInfo ReadShellLinkInfo(ByteReader byteReader)
        {
            ShellLinkInfo shellLinkInfo = new ShellLinkInfo();
            shellLinkInfo.mHeader = ReadShellLinkInfoHeader(byteReader);
            shellLinkInfo.mVolumeInfo = ReadVolumeInformation(byteReader);
            shellLinkInfo.mLocalPath = byteReader.read_AsciiString();
            shellLinkInfo.mNetworkCommonPath = byteReader.read_AsciiString();
            shellLinkInfo.mLocalPathW = (shellLinkInfo.mHeader.mOffsetLocalPathW > 0) ? byteReader.read_UnicodeString() : "";
            shellLinkInfo.mNetworkCommonPathW = (shellLinkInfo.mHeader.mOffsetCommonPathW > 0) ? byteReader.read_UnicodeString() : "";
            return shellLinkInfo;
        }

        ShellVolumeInfo ReadVolumeInformation(ByteReader byteReader)
        {
            ShellVolumeInfo shellVolumeInfo = new ShellVolumeInfo();
            shellVolumeInfo.mSize = byteReader.read_uint();
            shellVolumeInfo.mDriveType = byteReader.read_uint();
            shellVolumeInfo.mDriveSN = byteReader.read_uint();
            shellVolumeInfo.mOffsetLabel = byteReader.read_uint();
            shellVolumeInfo.mOffsetLabelW = shellVolumeInfo.mOffsetLabel > 16 ? byteReader.read_uint() : 0;
            shellVolumeInfo.mVolumeLabel = byteReader.read_AsciiString();
            shellVolumeInfo.mVolumeLabelW = byteReader.read_UnicodeString((int)shellVolumeInfo.mOffsetLabelW);
            return shellVolumeInfo;
        }

        ShimLayerPropertyBlock ReadShimLayerPropertyBlock(ByteReader byteReader)
        {
            ShimLayerPropertyBlock shimLayerPropertyBlock = new ShimLayerPropertyBlock();
            shimLayerPropertyBlock.mSize = byteReader.read_uint();
            shimLayerPropertyBlock.mSigniture = byteReader.read_uint();
            shimLayerPropertyBlock.mName = byteReader.read_UnicodeString((int)shimLayerPropertyBlock.mSize - 8);
            return shimLayerPropertyBlock;
        }


        void ReadPropertyStoreDataBlock(ByteReader byteReader)
        {
            ByteReader propertyReader = new ByteReader(byteReader.read_bytes(byteReader.scan_uint()));

            uint mSize = propertyReader.read_uint();
            uint mSigniture = propertyReader.read_uint();
                
            return;
            /* //Do nothing for now
            while (!propertyReader.End() && propertyReader.scan_uint() != 0)
            {
                WindowsPropertySet propertySet = new WindowsPropertySet(propertyReader);                   
            }*/
        }
        
    }
}
