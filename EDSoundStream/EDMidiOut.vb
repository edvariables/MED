Imports System.Runtime.InteropServices

Public Class EDMidiOutNames
    Public Structure Note
        Public Frequence As Single
        Public FullName As String
        Public Name As String
        Public Index As Integer '0->127
        Public Note As Byte     '1->12
        Public Octave As Short  '-1->9
    End Structure
    Public Shared Notes(127) As Note
    Public Shared NotesNames() As String
    Private Shared Sub MakeNotes()
        Dim lIndex As Byte
        Dim m_NoteFreq(12, 10) As Single
        m_NoteFreq(0, 0) = 16.3
        m_NoteFreq(1, 0) = 17.3
        m_NoteFreq(2, 0) = 18.3
        m_NoteFreq(3, 0) = 19.4

        m_NoteFreq(4, 0) = 20.5
        m_NoteFreq(5, 0) = 21.8
        m_NoteFreq(6, 0) = 23.1
        m_NoteFreq(7, 0) = 24.5

        m_NoteFreq(8, 0) = 26
        m_NoteFreq(9, 0) = 27.5
        m_NoteFreq(10, 0) = 29.1
        m_NoteFreq(11, 0) = 30.8

        m_NoteFreq(0, 1) = 32.7
        m_NoteFreq(1, 1) = 34.6
        m_NoteFreq(2, 1) = 36.7
        m_NoteFreq(3, 1) = 38.9

        m_NoteFreq(4, 1) = 41.2
        m_NoteFreq(5, 1) = 43.6
        m_NoteFreq(6, 1) = 46.2
        m_NoteFreq(7, 1) = 49

        m_NoteFreq(8, 1) = 51.9
        m_NoteFreq(9, 1) = 55
        m_NoteFreq(10, 1) = 58
        m_NoteFreq(11, 1) = 62

        m_NoteFreq(0, 2) = 65
        m_NoteFreq(1, 2) = 69
        m_NoteFreq(2, 2) = 74
        m_NoteFreq(3, 2) = 78

        m_NoteFreq(4, 2) = 83
        m_NoteFreq(5, 2) = 87
        m_NoteFreq(6, 2) = 92.5
        m_NoteFreq(7, 2) = 98

        m_NoteFreq(8, 2) = 104
        m_NoteFreq(9, 2) = 110
        m_NoteFreq(10, 2) = 117
        m_NoteFreq(11, 2) = 123

        m_NoteFreq(0, 3) = 131
        m_NoteFreq(1, 3) = 139
        m_NoteFreq(2, 3) = 147
        m_NoteFreq(3, 3) = 156

        m_NoteFreq(4, 3) = 165
        m_NoteFreq(5, 3) = 175
        m_NoteFreq(6, 3) = 185
        m_NoteFreq(7, 3) = 196

        m_NoteFreq(8, 3) = 208
        m_NoteFreq(9, 3) = 220
        m_NoteFreq(10, 3) = 233
        m_NoteFreq(11, 3) = 247

        m_NoteFreq(0, 4) = 262
        m_NoteFreq(1, 4) = 277
        m_NoteFreq(2, 4) = 294
        m_NoteFreq(3, 4) = 311

        m_NoteFreq(4, 4) = 330
        m_NoteFreq(5, 4) = 349
        m_NoteFreq(6, 4) = 370
        m_NoteFreq(7, 4) = 392

        m_NoteFreq(8, 4) = 415
        m_NoteFreq(9, 4) = 440
        m_NoteFreq(10, 4) = 466
        m_NoteFreq(11, 4) = 494

        m_NoteFreq(0, 5) = 523
        m_NoteFreq(1, 5) = 554
        m_NoteFreq(2, 5) = 587
        m_NoteFreq(3, 5) = 622

        m_NoteFreq(4, 5) = 659
        m_NoteFreq(5, 5) = 694
        m_NoteFreq(6, 5) = 740
        m_NoteFreq(7, 5) = 784

        m_NoteFreq(8, 5) = 831
        m_NoteFreq(9, 5) = 880
        m_NoteFreq(10, 5) = 932
        m_NoteFreq(11, 5) = 988

        m_NoteFreq(0, 6) = 1046.5
        m_NoteFreq(1, 6) = 1109
        m_NoteFreq(2, 6) = 1175
        m_NoteFreq(3, 6) = 1244.5

        m_NoteFreq(4, 6) = 1318.5
        m_NoteFreq(5, 6) = 1397
        m_NoteFreq(6, 6) = 1480
        m_NoteFreq(7, 6) = 1568

        m_NoteFreq(8, 6) = 1661
        m_NoteFreq(9, 6) = 1760
        m_NoteFreq(10, 6) = 1865
        m_NoteFreq(11, 6) = 1975

        m_NoteFreq(0, 7) = 2093
        m_NoteFreq(1, 7) = 2217
        m_NoteFreq(2, 7) = 2349
        m_NoteFreq(3, 7) = 2489

        m_NoteFreq(4, 7) = 2637
        m_NoteFreq(5, 7) = 2794
        m_NoteFreq(6, 7) = 2960
        m_NoteFreq(7, 7) = 3136

        m_NoteFreq(8, 7) = 3322
        m_NoteFreq(9, 7) = 3520
        m_NoteFreq(10, 7) = 3729
        m_NoteFreq(11, 7) = 3951

        m_NoteFreq(0, 8) = 4186
        m_NoteFreq(1, 8) = 4435
        m_NoteFreq(2, 8) = 4698
        m_NoteFreq(3, 8) = 4978

        m_NoteFreq(4, 8) = 5274
        m_NoteFreq(5, 8) = 5588
        m_NoteFreq(6, 8) = 5920
        m_NoteFreq(7, 8) = 6272

        m_NoteFreq(8, 8) = 6645
        m_NoteFreq(9, 8) = 7040
        m_NoteFreq(10, 8) = 7458
        m_NoteFreq(11, 8) = 7902

        m_NoteFreq(0, 9) = 8372
        m_NoteFreq(1, 9) = 8870
        m_NoteFreq(2, 9) = 9396
        m_NoteFreq(3, 9) = 9956

        m_NoteFreq(4, 9) = 10548
        m_NoteFreq(5, 9) = 11176
        m_NoteFreq(6, 9) = 11840
        m_NoteFreq(7, 9) = 12544

        m_NoteFreq(8, 9) = 13290
        m_NoteFreq(9, 9) = 14080
        m_NoteFreq(10, 9) = 14918
        m_NoteFreq(11, 9) = 15804

        Dim lNames() As String = {"do", "do#", "ré", "ré#", "mi", "mi#", "fa", "sol", "sol#", "la", "la#", "si"}
        ReDim NotesNames(127)
        For lIndex = 0 To 127
            With Notes(lIndex)
                .Index = lIndex
                .Note = lIndex Mod 12
                .Octave = lIndex \ 12
                .Frequence = m_NoteFreq(.Note, .Octave)
                .Name = lNames(.Note)
                .FullName = .Name & " " & .Octave
                NotesNames(.Index) = .FullName
            End With
        Next

    End Sub
    Public Shared Function GetNoteIndex(ByVal pNoteName As String) As Integer
        Return NotesNames(pNoteName)
    End Function
    Public Shared AccordsNames() As String
    Public Shared AccordsVals()() As Byte 'Notes à jouer en plus de la note de base
    Private Shared Sub MakeAccords()
        Dim lNotes() As String = {"do", "do#", "ré", "ré#", "mi", "mi#", "fa", "sol", "sol#", "la", "la#", "si"}
        AccordsNames = New String() {"Majeur",
                                    "Mineur",
                                    "Dominant 7th",
                                    "Dim",
                                    "5",
                                    "Majeur 7th",
                                    "Mineur 7th",
                                    "Minor Major 7th",
                                    "Sus 4",
                                    "Sus 2",
                                    "6",
                                    "Minor 6",
                                    "9",
                                    "Minor 9",
                                    "Major 9",
                                    "Minor Major 9",
                                    "11",
                                    "Minor 11",
                                    "Major 11",
                                    "Minor Major 11",
                                    "13",
                                    "Minor 13",
                                    "Major 13",
                                    "Minor Major 13",
                                    "add 9",
                                    "Minor add 9",
                                    "6 add 9",
                                    "Minor 6 add 9",
                                    "Dominant 7th add 11",
                                    "Major 7th add 11",
                                    "Minor 7th add 11",
                                    "Minor Major 7th add 11",
                                    "Dominant 7th add 13",
                                    "Major 7th add 13",
                                    "Minor 7th add 13",
                                    "Minor Major 7th add 13",
                                    "7b5",
                                    "7#5",
                                    "7b9",
                                    "7#9",
                                    "7#5b9",
                                    "m7b5",
                                    "m7#5",
                                    "m7b9",
                                    "9#11",
                                    "9b13",
                                    "6sus4",
                                    "7sus4",
                                    "Major 7th Sus4",
                                    "9sus4",
                                    "Major 9 Sus4"}
        'Notes à jouer en plus de la note de base
        AccordsVals = New Byte()() {New Byte() {5, 8},
                                    New Byte() {4, 8},
                                    New Byte() {5, 8, 11},
                                    New Byte() {4, 7},
                                    New Byte() {8},
                                    New Byte() {5, 8, 12},
                                    New Byte() {4, 8, 11},
                                    New Byte() {4, 8, 12},
                                    New Byte() {6, 8},
                                    New Byte() {3, 8},
                                    New Byte() {5, 8, 10},
                                    New Byte() {4, 8, 10},
                                    New Byte() {5, 8, 15},
                                    New Byte() {4, 8, 15},
                                    New Byte() {5, 8, 16},
                                    New Byte() {4, 8, 12, 14},
                                    New Byte() {5, 8, 11, 3, 6},
                                    New Byte() {4, 8, 11, 3, 6},
                                    New Byte() {5, 8, 11, 3, 6},
                                    New Byte() {4, 8, 12, 3, 6},
                                    New Byte() {5, 8, 11, 3, 10},
                                    New Byte() {4, 8, 11, 3, 10},
                                    New Byte() {5, 8, 12, 3, 10},
                                    New Byte() {4, 8, 12, 3, 10},
                                    New Byte() {5, 8, 3},
                                    New Byte() {4, 8, 3},
                                    New Byte() {5, 8, 10, 3},
                                    New Byte() {4, 8, 10, 3},
                                    New Byte() {5, 8, 11, 6},
                                    New Byte() {5, 8, 12, 6},
                                    New Byte() {4, 8, 11, 6},
                                    New Byte() {4, 8, 12, 6},
                                    New Byte() {5, 8, 11, 10},
                                    New Byte() {5, 8, 12, 10},
                                    New Byte() {4, 8, 11, 10},
                                    New Byte() {4, 8, 12, 10},
                                    New Byte() {5, 7, 11},
                                    New Byte() {5, 9, 11},
                                    New Byte() {5, 8, 11, 2},
                                    New Byte() {5, 8, 11, 4},
                                    New Byte() {5, 9, 11, 2},
                                    New Byte() {4, 7, 11},
                                    New Byte() {4, 9, 11},
                                    New Byte() {4, 8, 11, 2},
                                    New Byte() {5, 8, 11, 3, 7},
                                    New Byte() {5, 8, 11, 3, 9},
                                    New Byte() {6, 8, 10},
                                    New Byte() {6, 8, 11},
                                    New Byte() {6, 8, 12},
                                    New Byte() {6, 8, 11, 3},
                                    New Byte() {6, 8, 12, 3}}

    End Sub
    Shared Sub New()
        MakeNotes()
        MakeAccords()
    End Sub
