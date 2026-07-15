
    Public Class EDFrqPlay
#Region "Notes"
        Public Structure Note
            Public Frequence As Single
            Public FullName As String
            Public Name As String
            Public Index As Integer '0->127
            Public Note As Byte     '1->12
            Public Octave As Short  '-1->9
        End Structure
        Public Notes(127) As Note
        Public mNotesNames As Collection

        Private Sub MakeNotes()
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
            mNotesNames = New Collection
            For lIndex = 0 To 127
                With Notes(lIndex)
                    .Index = lIndex
                    .Note = lIndex Mod 12
                    .Octave = lIndex \ 12 - 1
                    .Frequence = m_NoteFreq(.Note, .Octave + 1)
                    .Name = lNames(.Note)
                    .FullName = .Name & " " & .Octave
                    mNotesNames.Add(.Index, .FullName)
                End With
            Next

        End Sub
        Public Function GetNoteIndex(ByVal pNoteName As String) As Integer
            Return mNotesNames(pNoteName)
        End Function
#End Region

        Const PI2 As Single = Math.PI * 2
        Private mDuree As Long
        Private mCount As Long = 0
        Public Structure Channel
            Public Frequence As Single
            Public Volume As Single 'entre 0 et 1
            Public Enabled As Boolean
            Public Wawoua As Single
            Public LeftRight() As Boolean

            Friend Frq As Single
            Friend FrqMod As Single
            Friend WawouaTaux() As Single
            Friend WawouaCumul As Single
        End Structure
        Public Channels() As Channel 'Pas au sens Gauche/Droite
        Public Saturation As Boolean 'Sature ou moyenne les signaux
        Public Property NbreChannels() As Byte
            Get
                Return Channels.GetUpperBound(0) + 1
            End Get
            Set(ByVal Value As Byte)
                If Value > 0 Then
                    ReDim Channels(Value - 1)
                    For Value = 0 To Value - 1
                        With Channels(Value)
                            ReDim .WawouaTaux(1)
                            ReDim .LeftRight(1)
                            .LeftRight(0) = True
                            .LeftRight(1) = True
                            .Volume = 1
                            .Enabled = True
                        End With
                    Next
                End If
            End Set
        End Property
        Private mBalance As Single = 0
        Private mBalanceVol() As Single = {1, 1}
        Public Property Balance() As Single                 'entre -100 et 100
            Get
                Return mBalance
            End Get
            Set(ByVal Value As Single)
                mBalance = Value
                If Value > 0 Then
                    mBalanceVol(0) = (1 - Value / 100) * mVolume
                Else
                    mBalanceVol(0) = mVolume
                End If
                If Value < 0 Then
                    mBalanceVol(1) = (1 + Value / 100) * mVolume
                Else
                    mBalanceVol(1) = mVolume
                End If
            End Set
        End Property

        Private mVolume As Single = 1
        Public Property Volume() As Single                 'entre 0 et 100
            Get
                Return mVolume * 100
            End Get
            Set(ByVal Value As Single)
                mVolume = Value / 100
                Balance = mBalance
            End Set
        End Property

        Public WithEvents mEDSndOut As EDSoundOut

        Public Event Data(ByVal pNumBuff As Short, ByVal pData() As Byte, ByVal pLength As Integer)

        Private mPhase As Integer

        Public Sub New(Optional ByVal pEDSndOut As EDSoundOut = Nothing)
            NbreChannels = 1
            If pEDSndOut Is Nothing Then
                mEDSndOut = New EDSoundOut
            Else
                mEDSndOut = pEDSndOut
            End If
            MakeNotes()
        End Sub
        Public Sub StartPlay()
            mEDSndOut.StartPlay()
            mDuree = 0
            mCount = 0
            Randomize()
        End Sub
        Public Sub StopPlay()
            mEDSndOut.StopPlay()
        End Sub

        Private Sub mEDSndOut_Data(ByVal pNumBuff As Short, ByVal pData() As Byte, ByVal pLength As Integer) Handles mEDSndOut.Data
            Dim lPhase As Single
            Dim lPhaseMemo As Single
            Dim lHauteur As Single
            Dim lGaucheDroite As Integer
            Dim lnByte As Integer
            Dim lRound As Integer = 1
            Dim lNbreChannels As Single
            Dim lnFrq As Integer
            Dim lnByte2 As Integer
            Dim lDureeDeb As Long = Now.Ticks
            Dim lNbBytesHalf As Integer = pLength \ 2
            Dim lbSaturation As Boolean = Saturation

            For lnFrq = 0 To Channels.GetUpperBound(0)
                With Channels(lnFrq)
                    If .Frequence > 0 AndAlso .Enabled Then
                        .FrqMod = mEDSndOut.BytesPerSec / .Frequence
                        .Frq = PI2 / .FrqMod
                        lHauteur = lRound * .Wawoua / lNbBytesHalf * 3.0! / System.Math.Log(.Frequence)
                        .WawouaTaux(0) = lHauteur * (Rnd() - 0.5!)
                        .WawouaTaux(1) = lHauteur * (Rnd() - 0.5!)
                    End If
                End With
            Next
            For lGaucheDroite = 0 To 1
                lPhaseMemo = mPhase
                For lnByte = lGaucheDroite To pLength - lRound + lGaucheDroite Step lRound * 2
                    lHauteur = 0.0!
                    lNbreChannels = 0.0!
                    For lnFrq = 0 To Channels.GetUpperBound(0)
                        With Channels(lnFrq)
                            If .Enabled AndAlso .Frequence > 0.0! AndAlso .LeftRight(lGaucheDroite) Then
                                lPhase = (lPhaseMemo Mod .FrqMod) * .Frq
                                If .Wawoua > 0 Then
                                    lPhase = lPhase * (1.0! + .WawouaCumul)
                                    If lnByte < lNbBytesHalf Then
                                        .WawouaCumul += .WawouaTaux(lGaucheDroite)
                                    Else
                                        .WawouaCumul -= .WawouaTaux(lGaucheDroite)
                                    End If
                                End If
                                lHauteur += System.Math.Sin(lPhase) * .Volume
                                lNbreChannels += .Volume
                            End If
                        End With
                    Next
                    If lNbreChannels > 0.0! Then
                        If Not lbSaturation Then lHauteur /= lNbreChannels
                        lHauteur *= mVolume * mBalanceVol(lGaucheDroite)
                        If lHauteur > 1.0! Then
                            lHauteur = 1.0!
                        ElseIf lHauteur < -1.0! Then
                            lHauteur = -1.0!
                        End If
                    End If
                    If Single.IsNaN(lHauteur) Then
                        lNbreChannels += 1.0!
                    End If
                    For lnByte2 = 0 To lRound - 1
                        pData(lnByte + lnByte2) = CByte(127.0! + lHauteur * 127.0!)
                    Next
                    lPhaseMemo += CSng(lRound)
                Next
            Next
            mPhase = lPhaseMemo Mod CSng(pLength * 100)

            mDuree += Now.Ticks - lDureeDeb
            mCount += 1&

            RaiseEvent Data(pNumBuff, pData, pLength)
        End Sub

        Public Function GetStats() As String
            If mCount > 0 Then
                Return Format(mDuree / mCount / TimeSpan.TicksPerMillisecond, "0.00")
            Else
                Return ""
            End If
        End Function
    End Class

