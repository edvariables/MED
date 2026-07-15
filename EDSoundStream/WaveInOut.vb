''Original author: Pieter Philippaerts
'Imports System.io
'Imports System.Runtime.InteropServices
'
'Public Module EDWave
'#Region "<API-DECLARES>"
'    Public Const CALLBACK_WINDOW As Integer = &H10000
'    Public Const CALLBACK_FUNCTION As Integer = &H30000
'    Public Const WHDR_DONE As Integer = &H1
'    Public Const MM_WOM_DONE As Integer = &H3BD
'    Public Const WOM_OPEN As Integer = &H3BB
'    Public Const WOM_CLOSE As Integer = &H3BC
'    Friend Const MMSYSERR_NOERROR As Integer = 0
'    Public Enum MMSYS_ERRORS
'        MMSYSERR_NOERROR = 0
'        MMSYSERR_ERROR = 1
'        MMSYSERR_BADDEVICEID = 2
'        MMSYSERR_NOTENABLED = 3
'        MMSYSERR_ALLOCATED = 4
'        MMSYSERR_INVALHANDLE = 5
'        MMSYSERR_NODRIVER = 6
'        MMSYSERR_NOMEM = 7
'        MMSYSERR_NOTSUPPORTED = 8
'        MMSYSERR_BADERRNUM = 9
'        MMSYSERR_INVALFLAG = 10
'        MMSYSERR_INVALPARAM = 11
'        MMSYSERR_HANDLEBUSY = 12
'        MMSYSERR_INVALIDALIAS = 13
'        MMSYSERR_BADDB = 14
'        MMSYSERR_KEYNOTFOUND = 15
'        MMSYSERR_READERROR = 16
'        MMSYSERR_WRITEERROR = 17
'        MMSYSERR_DELETEERROR = 18
'        MMSYSERR_VALNOTFOUND = 19
'        MMSYSERR_NODRIVERCB = 20
'        WAVERR_BASE = 32
'        WAVERR_BADFORMAT = (WAVERR_BASE + 0) ' unsupported wave format
'        WAVERR_SYNC = (WAVERR_BASE + 3) ' device is synchronous
'    End Enum


'    Public Structure WAVEHDR
'        Public lpData As Integer
'        Public dwBufferLength As Integer
'        Public dwBytesRecorded As Integer
'        Public dwUser As Integer
'        Public dwFlags As Integer
'        Public dwLoops As Integer
'        Public lpNext As Integer
'        Public Reserved As Integer
'    End Structure

'    Public Structure WAVEINCAPS
'        Public wMid As Short
'        Public wPid As Short
'        Public vDriverVersion As Integer
'        Public szPname As String
'        Public dwFormats As Integer
'        Public wChannels As Short
'    End Structure

'    Public Structure WAVEFORMAT
'        Public wFormatTag As Short
'        Public nChannels As Short
'        Public nSamplesPerSec As Integer
'        Public nAvgBytesPerSec As Integer
'        Public nBlockAlign As Short
'        Public wBitsPerSample As Short
'        Public cbSize As Short
'    End Structure

'    Public Declare Ansi Function waveOutOpen Lib "winmm.dll" (ByRef hWaveOut As IntPtr, ByVal uDeviceID As Integer, ByVal format() As Byte, ByVal dwCallback As IntPtr, ByRef fPlaying As Integer, ByVal dwFlags As Integer) As Integer
'    Public Declare Function waveOutPrepareHeader Lib "winmm.dll" (ByVal hWaveIn As IntPtr, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
'    Public Declare Function waveOutPrepareHeaderPtr Lib "winmm.dll" (ByVal hWaveIn As IntPtr, ByVal lpWaveInHdr As Integer, ByVal uSize As Integer) As Integer
'    Public Declare Function waveOutReset Lib "winmm.dll" (ByVal hWaveIn As IntPtr) As Integer
'    Public Declare Function waveOutUnprepareHeader Lib "winmm.dll" (ByVal hWaveIn As IntPtr, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
'    Public Declare Function waveOutClose Lib "winmm.dll" (ByVal hWaveIn As IntPtr) As Integer
'    Public Declare Function waveOutWrite Lib "winmm.dll" (ByVal hWaveOut As IntPtr, ByRef lpWaveOutHdr As WAVEHDR, ByVal uSize As Integer) As Integer
'    Public Declare Function waveOutPause Lib "winmm.dll" (ByVal hWaveOut As IntPtr) As Integer
'    Public Declare Function waveOutRestart Lib "winmm.dll" (ByVal hWaveOut As IntPtr) As Integer
'    Public Declare Function waveOutSetVolume Lib "winmm.dll" (ByVal uDeviceID As IntPtr, ByVal dwVolume As UInt32) As Integer
'    Public Declare Function waveOutGetVolume Lib "winmm.dll" (ByVal uDeviceID As IntPtr, ByRef lpdwVolume As Integer) As Integer

