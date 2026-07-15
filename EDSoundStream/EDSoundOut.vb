Option Strict Off
Option Explicit On 
Imports System.Runtime.InteropServices

    Public Class EDSoundOut
        Implements IDisposable

        ' Subclassed window
        'Private mFrmMsg As Form
        'Private mFrmMsgHwnd As IntPtr

        ' Wave Out Handle
        Private hWaveOut As IntPtr

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
        Private btBuffersInit As Integer
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
        Public BitsPerSample As Byte = 8
        Public nChannels As Byte = 2
        Private mbPlaying As Boolean

        Private m_Callback As WaveDelegate

        Public Event Data(ByVal pNumBuff As Short, ByVal pData() As Byte, ByVal pLength As Integer)

        Private m_FileData() As Byte
        Private m_PlayFileData As Boolean = False
        Private m_PosFileData As Integer

        Private m_DeviceID As Integer

        Public Sub New()
            mBytesPerSec = 8000 '44100   ' 11025&'
            mBuffPerSec = 10
        End Sub
        Public Sub New(ByVal pBytesPerSec As Integer, ByVal pBuffPerSec As Byte)
            mBytesPerSec = pBytesPerSec
            mBuffPerSec = pBuffPerSec
        End Sub
        Public Sub New(ByVal pBytesPerSec As Integer, ByVal pBuffPerSec As Byte, ByVal pbStereo As Boolean)
            mBytesPerSec = pBytesPerSec
            mBuffPerSec = pBuffPerSec
            nChannels = IIf(pbStereo, 2, 1)
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            On Error Resume Next
            'Rééquilibre la balance de l'ordi
            VolumeTempL = Math.Max(mVolumeLR(0), mVolumeLR(1))
            VolumeTempR = Math.Max(mVolumeLR(0), mVolumeLR(1))
            SetVolume()

            StopPlay()
            DeInitWaveOut()
        End Sub
        Protected Overrides Sub Finalize()
            Dispose()
            MyBase.Finalize()
        End Sub
        Private Function InitWaveOut() As Boolean
            'Create a new window
            'mFrmMsg = New Form
            'mFrmMsgHwnd = mFrmMsg.Handle
            ' got a hWnd?
            'If Not mFrmMsgHwnd.Equals(IntPtr.Zero) Then
            '    'Subclass the window
            '    mOrginWndProc = GetWindowLong(mFrmMsgHwnd, GWL_WNDPROC)
            '    mDelegWndProc = AddressOf WndProc
            '    SetWindowLong(mFrmMsgHwnd, GWL_WNDPROC, mDelegWndProc)
            'Else
            '    Return False
            'End If

            ' open WaveOut
            If Not OpenDevice() Then 'mFrmMsgHwnd()
                DeInitWaveOut()
            Else
                InitVolumeLR()
                InitWaveOut = True
            End If

            Return True
        End Function

        Private Function DeInitWaveOut() As Boolean
            ' close wave out
            CloseDevice()

            'If Not mFrmMsgHwnd.Equals(IntPtr.Zero) Then
            '    ' unsubclass the window
            '    ' and destroy it
            '    SetWindowLong(mFrmMsgHwnd, GWL_WNDPROC, mOrginWndProc)
            '    DestroyWindow(mFrmMsgHwnd)
            '    If Not mFrmMsg Is Nothing Then mFrmMsg.Dispose()
            'End If
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

            ' open the waveout
            'OpenDevice = MMSYSERR_NOERROR = waveOutOpen(hWaveOut, 0, wfx, hWnd, 0, CALLBACKS.CALLBACK_WINDOW)
            m_Callback = AddressOf WaveCallBack
            OpenDevice = MMSYSERR_NOERROR = waveOutOpen(hWaveOut, m_DeviceID, wfx, m_Callback, 0, CALLBACKS.CALLBACK_FUNCTION + CALLBACKS.WAVE_ALLOWSYNC)
        End Function

        Private Sub CloseDevice()
            waveOutClose(hWaveOut)
        End Sub

        Public Sub StartPlay()

            Dim i As Integer

            DeInitWaveOut()
            If Not InitWaveOut() Then
                MsgBox("Can not initialize module.")
            End If

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
                    waveOutPrepareHeader(hWaveOut, .hdr, Len(.hdr))
                End With
            Next

            mbPlaying = True

            ' fill the just prepared buffers
            ' to write them to WaveOut
            btBuffersInit = 0
            For i = 0 To BUFFERS - 1
                btBuffers(i).hdrPtr = 0
                WaveCallBack(hWaveOut, MM_WOM_DONE, 0, btBuffers(i).hdr, 0)
                'SendMessageA(mFrmMsgHwnd, MM_WOM_DONE, 0, btBuffers(i).hdr)
            Next

        End Sub

        '###########################################

        Public Sub StopPlay()

            Dim i As Integer
            Dim j As Integer

            mbPlaying = False

            ' reset the buffers associated
            ' with our wave out handle
            waveOutReset(hWaveOut)

            ' del buffers
            For i = 0 To UBound(btBuffers)
                Do While 0 <> waveOutUnprepareHeader(hWaveOut, btBuffers(i).hdr, Len(btBuffers(i).hdr))
                    If j = 10 Then Exit Do
                    Threading.Thread.Sleep(50)
                    j = j + 1
                Loop
                GlobalFree(btBuffers(i).memHdl)
                btBuffers(i).memHdl = IntPtr.Zero
                btBuffers(i).memPtr = IntPtr.Zero
                If btBuffers(i).hHdr.IsAllocated Then btBuffers(i).hHdr.Free()
            Next
        End Sub

        'Private Function WndProc(ByVal hWnd As IntPtr, ByVal Msg As Integer, ByRef wParam As Integer, ByRef lParam As Integer) As Integer
        '    If Msg = MM_WOM_DONE Then
        '        Dim hdr As WAVEHDR
        '        Dim lBuff As WaveBuffer
        '        If Not mbPlaying Then Exit Function
        '        If btBuffersInit < BUFFERS Then
        '            ' copy the just played wave header
        '            CopyMemoryWHD(hdr, lParam, Len(hdr))
        '            lBuff = btBuffers(hdr.dwUser)
        '            If lBuff.hdrPtr = 0 Then btBuffersInit = btBuffersInit + 1
        '            lBuff.hdrPtr = lParam
        '        Else
        '            Dim i As Integer
        '            For i = 0 To BUFFERS - 1
        '                If btBuffers(i).hdrPtr = lParam Then
        '                    lBuff = btBuffers(i)
        '                    hdr = btBuffers(i).hdr
        '                    Exit For
        '                End If
        '            Next
        '            If i = BUFFERS Then Exit Function
        '        End If
        '        ' fill data 
        '        RaiseEvent Data(hdr.dwUser, lBuff.Data, mBuffSize)
        '        Marshal.Copy(lBuff.Data, 0, lBuff.memPtr, mBuffSize)
        '        ' play the buffer
        '        On Error Resume Next
        '        waveOutWrite(hWaveOut, lBuff.hdr, Len(lBuff.hdr))
        '        If Err.Number <> 0 Then
        '            lParam *= 1
        '        End If
        '    End If
        'End Function
        Private Sub WaveCallBack(ByVal hwo As IntPtr, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef wavhdr As WAVEHDR, ByVal dwParam2 As Integer)
            If uMsg = MM_WOM_DONE AndAlso mbPlaying Then
                If m_PlayFileData Then
                    Dim lLen As Integer
                    lLen = m_FileData.GetLength(0) - m_PosFileData
                    If lLen > mBuffSize Then 'Si on a de quoi fournir en une fois
                        Array.Copy(m_FileData, m_PosFileData, btBuffers(wavhdr.dwUser).Data, 0, mBuffSize)
                        m_PosFileData += mBuffSize
                    Else
                        Array.Copy(m_FileData, m_PosFileData, btBuffers(wavhdr.dwUser).Data, 0, lLen)

                        Array.Copy(m_FileData, 0, btBuffers(wavhdr.dwUser).Data, lLen, mBuffSize - lLen)
                        m_PosFileData = mBuffSize - lLen
                    End If
                    Marshal.Copy(btBuffers(wavhdr.dwUser).Data, 0, btBuffers(wavhdr.dwUser).memPtr, mBuffSize)
                Else
                    RaiseEvent Data(wavhdr.dwUser, btBuffers(wavhdr.dwUser).Data, mBuffSize)
                    Marshal.Copy(btBuffers(wavhdr.dwUser).Data, 0, btBuffers(wavhdr.dwUser).memPtr, mBuffSize)
                End If
                'On Error Resume Next
                waveOutWrite(hWaveOut, wavhdr, Len(wavhdr))
                'If Err.Number <> 0 Then
                '    dwParam2 += 1
                'End If
            End If
        End Sub
        Private mVolumeLR() As Integer = {&HFFFF, &HFFFF}
        Private mbVolChanged As Boolean
        Private Sub InitVolumeLR()
            Dim lVol As Integer
            waveOutGetVolume(hWaveOut, lVol)
            mVolumeLR(0) = lVol And &HFFFF
            mVolumeLR(1) = ((lVol And -&H10000) / &H10000) And &HFFFF
        End Sub
        Public WriteOnly Property VolumeTempL() As Integer
            Set(ByVal Value As Integer)
                mVolumeLR(0) = Value
                mbVolChanged = True
            End Set
        End Property
        Public Property VolumeL() As Integer
            Get
                InitVolumeLR()
                Return mVolumeLR(0)
            End Get
            Set(ByVal Value As Integer)
                mVolumeLR(0) = Value
                mbVolChanged = True
                SetVolume()
            End Set
        End Property
        Public WriteOnly Property VolumeTempR() As Integer
            Set(ByVal Value As Integer)
                mVolumeLR(1) = Value
                mbVolChanged = True
            End Set
        End Property
        Public Property VolumeR() As Integer
            Get
                InitVolumeLR()
                Return mVolumeLR(1)
            End Get
            Set(ByVal Value As Integer)
                mVolumeLR(1) = Value
                mbVolChanged = True
                SetVolume()
            End Set
        End Property
        Public Sub SetVolume()
            If Not mbVolChanged Then Exit Sub
            Dim TempG, TempD As Integer
            TempG = mVolumeLR(0)
            TempD = mVolumeLR(1)

            'on place TempD sur les 2 premiers octets (en faisant abstraction du signe)
            If TempD < &H8000 Then
                TempD = TempD * &H10000
            Else
                TempD = ((TempD - &H8000) * &H10000) Or &H80000000  'rajoute le signe
            End If

            mbVolChanged = False
            'concatenation des deux valeurs (gauche et droite) par opération booléenne
            waveOutSetVolume(hWaveOut, TempG Or TempD)
        End Sub

        Public Sub PlayFileData(ByVal psFile As String)
            m_PlayFileData = True
            With New WaveFileGetData(psFile)
                m_FileData = .Data
                BitsPerSample = .BitsPerSample
                BytesPerSec = .SamplesPerSec
                nChannels = .nChannels
            End With
            StartPlay()
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
            Dim Devices As Int32 = waveOutGetNumDevs()
            If Devices > 0 Then
                Dim Result As Int32
                Dim woc As New WAVEOUTCAPS
                Dim TList As New ArrayList
                For n As Int32 = 0 To Devices - 1
                    Result = (waveOutGetDevCaps(n, woc, Marshal.SizeOf(woc)))
                    If Result = MMSYSERR_NOERROR Then
                        TList.Add(woc.szPName)
                    Else
                        TList.Add("?")
                    End If
                Next
                Return TList
            End If
            Return Nothing
        End Function

        Private m_PosCopyData As Integer
        'Le client fournit les données à utiliser en boucle
        Public Sub UseThisData(ByVal pData() As Byte)
            '        Dim lLen As Integer
            '        Dim lPosSrce As Integer = m_PosCopyData
            '        Dim lPosDest As Integer = 0

            '        On Error GoTo Erreur
            '        If mDataSnd Is Nothing Then Return
            '        'Plus de srce que de dest
            '        If mDataSnd.GetLength(0) >= pLength Then
            '            If lPosSrce + pLength > mDataSnd.GetLength(0) Then
            '                'Reste de la source
            '                Array.Copy(mDataSnd, lPosSrce, pData, lPosDest, mDataSnd.GetLength(0) - lPosSrce)
            '                lPosDest += mDataSnd.GetLength(0) - lPosSrce
            '                'Début de la source
            '                lPosSrce = 0
            '                Array.Copy(mDataSnd, lPosSrce, pData, lPosDest, pLength - lPosSrce)
            '                m_PosCopyData = pLength - lPosSrce
            '            Else
            '                'Suite de la source
            '                Array.Copy(mDataSnd, lPosSrce, pData, lPosDest, pLength)
            '                m_PosCopyData += pLength
            '            End If
            '        Else
            '            'Plus de dest que de srce

            '            'Copie une 1ère fois la fin du tour précéd
            '            lPosDest = 0
            '            If lPosSrce > 0 Then
            '                Array.Copy(mDataSnd, lPosSrce, pData, lPosDest, mDataSnd.GetLength(0) - lPosSrce)
            '                lPosDest += mDataSnd.GetLength(0) - lPosSrce
            '            End If

            '            'Copie en boucle
            '            For lPosDest = lPosDest To pLength - 1 - mDataSnd.GetLength(0) Step mDataSnd.GetLength(0)
            '                Array.Copy(mDataSnd, 0, pData, lPosDest, mDataSnd.GetLength(0))
            '            Next

            '            'Copie la fin
            '            m_PosCopyData = (pLength - lPosDest) Mod mDataSnd.GetLength(0)
            '            If m_PosCopyData > mDataSnd.GetLength(0) Then Debugger.Break()
            '            Array.Copy(mDataSnd, 0, pData, lPosDest, m_PosCopyData)
            '        End If
            '        GoTo Fin
            'Erreur:
            '        m_PosCopyData = 0
            'Fin:
        End Sub
    End Class

