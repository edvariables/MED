Option Strict Off
Option Explicit On
Imports System.Runtime.InteropServices

    Module modWaveOut

        ' waveOut functions

        'UPGRADE_WARNING: La structure WAVEFORMAT peut nécessiter que des attributs de marshaling soient passés en tant qu'argument dans cette instruction Declare. Cliquez ici : 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1050"'
        Public Declare Function waveOutOpen Lib "winmm.dll" (ByRef lphWaveOut As IntPtr, ByVal uDeviceID As Integer, ByRef lpFormat As WAVEFORMAT, ByVal dwCallback As IntPtr, ByVal dwInstance As Integer, ByVal dwFlags As Integer) As Integer

        Public Delegate Sub WaveDelegate(ByVal hwo As IntPtr, ByVal uMsg As Integer, ByVal dwInstance As Integer, ByRef wavhdr As WAVEHDR, ByVal dwParam2 As Integer)
        Public Declare Ansi Function waveOutOpen Lib "winmm.dll" (ByRef hWaveOut As IntPtr, ByVal uDeviceID As Integer, ByRef Format As WAVEFORMAT, ByVal dwCallback As WaveDelegate, ByRef fPlaying As Integer, ByVal dwFlags As Integer) As Integer

        'UPGRADE_WARNING: La structure WAVEHDR peut nécessiter que des attributs de marshaling soient passés en tant qu'argument dans cette instruction Declare. Cliquez ici : 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1050"'
        Public Declare Function waveOutPrepareHeader Lib "winmm.dll" (ByVal hWaveOut As IntPtr, ByRef lpWaveOutHdr As WAVEHDR, ByVal uSize As Integer) As Integer

        'UPGRADE_WARNING: La structure WAVEHDR peut nécessiter que des attributs de marshaling soient passés en tant qu'argument dans cette instruction Declare. Cliquez ici : 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1050"'
        Public Declare Function waveOutWrite Lib "winmm.dll" (ByVal hWaveOut As IntPtr, ByRef lpWaveOutHdr As WAVEHDR, ByVal uSize As Integer) As Integer

        'UPGRADE_WARNING: La structure WAVEHDR peut nécessiter que des attributs de marshaling soient passés en tant qu'argument dans cette instruction Declare. Cliquez ici : 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1050"'
        Public Declare Function waveOutUnprepareHeader Lib "winmm.dll" (ByVal hWaveOut As IntPtr, ByRef lpWaveOutHdr As WAVEHDR, ByVal uSize As Integer) As Integer

        Public Declare Function waveOutClose Lib "winmm.dll" (ByVal hWaveOut As IntPtr) As Integer

        Public Declare Function waveOutReset Lib "winmm.dll" (ByVal hWaveOut As IntPtr) As Integer

        Public Declare Function waveOutSetVolume Lib "winmm.dll" (ByVal hWaveOut As IntPtr, ByVal dwVolume As Integer) As Integer
        Public Declare Function waveOutGetVolume Lib "winmm.dll" (ByVal hWaveOut As IntPtr, ByRef dwVolume As Integer) As Integer

        Public Declare Function waveOutPause Lib "winmm.dll" (ByVal hWaveOut As IntPtr) As Integer

        Public Declare Function waveOutRestart Lib "winmm.dll" (ByVal hWaveOut As IntPtr) As Integer

        Public Declare Function waveOutGetNumDevs Lib "winmm.dll" () As Integer
        Public Declare Auto Function waveOutGetDevCaps Lib "winmm.dll" (ByVal uDeviceID As Int32,
                                                                        ByRef pwoc As WAVEOUTCAPS,
                                                                        ByVal cbwoc As Int32) As Int32

        'UPGRADE_ISSUE: La déclaration d'un paramètre 'As Any' n'est pas prise en charge. Cliquez ici : 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1016"'
        'Public Declare Function SendMessageA Lib "user32" (ByVal hWnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Any) As Integer
        Public Declare Function SendMessageA Lib "user32" (ByVal hWnd As IntPtr, ByVal wMsg As Integer, ByRef wParam As Integer, ByRef lParam As WAVEHDR) As Integer

        Public Declare Function CreateWindowEx Lib "user32" Alias "CreateWindowExA" (ByVal dwExStyle As Integer, ByVal lpClassName As String, ByVal lpWindowName As String, ByVal dwStyle As Integer, ByVal x As Integer, ByVal y As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer, ByVal hWndParent As Integer, ByVal hMenu As Integer, ByVal hInstance As Integer, ByVal lpParam As Integer) As IntPtr

        Public Declare Function DestroyWindow Lib "user32" (ByVal hWnd As IntPtr) As Integer

        Public Declare Function SetWindowLong Lib "user32" Alias "SetWindowLongA" (ByVal hWnd As Integer, ByVal nIndex As Integer, ByVal dwNewLong As Integer) As Integer

        Public Declare Function CallWindowProc Lib "user32" Alias "CallWindowProcA" (ByVal lpPrevWndFunc As IntPtr, ByVal hWnd As IntPtr, ByRef Msg As Integer, ByRef wParam As Integer, ByVal lParam As Integer) As Integer

        Public Const MM_WOM_DONE As Integer = &H3BD
        Public Const WOM_OPEN As Integer = &H3BB
        Public Const WOM_CLOSE As Integer = &H3BC

        Public Const WHDR_DONE As Integer = &H1S

        Private Const MAXPNAMELEN As Integer = 32
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
        Structure WAVEOUTCAPS
            Public wMid As Short     'Manufacturer identifier for the device driver for the device.
            Public wPid As Short     'Product identifier for the device.
            Public vDriverVersion As Int32   'Version number of the device driver for the device.
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAXPNAMELEN)>
            Public szPName As String    'Product name in a null-terminated string.
            Public dwFormats As Int32    '***
            Public wChannels As Short    'Number specifying whether the device supports mono (1) 
            'or stereo (2) output.
            Public wReserved1 As Short    'Packing.
            Public dwSupport As Int32    '****
        End Structure


        <StructLayout(LayoutKind.Sequential)>
        Public Structure WAVEHDR
            Dim lpData As Integer
            Dim dwBufferLength As Integer
            Dim dwBytesRecorded As Integer
            Dim dwUser As Integer
            Dim dwFlags As Integer
            Dim dwLoops As Integer
            Dim lpNext As Integer
            Dim reserved As Integer
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Public Structure WAVEFORMAT
            Dim wFormatTag As Short
            Dim nChannels As Short
            Dim nSamplesPerSec As Integer
            Dim nAvgBytesPerSec As Integer
            Dim nBlockAlign As Short
            Dim wBitsPerSample As Short
            Dim cbSize As Short
        End Structure

        Public Declare Function GlobalAlloc Lib "kernel32" (ByVal wFlags As Integer, ByVal dwBytes As Integer) As IntPtr
        Public Declare Function GlobalLock Lib "kernel32" (ByVal hmem As IntPtr) As IntPtr
        Public Declare Function GlobalFree Lib "kernel32" (ByVal hmem As IntPtr) As Integer
        Public Declare Sub CopyMemoryWHD Lib "kernel32" Alias "RtlMoveMemory" (ByRef lpvDest As WAVEHDR, ByRef lpvSource As Integer, ByVal cbCopy As Integer)
        Public Const MMSYSERR_NOERROR As Integer = 0
        Public Enum CALLBACKS
            CALLBACK_NULL = &H0
            WAVE_ALLOWSYNC = &H2
            CALLBACK_EVENT = &H50000
            CALLBACK_WINDOW = &H10000
            CALLBACK_THREAD = &H20000
            CALLBACK_TYPEMASK = &H70000
            CALLBACK_FUNCTION = &H30000
        End Enum

        Public Delegate Function DelegWndProc(ByVal hWnd As IntPtr, ByVal Msg As Integer, ByRef wParam As Integer, ByRef lParam As Integer) As Integer
        Public mDelegWndProc As DelegWndProc
        Public mOrginWndProc As Integer
        Public Declare Function GetWindowLong Lib "user32" Alias "GetWindowLongA" _
                                    (ByVal hWnd As IntPtr, ByVal nIndex As Integer) As Integer
        Public Declare Function SetWindowLong Lib "user32" Alias "SetWindowLongA" _
                                    (ByVal hWnd As IntPtr, ByVal nIndex As Integer,
                                    ByVal dwNewLong As DelegWndProc) As Integer
        Public Declare Function SetWindowLong Lib "user32" Alias "SetWindowLongA" _
                                    (ByVal hWnd As IntPtr, ByVal nIndex As Integer,
                                    ByVal dwNewLong As Integer) As Integer
        Public Declare Function CallWindowProc Lib "user32" Alias "CallWindowProcA" _
                                    (ByVal lpPrevWndFunc As Integer, ByVal hWnd As IntPtr,
                                    ByVal Msg As Integer, ByVal wParam As Integer,
                                    ByVal lParam As Integer) As Integer
        Public Const GWL_WNDPROC As Integer = (-4)

        Public Sub WaveOutCallBack(ByVal hwo As Integer, ByVal uMsg As Integer, ByVal ByValdwInstance As Integer, ByRef wavhdr As WAVEHDR, ByVal dwParam2 As Integer)

        End Sub
    End Module

