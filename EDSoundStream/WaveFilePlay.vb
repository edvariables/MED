'Original author: Pieter Philippaerts
Imports System.IO
Imports System.Runtime.InteropServices

    Public Class WaveFilePlay

#Region "<API-DECLARES>"
        Private Const CALLBACK_WINDOW As Integer = &H10000
        Private Const CALLBACK_FUNCTION As Integer = &H30000
        Private Const MMIO_READ As Integer = &H0
        Private Const MMIO_FINDCHUNK As Integer = &H10
        Private Const MMIO_FINDRIFF As Integer = &H20
        Private Const MM_WOM_DONE As Integer = &H3BD
        Private Const MMSYSERR_NOERROR As Integer = 0
        Private Const SEEK_CUR As Integer = 1
        Private Const SEEK_END As Integer = 2
        Private Const SEEK_SET As Integer = 0
        Private Const TIME_BYTES As Integer = &H4
        Private Const WHDR_DONE As Integer = &H1
        Private Const NUM_BUFFERS As Integer = 5
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
            Public lpData As Integer
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

        Private Declare Function waveOutGetPosition Lib "winmm.dll" (ByVal hWaveOut As IntPtr, ByRef lpInfo As MMTIME, ByVal uSize As Integer) As Integer
        Private Declare Ansi Function waveOutOpen Lib "winmm.dll" (ByRef hWaveOut As IntPtr, ByVal uDeviceID As Integer, ByVal format() As Byte, ByVal dwCallback As WaveDelegate, ByRef fPlaying As Integer, ByVal dwFlags As Integer) As Integer
        Private Declare Function waveOutPrepareHeader Lib "winmm.dll" (ByVal hWaveIn As IntPtr, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Private Declare Function waveOutPrepareHeaderPtr Lib "winmm.dll" (ByVal hWaveIn As IntPtr, ByVal lpWaveInHdr As Integer, ByVal uSize As Integer) As Integer
        Private Declare Function waveOutReset Lib "winmm.dll" (ByVal hWaveIn As IntPtr) As Integer
        Private Declare Function waveOutUnprepareHeader Lib "winmm.dll" (ByVal hWaveIn As IntPtr, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Private Declare Function waveOutClose Lib "winmm.dll" (ByVal hWaveIn As IntPtr) As Integer
        Private Declare Function waveOutWrite Lib "winmm.dll" (ByVal hWaveOut As IntPtr, ByRef lpWaveOutHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Private Declare Function waveOutPause Lib "winmm.dll" (ByVal hWaveOut As IntPtr) As Integer
        Private Declare Function waveOutRestart Lib "winmm.dll" (ByVal hWaveOut As IntPtr) As Integer
        Private Declare Function waveOutSetVolume Lib "winmm.dll" (ByVal uDeviceID As IntPtr, ByVal dwVolume As UInt32) As Integer
        Private Declare Function waveOutGetVolume Lib "winmm.dll" (ByVal uDeviceID As IntPtr, ByRef lpdwVolume As Integer) As Integer
        Private Declare Function mmioClose Lib "winmm.dll" (ByVal hmmio As IntPtr, ByVal uFlags As Integer) As Integer
        Private Declare Function mmioDescend Lib "winmm.dll" (ByVal hmmio As IntPtr, ByRef lpck As MMCKINFO, ByRef lpckParent As MMCKINFO, ByVal uFlags As Integer) As Integer
        Private Declare Function mmioDescendParent Lib "winmm.dll" Alias "mmioDescend" (ByVal hmmio As IntPtr, ByRef lpck As MMCKINFO, ByVal x As Integer, ByVal uFlags As Integer) As Integer
        Private Declare Ansi Function mmioOpen Lib "winmm.dll" Alias "mmioOpenA" (ByVal szFileName As String, ByRef lpmmioinfo As MMIOINFO, ByVal dwOpenFlags As Integer) As IntPtr
        Private Declare Function mmioRead Lib "winmm.dll" (ByVal hmmio As IntPtr, ByVal pch As Integer, ByVal cch As Integer) As Integer
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

        Private Delegate Sub WaveDelegate(ByVal hwo As IntPtr, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef wavhdr As WAVEHDR, ByVal dwParam2 As Integer)

        'Private variables
        Private m_Tel As Integer
        Private m_Filename As String
        Private m_Initialized As Boolean = False
        Private m_MmioIn As IntPtr = IntPtr.Zero
        Private m_DataOffset As Integer = 0
        Private m_AudioLength As Integer = 0
        Private m_BufferSize As Integer = 0
        Private hmem(NUM_BUFFERS - 1) As IntPtr ' memory handles
        Private pmem(NUM_BUFFERS - 1) As IntPtr ' memory pointers
        Private hdr(NUM_BUFFERS - 1) As WAVEHDR ' wave headers
        Private m_Format As WAVEFORMAT ' waveformat structure
        Private m_WaveOut As IntPtr = IntPtr.Zero
        Private m_Playing As Boolean = False
        Private m_StartPos As Integer = 0
        Private m_DataRemaining As Integer = 0
        Private m_FormatBuffer(49) As Byte
        Private m_Callback As WaveDelegate
        Private m_Paused As Boolean
        Private hHdr As GCHandle

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

        '/// <summary>Starts playing the wave file.</summary>
        '/// <exceptions cref="MediaException">Thrown when an error occured while    reading from the file.</exceptions>
        Public Sub Play()
            If m_Paused Then
                m_Paused = False
                waveOutRestart(WaveOutHandle)
                Exit Sub
            End If

            If m_Playing Then Exit Sub

            Dim rc, i As Integer

            m_Callback = AddressOf WaveCallBack
            rc = waveOutOpen(m_WaveOut, 0, m_FormatBuffer, m_Callback, Nothing, CALLBACK_FUNCTION)
            If (rc <> MMSYSERR_NOERROR) Then
                Err.Raise("Unable to open the WAVE file.")
            End If
            hHdr = GCHandle.Alloc(hdr, GCHandleType.Pinned)
            For i = 0 To NUM_BUFFERS - 1
                hdr(i).lpData = pmem(i).ToInt32
                hdr(i).dwBufferLength = m_BufferSize
                hdr(i).dwFlags = 0
                hdr(i).dwLoops = 0
                rc = waveOutPrepareHeader(WaveOutHandle, hdr(i), Len(hdr(i)))
                If (rc <> MMSYSERR_NOERROR) Then
                    Err.Raise("Unable to prepare the WAVE buffers.")
                End If
            Next

            m_Playing = True
            m_Paused = False
            m_StartPos = mmioSeek(InputHandle, 0, SEEK_CUR) - m_DataOffset
            For i = 0 To NUM_BUFFERS - 1
                WaveCallBack(InputHandle, MM_WOM_DONE, 0, hdr(i), 0)
            Next i
        End Sub

        '/// <summary>Stops the playback of the wave file.</summary>
        Public Sub StopPlay()
            Dim i As Integer

            m_Playing = False
            waveOutReset(WaveOutHandle)
            For i = 0 To NUM_BUFFERS - 1
                waveOutUnprepareHeader(WaveOutHandle, hdr(i), Len(hdr(i)))
            Next
            waveOutClose(WaveOutHandle)
            If hHdr.IsAllocated Then hHdr.Free()
            Position = 0
        End Sub

        '/// <summary>Specifies whether the WAVE file is currently paused or not.</summary>
        '/// <value>True when the file is paused, False otherwise.</value>
        Public Property Paused() As Boolean
            Get
                Return m_Paused
            End Get

            Set(ByVal Value As Boolean)
                If m_Playing Then
                    m_Paused = Value
                    If m_Paused Then
                        waveOutPause(WaveOutHandle)
                    Else
                        waveOutRestart(WaveOutHandle)
                    End If
                End If
            End Set
        End Property

        '/// <summary>Gets the name of the WAVE file.</summary>
        '/// <value>The name of the WAVE file.</value>
        Public ReadOnly Property Filename() As String
            Get
                Return m_Filename
            End Get
        End Property

        '/// <summary>Gets the length of the WAVE file.</summary>
        '/// <value>The length of the WAVE file.</value>
        Public ReadOnly Property Length() As Integer
            Get
                Return m_AudioLength \ m_Format.nBlockAlign
            End Get
        End Property

        '/// <summary>Specifies whether the WAVE file is currently playing.</summary>
        '/// <remarks>This property is only influenced by the methods 'Play' and
        'StopPlay'. If you pause a file, Playing will still be True.</remarks>
        '/// <value>True if the WAVE file is currently playing, False      otherwise.</value>
        Public ReadOnly Property Playing() As Boolean
            Get
                Dim tm As MMTIME
                tm.wType = TIME_BYTES
                Return (waveOutGetPosition(WaveOutHandle, tm, Len(tm)) = MMSYSERR_NOERROR)
            End Get
        End Property

        '/// <summary>Specifies the output volume.</summary>
        '/// <remarks>This value must be between 0 and 65535.</remarks>
        '/// <value>The output volume.</value>
        Public Property Volume() As Integer
            Get
                waveOutGetVolume(WaveOutHandle, Volume)
                Volume = CType(Volume And &HFFFF&, Integer)
            End Get

            Set(ByVal Value As Integer)
                If Value < &H0 OrElse Value > &HFFFF Then Throw New ArgumentException
                waveOutSetVolume(WaveOutHandle, UInt32.Parse((Value + 2 ^ 16 * Value).ToString))
            End Set
        End Property

        '/// <summary>Specifies the position in the WAVE file.</summary>
        '/// <value>The position in the WAVE file.</value>
        Public Property Position() As Integer
            Get
                Dim tm As MMTIME
                tm.wType = TIME_BYTES
                If (waveOutGetPosition(WaveOutHandle, tm, Len(tm)) = MMSYSERR_NOERROR) Then
                    Position = (m_StartPos + tm.u) \ m_Format.nBlockAlign
                Else
                    Position = (mmioSeek(InputHandle, 0, SEEK_CUR) - m_DataOffset + m_BufferSize * NUM_BUFFERS) \ m_Format.nBlockAlign
                End If
            End Get

            Set(ByVal Value As Integer)
                If Not m_Initialized Then Exit Property
                Dim bytepos As Integer = Value * m_Format.nBlockAlign
                mmioSeek(InputHandle, bytepos + m_DataOffset, SEEK_SET)
            End Set
        End Property

        '/// <summary>Returns the handle of the input device.</summary>
        '/// <value>The handle of the input device.</value>
        Private ReadOnly Property InputHandle() As IntPtr
            Get
                Return m_MmioIn
            End Get
        End Property

        '/// <summary>Returns the handle of the output device.</summary>
        '/// <value>The handle of the output device.</value>
        Private ReadOnly Property WaveOutHandle() As IntPtr
            Get
                Return m_WaveOut
            End Get
        End Property

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
            Dim rc, i As Integer
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
            For i = 0 To NUM_BUFFERS - 1
                GlobalFree(hmem(i))
                hmem(i) = GlobalAlloc(0, m_BufferSize)
                pmem(i) = GlobalLock(hmem(i))
            Next
            'The class in successfully initialized
            m_Initialized = True
        End Sub
        '/// <summary>Used internally to close a wave file and free the used   memory.</summary>
        Protected Sub CloseFile()
            Dim i As Integer
            If Playing Then waveOutReset(WaveOutHandle)
            mmioClose(InputHandle, 0)
            For i = 0 To NUM_BUFFERS - 1
                GlobalFree(hmem(i))
            Next i
            If hHdr.IsAllocated Then hHdr.Free()
        End Sub
        '/// <summary>The callback function.</summary>
        Private Sub WaveCallBack(ByVal hwo As IntPtr, ByVal uMsg As Integer, ByVal ByValdwInstance As Integer, ByRef wavhdr As WAVEHDR, ByVal dwParam2 As Integer)
            If uMsg = MM_WOM_DONE AndAlso m_Playing Then
                Dim rc As Integer
                m_DataRemaining = (m_DataOffset + m_AudioLength - mmioSeek(InputHandle, 0, SEEK_CUR))
                If (m_BufferSize < m_DataRemaining) Then
                    rc = mmioRead(InputHandle, wavhdr.lpData, m_BufferSize)
                Else
                    rc = mmioRead(InputHandle, wavhdr.lpData, m_DataRemaining)
                    m_Playing = False
                End If
                If rc <> -1 Then
                    wavhdr.dwBufferLength = rc
                    rc = waveOutWrite(WaveOutHandle, wavhdr, Len(wavhdr))
                End If
            End If
        End Sub
    End Class