End Class
Public Class EDMidiOut
#Region "Midi"
    'Note = (int)Round((log(Freq)-log(440.0))/log(2.0)*12+69,0);
    'http://www.midi.org/about-midi/table1.shtml
    Public Enum OctaveNames
        [Treble] = 0
        [Treble_p8] = 1
        [Treble_m8] = 2
        [Soprano] = 3
        [Mezzosoprano] = 4
        [Alto] = 5
        [Tenor] = 6
        [Bass_m8] = 7
        [Bass] = 8
    End Enum
    'Channel 8 : batterie (percu)
    Private Const MIDI_CHANNELS_MAX As Byte = 16
    Public Enum PATCH
        AcousticGrandPiano = 0
        BrightAcousticPiano = 1
        ElectricGrandPiano = 2
        HonkytonkPiano = 3
        RhodesPiano = 4
        ChorusPiano = 5
        Harpsichord = 6
        Clavinet = 7
        Celesta = 8
        Glockenspiel = 9
        MusicBox = 10
        Vibraphone = 11
        Marimba = 12
        Xylophone = 13
        TubularBells = 14
        Dulcimer = 15
        HammondOrgan = 16
        Percuss_Organ = 17
        RockOrgan = 18
        ChurchOrgan = 19
        ReedOrgan = 20
        Accordion = 21
        Harmonica = 22
        TangoAccordion = 23
        AcousticGuitar_nylon = 24
        AcousticGuitar_steel = 25
        ElectricGuitar_jazz = 26
        ElectricGuitar_clean = 27
        ElectricGuitar_muted = 28
        OverdrivenGuitar = 29
        DistortionGuitar = 30
        GuitarHarmonics = 31
        AcousticBass = 32
        ElectricBass_finger = 33
        ElectricBass_pick = 34
        FretlessBass = 35
        SlapBass1 = 36
        SlapBass2 = 37
        SynthBass1 = 38
        SynthBass2 = 39
        Violin = 40
        Viola = 41
        Cello = 42
        ContraBass = 43
        TremoloStrings = 44
        PizzicatoStrings = 45
        OrchestralHarp = 46
        Timpani = 47
        StringEnsemble1 = 48
        StringEnsemble2 = 49
        SynthStrings1 = 50
        SynthStrings2 = 51
        ChoirAahs = 52
        VoiceOohs = 53
        SynthVoice = 54
        OrchestraHit = 55
        Trumpet = 56
        Trombone = 57
        Tuba = 58
        MutedTrumpet = 59
        FrenchHorn = 60
        BrassSection = 61
        SynthBrass1 = 62
        SynthBrass2 = 63
        SopranoSax = 64
        AltoSax = 65
        TenorSax = 66
        BaritoneSax = 67
        Oboe = 68
        EnglishHorn = 69
        Bassoon = 70
        Clarinet = 71
        Piccolo = 72
        Flute = 73
        Recorder = 74
        PanFlute = 75
        BottleBlow = 76
        Shaku = 77
        Whistle = 78
        Ocarina = 79
        Lead1_square = 80
        Lead2_sawtooth = 81
        Lead3_calliopelead = 82
        Lead4_chifflead = 83
        Lead5_charang = 84
        Lead6_voice = 85
        Lead7_fifths = 86
        Lead8_bass_lead = 87
        Pad1_newage = 88
        Pad2_warm = 89
        Pad3_polysynth = 90
        Pad4_choir = 91
        Pad5_bowed = 92
        Pad6_metallic = 93
        Pad7_halo = 94
        Pad8_sweep = 95
        FX1_rain = 96
        FX2_soundtrack = 97
        FX3_crystal = 98
        FX4_atmosphere = 99
        FX5_bright = 100
        FX6_goblins = 101
        FX7_echoes = 102
        FX8_scifi = 103
        Sitar = 104
        Banjo = 105
        Shamisen = 106
        Koto = 107
        Kalimba = 108
        Bagpipe = 109
        Fiddle = 110
        Shanai = 111
        TinkleBell = 112
        Agogo = 113
        SteelDrums = 114
        Woodblock = 115
        TaikoDrum = 116
        MelodicTom = 117
        SynthDrum = 118
        ReverseCymbal = 119
        GuitarFretNoise = 120
        BreathNoise = 121
        Seashore = 122
        BirdTweet = 123
        TelephoneRing = 124
        Helicopter = 125
        Applause = 126
        Gunshot = 127
    End Enum
    '
    ' Standard MIDI status messages
    '
    Private Enum MidiStatusMsg
        NOTE_OFF = &H80
        NOTE_ON = &H90
        POLY_KEY_PRESS = &HA0
        CONTROLLER_CHANGE = &HB0
        PROGRAM_CHANGE = &HC0
        CHANNEL_PRESSURE = &HD0
        PITCH_BEND = &HE0
        SYSEX = &HF0
        MTC_QFRAME = &HF1
        MTC_SNGPTR = &HF2
        MTC_SNGSEL = &HF3
        MTC_TUNE = &HF6
        EOX = &HF7
        MIDI_CLOCK = &HF8
        MIDI_START = &HFA
        MIDI_CONTINUE = &HFB
        MIDI_STOP = &HFC
        ACTIVE_SENSE = &HFE
        SYSTEM_RESET = &HFF
    End Enum
    '
    ' Standard CONTROLLER_CHANGE, MIDI Controller Numbers Constants
    '
    Private Enum MidiCtrlChg
        MOD_WHEEL = 1
        BREATH_CONTROLLER = 2
        FOOT_CONTROLLER = 4
        PORTAMENTO_TIME = 5
        MAIN_VOLUME = 7
        BALANCE = 8
        PAN = 10
        EXPRESS_CONTROLLER = 11
        DAMPER_PEDAL = 64 ' also known as, sustain : >= &h40 = ON, < &h40 = OFF
        PORTAMENTO = 65
        SOSTENUTO = 66
        SOFT_PEDAL = 67
        HOLD_2 = 69
        EXTERNAL_FX_DEPTH = 91
        TREMELO_DEPTH = 92
        CHORUS_DEPTH = 93
        DETUNE_DEPTH = 94
        PHASER_DEPTH = 95
        DATA_INCREMENT = 96
        DATA_DECREMENT = 97
        ALL_SOUNDS_OFF = &H78
        ALL_NOTES_OFF = &H7B
    End Enum
    Private Declare Function midiOutClose Lib "winmm.dll" (ByVal hMidiOut As IntPtr) As Integer
    Private Declare Function midiOutOpen Lib "winmm.dll" (ByRef lphMidiOut As IntPtr, ByVal uDeviceID As Integer, ByVal dwCallback As Integer, ByVal dwInstance As Integer, ByVal dwFlags As Integer) As Integer
    Private Declare Function midiOutShortMsg Lib "winmm.dll" (ByVal hMidiOut As IntPtr, ByVal dwMsg As Integer) As Integer
    Private mhMidi As IntPtr

    Private Declare Function midiOutGetNumDevs Lib "winmm" () As Short
    Private Declare Function midiOutGetDevCaps Lib "winmm.dll" Alias "midiOutGetDevCapsA" (ByVal uDeviceID As Integer, ByRef lpCaps As MIDIOUTCAPS, ByVal uSize As Integer) As Integer
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi, Pack:=1)>
    Public Structure MIDIOUTCAPS
        Dim wMid As Short
        Dim wPid As Short
        Dim vDriverVersion As Integer
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=32)> Public szPname As String
        Dim wTechnology As Short
        Dim wVoices As Short
        Dim wNotes As Short
        Dim wChannelMask As Short
        Dim dwSupport As Integer
    End Structure

    Public Function GetDevices() As String()
        Dim Caps As MIDIOUTCAPS = New MIDIOUTCAPS
        Dim i As Integer
        Dim lNumDev As Short
        Dim lDevices() As String
        lNumDev = midiOutGetNumDevs - 1
        ReDim lDevices(lNumDev)
        For i = 0 To lNumDev
            If midiOutGetDevCaps(i, Caps, Len(Caps)) = 0 Then
                lDevices(i) = Caps.szPname
            End If
        Next
        Return lDevices
    End Function

