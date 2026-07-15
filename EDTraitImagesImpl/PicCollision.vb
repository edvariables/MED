Imports System.Drawing.Drawing2D
Imports System.Drawing.Graphics
Imports System.Drawing
Imports System.Windows.Forms
Imports EDSoundStream

Public Class PicCollision
    Implements IDisposable
    Private mBMPDest As Bitmap
    Private mBMPSrce As Bitmap
    Private mWidth As Integer = 32
    Private mHeight As Integer = 32
    Private mReleaseCollision As Byte
    Private mReleaseCollisionFixe As Byte
    Public msStats As String
    Public SpeedMax As Short = 300
    Public Color As Color
    Private mMatrixRot As Matrix
    Private mRotIncr As Single
    Private mRotFactIncr As Integer = 70
    Private mRotFactIncrHalf As Integer = mRotFactIncrHalf / 2
    Private mRotation As Single
    Public Masse As Single = 1
    Public mRectSrceInDest As RectangleF 'Zone à prendre en compte par les autres
    Public mWaveF As WaveFilePlay
    Public mbWithSound As Boolean = True
    Public mLatence As Byte = 15
    Public IsFixe As Boolean = False

    Public Sub Dispose() Implements IDisposable.Dispose
        If mWaveF IsNot Nothing Then
            mWaveF.StopPlay()
            mWaveF = Nothing
        End If
    End Sub

    Public Property Width() As Integer
        Get
            Return mWidth
        End Get
        Set(ByVal Value As Integer)
            If mVisible Then CreateRegion()
            mWidth = Value
        End Set
    End Property
    Public Property Height() As Integer
        Get
            Return mHeight
        End Get
        Set(ByVal Value As Integer)
            If mVisible Then CreateRegion()
            mHeight = Value
        End Set
    End Property
    Public Vx As Single
    Public Vy As Single
    Private mX As Single
    Public Property X() As Integer
        Get
            Return mX
        End Get
        Set(ByVal Value As Integer)
            If mVisible Then CreateRegion()
            mX = Value
        End Set
    End Property
    Private mY As Single
    Public Property Y() As Integer
        Get
            Return mY
        End Get
        Set(ByVal Value As Integer)
            If mVisible Then CreateRegion()
            mY = Value
        End Set
    End Property

    Private mVisible As Boolean
    Public Property Visible() As Boolean
        Get
            Return mVisible
        End Get
        Set(ByVal Value As Boolean)
            mVisible = Value
            If mVisible Then CreateRegion()
        End Set
    End Property
    Private mRegion As Region
    Private mRegionSrce As Region
    Private mRegionPrev As Region
    Public ReadOnly Property RegionPrev() As Region
        Get
            Return mRegionPrev
        End Get
    End Property
    Public Property Region() As Region
        Get
            Return mRegion
        End Get
        Set(ByVal Value As Region)
            Dim lGr As Graphics = Graphics.FromImage(mBMPDest)
            mRegionPrev = mRegion
            mRegion = Value
            Dim lRectPrev As RectangleF = mRegionPrev.GetBounds(lGr)
            Dim lRectCur As RectangleF = mRegion.GetBounds(lGr)
            lGr.Dispose()
            mX = lRectCur.Left
            mY = lRectCur.Top
            Vx = lRectCur.Left - lRectPrev.Left
            Vy = lRectCur.Top - lRectPrev.Top
        End Set
    End Property
    ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Public WriteOnly Property BmpDest() As Bitmap
        Set(ByVal Value As Bitmap)
            mBMPDest = Value
            If Not mBMPDest Is Nothing And mRectSrceInDest.IsEmpty Then
                mRectSrceInDest = New RectangleF(0, 0, mBMPDest.Width, mBMPDest.Height)
            End If
            Color = Choose(Math.Round(Rnd() * 11), Color.Violet,
                                Color.Yellow,
                                Color.RoyalBlue,
                                Color.SandyBrown,
                                Color.Teal,
                                Color.SaddleBrown,
                                Color.Green,
                                Color.HotPink,
                                Color.SkyBlue,
                                Color.PaleVioletRed,
                                Color.Gold)
            CreateRegion()
        End Set
    End Property

    Public Function CheckCollision(ByVal pGr As Graphics, ByVal PicOther As PicCollision) As Boolean
        If Not Visible Then Exit Function
        Dim lbValide As Boolean
        Dim lsStats As String
        Dim lx, ly As Short
        On Error GoTo Erreur
        If (Not PicOther.IsFixe AndAlso mReleaseCollision = 0) _
        OrElse (PicOther.IsFixe AndAlso mReleaseCollisionFixe = 0) Then
            'Console.WriteLine("Check collision")
            'Check collision
            Dim lRegion As Region = mRegion.Clone
            lRegion.Intersect(PicOther.mRectSrceInDest)
            If Not lRegion.IsEmpty(pGr) Then
                lRegion.Intersect(PicOther.Region)
                lsStats = "1st"
                'Console.WriteLine(lsStats)
                If lRegion.IsEmpty(pGr) Then
                    'lRegion = mRegion.Clone
                    'lRegion.Translate(-PicOther.Vx, -PicOther.Vy)
                    'lRegion.Intersect(PicOther.Region)
                    'lsStats = "2nd"
                    'If lRegion.IsEmpty(pGr) Then
                    'lRegion = mRegion.Clone
                    'lRegion.Translate(Vx, Vy)
                    'lRegion.Intersect(PicOther.Region)
                    'lsStats = "3rd"
                    'If lRegion.IsEmpty(pGr) Then
                    lbValide = False
                    'Else
                    '    lbValide = True
                    '    'lRegion.Translate(-Vx, -Vy)
                    'End If
                    'Else
                    '    lbValide = True
                    '    'lRegion.Translate(PicOther.Vx, PicOther.Vy)
                    'End If
                Else
                    lbValide = True
                End If
                If lbValide Then  'Collision
                    'Console.WriteLine("Collision valide")
                    CheckCollision = True
                    lbValide = False
                    msStats = lsStats
                    Dim lnTest As Byte
                    'Tente de calculer la vitesse à l'endroit de la collision
                    Dim lRecCol As RectangleF = lRegion.GetBounds(pGr)
                    Dim lNewPos As PointF = New PointF(lRecCol.Left + lRecCol.Width / 2, lRecCol.Top + lRecCol.Height / 2)

                    If PicOther.RegionPrev IsNot Nothing Then
                        lx = Math.Abs(PicOther.Vx)
                        ly = Math.Abs(PicOther.Vy)
                        If lx < SpeedMax Then lx = SpeedMax
                        If ly < SpeedMax Then ly = SpeedMax
                        For lnTest = 0 To 4
                            'Console.WriteLine("PicOther.RegionPrev " & lnTest)
                            lRecCol.Width += 2 * lx
                            lRecCol.Height += 2 * ly
                            lRecCol.X -= lx
                            lRecCol.Y -= ly
                            lRegion = PicOther.RegionPrev.Clone
                            lRegion.Intersect(lRecCol)
                            If Not lRegion.IsEmpty(pGr) Then
                                lbValide = True
                                Exit For
                            End If
                        Next
                    End If
                    If lbValide Then
                        mReleaseCollision = mLatence

                        'On prend la vitesse de cette seule partie en collision
                        Dim lRecColOldPos As RectangleF = lRegion.GetBounds(pGr)
                        Vx = lNewPos.X - (lRecColOldPos.Left + lRecColOldPos.Width / 2)
                        Vy = lNewPos.Y - (lRecColOldPos.Top + lRecColOldPos.Height / 2)
                        msStats &= " test " & lnTest
                        msStats &= " X=" & PicOther.Vx & "->" & Vx
                        msStats &= " Y=" & PicOther.Vy & "->" & Vy
                    ElseIf PicOther.IsFixe OrElse (PicOther.Vx = 0 And PicOther.Vy = 0) Then
                        'Console.WriteLine("PicOther.IsFixe ")
                        mReleaseCollisionFixe = PicOther.mLatence
                        lbValide = True
                        lRecCol.Width += 10
                        lRecCol.Height += 10
                        lRecCol.X -= 5
                        lRecCol.Y -= 5
                        lRegion = PicOther.Region.Clone
                        lRegion.Intersect(lRecCol)
                        If Not lRegion.IsEmpty(pGr) Then
                            lRecCol = lRegion.GetBounds(pGr)
                            If lRecCol.Width > lRecCol.Height Then
                                Vy *= -1
                                If Vy > 0 Then 'Redescend
                                    If mY < lRecCol.Bottom Then mY = lRecCol.Bottom + 1
                                Else
                                    If mY + mHeight > lRecCol.Top Then mY = lRecCol.Top - mHeight
                                End If
                                If mY <= 1 Then
                                    mY = lRecCol.Bottom + 1
                                ElseIf mY >= mBMPDest.Height - mHeight - 1 Then
                                    mY = lRecCol.Top - mHeight
                                End If
                            Else
                                Vx *= -1
                                If Vx > 0 Then 'Repart à droite
                                    If mX < lRecCol.Right Then mX = lRecCol.Right + 1
                                Else
                                    If mX + mWidth > lRecCol.Left Then mX = lRecCol.Left - mWidth
                                End If
                                If mX <= 1 Then
                                    mX = lRecCol.Right + 1
                                ElseIf mX >= mBMPDest.Width - mWidth - 1 Then
                                    mX = lRecCol.Left - mWidth
                                End If
                            End If
                        Else
                            Vx *= -1
                            Vy *= -1
                        End If

                    Else
                        mReleaseCollision = mLatence
                        'Rebond classique
                        Vx *= -1.5
                        Vy *= -1.5
                        mX += Vx / 2
                        mY += Vy / 2
                        msStats &= " v rebond"
                    End If

                    'Console.WriteLine("Vitesse limite")

                    'Vitesse limite
                    If Math.Abs(Vx) > SpeedMax Then
                        Vy *= SpeedMax / Math.Abs(Vx)
                        If Vx > 0 Then Vx = SpeedMax Else Vx = -SpeedMax
                        msStats &= " Max"
                    ElseIf Math.Abs(Vy) > SpeedMax Then
                        Vx *= SpeedMax / Math.Abs(Vy)
                        If Vy > 0 Then Vy = SpeedMax Else Vy = -SpeedMax
                        msStats &= " Max"
                    End If
                    mRotIncr = Rnd() * mRotFactIncr - mRotFactIncrHalf
                    lbValide = True
                Else   'Pas de collision
                    'msStats = ""
                    Vx *= 0.98
                    Vy = (Vy + Masse / 20) * 0.98
                    mRotIncr *= 0.98
                End If
            End If
        End If
        If mReleaseCollisionFixe > 0 Then mReleaseCollisionFixe -= 1
        If mReleaseCollision > 0 Then mReleaseCollision -= 1
        If Not lbValide Then
            'Pas de collision
            If mX + Vx <= 0 Then
                If Vx = 0 Then
                    Vx = 0.1
                Else
                    Vx *= -1
                End If
                mX = 0
            ElseIf mX + Vx + mWidth > mBMPDest.Width Then
                If Vx = 0 Then
                    Vx = -0.1
                Else
                    Vx *= -1
                End If
                mX = mBMPDest.Width - mWidth
            End If
            If mY + Vy <= 0 Then
                If Vy = 0 Then
                    Vy = 0.1
                Else
                    Vy *= -1
                End If
                mY = 0
            ElseIf mY + Vy + mHeight > mBMPDest.Height Then
                If Vy = 0 Then
                    Vy = -0.1
                Else
                    Vy *= -1
                End If
                mY = mBMPDest.Height - mHeight
            End If
        Else
            If PicOther.IsFixe Then
                PicOther.PlaySound()
            Else
                PlaySound()
            End If
        End If

        mX += Vx
        mY += Vy

        mRegion = mRegionSrce.Clone
        mRegion.Translate(mX, mY)

        GoTo Fin
