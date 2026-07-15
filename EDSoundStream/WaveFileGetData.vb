'Adapted from Pieter Philippaerts
Imports System.IO
Imports System.Runtime.InteropServices

    Public Class WaveFileGetData
#Region "<API-DECLARES>"
        Private Const MMIO_READ As Integer = &H0
        Private Const MMIO_FINDCHUNK As Integer = &H10
        Private Const MMIO_FINDRIFF As Integer = &H20
        Private Const MMSYSERR_NOERROR As Integer = 0
        Private Const SEEK_CUR As Integer = 1
        Private Const SEEK_END As Integer = 2
        Private Const SEEK_SET As Integer = 0
        Private Const BUFFER_SECONDS As Single = 0.1
        Private Structure MMIOINFO
            Public dwFlags As Integer
            Public fccIOProc As Integer
            Public pIOProc As Integer
            Public wErrorRet As Integer
            Public htask As Integer
            Public cchBuffer As Integer
            Public pchBuffer As String
            Public pchNext As String
            Public pchEndRead As String
            Public pchEndWrite As String
            Public lBufOffset As Integer
            Public lDiskOffset As Integer
            Public adwInfo1 As Integer
            Public adwInfo2 As Integer
            Public adwInfo3 As Integer
            Public adwInfo4 As Integer
            Public dwReserved1 As Integer
            Public dwReserved2 As Integer
            Public hmmio As Integer
        End Structure

        Private Structure WAVEHDR
            Public lpData As IntPtr
            Public dwBufferLength As Integer
            Public dwBytesRecorded As Integer
            Public dwUser As Integer
            Public dwFlags As Integer
            Public dwLoops As Integer
            Public lpNext As Integer
            Public Reserved As Integer
        End Structure

        Private Structure WAVEINCAPS
            Public wMid As Short
            Public wPid As Short
            Public vDriverVersion As Integer
            Public szPname As String
            Public dwFormats As Integer
            Public wChannels As Short
        End Structure

        Private Structure WAVEFORMAT
            Public wFormatTag As Short
            Public nChannels As Short
            Public nSamplesPerSec As Integer
            Public nAvgBytesPerSec As Integer
            Public nBlockAlign As Short
            Public wBitsPerSample As Short
            Public cbSize As Short
        End Structure

        Private Structure MMCKINFO
            Public ckid As Integer
            Public ckSize As Integer
            Public fccType As Integer
            Public dwDataOffset As Integer
            Public dwFlags As Integer
        End Structure

        Private Structure MMTIME
            Public wType As Integer
            Public u As Integer
            Public x As Integer
        End Structure

        Private Declare Function mmioClose Lib "winmm.dll" (ByVal hmmio As IntPtr, ByVal uFlags As Integer) As Integer
        Private Declare Function mmioDescend Lib "winmm.dll" (ByVal hmmio As IntPtr, ByRef lpck As MMCKINFO, ByRef lpckParent As MMCKINFO, ByVal uFlags As Integer) As Integer
        Private Declare Function mmioDescendParent Lib "winmm.dll" Alias "mmioDescend" (ByVal hmmio As IntPtr, ByRef lpck As MMCKINFO, ByVal x As Integer, ByVal uFlags As Integer) As Integer
        Private Declare Ansi Function mmioOpen Lib "winmm.dll" Alias "mmioOpenA" (ByVal szFileName As String, ByRef lpmmioinfo As MMIOINFO, ByVal dwOpenFlags As Integer) As IntPtr
        Private Declare Function mmioRead Lib "winmm.dll" (ByVal hmmio As IntPtr, ByVal pch As IntPtr, ByVal cch As Integer) As Integer
        Private Declare Function mmioReadString Lib "winmm.dll" Alias "mmioRead" (ByVal hmmio As IntPtr, ByVal pch() As Byte, ByVal cch As Integer) As Integer
        Private Declare Function mmioSeek Lib "winmm.dll" (ByVal hmmio As IntPtr, ByVal lOffset As Integer, ByVal iOrigin As Integer) As Integer
        Private Declare Ansi Function mmioStringToFOURCC Lib "winmm.dll" Alias "mmioStringToFOURCCA" (ByVal sz As String, ByVal uFlags As Integer) As Integer
        Private Declare Function mmioAscend Lib "winmm.dll" (ByVal hmmio As IntPtr, ByRef lpck As MMCKINFO, ByVal uFlags As Integer) As Integer
        Private Declare Function GlobalAlloc Lib "kernel32" (ByVal wFlags As Integer, ByVal dwBytes As Integer) As IntPtr
        Private Declare Function GlobalLock Lib "kernel32" (ByVal hmem As IntPtr) As IntPtr
        Private Declare Function GlobalFree Lib "kernel32" (ByVal hmem As IntPtr) As Integer
        Private Declare Sub CopyWaveFormatFromBytes Lib "kernel32" Alias "RtlMoveMemory" (ByRef dest As WAVEFORMAT, ByVal source() As Byte, ByVal cb As Integer)
        Private Declare Sub CopyWaveHeaderFromPointer Lib "kernel32" Alias "RtlMoveMemory" (ByRef dest As WAVEHDR, ByVal source As Integer, ByVal cb As Integer)
        Private Declare Function IsBadWritePtr Lib "kernel32" (ByVal lp As Integer, ByVal ucb As Integer) As Integer