'#End Region
'End Module

'Public Class WaveOut
'    Private Declare Function GlobalAlloc Lib "kernel32" (ByVal wFlags As Integer, ByVal dwBytes As Integer) As IntPtr
'    Private Declare Function GlobalLock Lib "kernel32" (ByVal hmem As IntPtr) As IntPtr
'    Private Declare Function GlobalFree Lib "kernel32" (ByVal hmem As IntPtr) As Integer
'    Private Declare Sub CopyWaveFormatFromBytes Lib "kernel32" Alias "RtlMoveMemory" (ByRef dest As WAVEFORMAT, ByVal source() As Byte, ByVal cb As Integer)
'    Private Declare Sub CopyWaveHeaderFromPointer Lib "kernel32" Alias "RtlMoveMemory" (ByRef dest As WAVEHDR, ByVal source As Integer, ByVal cb As Integer)
'    Private Declare Function IsBadWritePtr Lib "kernel32" (ByVal lp As Integer, ByVal ucb As Integer) As Integer

'    Private Const NUM_BUFFERS As Integer = 1
'    Private Const BUFFER_SECONDS As Single = 0.1
'    Private Const WAVE_FORMAT_PCM As Integer = 1

'    Private Delegate Sub WaveDelegate(ByVal hwo As IntPtr, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef wavhdr As WAVEHDR, ByVal dwParam2 As Integer)
'    Private Declare Ansi Function waveOutOpen Lib "winmm.dll" (ByRef hWaveOut As IntPtr, ByVal uDeviceID As Integer, ByRef Format As WAVEFORMAT, ByVal dwCallback As WaveDelegate, ByRef fPlaying As Integer, ByVal dwFlags As Integer) As Integer

'    'Private variables
'    Private m_Initialized As Boolean = False
'    Private m_BufferSize As Integer
'    Private hmem(NUM_BUFFERS - 1) As IntPtr ' memory handles
'    Private pmem(NUM_BUFFERS - 1) As IntPtr ' memory pointers
'    Private hdr(NUM_BUFFERS - 1) As WAVEHDR ' wave headers
'    Private m_Format As WAVEFORMAT ' waveformat structure
'    Private m_WaveOut As IntPtr = IntPtr.Zero
'    Private m_Playing As Boolean = False
'    Private m_StartPos As Integer = 0
'    Private m_DataRemaining As Integer = 0
'    Private m_Callback As WaveDelegate
'    Private hHdr As GCHandle

'    Public Sub New(ByVal pFormat As WAVEFORMAT)
'        m_Format.wFormatTag = WAVE_FORMAT_PCM
'        m_Format.nChannels = 1
'        m_Format.nSamplesPerSec = 8000 'pFormat.nSamplesPerSec
'        m_Format.wBitsPerSample = 8
'        m_Format.nBlockAlign = 1 'm_Format.nChannels * (m_Format.wBitsPerSample / 8)
'        m_Format.nAvgBytesPerSec = 8000 ' pFormat.nSamplesPerSec


'        m_Format.cbSize = 0 'Len(m_Format)
'        m_Format = pFormat
'        OpenDevice()
'    End Sub

'    '/// <summary>Starts playing the wave file.</summary>
'    '/// <exceptions cref="MediaException">Thrown when an error occured while    reading from the file.</exceptions>
'    Public Sub StartPlay()

'        If m_Playing Then Exit Sub

'        Dim rc, i As Integer

'        m_Callback = AddressOf WaveCallBack
'        rc = waveOutOpen(m_WaveOut, 0, m_Format, m_Callback, 1, CALLBACK_FUNCTION)
'        If (rc <> MMSYSERR_NOERROR) Then
'            Err.Raise(-1, , "Unable to open the output device.")
'        End If
'        hHdr = GCHandle.Alloc(hdr, GCHandleType.Pinned)
'        For i = 0 To NUM_BUFFERS - 1
'            hdr(i).lpData = pmem(i).ToInt32
'            hdr(i).dwBufferLength = m_BufferSize
'            hdr(i).dwFlags = 0
'            hdr(i).dwLoops = 0
'            rc = waveOutPrepareHeader(m_WaveOut, hdr(i), Len(hdr(i)))
'            If (rc <> MMSYSERR_NOERROR) Then
'                Err.Raise(-1, , "Unable to prepare the WAVE output buffers.")
'            End If
'        Next

