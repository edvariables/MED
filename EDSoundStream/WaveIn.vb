Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Windows.Forms

    Public Class WaveIn
#Region "API"

        'Constants... 
        Private Const WAVE_FORMAT_PCM As Integer = 1
        Private Const WAVE_MAPPER As Integer = -1
        Private Const CALLBACK_FUNCTION As Integer = &H30000
        Private Const CALLBACK_THREAD As Integer = 2097153
        Private Const MEM_COMMIT As Integer = 4096
        Private Const PAGE_READWRITE As Integer = 4
        Private Const WIM_OPEN As Integer = &H3BE
        Private Const WIM_CLOSE As Integer = &H3BF
        Private Const WIM_DATA As Integer = &H3C0

        'API Declarations... 
        Private Declare Function waveInOpen Lib "winmm.dll" Alias "waveInOpen" (ByRef lphWaveIn As Integer, ByVal uDeviceID As Integer, ByRef lpFormat As WAVEFORMAT, ByVal dwCallback As waveInProcDelegate, ByVal dwInstance As Integer, ByVal dwFlags As Integer) As Integer
        Private Declare Function waveInStart Lib "winmm.dll" Alias "waveInStart" (ByVal hWaveIn As Integer) As Integer
        Private Declare Function waveInStop Lib "winmm.dll" Alias "waveInStop" (ByVal hWaveIn As Integer) As Integer
        Private Declare Function waveInAddBuffer Lib "winmm.dll" Alias "waveInAddBuffer" (ByVal hWaveIn As Integer, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Private Declare Function waveInGetNumDevs Lib "winmm.dll" Alias "waveInGetNumDevs" () As Integer
        Private Declare Function waveInGetDevCaps Lib "winmm.dll" Alias "waveInGetDevCapsA" (ByVal uDeviceID As Integer, ByRef lpCaps As WAVEINCAPS, ByVal uSize As Integer) As Integer
        Private Declare Function waveInPrepareHeader Lib "winmm.dll" Alias "waveInPrepareHeader" (ByVal hWaveIn As Integer, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Private Declare Function waveInUnPrepareHeader Lib "winmm.dll" Alias "waveInUnprepareHeader" (ByVal hWaveIn As Integer, ByRef lpWaveInHdr As WAVEHDR, ByVal uSize As Integer) As Integer
        Private Declare Function VirtualAlloc Lib "kernel32" (ByVal lpAddress As Integer, ByVal dwSize As Integer, ByVal flAllocationType As Integer, ByVal flProtect As Integer) As Integer
        'Alternative - with Thread - Declare Function waveInOpen Lib "winmm.dll" Alias "waveInOpen" (ByRef lphWaveIn As Integer, ByVal uDeviceID As Integer, ByRef lpFormat As WAVEFORMATEX, ByVal dwCallback As Integer, ByVal dwInstance As Integer, ByVal dwFlags As Integer) As Integer 

        'Define the signature of our callback procedure... 
        Private Delegate Sub waveCallbackProc(ByVal hwi As Integer, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef dwParam1 As WAVEHDR, ByVal dwparam2 As Integer)

        Structure WAVEINCAPS
            Dim wMid As Short
            Dim wPid As Short
            Dim vDriverVersion As Long
            <MarshalAs(UnmanagedType.ByValArray, SizeConst:=32)> Dim szpName() As Char 'String of length 32 
            Dim dwFormats As Integer
            Dim wChannels As Short
            Dim wReserved1 As Short
        End Structure

        Private Const WHDR_DONE = &H1&              '/* done bit */
        Private Const WHDR_PREPARED = &H2&          '/* set if this header has been prepared */
        Private Const WHDR_BEGINLOOP = &H4&         '/* loop start block */
        Private Const WHDR_ENDLOOP = &H8&           '/* loop end block */
        Private Const WHDR_INQUEUE = &H10&          '/* reserved for driver */

#End Region
        Private Const BUFFER_SIZE As Integer = 8192 / 2
        Private Const NUMBER_OF_BUFFERS As Integer = 2
        Private Const FREQUENCE As Integer = 11025 '44100 ' 

        'Create a number of WAVEHDRs for n-tuple buffering... 
        'One will be processed while another is being filled. 
        Private bufferHeader(NUMBER_OF_BUFFERS - 1) As WAVEHDR

        'Define the callback procedure... 
        Private Delegate Sub waveInProcDelegate(ByVal hwi As Integer, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef dwParam1 As WAVEHDR, ByVal dwparam2 As Integer)
        Private pCallback As waveInProcDelegate = New waveInProcDelegate(AddressOf waveInProc)

        'Store the handle to the waveIn device we are using... 
        Private waveInHandle As Integer

        'This array of bytes will store each chunk of input data... 
        Private mWaveBytes(NUMBER_OF_BUFFERS - 1)() As Byte            'BUFFER_SIZE - 1
        Private mFourierTransData(1024) As Single

        'This keeps track of the buffer number we are working with... 
        Private bufferNumber As Byte

        'Declare a form to draw the new graph on... 
        Public WithEvents mPic As PictureBox

        'Calculates real FPS : use FPS property while running
        Private mFPSStart As Long
        Private mFPSCount As Integer
        'Calculates times elapse to perform operations : use GetPerf to get results
        Private mPerfCount(1) As Integer
        Private mPerfTime(1) As Long
        Private mPerfErrors As Integer

        Public Event State(ByVal pbRunning As Boolean)
        Public Event Streamed(ByVal pmWaveBytes()() As Byte)

        Private WithEvents mTimer As New Timer

        Private mFourierT As FourierTransAudio

        Private mVolume As Single

        Public mVolumeToRaise As Byte
        Public Event VolumeRaised(ByVal pVolume As Byte)

        Private mWaveFormat As WAVEFORMAT

        'Private mWaveOut As WaveOut

        'This procedure opens the given wave device and starts waveIn on it... 
        Public Sub openWaveDevice(ByVal devName As Integer)
            'result of MM operations... 
            Dim result As Integer

            Dim b As Integer

            'Create a wave format object and set it up for 16bit 44khz stereo... 
            mWaveFormat.wFormatTag = WAVE_FORMAT_PCM
            mWaveFormat.nChannels = 1
            mWaveFormat.nSamplesPerSec = FREQUENCE
            mWaveFormat.wBitsPerSample = 8 '16
            mWaveFormat.nBlockAlign = mWaveFormat.nChannels * (mWaveFormat.wBitsPerSample / 8)
            mWaveFormat.nAvgBytesPerSec = mWaveFormat.nSamplesPerSec * mWaveFormat.nBlockAlign

            mWaveFormat.cbSize = 0

            'Try to open the preferred device... 
            result = waveInOpen(waveInHandle, WAVE_MAPPER, mWaveFormat, pCallback, 0, CALLBACK_FUNCTION)
            If (result) Then MsgBox("There was an error opening the device: " & result)

            'Allocate the memory for all buffers at once... 
            For b = 0 To NUMBER_OF_BUFFERS - 1
                ReDim mWaveBytes(b)(BUFFER_SIZE - 1)
                bufferHeader(b).dwBufferLength = BUFFER_SIZE
                bufferHeader(b).dwFlags = 0
                bufferHeader(b).lpData = VirtualAlloc(0, bufferHeader(b).dwBufferLength * NUMBER_OF_BUFFERS, MEM_COMMIT, PAGE_READWRITE)
            Next
            For b = 0 To NUMBER_OF_BUFFERS - 1
                'Prepare header... 
                result = waveInPrepareHeader(waveInHandle, bufferHeader(b), Marshal.SizeOf(bufferHeader(b)))
                If (result) Then MsgBox("Error preparing the header. Error: " & result)
                result = waveInAddBuffer(waveInHandle, bufferHeader(b), Marshal.SizeOf(bufferHeader(b)))
                'If (result) Then MsgBox("There was an error adding the " & b & "th buffer. Error: " & result.ToString)
                'result = waveInUnPrepareHeader(waveInHandle, bufferHeader(b), Marshal.SizeOf(bufferHeader(b)))
            Next

            mFourierT = New FourierTransAudio(BUFFER_SIZE)

            'Try to start waveIn... 
            result = waveInStart(waveInHandle)
            If (result) Then MsgBox("There was an error starting wave input. Error: " & result)
            mFPSStart = 0

            'mWaveOut = New WaveOut(mWaveFormat)
            'mWaveOut.StartPlay()
        End Sub

        'This procedure just stops waveIn... 
        Public Sub stopWave()
            'Try to stop the waveIn... 
            Dim result As Integer = waveInStop(waveInHandle)
            If (result) Then MsgBox("There was an error stopping wav input " & result)
        End Sub

        'Here's the callback. 
        Private Sub waveInProc(ByVal hwi As Integer, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef dwParam1 As WAVEHDR, ByVal dwparam2 As Integer)
            'If we've received data... 
            Select Case uMsg
                Case WIM_DATA
                    'Copy the current buffer into the bytes array... 
                    If dwParam1.dwBytesRecorded > 0 Then
                        'Try to requeue the buffer... 
                        For bufferNumber = 0 To NUMBER_OF_BUFFERS - 1
                            If (bufferHeader(bufferNumber).dwFlags And WHDR_DONE) Then
                                Marshal.Copy(IntPtr.op_Explicit(dwParam1.lpData), mWaveBytes(bufferNumber), 0, BUFFER_SIZE)
                                waveInAddBuffer(waveInHandle, dwParam1, Marshal.SizeOf(dwParam1))
                                'mWaveOut.PlayData(dwParam1)
                                'result = waveInAddBuffer(waveInHandle, bufferHeader(bufferNumber), Marshal.SizeOf(bufferHeader(bufferNumber)))
                            End If
                        Next
                        If mPic Is Nothing _
                        And mVolumeToRaise > 0 Then
                            If Not CheckVolume() Then
                                RaiseEvent Streamed(mWaveBytes)
                            End If
                        End If
                        CalcFPS()
                    Else
                        mPerfErrors += 1
                    End If
                Case WIM_OPEN
                    RaiseEvent State(True)
                    If Not mPic Is Nothing Then
                        mTimer.Interval = 1000 / 25
                        mTimer.Enabled = True
                    End If
                Case WIM_CLOSE
                    RaiseEvent State(False)
                    mTimer.Enabled = False
            End Select
        End Sub

        'Lists wave devices. Not relevant here... 
        Public Sub listWaveDevices()
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
        End Sub

        Public ReadOnly Property FPS() As Single
            Get
                Return mFPSCount / (Now.Ticks - mFPSStart) * TimeSpan.TicksPerSecond
            End Get
        End Property
        ''''''''''''''''''''''''''''''''''''''''''''''
        Public Function GetPerf() As String
            Dim lsPerf As String
            If mPerfCount(0) > 0 And mPerfCount(1) > 0 Then
                lsPerf = Format(mPerfTime(0) / mPerfCount(0) / TimeSpan.TicksPerMillisecond, "0.000") & ", " & Format(mPerfTime(1) / mPerfCount(1) / TimeSpan.TicksPerMillisecond, "0.000")
            End If
            lsPerf &= ", Err=" & mPerfErrors & ", Vol=" & mVolume
            Return lsPerf
        End Function
        Private Sub CalcFPS()
            Dim lNow As Long = Now.Ticks
            If mFPSStart = 0 Then
                mFPSStart = lNow
                mFPSCount = 1
            ElseIf (lNow - mFPSStart) > 10 * TimeSpan.TicksPerSecond Then 'Reset every 10 sec
                mPerfCount(0) = 0
                mPerfTime(0) = 0
                mPerfCount(1) = 0
                mPerfTime(1) = 0
                mFPSStart = lNow
                mFPSCount = 1
                mPerfErrors = 0
            Else
                mFPSCount += 1
            End If
        End Sub

        Private Sub mPic_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles mPic.Paint
            Dim g As Graphics = e.Graphics
            g.Clear(Color.Black)
            'Start drawing at X = 0, Y = middle of the window. 
            Dim MidY As Integer = (g.VisibleClipBounds.Height / 2)
            Dim dY As Single = g.VisibleClipBounds.Height / 256
            Dim y As Integer
            Dim oldPoint As Point
            Dim newPoint As Point
            Dim lColor As Color
            Dim lPen As Pen
            'For each byte... 
            Dim i As Integer
            Dim dx As Single = (g.VisibleClipBounds.Width / BUFFER_SIZE)
            Dim lnBuffer As Integer

            Dim Data As Single
            Dim Moyenne As Single
            Static OldMoyenne As Single

            For lnBuffer = 0 To 0 'NUMBER_OF_BUFFERS - 1
                'lColor = Choose(lnBuffer + 1 + 1, Color.Red, Color.Green, Color.Blue)
                'lPen = New Pen(lColor, 1)
                'y = MidY
                'oldPoint = New Point(0, y)
                'For i = 0 To BUFFER_SIZE - 1 Step 2
                '    'Set the Y coordinate... 
                '    y = MidY + dY * (mWaveBytes(lnBuffer)(i) - 127)
                '    'create a new point to draw to... 
                '    newPoint = New Point(dx * i, y)
                '    'Draw to that point... 
                '    g.DrawLine(lPen, oldPoint, newPoint)
                '    'set the old point to the new point... 
                '    oldPoint = newPoint
                'Next
                lColor = Choose(lnBuffer + 1 + 1, Color.Red, Color.Green, Color.Blue)
                lPen = New Pen(lColor, 1)
                y = MidY
                oldPoint = New Point(1, y)
                mVolume = 0
                For i = 1 To BUFFER_SIZE - 1 Step 2
                    If mWaveBytes(lnBuffer)(i) > 127 Then
                        'Set the Y coordinate... 
                        y = MidY + dY * (mWaveBytes(lnBuffer)(i) - 127)
                        mVolume += mWaveBytes(lnBuffer)(i)
                        'create a new point to draw to... 
                        newPoint = New Point(dx * i, y)
                        'Draw to that point... 
                        g.DrawLine(lPen, oldPoint, newPoint)
                        'set the old point to the new point... 
                        oldPoint = newPoint
                    Else
                        mWaveBytes(lnBuffer)(i - 1) = mWaveBytes(lnBuffer)(i)
                    End If
                Next
                mVolume /= BUFFER_SIZE / 2
                mVolume = Math.Round(256 - mVolume)
            Next

            dx = (g.VisibleClipBounds.Width / 256)
            MidY = g.VisibleClipBounds.Height '+ 10
            For lnBuffer = 0 To 0 'NUMBER_OF_BUFFERS - 1
                lColor = Choose(lnBuffer + 1, Color.Red, Color.Green, Color.Blue)
                lPen = New Pen(lColor, 1)
                mFourierT.Convert(mWaveBytes(lnBuffer), mFourierTransData)
                For i = 0 To 255
                    oldPoint = New Point(i * dx, MidY)
                    Data = (Math.Sqrt(Math.Abs(mFourierTransData(i))) + Math.Sqrt(Math.Abs(mFourierTransData(i + 1))))
                    Moyenne = Moyenne + Data
                    Data = Math.Abs(Data - OldMoyenne)
                    If Math.Abs(Data - OldMoyenne) > 10 Then
                        newPoint = New Point(i * dx, MidY - Data)
                        g.DrawLine(lPen, oldPoint, newPoint)
                    End If
                Next
                Moyenne = Moyenne / (i + 1)
                OldMoyenne = (Moyenne + OldMoyenne) / 2
            Next

            If mVolume >= mVolumeToRaise Then
                RaiseEvent VolumeRaised(mVolume)
            End If

        End Sub

        Private Sub mTimer_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles mTimer.Tick
            RaiseEvent Streamed(mWaveBytes)
            If Not mPic Is Nothing Then
                mPic.Invalidate()
            ElseIf mVolumeToRaise > 0 Then
                CheckVolume()
            End If
        End Sub
        Private Function CheckVolume() As Boolean
            Dim i As Integer
            Dim lnBuffer As Integer
            Dim lCount As Integer
            Dim lVal As Byte
            Dim lSum As Integer
            For lnBuffer = 0 To 0
                mVolume = 0
                For i = 1 To BUFFER_SIZE - 1 Step (BUFFER_SIZE \ 256)
                    lVal = mWaveBytes(lnBuffer)(i)
                    If lVal < &H80 Then
                        lSum += CInt(lVal)
                        lCount += 1
                    End If
                Next
                mVolume = lSum / lCount
                'mVolume = Math.Round(256 - mVolume)
            Next
            If mVolume >= mVolumeToRaise Then
                RaiseEvent VolumeRaised(mVolume)
                Return True
            End If
        End Function

        Public ReadOnly Property Volume() As Short
            Get
                Return CShort(mVolume)
            End Get
        End Property
    End Class