#End Region

        'Private variables
        Private m_Filename As String
        Private m_Initialized As Boolean = False
        Private m_MmioIn As IntPtr = IntPtr.Zero
        Private m_DataOffset As Integer = 0
        Private m_AudioLength As Integer = 0
        Private m_BufferSize As Integer = 0
        Private hmem As IntPtr ' memory handles
        Private pmem As IntPtr ' memory pointers
        Private m_Format As WAVEFORMAT ' waveformat structure
        Private m_DataRemaining As Integer = 0
        Private m_FormatBuffer(49) As Byte
        'Private hHdr As GCHandle

        Private m_Data() As Byte
        Private m_BitsPerSample As Byte
        Private m_SamplesPerSec As Integer
        Private m_nChannels As Byte

        '/// <summary>Creates a new WaveFile object.</summary>
        '/// <param name="Filename">Specifies the file to play.<param>
        '/// <exceptions cref="MediaException">Thrown when there was an error    opening the file or allocating the necessary buffers.</exceptions>
        '/// <exceptions cref="FileNotFoundException">Thrown when the specified file could not be found.</exceptions>
        '/// <exceptions cref="ArgumentException">Thrown when the specified   parameter is Nothing (C#,VC++: NULL).</exceptions>

        Public Sub New(ByVal Filename As String)
            If Filename Is Nothing Then Throw New ArgumentException
            m_Filename = Filename
            OpenFile()
        End Sub
        '/// <summary>Called when the class gets GCed.</summary>
        Protected Overrides Sub Finalize()
            CloseFile()
            MyBase.Finalize()
        End Sub

        '/// <summary>Used internally to open a wave file and initializing the  required memory.</summary>
        '/// <exceptions cref="MediaException">Thrown when there was an error   opening the file or allocating the necessary buffers.</exceptions>
        '/// <exceptions cref="FileNotFoundException">Thrown when the specified file  could not be found.</exceptions>
        Protected Sub OpenFile()
            'Make sure the file exists
            If Not File.Exists(Filename) Then
                Throw New FileNotFoundException
                Exit Sub
            End If
            Dim mmckinfoParentIn As MMCKINFO
            Dim mmckinfoSubchunkIn As MMCKINFO
            Dim mmioinf As MMIOINFO
            Dim rc As Integer
            'Open the input file
            m_MmioIn = mmioOpen(Filename, mmioinf, MMIO_READ)
            If (InputHandle.ToInt64 = 0) Then
                Err.Raise("Error while opening the input file.")
            End If
            'Check if this is a wave file
            mmckinfoParentIn.fccType = mmioStringToFOURCC("WAVE", 0)
            rc = mmioDescendParent(InputHandle, mmckinfoParentIn, 0, MMIO_FINDRIFF)
            If (rc <> MMSYSERR_NOERROR) Then
                CloseFile()
                Err.Raise("Invalid file type.")
            End If
            'Get format info
            mmckinfoSubchunkIn.ckid = mmioStringToFOURCC("fmt", 0)
            rc = mmioDescend(InputHandle, mmckinfoSubchunkIn, mmckinfoParentIn, MMIO_FINDCHUNK)
            If (rc <> MMSYSERR_NOERROR) Then
                CloseFile()
                Err.Raise("Couldn't find format chunk.")
            End If
            rc = mmioReadString(InputHandle, m_FormatBuffer, mmckinfoSubchunkIn.ckSize)
            If (rc = -1) Then
                CloseFile()
                Err.Raise("Couldn't read from WAVE file.")
            End If
            rc = mmioAscend(InputHandle, mmckinfoSubchunkIn, 0)
            CopyWaveFormatFromBytes(m_Format, m_FormatBuffer, Len(m_Format))
            'Find the data subchunk
            mmckinfoSubchunkIn.ckid = mmioStringToFOURCC("data", 0)
            rc = mmioDescend(InputHandle, mmckinfoSubchunkIn, mmckinfoParentIn, MMIO_FINDCHUNK)
            If (rc <> MMSYSERR_NOERROR) Then
                CloseFile()
                Err.Raise("Unable to find the data chunk.")
            End If
            m_DataOffset = mmioSeek(InputHandle, 0, SEEK_CUR)
            'Get the length of the audio
            m_AudioLength = mmckinfoSubchunkIn.ckSize

            'Allocate audio buffers
            m_BufferSize = CType(m_Format.nSamplesPerSec * m_Format.nBlockAlign * m_Format.nChannels * BUFFER_SECONDS, Integer)
            m_BufferSize = m_BufferSize - (m_BufferSize Mod m_Format.nBlockAlign)

            m_SamplesPerSec = m_Format.nSamplesPerSec
            m_BitsPerSample = m_Format.wBitsPerSample
            m_nChannels = m_Format.nChannels

            ReDim m_Data(m_AudioLength - m_DataOffset - 1)

            GlobalFree(hmem)
            hmem = GlobalAlloc(0, m_BufferSize)
            pmem = GlobalLock(hmem)

            GetData()

            'The class in successfully initialized
            m_Initialized = True
        End Sub
        '/// <summary>Used internally to close a wave file and free the used   memory.</summary>
        Protected Sub CloseFile()
            Dim i As Integer
            mmioClose(InputHandle, 0)
            'For i = 0 To NUM_BUFFERS - 1
            GlobalFree(hmem)
            'Next i
            'If hHdr.IsAllocated Then hHdr.Free()
        End Sub
        '/// <summary>Gets the name of the WAVE file.</summary>
        '/// <value>The name of the WAVE file.</value>
        Public ReadOnly Property Filename() As String
            Get
                Return m_Filename
            End Get
        End Property

        '/// <summary>Returns the handle of the input device.</summary>
        '/// <value>The handle of the input device.</value>
        Private ReadOnly Property InputHandle() As IntPtr
            Get
                Return m_MmioIn
            End Get
        End Property

        '/// <summary>The callback function.</summary>
        Private Function GetData() As Boolean
            Dim rc As Integer
            Dim lCurPos As Integer = mmioSeek(InputHandle, 0, SEEK_CUR)
            m_DataRemaining = (m_AudioLength - lCurPos)   'm_DataOffset +
            If (m_BufferSize < m_DataRemaining) Then
                rc = mmioRead(InputHandle, pmem, m_BufferSize)
                GetData = rc <> -1
            Else
                rc = mmioRead(InputHandle, pmem, m_DataRemaining)
                GetData = False
            End If
            If rc > 0 Then
                Dim l_Data(rc - 1) As Byte
                Marshal.Copy(pmem, l_Data, 0, rc)
                Array.Copy(l_Data, 0, m_Data, lCurPos - m_DataOffset, rc)
            End If
            If GetData Then GetData()
        End Function

        Public ReadOnly Property Data() As Byte()
            Get
                Return m_Data
            End Get
        End Property

        Public ReadOnly Property SamplesPerSec() As Integer
            Get
                Return m_SamplesPerSec
            End Get
        End Property
        Public ReadOnly Property BitsPerSample() As Byte
            Get
                Return m_BitsPerSample
            End Get
        End Property
        Public ReadOnly Property nChannels() As Byte
            Get
                Return m_nChannels
            End Get
        End Property
    End Class
