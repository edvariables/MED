Option Strict Off
Option Explicit On 
Imports System.Runtime.InteropServices

    Public Class EDSoundIn
        Implements IDisposable

        'API Declarations... 
        Private Declare Function waveInOpen Lib "winmm.dll" Alias "waveInOpen" (ByRef lphWaveIn As IntPtr, ByVal uDeviceID As Integer, ByRef lpFormat As WAVEFORMAT, ByVal dwCallback As waveInProcDelegate, ByVal dwInstance As Integer, ByVal dwFlags As Integer) As Integer
        Private Declare Function waveInStart Lib "winmm.dll" Alias "waveInStart" (ByVal hWaveIn As IntPtr) As Integer
        Private Declare Function waveInStop Lib "winmm.dll" Alias "waveInStop" (ByVal hWaveIn As IntPtr) As Integer
        Private Declare Function waveInAddBuffer Lib "winmm.dll" Alias "waveInAddBuffer" (ByVal hWaveIn As IntPtr, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Private Declare Function waveInGetNumDevs Lib "winmm.dll" Alias "waveInGetNumDevs" () As Integer
        Private Declare Function waveInGetDevCaps Lib "winmm.dll" Alias "waveInGetDevCapsA" (ByVal uDeviceID As Integer, ByRef lpCaps As WAVEINCAPS, ByVal uSize As Integer) As Integer
        Private Declare Function waveInPrepareHeader Lib "winmm.dll" Alias "waveInPrepareHeader" (ByVal hWaveIn As IntPtr, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Private Declare Function waveInUnPrepareHeader Lib "winmm.dll" Alias "waveInUnprepareHeader" (ByVal hWaveIn As IntPtr, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Structure WAVEINCAPS
            Dim wMid As Short
            Dim wPid As Short
            Dim vDriverVersion As Long
            <MarshalAs(UnmanagedType.ByValArray, SizeConst:=32)> Dim szpName() As Char 'String of length 32 
            Dim dwFormats As Integer
            Dim wChannels As Short
            Dim wReserved1 As Short
        End Structure
        Private Const WIM_OPEN As Integer = &H3BE
        Private Const WIM_CLOSE As Integer = &H3BF
        Private Const WIM_DATA As Integer = &H3C0

        Private Delegate Sub waveInProcDelegate(ByVal hwi As Integer, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef dwParam1 As WAVEHDR, ByVal dwparam2 As Integer)
        Private pCallback As waveInProcDelegate = New waveInProcDelegate(AddressOf waveInProc)

        ' Wave In Handle
        Private hWaveIn As IntPtr

        ' number of buffers
        Private Const BUFFERS As Integer = 4

        ' buffer
        <StructLayout(LayoutKind.Sequential)>
        Private Class WaveBuffer
            Public hHdr As GCHandle
            Public hdr As WAVEHDR
            Public hdrPtr As Integer
            Public memHdl As IntPtr
            Public memPtr As IntPtr
            Public Data() As Byte
        End Class

        ' buffer collection
        Private btBuffers(BUFFERS - 1) As WaveBuffer
        Private mBuffSize As Integer
        Private mBytesPerSec As Integer
        Private mBuffPerSec As Byte '10

        Public Property BytesPerSec() As Integer 'par channel
            Get
                Return mBytesPerSec
            End Get
            Set(ByVal Value As Integer)
                mBytesPerSec = Value
            End Set
        End Property
        Public ReadOnly Property BufferSize() As Integer
            Get
                Return mBuffSize
            End Get
        End Property
        Public BitsPerSample As Byte = 16
        Public nChannels As Byte = 2     'Si 1 mettre 8 dans BitsPerSample 
        Private mbRecording As Boolean

        Public Event Data(ByVal pNumBuff As Short, ByVal pData() As Byte, ByVal pLength As Integer)

        Private m_DeviceID As Integer

        Public Event State(ByVal pbRunning As Boolean)
        Public Event Streamed(ByVal pmWaveBytes() As Byte)

        Public Sub New()
            mBytesPerSec = 8000 '44100   ' 11025&'
            mBuffPerSec = 10
        End Sub
        Public Sub New(ByVal pBytesPerSec As Integer, ByVal pBuffPerSec As Byte)
            mBytesPerSec = pBytesPerSec
            mBuffPerSec = pBuffPerSec
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            On Error Resume Next
            StopRec()
            DeInitWaveIn()
        End Sub
        Protected Overrides Sub Finalize()
            Dispose()
            MyBase.Finalize()
        End Sub

        Private Function InitWaveIn() As Boolean
            ' open WaveIn
            If Not OpenDevice() Then
                DeInitWaveIn()
            Else
                InitWaveIn = True
            End If

            Return True
        End Function

        Private Function DeInitWaveIn() As Boolean
            ' close wave out
            CloseDevice()
        End Function

        Private Function OpenDevice() As Boolean   'ByVal hWnd As IntPtr
            Dim wfx As WAVEFORMAT
            Dim lSecParBuff As Single

            'Audio header
            'un sample est pour une voie, un byte par sample
            BytesPerSec = mBytesPerSec '8000 '44100   ' 11025&'

            With wfx
                .cbSize = 0
                .nSamplesPerSec = BytesPerSec
                .wBitsPerSample = BitsPerSample
                .nChannels = nChannels
                .nBlockAlign = .nChannels * (.wBitsPerSample / 8)
                .nAvgBytesPerSec = .nSamplesPerSec * .nBlockAlign
                .wFormatTag = 1

                lSecParBuff = 1 / mBuffPerSec '0.1
                mBuffSize = .nSamplesPerSec * .nBlockAlign * .nChannels * lSecParBuff
                mBuffSize = mBuffSize - (mBuffSize Mod .nBlockAlign)
            End With

            ' open the WaveIn
            'OpenDevice = MMSYSERR_NOERROR = WaveInOpen(hWaveIn, 0, wfx, hWnd, 0, CALLBACKS.CALLBACK_WINDOW)
            OpenDevice = MMSYSERR_NOERROR = waveInOpen(hWaveIn, m_DeviceID, wfx, pCallback, 0, CALLBACKS.CALLBACK_FUNCTION)              '+ CALLBACKS.WAVE_ALLOWSYNC

            PrepareBuffers()
        End Function

        Private Sub PrepareBuffers()
            Dim i As Short
            Dim lPtr As IntPtr

            ' prepare the buffers
            For i = 0 To UBound(btBuffers)
                btBuffers(i) = New WaveBuffer
                GlobalFree(btBuffers(i).memHdl)
                With btBuffers(i)
                    .hHdr = GCHandle.Alloc(.hdr, GCHandleType.Pinned)
                    .memHdl = GlobalAlloc(0, mBuffSize)
                    .memPtr = GlobalLock(btBuffers(i).memHdl)
                    .hdr.lpData = .memPtr.ToInt32
                    .hdr.dwBufferLength = mBuffSize
                    .hdr.dwUser = i
                    ReDim .Data(mBuffSize - 1)
                    waveInPrepareHeader(hWaveIn, .hdr, Len(.hdr))
                    waveInAddBuffer(hWaveIn, .hdr, Len(.hdr))
                End With
            Next
            mbRecording = True
        End Sub
        Private Sub CloseDevice()
            waveInStop(hWaveIn)
        End Sub

        Public Sub StartRec()

            DeInitWaveIn()
            If Not InitWaveIn() Then
                MsgBox("Can not initialize module.")
            End If

            Dim lResult As Integer
            lResult = waveInStart(hWaveIn)

            mbRecording = lResult = 0

        End Sub

        '###########################################

        Public Sub StopRec()

            Dim i As Short
            Dim j As Short

            mbRecording = False

            ' reset the buffers associated
            ' with our wave out handle
            waveInStop(hWaveIn)

            ' del buffers
            For i = 0 To UBound(btBuffers)
                Do While 0 <> waveInUnPrepareHeader(hWaveIn, btBuffers(i).hdr, Len(btBuffers(i).hdr))
                    If j = 10 Then Exit Do
                    Threading.Thread.CurrentThread.Sleep(50)
                    j = j + 1
                Loop
                GlobalFree(btBuffers(i).memHdl)
                btBuffers(i).memHdl = IntPtr.Zero
                btBuffers(i).memPtr = IntPtr.Zero
                If btBuffers(i).hHdr.IsAllocated Then btBuffers(i).hHdr.Free()
            Next
        End Sub

        Private Sub WaveCallBack(ByVal hwo As IntPtr, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef wavhdr As WAVEHDR, ByVal dwParam2 As Integer)
            If uMsg = MM_WOM_DONE AndAlso mbRecording Then
                'If m_PlayFileData Then
                '    Dim lLen As Integer
                '    lLen = m_FileData.GetLength(0) - m_PosFileData
                '    If lLen > mBuffSize Then 'Si on a de quoi fournir en une fois
                '        Array.Copy(m_FileData, m_PosFileData, btBuffers(wavhdr.dwUser).Data, 0, mBuffSize)
                '        m_PosFileData += mBuffSize
                '    Else
                '        Array.Copy(m_FileData, m_PosFileData, btBuffers(wavhdr.dwUser).Data, 0, lLen)

                '        Array.Copy(m_FileData, 0, btBuffers(wavhdr.dwUser).Data, lLen, mBuffSize - lLen)
                '        m_PosFileData = mBuffSize - lLen
                '    End If
                '    Marshal.Copy(btBuffers(wavhdr.dwUser).Data, 0, btBuffers(wavhdr.dwUser).memPtr, mBuffSize)
                'Else
                '    RaiseEvent Data(wavhdr.dwUser, btBuffers(wavhdr.dwUser).Data, mBuffSize)
                '    Marshal.Copy(btBuffers(wavhdr.dwUser).Data, 0, btBuffers(wavhdr.dwUser).memPtr, mBuffSize)
                'End If
                ''On Error Resume Next
                'WaveInWrite(hWaveIn, wavhdr, Len(wavhdr))
                'If Err.Number <> 0 Then
                '    dwParam2 += 1
                'End If
            End If
        End Sub

        Public Property DeviceID() As Integer
            Get
                Return m_DeviceID
            End Get
            Set(ByVal Value As Integer)
                m_DeviceID = Value
            End Set
        End Property

        Public Function GetDevicesList() As ArrayList
            Dim wic As WAVEINCAPS
            ReDim wic.szpName(32)
            Dim iNumDevs As Long
            Dim i As Long
            '/* Get the number of Digital Audio In devices in this computer */ 
            iNumDevs = waveInGetNumDevs()
            For i = 0 To iNumDevs
                If (waveInGetDevCaps(i, wic, Marshal.SizeOf(wic)) = 0) Then
                    MsgBox(wic.szpName)
                End If
            Next
        End Function

        'Here's the callback. 
        Private Sub waveInProc(ByVal hwi As Integer, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef dwParam1 As WAVEHDR, ByVal dwparam2 As Integer)
            'If we've received data... 
            Select Case uMsg
                Case WIM_DATA
                    'Copy the current buffer into the bytes array... 
                    If dwParam1.dwBytesRecorded > 0 Then
                        'Try to requeue the buffer... 
                        For bufferNumber As Integer = 0 To BUFFERS - 1
                            If (btBuffers(bufferNumber).hdr.dwFlags And WHDR_DONE) Then
                                Marshal.Copy(IntPtr.op_Explicit(dwParam1.lpData), btBuffers(bufferNumber).Data, 0, mBuffSize)
                                waveInAddBuffer(hWaveIn, dwParam1, Marshal.SizeOf(dwParam1))
                                RaiseEvent Streamed(btBuffers(bufferNumber).Data)
                                'mWaveOut.PlayData(dwParam1)
                                'result = waveInAddBuffer(waveInHandle, bufferHeader(bufferNumber), Marshal.SizeOf(bufferHeader(bufferNumber)))
                            End If
                        Next
                    End If
                Case WIM_OPEN
                    RaiseEvent State(True)
                Case WIM_CLOSE
                    RaiseEvent State(False)
            End Select
        End Sub
    End Class