'        m_Playing = True
'        For i = 0 To NUM_BUFFERS - 1
'            WaveCallBack(m_WaveOut, MM_WOM_DONE, 0, hdr(i), 0)
'        Next i
'    End Sub

'    '/// <summary>Stops the playback of the wave file.</summary>
'    Public Sub StopPlay()
'        Dim i As Integer

'        m_Playing = False
'        waveOutReset(m_WaveOut)
'        For i = 0 To NUM_BUFFERS - 1
'            waveOutUnprepareHeader(m_WaveOut, hdr(i), Len(hdr(i)))
'        Next
'        waveOutClose(m_WaveOut)
'    End Sub

'    '/// <summary>Specifies the output volume.</summary>
'    '/// <remarks>This value must be between 0 and 65535.</remarks>
'    '/// <value>The output volume.</value>
'    Public Property Volume() As Integer
'        Get
'            waveOutGetVolume(m_WaveOut, Volume)
'            Volume = CType(Volume And &HFFFF&, Integer)
'        End Get

'        Set(ByVal Value As Integer)
'            If Value < &H0 OrElse Value > &HFFFF Then Throw New ArgumentException
'            waveOutSetVolume(m_WaveOut, UInt32.Parse((Value + 2 ^ 16 * Value).ToString))
'        End Set
'    End Property


'    '/// <summary>Called when the class gets GCed.</summary>
'    Protected Overrides Sub Finalize()
'        CloseDevice()
'        MyBase.Finalize()
'    End Sub

'    '/// <summary>Used internally to open a wave file and initializing the  required memory.</summary>
'    '/// <exceptions cref="MediaException">Thrown when there was an error   opening the file or allocating the necessary buffers.</exceptions>
'    '/// <exceptions cref="FileNotFoundException">Thrown when the specified file  could not be found.</exceptions>
'    Private Sub OpenDevice()
'        Dim i As Integer
'        m_BufferSize = CType(m_Format.nSamplesPerSec * m_Format.nBlockAlign * m_Format.nChannels * BUFFER_SECONDS, Integer)
'        m_BufferSize = m_BufferSize - (m_BufferSize Mod m_Format.nBlockAlign)
'        Try
'            For i = 0 To NUM_BUFFERS - 1
'                GlobalFree(hmem(i))
'                hmem(i) = GlobalAlloc(0, m_BufferSize)
'                pmem(i) = GlobalLock(hmem(i))
'            Next

'            'The class in successfully initialized
'            m_Initialized = True
'        Catch ex As Exception
'            MsgBox(ex.Message)
'        End Try
'    End Sub
'    '/// <summary>Used internally to close a wave file and free the used   memory.</summary>
'    Protected Sub CloseDevice()
'        Dim i As Integer
'        Try
'            waveOutReset(m_WaveOut)
'        Catch ex As Exception

'        End Try
'        For i = 0 To NUM_BUFFERS - 1
'            GlobalFree(hmem(i))
'        Next i
'        If hHdr.IsAllocated Then hHdr.Free()
'    End Sub

'    'Private mHdrSrce As WAVEHDR
'    Private mbHdrSrce As Boolean
'    Private mCountCallBack As Integer
'    Public Sub PlayData(ByVal pHdrSrce As WAVEHDR)
'        hdr(0).lpData = pHdrSrce.lpData
'        mbHdrSrce = True
'        If mCountCallBack > 10 Then
'            waveOutPrepareHeader(m_WaveOut, hdr(0), Len(hdr(0)))
'            waveOutWrite(m_WaveOut, hdr(0), Len(hdr(0)))
'            mCountCallBack = 0
'        Else
'            mCountCallBack += 1
'        End If
'    End Sub
'    '/// <summary>The callback function.</summary>
'    Private Sub WaveCallBack(ByVal hwo As IntPtr, ByVal uMsg As Integer, ByVal ByValdwInstance As Integer, ByRef wavhdr As WAVEHDR, ByVal dwParam2 As Integer)
'        If uMsg = MM_WOM_DONE AndAlso m_Playing Then
'            If mbHdrSrce Then
'                mCountCallBack = 0
'                waveOutPrepareHeader(m_WaveOut, hdr(0), Len(hdr(0)))
'                waveOutWrite(m_WaveOut, hdr(0), Len(hdr(0)))
'                mbHdrSrce = False
'            End If
'        ElseIf uMsg = WOM_OPEN Then
'            m_Playing = True
'        ElseIf uMsg = WOM_CLOSE Then
'            m_Playing = False
'        End If
'    End Sub
'End Class
'