#End Region

    Public DeviceID As Integer = -1

    Private mChannel As Byte
    Private mPatch As PATCH
    Private mBalance As Byte
    Private mVolume As Byte
    Private mPitch As Integer
    Public Sub New(Optional ByVal pDeviceID As Integer = -1)
        Dim lErr As Integer
        GetDevices()
        DeviceID = pDeviceID
        lErr = midiOutOpen(mhMidi, DeviceID, 0, 0, 0)
        If lErr <> 0 Then
            mhMidi = IntPtr.Zero
            Err.Raise(-1, "EDMidiOut.OutOpen Dev " & DeviceID, "Error #" & lErr)
        End If
        mChannel = 13
        mBalance = &H3F
        mVolume = &H7F
        mPitch = &HEFEF / 2
    End Sub

    Protected Overrides Sub Finalize()
        Dispose()
        MyBase.Finalize()
    End Sub
    Public Sub Dispose()
        If Not mhMidi.Equals(IntPtr.Zero) Then
            midiOutClose(mhMidi)
            mhMidi = IntPtr.Zero
        End If
    End Sub
    Public Property Channel() As Byte
        Get
            Return mChannel
        End Get
        Set(ByVal Value As Byte)
            If Value = 0 OrElse Value > MIDI_CHANNELS_MAX Then
                Err.Raise(-1, "EDMidiOut", Value & " is incorrect. Channel must be between 1 and 16.")
            End If
            mChannel = Value
        End Set
    End Property
    Public Sub Play(ByVal pNote As Integer, ByVal pOctave As Byte, ByVal pVol As Byte, Optional ByVal pChannel As Byte = &HFF)
        Dim lMsg As Integer
        Dim lNote As Integer
        If pChannel = &HFF Then pChannel = mChannel
        lNote = pNote + pOctave * 12
        lMsg = pVol * &H10000
        lMsg += lNote * &H100
        lMsg += MidiStatusMsg.NOTE_ON + pChannel - 1
        midiOutShortMsg(mhMidi, lMsg)
    End Sub

    Public Sub StopPlaying(Optional ByVal pChannel As Byte = &HFF, Optional ByVal pbAllChannels As Boolean = False)
        Dim lMsg As Integer
        Dim lIndex As Integer
        If pbAllChannels Then
            For lIndex = 1 To 16
                lMsg = MidiCtrlChg.ALL_SOUNDS_OFF * &H100 + MidiStatusMsg.CONTROLLER_CHANGE + lIndex - 1
                lMsg = MidiCtrlChg.ALL_NOTES_OFF * &H100 + MidiStatusMsg.CONTROLLER_CHANGE + lIndex - 1
                midiOutShortMsg(mhMidi, lMsg)
            Next
        Else
            If pChannel = &HFF Then pChannel = mChannel
            lMsg = MidiCtrlChg.ALL_SOUNDS_OFF * &H100 + MidiStatusMsg.CONTROLLER_CHANGE + pChannel - 1
            lMsg = MidiCtrlChg.ALL_NOTES_OFF * &H100 + MidiStatusMsg.CONTROLLER_CHANGE + pChannel - 1
            midiOutShortMsg(mhMidi, lMsg)
        End If

    End Sub

    'Instrument
    Public Property Instrument(Optional ByVal pChannel As Byte = &HFF) As PATCH
        Get
            Return mPatch
        End Get
        Set(ByVal Value As PATCH)
            Dim lMsg As Integer
            If pChannel = &HFF Then pChannel = mChannel
            mPatch = Value
            lMsg = Value * &H100
            lMsg += MidiStatusMsg.PROGRAM_CHANGE + pChannel - 1
            lMsg = midiOutShortMsg(mhMidi, lMsg)
        End Set
    End Property
    'Balance 0 à 127 (3F=milieu)
    Public Property Balance(Optional ByVal pChannel As Byte = &HFF) As Byte
        Get
            Return mBalance
        End Get
        Set(ByVal Value As Byte)
            Dim lMsg As Integer
            If pChannel = &HFF Then pChannel = mChannel
            mBalance = Value
            lMsg = Value * &H10000
            lMsg += MidiCtrlChg.PAN * &H100
            lMsg += MidiStatusMsg.CONTROLLER_CHANGE + pChannel - 1
            midiOutShortMsg(mhMidi, lMsg)
        End Set
    End Property
    'Volume 0 à 127 
    Public Property Volume(Optional ByVal pChannel As Byte = &HFF) As Byte
        Get
            Return mVolume
        End Get
        Set(ByVal Value As Byte)
            Dim lMsg As Integer
            If pChannel = &HFF Then pChannel = mChannel
            mVolume = Value
            lMsg = Value * &H10000
            lMsg += MidiCtrlChg.MAIN_VOLUME * &H100
            lMsg += MidiStatusMsg.CONTROLLER_CHANGE + pChannel - 1
            midiOutShortMsg(mhMidi, lMsg)
        End Set
    End Property
    'Pitch 0 à 127       (décalage de hauteur de note)
    Public Property Pitch(Optional ByVal pChannel As Byte = &HFF) As Integer
        Get
            Return mPitch
        End Get
        Set(ByVal Value As Integer)
            Dim lMsg As Integer
            If pChannel = &HFF Then pChannel = mChannel
            mPitch = Value And &H7F7F
            lMsg = mPitch * &H100
            lMsg += MidiStatusMsg.PITCH_BEND + pChannel - 1
            midiOutShortMsg(mhMidi, lMsg)
        End Set
    End Property


End Class