Erreur:
        If False Then
            Resume
        End If
Fin:
    End Function
    Public Sub Draw(ByVal pGr As Graphics)
        If Not mMatrixRot Is Nothing Then
            mMatrixRot.Reset()
            mRotation += mRotIncr : mRotation = mRotation Mod 360
            mMatrixRot.RotateAt(mRotation, New PointF(mX + mWidth / 2, mY + mHeight / 2))
            pGr.Transform = mMatrixRot
            pGr.DrawImage(mBMPSrce, mX, mY)
            pGr.Transform = New Matrix
        Else
            pGr.DrawImage(mBMPSrce, mX, mY)
        End If
    End Sub

    Public Sub CreateRegion(Optional ByVal psFichier As String = "",
                            Optional ByVal pbRotation As Boolean = True,
                            Optional ByVal pbg_color As Integer = 1)
        Dim lColorBackG As Color
        Dim lsFichier As String
        Dim lColFichiers As New Collection
        Dim lsDir As String = Application.StartupPath & "\"
        If psFichier = "" Then
            lsFichier = Dir(lsDir & "*.bmp")
        Else
            lsFichier = psFichier
        End If
        If lsFichier = "" Then
            lsDir = lsDir & "..\"
            lsFichier = Dir(lsDir & "*.bmp")
        End If
        If lsFichier <> "" Then
            If psFichier = "" Then
                Do While lsFichier <> ""
                    lColFichiers.Add(lsDir & lsFichier)
                    lsFichier = Dir()
                Loop
                Do
                    lsFichier = lColFichiers(Math.Floor(Rnd() * lColFichiers.Count) + 1)
                    mBMPSrce = New Bitmap(lsFichier, False)
                Loop Until mBMPSrce.Width < 300
            Else
                mBMPSrce = New Bitmap(psFichier, False)
            End If
            If pbg_color = 1 Then
                lColorBackG = mBMPSrce.GetPixel(1, 1)
            Else
                lColorBackG = Color.FromArgb(pbg_color)
            End If
            mRegionSrce = GetRegion(mBMPSrce, lColorBackG)
            If psFichier = "" Then
                'mHeight = mBMPSrce.Height * mWidth / mBMPSrce.Width
                mWidth = mBMPSrce.Width * mHeight / mBMPSrce.Height
                mRegionSrce.Transform(New Matrix(mWidth / mBMPSrce.Width, 0, 0, mHeight / mBMPSrce.Height, 0, 0))
            Else
                mHeight = mBMPSrce.Height
                mWidth = mBMPSrce.Width
            End If
            mBMPSrce.MakeTransparent(lColorBackG)
            mBMPSrce = New Bitmap(mBMPSrce, mWidth, mHeight)
        Else
            Dim lPath As GraphicsPath = New GraphicsPath
            lPath.AddEllipse(0, 0, mWidth, mHeight)
            mRegionSrce = New Region(lPath)
            Dim lBrush As PathGradientBrush = New PathGradientBrush(lPath)
            lBrush.CenterPoint = New PointF(mWidth / 3, mHeight / 3)
            lBrush.CenterColor = Color.White
            lBrush.SurroundColors = New Color() {Color}
            mBMPSrce = New Bitmap(mWidth, mHeight)
            With Graphics.FromImage(mBMPSrce)
                .FillRegion(lBrush, mRegionSrce)
                .Dispose()
            End With
        End If

        mRegion = mRegionSrce.Clone
        mRegion.Translate(mX, mY)

        If pbRotation Then
            mMatrixRot = New Matrix
            mRotIncr = Rnd() * 40 - 20
        Else
            mMatrixRot = Nothing
        End If
        InitSound()
    End Sub
    Private Function GetRegion(ByVal bm As Bitmap, ByVal bg_color As Color) As Region
        Dim new_region As New Region
        new_region.MakeEmpty()

        Dim rect As New Rectangle
        Dim in_image As Boolean = False
        Dim X As Integer

        For Y As Integer = 0 To bm.Height - 1
            X = 0
            Do While (X < bm.Width)
                If Not in_image Then
                    If Not bm.GetPixel(X, Y).Equals(bg_color) _
                        Then
                        in_image = True
                        rect.X = X
                        rect.Y = Y
                        rect.Height = 1
                    End If
                ElseIf bm.GetPixel(X, Y).Equals(bg_color) Then
                    in_image = False
                    rect.Width = (X - rect.X)
                    new_region.Union(rect)
                End If
                X = (X + 1)
            Loop

            ' Add the final piece if necessary.
            If in_image Then
                in_image = False
                rect.Width = (bm.Width - rect.X)
                new_region.Union(rect)
            End If
        Next Y

        Return new_region
    End Function
    Public Sub PlaySound()
        If Not mbWithSound OrElse mWaveF Is Nothing Then Exit Sub
        mWaveF.StopPlay()
        mWaveF.Play()
    End Sub
    Public Sub InitSound(Optional ByVal psFichier As String = "")
        If Not mbWithSound Then Return
        Dim lsFichier As String
        Dim lColFichiers As New Collection
        Dim lsDir As String = Application.StartupPath & "\"
        If psFichier <> "" Then
            If Dir(psFichier) = "" Then psFichier = ""
        End If
        If psFichier <> "" Then
            lsFichier = Dir(psFichier)
        Else
            lsFichier = Dir(lsDir & "alea_*.wav")
        End If

        If lsFichier = "" Then
            lsDir = lsDir & "..\"
            lsFichier = Dir(lsDir & "*.wav")
        End If
        If lsFichier <> "" Then
            Do While lsFichier <> ""
                lColFichiers.Add(lsDir & lsFichier)
                lsFichier = Dir()
            Loop
            lsFichier = lColFichiers(Math.Floor(Rnd() * lColFichiers.Count) + 1)
            mWaveF = New WaveFilePlay(lsFichier)
        Else
            mWaveF = Nothing
        End If
    End Sub
End Class