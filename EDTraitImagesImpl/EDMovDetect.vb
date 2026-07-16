Imports System.Runtime.InteropServices
Imports System.Drawing.Graphics
Imports System.Drawing.Imaging
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.ComponentModel

Public Class EDMovDetect
    Private Const Angle90 As Double = Math.PI / 2
    Private Const Angle360 As Double = Math.PI * 2

#Region "Properties"

    Public mBMPLock As BitmapBytesRGB24
    Private msStats As String
    Private mXMax As Integer
    Private mYMax As Integer
    Private mMatrix0 As Matrix = New Matrix
    Private mImPrev() As Byte
    Private mLumPrev() As Integer
    Private mLumPrev2() As Integer

    Private mLimites As Collection
    'Duplicate limit values because really faster (x3)
    Private mL(1) As Integer
    Private mR(1) As Byte
    Private mG(1) As Byte
    Private mB(1) As Byte
    Private mSizeMin As Integer
    Private mMvt As Integer
    Private mIdxLimites As Integer = 1
    Private mMatrixRes As Matrix
    Private mResX As Integer = 1
    Private mResY As Integer = 1
    Private mPts(,) As Boolean
    Private mBords(,) As Integer
    Private mZonesMove As ZonesMove
    Private mNbreZonesMove As Integer '0 pour aucune
    Private mLimContours As Integer
    Public mContours As GraphicsPath
    Private mMinXForY() As Integer
    Private mMinYForX() As Integer

    Public Property ResolutionX() As Byte
        Get
            Return mResX
        End Get
        Set(ByVal Value As Byte)
            mResX = CInt(Value)
            mMatrixRes = New Matrix(mResX, 0, 0, mResY, 0, 0)
        End Set
    End Property
    Public Property ResolutionY() As Byte
        Get
            Return mResY
        End Get
        Set(ByVal Value As Byte)
            mResY = CInt(Value)
            mMatrixRes = New Matrix(mResX, 0, 0, mResY, 0, 0)
        End Set
    End Property

    Public Property IdxLimites() As Integer
        Get
            Return mIdxLimites
        End Get
        Set(ByVal Value As Integer)
            With Limites(Value)
                mL(0) = .LMin
                mL(1) = .LMax
                mR(0) = .RMin
                mR(1) = .RMax
                mG(0) = .GMin
                mG(1) = .GMax
                mB(0) = .BMin
                mB(1) = .BMax
                mSizeMin = .SizeMin
                mMvt = .Mvt
            End With
        End Set
    End Property

    Public ReadOnly Property Limites(Optional ByVal pIdxLimites As Integer = 0) As EDMovDetectLimites
        Get
            If pIdxLimites > 0 Then mIdxLimites = pIdxLimites
            Return mLimites(mIdxLimites)
        End Get
    End Property
    Public Property LMin() As Integer
        Get
            Return Limites.LMin
        End Get
        Set(ByVal Value As Integer)
            mL(0) = Value
            Limites.LMin = Value
        End Set
    End Property
    Public Property LMax() As Integer
        Get
            Return Limites.LMax
        End Get
        Set(ByVal Value As Integer)
            mL(1) = Value
            Limites.LMax = Value
        End Set
    End Property
    Public Property RMin() As Byte
        Get
            Return Limites.RMin
        End Get
        Set(ByVal Value As Byte)
            mR(0) = Value
            Limites.RMin = Value
        End Set
    End Property
    Public Property RMax() As Byte
        Get
            Return Limites.RMax
        End Get
        Set(ByVal Value As Byte)
            mR(1) = Value
            Limites.RMax = Value
        End Set
    End Property
    Public Property GMin() As Byte
        Get
            Return Limites.GMin
        End Get
        Set(ByVal Value As Byte)
            mG(0) = Value
            Limites.GMin = Value
        End Set
    End Property
    Public Property GMax() As Byte
        Get
            Return Limites.GMax
        End Get
        Set(ByVal Value As Byte)
            mG(1) = Value
            Limites.GMax = Value
        End Set
    End Property
    Public Property BMin() As Byte
        Get
            Return Limites.BMin
        End Get
        Set(ByVal Value As Byte)
            mB(0) = Value
            Limites.BMin = Value
        End Set
    End Property
    Public Property BMax() As Byte
        Get
            Return Limites.BMax
        End Get
        Set(ByVal Value As Byte)
            mB(1) = Value
            Limites.BMax = Value
        End Set
    End Property
    Public Property SizeMin() As Integer
        Get
            Return Limites.SizeMin
        End Get
        Set(ByVal Value As Integer)
            mSizeMin = Value
            Limites.SizeMin = Value
        End Set
    End Property
    Public Property Mvt() As Integer
        Get
            Return Limites.Mvt
        End Get
        Set(ByVal Value As Integer)
            mMvt = Value
            Limites.Mvt = Value
        End Set
    End Property
    Public Property NbreZonesMove() As Integer
        Get
            Return mNbreZonesMove
        End Get
        Set(ByVal Value As Integer)
            mNbreZonesMove = Value
        End Set
    End Property
    Public ReadOnly Property GetZonesMove() As ZonesMove
        Get
            Return mZonesMove
        End Get
    End Property
    Public Property LimContours() As Integer
        Get
            Return mLimContours
        End Get
        Set(ByVal Value As Integer)
            mLimContours = Value
        End Set
    End Property
#End Region

    '/////////////////////////////
    Public Sub New()
        mLimites = New Collection
        'Mouvement limits
        mLimites.Add(New EDMovDetectLimites)
        IdxLimites = mLimites.Count
        Limites.SetPreselLight(&H20)
        Limites.SizeMin = 5

        mLimites.Add(New EDMovDetectLimites)
        IdxLimites = mLimites.Count
    End Sub
    Public Sub Release()
        If Not mBMPLock Is Nothing Then
            mBMPLock = Nothing
            ReDim mImPrev(0)
            ReDim mLumPrev(0)
            ReDim mLumPrev2(0)
        End If
    End Sub
    '/////////////////////////////
    Private mRegionDetect As Region
    Private mOldRegion As Region

    <Browsable(False)>
    Public ReadOnly Property RegionDetect() As Region
        Get
            Return mRegionDetect
        End Get
    End Property

    'pBMP type is 24RGB
    Public Sub SetNewImage(ByVal pBMP As Bitmap)
        SetNewImage(pBMP, False, mbBackground)
    End Sub
    Private mbSecond As Boolean
    Public mbBackground As Boolean
    Public Sub SetBackGround(ByVal pBMP As Bitmap)
        mbBackground = Not pBMP Is Nothing
        If Not mbBackground Then
            mBMPLock = Nothing
            Return
        End If

        mBMPLock = New BitmapBytesRGB24(pBMP)
        mXMax = pBMP.Width \ mResX - 1
        mYMax = pBMP.Height \ mResY - 1
        ReDim mPts(mXMax + 2, mYMax + 2)
        ReDim mBords(mXMax + 2, mYMax + 2)

        'Copy data
        mBMPLock.LockBitmap(ImageLockMode.ReadOnly, True)
        mImPrev = mBMPLock.ImageBytes.Clone
        ReDim mLumPrev(mBMPLock.ImageBytes.GetLength(0))
        Dim lnPix As Integer
        For lnPix = 0 To mImPrev.GetUpperBound(0) - 1 Step 3
            mLumPrev(lnPix) = Get_MinPlusMax(mImPrev(lnPix), mImPrev(lnPix + 1), mImPrev(lnPix + 2))
        Next
        mbSecond = True
        mRegionDetect = New Region
        mBMPLock.UnlockBitmap()

        ReDim mMinXForY(mYMax)
        ReDim mMinYForX(mXMax)
        Dim i As Integer
        For i = 0 To mYMax
            mMinXForY(i) = Integer.MaxValue
        Next
        For i = 0 To mXMax
            mMinYForX(i) = Integer.MaxValue
        Next
    End Sub
    Public Sub SetNewImage(ByVal pBMP As Bitmap, ByVal pbFirst As Boolean,
                           Optional ByVal pbLast As Boolean = False)
        Dim lbFirst As Boolean = pbFirst
        If mBMPLock Is Nothing Then
            lbFirst = True
            mBMPLock = New BitmapBytesRGB24(pBMP)
            mXMax = CInt(pBMP.Width \ mResX) - 1
            mYMax = CInt(pBMP.Height \ mResY) - 1
            ReDim mPts(mXMax + 2, mYMax + 2)
            ReDim mBords(mXMax + 2, mYMax + 2)
            ReDim mMinXForY(mYMax)
            ReDim mMinYForX(mXMax)
            Dim i As Integer
            For i = 0 To mYMax
                mMinXForY(i) = Integer.MaxValue
            Next
            For i = 0 To mXMax
                mMinYForX(i) = Integer.MaxValue
            Next
        Else
            mBMPLock.Bitmap = pBMP
        End If
        'Copy data
        mBMPLock.LockBitmap(ImageLockMode.ReadOnly, True)

        If lbFirst Then
            mImPrev = mBMPLock.ImageBytes.Clone
            ReDim mLumPrev(mBMPLock.ImageBytes.GetLength(0))
            ReDim mLumPrev2(mBMPLock.ImageBytes.GetLength(0))
            Dim lnPix, X, Y As Integer
            For Y = 0 To mYMax - 1
                For X = 0 To mXMax
                    mLumPrev(lnPix) = Get_MinPlusMax(mBMPLock.ImageBytes(lnPix), mBMPLock.ImageBytes(lnPix + 1), mBMPLock.ImageBytes(lnPix + 2))
                    mLumPrev2(lnPix) = mLumPrev(lnPix)
                    lnPix += 3
                Next
            Next
            lbFirst = False
            mbSecond = True
            mRegionDetect = New Region
            If mNbreZonesMove > 0 Then mZonesMove = New ZonesMove
        Else
            DetectOriginal(mBMPLock, Limites.Mvt, Limites.RectAnalyse, mbSecond, pbLast)
            mbSecond = mbBackground
        End If
        mBMPLock.UnlockBitmap()
    End Sub
    '''''''''''''''''''''
#Region "Essai"
    Private Sub DetectSurface(ByRef pBMPLock As BitmapBytesRGB24, ByVal pMvt As Integer,
                            ByVal pRectAnalyse As RectangleF,
                            ByVal pbSecond As Boolean,
                            ByVal pbLast As Boolean)
        Dim lGrPath As GraphicsPath = New GraphicsPath
        Dim lPts(,) As Boolean = mPts
        Dim lBords(,) As Integer = mBords
        Dim X, Y, lnPix As Integer
        Dim XMin, YMin, XMax, YMax As Integer
        Dim XDebut, YDebut, X2, Y2 As Integer
        Dim lbValide As Boolean
        Dim lNbFound, lNbNotFound As Integer
        Dim lWithRGB As Boolean = Limites.WithRGB
        Dim lWithLum As Boolean = Limites.WithLum
        Dim lWithMvt As Boolean = pMvt > 0
        Dim lLimL(1) As Integer
        Dim lLimR(1) As Byte
        Dim lLimG(1) As Byte
        Dim lLimB(1) As Byte

        Dim lXMax As Integer = mXMax
        Dim lYMax As Integer = mYMax
        Dim lYMin As Integer = 0
        Dim lXMin As Integer = 0
        Dim lResX As Byte = mResX
        Dim lResY As Byte = mResY
        Dim lSizeMin As Short = mSizeMin
        Dim lWidth As Byte
        Dim lLum(2) As Integer
        Dim lCurPx(2) As Byte
        Dim lResX3 As Short = lResX * 3
        Dim lComplntLignePx As Integer = (lResY - 1) * (mXMax + 1) * lResX3

        Dim lbInSurf As Boolean
        Dim lCountBords As Integer
        Dim lPtDebut As Point = New Point(0, 0)
        Dim lPtsSurf As New ArrayList
        Dim lPtsSurfTmp As Array
        Dim lbClosed As Boolean
        Dim lbVoisinFounded As Boolean

        Dim lLumPrev() As Integer = mLumPrev
        Dim lLumPrev2() As Integer = mLumPrev2

        If Not pRectAnalyse.IsEmpty Then
            pRectAnalyse = New RectangleF(pRectAnalyse.X \ lResX, pRectAnalyse.Y \ lResY, pRectAnalyse.Width \ lResX, pRectAnalyse.Height \ lResY)
            lXMin = pRectAnalyse.Left
            lXMax = pRectAnalyse.Right - 1
            lYMin = pRectAnalyse.Top
            lYMax = pRectAnalyse.Bottom - 1
            lnPix = lResX3 * (mXMax + 1) * lYMin + lResX3 * lXMin
            lComplntLignePx = (lResY - 1) * (mXMax + 1) * lResX3 _
                            + (mXMax + 1 - pRectAnalyse.Width) * lResX3
            lnPix = lXMin * lResX3 _
                    + lYMin * lResY * (mXMax + 1) * lResX3
        Else
            X = 0
        End If

        If lWithLum Then lLimL = mL
        If lWithRGB Then
            lLimR = mR
            lLimG = mG
            lLimB = mB
        End If

        On Error GoTo Erreur
        For Y = lYMin To lYMax '- 1
            For X = lXMin To lXMax
                lCurPx(0) = pBMPLock.ImageBytes(lnPix)
                lCurPx(1) = pBMPLock.ImageBytes(lnPix + 1)
                lCurPx(2) = pBMPLock.ImageBytes(lnPix + 2)
                'Test sur une couleur
                If lWithRGB Then
                    If lCurPx(0) >= lLimB(0) _
                    AndAlso lCurPx(0) <= lLimB(1) _
                    AndAlso lCurPx(1) >= lLimG(0) _
                    AndAlso lCurPx(1) <= lLimG(1) _
                    AndAlso lCurPx(2) >= lLimR(0) _
                    AndAlso lCurPx(2) <= lLimR(1) Then
                        lbValide = True
                    Else
                        lbValide = False
                    End If
                Else
                    lbValide = True
                End If
                'Test de luminosité
                If lWithLum AndAlso lbValide Then
                    lLum(0) = Get_MinPlusMax(lCurPx(0), lCurPx(1), lCurPx(2))
                    If lLum(0) > lLimL(1) OrElse lLum(0) < lLimL(0) Then
                        lbValide = False
                    End If
                End If
                'Test de mouvement
                If lWithMvt AndAlso lbValide Then
                    'lLum(0) = Math.Abs(CShort(lCurPx(0)) - CShort(mImPrev(lnPix)))
                    'lLum(1) = Math.Abs(CShort(lCurPx(1)) - CShort(mImPrev(lnPix + 1)))
                    'lLum(2) = Math.Abs(CShort(lCurPx(2)) - CShort(mImPrev(lnPix + 2)))
                    'lbValide = Get_MinPlusMax(lLum(0), lLum(1), lLum(2)) >= pMvt
                    If Not lWithLum Then
                        lLum(0) = Get_MinPlusMax(lCurPx(0), lCurPx(1), lCurPx(2))
                    End If
                    lLum(1) = lLumPrev(lnPix)
                    lbValide = Math.Abs(lLum(0) - lLum(1)) >= pMvt
                    If lbValide AndAlso Not pbSecond Then
                        'Diff from previous previous (background)
                        'lLum(0) = Math.Abs(CShort(lCurPx(0)) - CShort(mImPrev2(lnPix)))
                        'lLum(1) = Math.Abs(CShort(lCurPx(1)) - CShort(mImPrev2(lnPix + 1)))
                        'lLum(2) = Math.Abs(CShort(lCurPx(2)) - CShort(mImPrev2(lnPix + 2)))
                        'lbValide = Get_MinPlusMax(lLum(0), lLum(1), lLum(2)) >= pMvt
                        lLum(2) = lLumPrev2(lnPix)  'Get_MinPlusMax(mImPrev2(lnPix + 0), mImPrev2(lnPix + 1), mImPrev2(lnPix + 2))
                        lbValide = Math.Abs(lLum(0) - lLum(2)) >= pMvt
                    End If
                End If
                'Gestion des points et bordures
                If lbValide Then
                    If Not pbLast Then
                        lLumPrev2(lnPix) = lLumPrev(lnPix)
                        lLumPrev(lnPix) = lLum(0)
                    End If
                    lNbFound += 1
                    'Maxi
                    If Y > YMax Then YMax = Y
                    If X > XMax Then XMax = X
                    'Si je n'étais pas dans une surface
                    If Not lbInSurf Then
                        'je le deviens
                        lBords(X, Y) = -1
                        lCountBords += 1
                        lbInSurf = True
                    ElseIf Y = lYMin _
                    OrElse Y = lYMax _
                    OrElse X = lXMin _
                    OrElse X = lXMax Then
                        '1ère ligne
                        '1ère colonne
                        'Dernière colonne
                        lBords(X, Y) = -1
                        lCountBords += 1
                    Else
                        'Si le pt d'au dessus est en dedans, on a pas de bordure
                        If mPts(X, Y - 1) Then
                            lBords(X, Y) = 0
                        Else
                            'Sinon bordure horizontale haute
                            lBords(X, Y) = -1
                            lCountBords += 1
                        End If
                    End If
                Else
                    'Si j'étais dans une surface
                    If lbInSurf Then
                        'On sort de la surface au pt précédent
                        lBords(X - 1, Y) = -1
                        lCountBords += 1
                        lbInSurf = False
                    End If
                    If Y <> lYMin Then
                        'Si le pt d'au dessus est en dedans, on a une bordure au dessus
                        If mPts(X, Y - 1) Then
                            lBords(X, Y - 1) = -1
                            lCountBords += 1
                        End If
                    End If
                    lBords(X, Y) = 0
                End If
                lPts(X, Y) = lbValide

                lnPix += lResX3
            Next X

            lbInSurf = False

            lnPix += lComplntLignePx 'Saute des lignes
        Next
        'Dernière ligne, bordure bas
        'Y = lYMax '- 1
        'For X = lXMin To lXMax
        '    If mPts(X, Y) Then
        '        mBords(X, Y) = -1
        '    End If
        'Next
        If lWithMvt And Not pbLast Then
            Array.Copy(pBMPLock.ImageBytes, mImPrev, pBMPLock.ImageBytes.Length)
        End If

        msStats = lNbFound.ToString & " found, canceled=" & lNbNotFound.ToString & ", Bords=" & lCountBords.ToString

        'Génère le GraphicPath à partir du tableau
        If False AndAlso lCountBords > 0 Then
            'Contours seuls
            lCountBords = 0
            For YDebut = YMax To lYMin Step -1
                For XDebut = XMax To lXMin Step -1
                    If lBords(XDebut, YDebut) = -1 Then
                        lGrPath.AddRectangle(New Rectangle(XDebut, YDebut, 1, 1))
                    End If
                Next
            Next
            mRegionDetect = New Region(lGrPath)
        Else
            mRegionDetect = New Region
            mRegionDetect.MakeEmpty()

            If lCountBords > 0 Then
                lCountBords = 0
                For YDebut = lYMin To lYMax
                    For XDebut = lXMin To lXMax
                        If lBords(XDebut, YDebut) = -1 Then
                            'Point de départ  
                            lPtsSurf.Clear()
                            lPtsSurf.Add(New Point(XDebut, YDebut))
                            lPtDebut.X = XDebut
                            lPtDebut.Y = YDebut
                            lNbFound = 1
                            lBords(XDebut, YDebut) = lNbFound
                            lbVoisinFounded = False
                            lbClosed = False
                            'Parcours la bordure
                            Do
                                'Pour les 8 voisins : on cherche un bord jamais parcouru
                                For Y = lPtDebut.Y - 1 To lPtDebut.Y + 1
                                    For X = lPtDebut.X - 1 To lPtDebut.X + 1
                                        If X >= 0 AndAlso Y >= 0 _
                                        AndAlso X <= lXMax AndAlso Y <= lYMax _
                                        AndAlso Not (Y = lPtDebut.Y And X = lPtDebut.X) Then
                                            If lBords(X, Y) = -1 Then 'nouvelle bordure = nouveau début
                                                lPtsSurf.Add(New Point(X, Y))
                                                lPtDebut.X = X
                                                lPtDebut.Y = Y
                                                lNbFound += 1
                                                lBords(X, Y) = lNbFound  'contient l'index dans la liste de points
                                                lbVoisinFounded = True
                                                Exit For
                                            End If
                                        End If
                                    Next
                                    If lbVoisinFounded Then Exit For
                                Next
                                'Si le 1er est seul, on passe à autre chose
                                If lNbFound = 1 Then Exit Do

                                If Not lbVoisinFounded Then
                                    'Pour les 8 voisins : on cherche la connexion au début
                                    For Y = lPtDebut.Y - 1 To lPtDebut.Y + 1
                                        For X = lPtDebut.X - 1 To lPtDebut.X + 1
                                            If X >= 0 AndAlso Y >= 0 _
                                            AndAlso X <= lXMax AndAlso Y <= lYMax _
                                            AndAlso Not (Y = lPtDebut.Y And X = lPtDebut.X) Then
                                                If lBords(X, Y) = 1 Then
                                                    lPtsSurf.Add(New Point(X, Y))
                                                    'Tout premier point
                                                    If lNbFound > 3 Then
                                                        lPtsSurfTmp = Array.CreateInstance(GetType(Point), lNbFound + 1)
                                                        lPtsSurf.CopyTo(lPtsSurfTmp)
                                                        lGrPath.Reset()
                                                        lGrPath.AddPolygon(CType(lPtsSurfTmp, Point()))
                                                        'Détermine si le contours est à remplir ou non
                                                        'Cherche un point à côté des bords
                                                        For Y2 = Y - 1 To Y + 1
                                                            For X2 = X - 1 To X + 1
                                                                If X2 >= 0 AndAlso Y2 >= 0 _
                                                                AndAlso X2 <= lXMax AndAlso Y2 <= lYMax _
                                                                AndAlso lPts(X2, Y2) = 0 Then
                                                                    If lGrPath.IsVisible(X2, Y2) Then
                                                                        mRegionDetect.Exclude(lGrPath)
                                                                    Else
                                                                        mRegionDetect.Union(lGrPath)
                                                                    End If
                                                                    lbVoisinFounded = True
                                                                    lCountBords += 1
                                                                    Exit For
                                                                End If
                                                            Next
                                                            If lbVoisinFounded Then Exit For
                                                        Next
                                                    End If
                                                    For Each lPtDebut In lPtsSurf
                                                        lBords(lPtDebut.X, lPtDebut.Y) = 0
                                                    Next
                                                    lbVoisinFounded = True
                                                    lbClosed = True
                                                    Exit For
                                                End If
                                            End If
                                        Next
                                        If lbVoisinFounded Then Exit For
                                    Next
                                End If

                                If Not lbVoisinFounded Then
                                    'Pour les 8 voisins : on cherche un bord déjà parcouru mais pas le début
                                    For Y = lPtDebut.Y - 1 To lPtDebut.Y + 1
                                        For X = lPtDebut.X - 1 To lPtDebut.X + 1
                                            If X >= 0 AndAlso Y >= 0 _
                                            AndAlso X <= lXMax AndAlso Y <= lYMax _
                                            AndAlso Not (Y = lPtDebut.Y And X = lPtDebut.X) Then
                                                If lBords(X, Y) > 1 Then  'fin de boucle intermédiaire
                                                    lPtsSurf.Add(New Point(X, Y))
                                                    lNbFound += 1
                                                    'La fin de la liste est une figure
                                                    lPtsSurfTmp = Array.CreateInstance(GetType(Point), lNbFound - (lBords(X, Y)) + 1)
                                                    lPtsSurf.CopyTo(lBords(X, Y) - 1, lPtsSurfTmp, 0, lNbFound - (lBords(X, Y)) + 1)
                                                    For Each lPtDebut In lPtsSurfTmp
                                                        If Not (lPtDebut.X = X And lPtDebut.Y = Y) Then
                                                            lBords(lPtDebut.X, lPtDebut.Y) = 0
                                                        End If
                                                    Next
                                                    If lPtsSurfTmp.Length > 3 Then
                                                        lGrPath.Reset()
                                                        lGrPath.AddPolygon(CType(lPtsSurfTmp, Point()))
                                                        'Détermine si le contours est à remplir ou non
                                                        'Cherche un point à côté des bords
                                                        For Y2 = Y - 1 To Y + 1
                                                            For X2 = X - 1 To X + 1
                                                                If X2 >= 0 AndAlso Y2 >= 0 _
                                                                AndAlso X2 <= lXMax AndAlso Y2 <= lYMax _
                                                                AndAlso lPts(X2, Y2) = 0 Then
                                                                    If lGrPath.IsVisible(X2, Y2) Then
                                                                        mRegionDetect.Exclude(lGrPath)
                                                                    Else
                                                                        mRegionDetect.Union(lGrPath)
                                                                    End If
                                                                    lbVoisinFounded = True
                                                                    lCountBords += 1
                                                                    Exit For
                                                                End If
                                                            Next
                                                            If lbVoisinFounded Then Exit For
                                                        Next
                                                    End If
                                                    'on garde l'intersection
                                                    lPtsSurf.RemoveRange(lBords(X, Y), lNbFound - (lBords(X, Y)))
                                                    lNbFound = (lBords(X, Y))
                                                    lPtDebut.X = X
                                                    lPtDebut.Y = Y

                                                    lbVoisinFounded = True
                                                End If
                                            End If
                                        Next
                                        If lbVoisinFounded Then Exit For
                                    Next
                                End If

                                If lbClosed Then
                                    Exit Do
                                Else
                                    lbVoisinFounded = False
                                End If
                            Loop
                        End If
                    Next
                Next
            End If
        End If

        msStats = msStats & ", Figures=" & lCountBords & ", Scans=" & mRegionDetect.GetRegionScans(mMatrix0).Length

        If lResX > 1 And lResY > 1 Then
            mRegionDetect.Transform(mMatrixRes)
        End If

        GoTo Fin
Erreur:
        If False Then
            Resume
        End If
Fin:
    End Sub
#End Region

    '''''''''''''''''''''
    Private Sub DetectOriginal(ByRef pBMPLock As BitmapBytesRGB24, ByVal pMvt As Integer,
                            ByVal pRectAnalyse As RectangleF,
                            ByVal pbSecond As Boolean,
                            ByVal pbLast As Boolean)
        Dim lGrPath As GraphicsPath = New GraphicsPath
        Dim lPts(,) As Boolean = mPts
        Dim lRect As Rectangle = New Rectangle(0, 0, 0, 1)
        Dim X, Y, XMax, YMax As Integer
        Dim lnPix As Integer
        Dim XLeftTop, XLeftBottom, YTopLeft, YTopRight As Integer
        Dim XRightTop, XRightBottom, YBottomRight As Integer
        Dim lbValide As Boolean
        Dim lbPrevValide As Boolean
        Dim lXValide As Integer
        Dim lNbFound, lNbFoundMemo, lNbNotFound As Integer
        Dim lWithRGB As Boolean = Limites.WithRGB
        Dim lWithLum As Boolean = Limites.WithLum
        Dim lWithMvt As Boolean = pMvt > 0
        Dim lLimL(1) As Integer
        Dim lLimR(1) As Byte
        Dim lLimG(1) As Byte
        Dim lLimB(1) As Byte

        Dim lXMax As Integer = mXMax
        Dim lYMin As Integer = 0
        Dim lYMax As Integer = mYMax
        Dim lXMin As Integer = 0
        Dim lResX As Integer = mResX
        Dim lResY As Integer = mResY
        Dim lResYm1 As Integer = lResY - 1
        Dim lSizeMin As Integer = mSizeMin
        Dim lWidth As Integer
        Dim lLum(2) As Integer
        Dim lCurPx(2) As Byte
        Dim lResX3 As Integer = lResX * 3
        Dim lComplntLignePx As Integer = (lResY - 1) * (mXMax + 1) * lResX3

        Dim lMinXForY() As Integer
        Dim lMaxXForY() As Integer
        Dim lMinYForX() As Integer
        Dim lMaxYForX() As Integer
        lMinXForY = mMinXForY.Clone
        lMinYForX = mMinYForX.Clone
        ReDim lMaxXForY(lYMax)
        ReDim lMaxYForX(lXMax)

        Dim lLumPrev() As Integer = mLumPrev
        Dim lLumPrev2() As Integer = mLumPrev2

        If Not pRectAnalyse.IsEmpty Then
            pRectAnalyse = New RectangleF(pRectAnalyse.X \ lResX, pRectAnalyse.Y \ lResY, pRectAnalyse.Width \ lResX, pRectAnalyse.Height \ lResY)
            lXMin = pRectAnalyse.Left
            lXMax = pRectAnalyse.Right - 1
            lYMin = pRectAnalyse.Top
            lYMax = pRectAnalyse.Bottom - 1
            lnPix = lResX3 * (mXMax + 1) * lYMin + lResX3 * lXMin
            lComplntLignePx = (lResY - 1) * (mXMax + 1) * lResX3 _
                            + (mXMax + 1 - pRectAnalyse.Width) * lResX3
            lnPix = lXMin * lResX3 _
                    + lYMin * lResY * (mXMax + 1) * lResX3
        Else
            X = 0
        End If

        If lWithLum Then lLimL = mL
        If lWithRGB Then
            lLimR = mR
            lLimG = mG
            lLimB = mB
        End If

        On Error GoTo Erreur
        For Y = lYMin To lYMax - 1S
            For X = lXMin To lXMax
                lCurPx(0) = pBMPLock.ImageBytes(lnPix)
                lCurPx(1) = pBMPLock.ImageBytes(lnPix + 1)
                lCurPx(2) = pBMPLock.ImageBytes(lnPix + 2)
                'Test sur une couleur
                If lWithRGB Then
                    If lCurPx(0) >= lLimB(0) _
                    AndAlso lCurPx(0) <= lLimB(1) _
                    AndAlso lCurPx(1) >= lLimG(0) _
                    AndAlso lCurPx(1) <= lLimG(1) _
                    AndAlso lCurPx(2) >= lLimR(0) _
                    AndAlso lCurPx(2) <= lLimR(1) Then
                        lbValide = True
                    Else
                        lbValide = False
                    End If
                Else
                    lbValide = True
                End If
                'Test de luminosité
                If lWithLum AndAlso lbValide Then
                    lLum(0) = Get_MinPlusMax(lCurPx(0), lCurPx(1), lCurPx(2))
                    If lLum(0) > lLimL(1) OrElse lLum(0) < lLimL(0) Then
                        lbValide = False
                    End If
                End If
                'Test de mouvement
                If lWithMvt AndAlso lbValide Then
                    'lLum(0) = Math.Abs(CInteger(lCurPx(0)) - CInteger(mImPrev(lnPix)))
                    'lLum(1) = Math.Abs(CInteger(lCurPx(1)) - CInteger(mImPrev(lnPix + 1)))
                    'lLum(2) = Math.Abs(CInteger(lCurPx(2)) - CInteger(mImPrev(lnPix + 2)))
                    'lbValide = Get_MinPlusMax(lLum(0), lLum(1), lLum(2)) >= pMvt
                    If Not lWithLum Then
                        lLum(0) = Get_MinPlusMax(lCurPx(0), lCurPx(1), lCurPx(2))
                    End If
                    lLum(1) = lLumPrev(lnPix)
                    lbValide = Math.Abs(lLum(0) - lLum(1)) >= pMvt
                    If lbValide AndAlso Not pbSecond Then
                        'Diff from previous previous (background)
                        'lLum(0) = Math.Abs(CInteger(lCurPx(0)) - CInteger(mImPrev2(lnPix)))
                        'lLum(1) = Math.Abs(CInteger(lCurPx(1)) - CInteger(mImPrev2(lnPix + 1)))
                        'lLum(2) = Math.Abs(CInteger(lCurPx(2)) - CInteger(mImPrev2(lnPix + 2)))
                        'lbValide = Get_MinPlusMax(lLum(0), lLum(1), lLum(2)) >= pMvt
                        lLum(2) = lLumPrev2(lnPix)  'Get_MinPlusMax(mImPrev2(lnPix + 0), mImPrev2(lnPix + 1), mImPrev2(lnPix + 2))
                        lbValide = Math.Abs(lLum(0) - lLum(2)) >= pMvt
                    End If
                End If
                If lbValide Then
                    If Not pbLast Then
                        lLumPrev2(lnPix) = lLumPrev(lnPix)
                        lLumPrev(lnPix) = lLum(0)
                    End If
                    If Not lbPrevValide Then
                        lXValide = X
                        lbPrevValide = True
                    End If
                Else
                    If lbPrevValide Then 'Changed
                        'Ajout                
                        lWidth = X - lXValide
                        If lWidth > lSizeMin Then
                            lNbFound += CInt(lWidth)
                            XMax = X
                            YMax = Y
                        ElseIf Y > 0 Then
                            'Le pt au dessus est valide, on est en mvt sur la hauteur
                            If Y > lResYm1 AndAlso lPts(lXValide, Y - lResY) Then
                                XMax = X
                                YMax = Y
                                lNbFound += CInt(lWidth)
                            Else
                                lNbNotFound += CInt(X - lXValide)
                                For lXValide = lXValide To X - 1
                                    lPts(lXValide, Y) = False
                                Next
                            End If
                        Else
                            lNbNotFound += X - lXValide
                            For lXValide = lXValide To X - 1
                                lPts(lXValide, Y) = False
                            Next
                        End If
                        lbPrevValide = False
                    End If
                End If
                lPts(X, Y) = lbValide

                lnPix += lResX3
            Next X

            If lbPrevValide Then 'End of row
                'Ajout    
                X -= 1
                lWidth = X - lXValide
                If lWidth > lSizeMin Then
                    lNbFound += lWidth
                    XMax = X
                    YMax = Y
                Else
                    lNbNotFound += X - lXValide
                    For lXValide = lXValide To X - 1
                        lPts(lXValide, Y) = False
                    Next
                End If
                lbPrevValide = False
            End If

            lnPix += lComplntLignePx 'Saute des lignes
        Next
        msStats = lNbFound.ToString & " found, canceled=" & lNbNotFound.ToString & ", XMax=" & XMax

        lRect.Width = 1
        lRect.Height = 1
        'Génère le GraphicPath à partir du tableau
        'A partir d'un point cherche le plus grand rectangle plein en allant vers le haut et la gauche
        If lNbFound > 0 Then
            lNbFoundMemo = lNbFound
            If lYMin < 0 Then lYMin = 1
            For YBottomRight = YMax To lYMin Step -1
                For XRightBottom = XMax To lXMin Step -1
                    'Point de départ  : en bas à droite
                    If lPts(XRightBottom, YBottomRight) Then
                        'Cherche à gauche en bas
                        For XLeftBottom = XRightBottom - 1 To 1 Step -1
                            If Not lPts(XLeftBottom, YBottomRight) Then
                                XLeftBottom += 1
                                Exit For
                            End If
                        Next
                        If XLeftBottom < 0 Then XLeftBottom = 0
                        'Cherche en haut à droite
                        For YTopRight = YBottomRight - 1 To 1 Step -1
                            If Not lPts(XRightBottom, YTopRight) Then
                                'Autorise à sauter un pixel
                                If Not lPts(XRightBottom, YTopRight - 1) Then
                                    YTopRight += 1
                                    Exit For
                                Else
                                    YTopRight -= 1  'Pixel précédent déjà testé et Ok
                                End If
                            End If
                        Next
                        If YTopRight < 0 Then YTopRight = 0
                        'Cherche en haut à gauche
                        For YTopLeft = YBottomRight - 1 To YTopRight + 1 Step -1
                            If Not lPts(XLeftBottom, YTopLeft) Then
                                'Autorise à sauter un pixel
                                If Not lPts(XLeftBottom, YTopLeft - 1) Then
                                    YTopLeft += 1
                                    Exit For
                                Else
                                    YTopLeft -= 1  'Pixel précédent déjà testé et Ok
                                End If
                            End If
                        Next
                        'Y haut le plus bas
                        If YTopRight > YTopLeft Then
                            lRect.Y = YTopRight
                        Else
                            lRect.Y = YTopLeft
                        End If
                        If lRect.Y < 0 Then lRect.Y = 0
                        'Cherche à gauche en haut pour vérifier la 1ère recherche
                        For XLeftTop = XRightBottom - 1 To XLeftBottom + 1 Step -1
                            If Not lPts(XLeftTop, lRect.Y) Then
                                XLeftTop += 1
                                Exit For
                            End If
                        Next
                        If XLeftTop > XLeftBottom Then
                            lRect.X = XLeftTop
                        Else
                            lRect.X = XLeftBottom
                        End If
                        lRect.Width = XRightBottom - lRect.X + 1
                        lRect.Height = YBottomRight - lRect.Y + 1

                        For Y = lRect.Y To YBottomRight
                            If lMinXForY(Y) > lRect.X Then lMinXForY(Y) = lRect.X
                            If lMaxXForY(Y) < XRightBottom Then lMaxXForY(Y) = XRightBottom
                        Next
                        For X = lRect.X To XRightBottom
                            If lMinYForX(X) > lRect.Y Then lMinYForX(X) = lRect.Y
                            If lMaxYForX(X) < YBottomRight Then lMaxYForX(X) = YBottomRight
                        Next

                        lGrPath.AddRectangle(lRect)
                        For X = lRect.X To XRightBottom
                            For Y = lRect.Y To YBottomRight
                                If lPts(X, Y) Then
                                    lPts(X, Y) = False
                                    lNbFound -= 1
                                    If lNbFound <= 0 Then GoTo GrPathEnded
                                End If
                            Next
                        Next
                        lNbNotFound = 0
                    End If
                Next
                XMax = lXMax 'Après la ligne la plus basse, on parcours toute la ligne (XMax valait le max de la dernière ligne)
            Next
        End If
GrPathEnded:
        If mLimContours > 0 Then
            DefPathContours(lMinXForY, lMaxXForY, lMinYForX, lMaxYForX, lXMin, lXMax, lNbFoundMemo)
            If lResX > 1 AndAlso lResY > 1 AndAlso Not mContours Is Nothing Then
                mContours.Transform(mMatrixRes)
            End If
        End If
        If Not mRegionDetect Is Nothing Then mOldRegion = mRegionDetect.Clone

        'msStats = msStats & ", Scans=" & mRegionDetect.GetRegionScans(mMatrix0).Length

        mRegionDetect = New Region(lGrPath)
        'Rétablit la résolution
        If lResX > 1 AndAlso lResY > 1 Then
            mRegionDetect.Transform(mMatrixRes)
        End If

        'Découpe en zones de mouvement
        If mNbreZonesMove > 0 Then DefZonesMove()

        If Not pbLast Then
            'Array.Copy(pBMPLock.ImageBytes, mImPrev, pBMPLock.ImageBytes.Length)
            System.Runtime.InteropServices.Marshal.Copy(pBMPLock.mPtrScan0, mImPrev, 0, pBMPLock.mTotal_size)
        End If

        GoTo Fin
Erreur:
        If False Then
            Resume
        End If
Fin:
    End Sub
    Private Sub DefPathContours(ByRef pMinXForY() As Integer, ByRef pMaxXForY() As Integer,
                                ByRef pMinYForX() As Integer, ByRef pMaxYForX() As Integer,
                                ByVal pXMin As Integer, ByVal pXMax As Integer,
                                ByVal pNbPts As Integer)
        mContours = Nothing
        If pXMax <= pXMin Then Return
        Dim X, Y, YMinPrec, YMaxPrec, XPrec As Integer
        Dim lStep As Integer = 1
        Dim lStepN As Integer = -lStep
        Dim lUpperPt As Integer = pNbPts 'mXMax * mYMax / (lStep ^ 2) '(pXMax - pXMin) * 2 - 1
        Dim lPtsMin() As Point = Array.CreateInstance(GetType(Point), lUpperPt + 1)
        Dim lPtsMax() As Point = Array.CreateInstance(GetType(Point), lUpperPt + 1)
        Dim lPtsAdd() As Point
        Dim lPtsRes() As Point
        Dim lnPtMin As Integer = -1
        Dim lnPtMax As Integer = -1
        Dim lnPtAdd As Integer = -1
        Dim lCountNoContours As Short
        Dim lLimContours As Integer = Math.Ceiling(mLimContours / lStep)
        Dim lbBackground As Boolean = mbBackground

        On Error GoTo Erreur

        'Parcours en même temps le haut et le bas
        For X = pXMin To pXMax Step lStep
            Y = pMinYForX(X)
            If Y < Integer.MaxValue Then
                'Points précédents
                If lCountNoContours > lLimContours AndAlso lnPtMin >= 0 Then
                    If lnPtMin > 0 Then
                        lPtsRes = Array.CreateInstance(GetType(Point), (lnPtMin + 1) + (lnPtMax + 1))
                        Array.Copy(lPtsMin, 0, lPtsRes, 0, lnPtMin + 1)
                        Array.Copy(lPtsMax, lUpperPt - lnPtMax, lPtsRes, lnPtMin + 1, lnPtMax + 1)
                        If mContours Is Nothing Then mContours = New GraphicsPath
                        mContours.AddClosedCurve(lPtsRes)
                        lnPtMin = -1
                        lnPtMax = -1
                        pXMin = X
                    End If
                End If

                'En haut : minima
                If lnPtMin < lUpperPt AndAlso lnPtMin >= 0 AndAlso lbBackground Then
                    'On descend on regarde le côté droit, donc max
                    If Y > YMinPrec Then
                        For Y = YMinPrec + 1 To Y - 1 Step lStep
                            XPrec = pMaxXForY(Y)
                            If XPrec > 0 AndAlso XPrec < X Then 'Seulement courbe vers la gauche
                                lnPtMin += 1
                                With lPtsMin(lnPtMin)
                                    .X = XPrec
                                    .Y = Y
                                End With
                            End If
                        Next
                    Else  'On monte on regarde le côté gauche, donc min
                        For Y = YMinPrec - 1 To Y + 1 Step lStepN
                            XPrec = pMinXForY(Y)
                            If XPrec < XPrec.MaxValue AndAlso XPrec > X Then 'Seulement courbe vers la droite
                                lnPtMin += 1
                                With lPtsMin(lnPtMin)
                                    .X = XPrec
                                    .Y = Y
                                End With
                            End If
                        Next
                    End If
                End If
                If lnPtMin < lUpperPt Then
                    lnPtMin += 1
                    With lPtsMin(lnPtMin)
                        .X = X
                        YMinPrec = pMinYForX(X)
                        .Y = YMinPrec
                    End With
                End If
                'En bas : maxima
                If lnPtMax >= 0 AndAlso lbBackground Then
                    Y = pMaxYForX(X)
                    'On descend on regarde le côté gauche, donc min
                    If Y > YMaxPrec Then
                        For Y = YMaxPrec + 1 To Y - 1 Step lStep
                            XPrec = pMinXForY(Y)
                            If XPrec < XPrec.MaxValue AndAlso XPrec > X AndAlso lnPtMax < lUpperPt Then  'Seulement courbe vers la droite
                                lnPtMax += 1
                                With lPtsMax(lUpperPt - lnPtMax)
                                    .X = XPrec
                                    .Y = Y
                                End With
                            End If
                        Next
                    Else 'On monte on regarde le côté droit, donc max
                        For Y = YMaxPrec - 1 To Y + 1 Step lStepN
                            XPrec = pMaxXForY(Y)
                            If XPrec > 0 AndAlso XPrec < X AndAlso lnPtMax < lUpperPt Then  'Seulement courbe vers la gauche
                                lnPtMax += 1
                                With lPtsMax(lUpperPt - lnPtMax)
                                    .X = XPrec
                                    .Y = Y
                                End With
                            End If
                        Next
                    End If
                End If
                lnPtMax += 1
                If lnPtMax <= lUpperPt Then
                    With lPtsMax(lUpperPt - lnPtMax)
                        .X = X
                        YMaxPrec = pMaxYForX(X)
                        .Y = YMaxPrec
                    End With
                End If
                lCountNoContours = 0
            Else
                lCountNoContours += 1
            End If
        Next

        If lnPtMin > 0 AndAlso lbBackground Then
            'Du bas gauche au haut gauche si à la vertical, sauf si on a plus d'un contours
            If mContours Is Nothing Then
                'si les x correspondent aux Y de xMin
                For pXMin = pXMin To pXMax Step lStep
                    'Cherche le vrai xMin
                    Y = pMinYForX(pXMin)
                    If Y < Integer.MaxValue Then
                        X = pMinXForY(Y)            'en haut
                        YMaxPrec = pMaxYForX(pXMin)
                        XPrec = pMinXForY(YMaxPrec) 'en bas
                        If X = XPrec Then '=pXMin
                            If Y < YMaxPrec - 1 Then
                                ReDim lPtsAdd(YMaxPrec - Y)
                                'On monte on regarde le côté gauche, donc min
                                For Y = YMaxPrec - 1 To Y + 1 Step lStepN
                                    X = pMinXForY(Y)  'X de la ligne du dessus, est à droite (sinon xmin serait <)
                                    If X < X.MaxValue Then  'Seulement courbe vers la droite
                                        lnPtAdd += 1
                                        With lPtsAdd(lnPtAdd)
                                            .X = X
                                            .Y = Y
                                        End With
                                    End If
                                Next
                            End If
                        End If
                        Exit For 'ne traite que le XMin
                    End If
                Next
            End If

            'Du haut droit au bas droit si à la vertical
            'si les x correspondent aux Y de xMin
            For pXMax = pXMax To pXMin Step lStepN
                'Cherche le vrai xMax
                Y = pMinYForX(pXMax)
                If Y < Integer.MaxValue Then
                    X = pMaxXForY(Y)            'en haut
                    YMaxPrec = pMaxYForX(pXMax)
                    XPrec = pMaxXForY(YMaxPrec) 'en bas
                    If X = XPrec Then '=pXMax
                        If Y < YMaxPrec - 1 Then
                            'On descend on regarde le côté droit, donc max
                            For Y = Y + 1 To YMaxPrec - 1 Step lStep
                                X = pMaxXForY(Y)  'X de la ligne du dessous, est à gauche (sinon xmax serait >)
                                If X > 0 AndAlso lnPtMin < lUpperPt Then 'Seulement courbe vers la droite
                                    lnPtMin += 1
                                    With lPtsMin(lnPtMin)
                                        .X = X
                                        .Y = Y
                                    End With
                                End If
                            Next
                        End If
                    End If
                    Exit For
                End If
            Next
        End If

        If lnPtMin > 0 AndAlso lUpperPt >= lnPtMax Then
            lPtsRes = Array.CreateInstance(GetType(Point), (lnPtMin + 1) + (lnPtMax + 1) + (lnPtAdd + 1))
            Array.Copy(lPtsMin, 0, lPtsRes, 0, lnPtMin + 1)
            Array.Copy(lPtsMax, lUpperPt - lnPtMax, lPtsRes, lnPtMin + 1, lnPtMax + 1)
            If lnPtAdd >= 0 Then Array.Copy(lPtsAdd, 0, lPtsRes, (lnPtMin + 1) + (lnPtMax + 1), lnPtAdd + 1)
            If mContours Is Nothing Then mContours = New GraphicsPath
            mContours.AddClosedCurve(lPtsRes)
        End If

        GoTo Fin
Erreur:
        MsgBox(Err.Source & " : " & Err.Description)
        If False Then
            Resume
        End If
Fin:
    End Sub

    Private Sub DefZonesMove()
        Dim lWidth As Integer
        Dim lHeight As Integer
        Dim lOldRegion As Region = mOldRegion
        If lOldRegion Is Nothing Then GoTo Fin
        Dim lGr As Graphics = Graphics.FromImage(mBMPLock.Bitmap)
        Dim lUnionRegion As Region = lOldRegion.Clone
        Dim lTestOldRegion As Region
        Dim lTestNewRegion As Region
        Dim lMidReg(2) As RectangleF
        lUnionRegion.Union(mRegionDetect)
        If lUnionRegion.IsInfinite(lGr) OrElse lUnionRegion.IsEmpty(lGr) Then Return
        Dim lBounds As RectangleF = lUnionRegion.GetBounds(lGr)
        Dim lSqrtNbreZonesMove As Single = Math.Sqrt(mNbreZonesMove)
        Dim X, Y As Integer
        Dim lRect As RectangleF
        Dim lZoneMove As ZoneMove

        'mNbreZonesMove représente le nombre total de zone
        lWidth = lBounds.Width \ ((lBounds.Width / lBounds.Height) * lSqrtNbreZonesMove)
        If lWidth = 0 Then lWidth = 1
        lHeight = lBounds.Height \ ((lBounds.Height / lBounds.Width) * lSqrtNbreZonesMove)
        If lHeight = 0 Then lHeight = 1
        lMidReg(2).X = lBounds.X + lBounds.Width / 2
        lMidReg(2).Y = lBounds.Y + lBounds.Height / 2

        mZonesMove.Clear()

        If lWidth = 1 OrElse lHeight = 1 Then GoTo Fin

        For Y = lBounds.Y To lBounds.Bottom - 1 Step lWidth
            For X = lBounds.X To lBounds.Right - 1 Step lHeight
                'lRect = RectangleF.FromLTRB(X, Y, Math.Max(X + lWidth - 1, lBounds.Right - 1), _
                '                                 Math.Max(Y + lHeight - 1, lBounds.Bottom - 1))
                lRect = New RectangleF(X, Y, lWidth, lHeight)

                lTestOldRegion = lOldRegion.Clone
                lTestOldRegion.Intersect(lRect)
                If Not lTestOldRegion.IsEmpty(lGr) Then
                    'Le rectangle existait avant
                    lTestNewRegion = mRegionDetect.Clone
                    lTestNewRegion.Intersect(lRect)
                    If Not lTestNewRegion.IsEmpty(lGr) Then
                        'Le rectangle existe toujours
                        lZoneMove = New ZoneMove(lRect)
                        lMidReg(0) = lTestOldRegion.GetBounds(lGr)
                        lMidReg(0).Offset(lMidReg(0).Width / 2, lMidReg(0).Height / 2) 'centre de la zone
                        lMidReg(1) = lTestNewRegion.GetBounds(lGr)
                        lMidReg(1).Offset(lMidReg(1).Width / 2, lMidReg(1).Height / 2) 'centre de la zone
                        With lZoneMove
                            .vX = lMidReg(1).X - lMidReg(0).X
                            .vY = lMidReg(1).Y - lMidReg(0).Y
                            'If .vX <> 0 OrElse .vY <> 0 Then
                            'If Math.Abs(lZoneMove.vX) > 5000 OrElse Math.Abs(lZoneMove.vY) > 5000 Then
                            '    Err.Raise(-1, "EDMovDetect.DefZonesMove", "To large")
                            'Else
                            mZonesMove.Add(lZoneMove)
                            'End If
                            'End If
                        End With
                    End If
                Else
                    lTestNewRegion = mRegionDetect.Clone
                    lTestNewRegion.Intersect(lRect)
                    If Not lTestNewRegion.IsEmpty(lGr) Then
                        'Le rectangle existe maintenant
                        lZoneMove = New ZoneMove(lRect)
                    End If
                End If
            Next
        Next
        ''Pour les zones de vitesses nulles, calcule en fonction des voisins
        'For Each lZoneMove In mZonesMove
        '    If lZoneMove.vX = 0 AndAlso lZoneMove.vY = 0 Then
        '        lNbVois = 0
        '        For Y = -lHeight To lHeight Step lHeight
        '            For X = -lWidth To lWidth Step lWidth
        '                If Not (X = 0 AndAlso Y = 0) Then
        '                    lNbVois += 1
        '                    lZoneVois = mZonesMove(X & "_" & Y)
        '                    If Not lZoneVois Is Nothing Then
        '                        lZoneMove.vX += lZoneVois.vX
        '                        lZoneMove.vY += lZoneVois.vY
        '                    End If
        '                End If
        '            Next
        '        Next
        '        If lNbVois > 0 Then
        '            lZoneMove.vX /= lNbVois
        '            lZoneMove.vY /= lNbVois
        '        End If
        '    End If
        'Next
Fin:
    End Sub
    'Traite le mouvement dans une zone
    Public Function GetZoneMove(ByVal pBounds As Rectangle) As ZoneMove
        Dim lOldRegion As Region = mOldRegion
        If lOldRegion Is Nothing Then GoTo Fin
        Dim lGr As Graphics = Graphics.FromImage(mBMPLock.Bitmap)
        Dim lUnionRegion As Region = lOldRegion.Clone
        Dim lTestOldRegion As Region
        Dim lTestNewRegion As Region
        Dim lMidReg(2) As RectangleF
        lUnionRegion.Intersect(pBounds)
        lUnionRegion.Union(mRegionDetect)
        Dim lBounds As RectangleF = lUnionRegion.GetBounds(lGr)
        Dim X, Y As Integer
        Dim lRect As RectangleF
        Dim lZoneMove As ZoneMove
        lMidReg(2).X = lBounds.X + lBounds.Width / 2
        lMidReg(2).Y = lBounds.Y + lBounds.Height / 2

        lRect = lBounds

        lTestOldRegion = lOldRegion.Clone
        lTestOldRegion.Intersect(lRect)
        If Not lTestOldRegion.IsEmpty(lGr) Then
            'Le rectangle existait avant
            lTestNewRegion = mRegionDetect.Clone
            lTestNewRegion.Intersect(lRect)
            If Not lTestNewRegion.IsEmpty(lGr) Then
                'Le rectangle existe toujours
                lZoneMove = New ZoneMove(lRect)
                lMidReg(0) = lTestOldRegion.GetBounds(lGr)
                lMidReg(0).Offset(lMidReg(0).Width / 2, lMidReg(0).Height / 2) 'centre de la zone
                lMidReg(1) = lTestNewRegion.GetBounds(lGr)
                lMidReg(1).Offset(lMidReg(1).Width / 2, lMidReg(1).Height / 2) 'centre de la zone
                With lZoneMove
                    .vX = lMidReg(1).X - lMidReg(0).X
                    .vY = lMidReg(1).Y - lMidReg(0).Y
                    If .vX <> 0 OrElse .vY <> 0 Then
                        Return lZoneMove
                    End If
                End With
            End If
        End If
Fin:
    End Function

    Public Function GetStats() As String
        Return msStats
    End Function

    Public Function Get_MinPlusMax(ByRef r As Short, ByRef g As Short, ByRef b As Short) As Short
        Dim var_Max, var_Min As Short
        If r >= g Then
            If r >= b Then
                var_Max = r
                If g >= b Then
                    var_Min = b
                Else
                    var_Min = g
                End If
            Else
                var_Max = b
                var_Min = g
            End If
        Else
            'g>r
            If b >= g Then
                var_Max = b
                var_Min = r
            Else
                var_Max = g
                If r > b Then
                    var_Min = b
                Else
                    var_Min = r
                End If
            End If
        End If
        Return var_Max + var_Min ' / 2 / 255
    End Function
    Private Function Get_MinPlusMax(ByRef r As Byte, ByRef g As Byte, ByRef b As Byte) As Short
        Dim var_Max, var_Min As Byte
        If r >= g Then
            If r >= b Then
                var_Max = r
                If g >= b Then
                    var_Min = b
                Else
                    var_Min = g
                End If
            Else
                var_Max = b
                var_Min = g
            End If
        Else
            'g>r
            If b >= g Then
                var_Max = b
                var_Min = r
            Else
                var_Max = g
                If r > b Then
                    var_Min = b
                Else
                    var_Min = r
                End If
            End If
        End If
        Return CShort(var_Max) + CShort(var_Min) ' / 2 / 255
    End Function

    'Renvoie la direction de la perpendiculaire à la tangente en un point
    'Parcours les pixels à 3 de distances
    Public Function GetDirExtAtPt(ByVal pPt As PointF, ByVal pRegion As Region, ByVal pGr As Graphics) As Single
        Dim x, y As Single
        Dim lAngle As Single
        Dim lAngles(1) As Double
        Dim lGrp(), lGrpMax() As Single
        Dim lGrpPts As ArrayList
        Dim lRectReg As RectangleF = pRegion.GetBounds(pGr)
        Dim lbInside As Boolean
        Dim lbInsideOld As Boolean
        Dim lCountMax As Integer
        Dim lStepRot As Single = Angle360 / 9

        lbInside = True
        lbInsideOld = True
        lGrpPts = New ArrayList(4)
        For lAngle = 0 To Angle360 - lStepRot Step lStepRot
            x = pPt.X + Math.Cos(lAngle) * 3
            y = pPt.Y + Math.Sin(lAngle) * 3
            If lRectReg.Contains(x, y) Then
                lbInside = pRegion.IsVisible(x, y)
            End If
            If Not lbInside Then
                If lbInsideOld <> lbInside Then
                    'Début de zone libre
                    If Not lGrp Is Nothing Then lGrpPts.Add(lGrp)
                    lGrp = Array.CreateInstance(GetType(Single), 3)
                    lGrp(0) = 1
                    lGrp(1) = lAngle
                Else
                    lGrp(0) += 1
                    lGrp(2) = lAngle
                End If
            Else
                If Not lGrp Is Nothing Then
                    lGrpPts.Add(lGrp)
                    lGrp = Nothing
                End If
            End If
            lbInsideOld = lbInside
        Next
        If Not lbInside AndAlso Not lGrp Is Nothing Then
            'Termine la zone
            If lGrpPts.Count > 0 Then
                If lGrpPts(0)(1) = 0 Then
                    lGrpPts(0)(0) += lGrp(0)
                    lGrpPts(0)(1) += lGrp(1) - Angle360
                    lGrp = Nothing
                End If
            End If
            If Not lGrp Is Nothing Then lGrpPts.Add(lGrp)
        End If
        'Cherche la zone d'ouverture maximale
        lCountMax = 0
        For Each lGrp In lGrpPts
            If lGrp(0) > lCountMax Then
                lCountMax = lGrp(0)
                lGrpMax = lGrp
            End If
        Next
        If lCountMax > 0 Then
            lAngle = (lGrpMax(1) + lGrpMax(2)) / 2
            Return lAngle
        End If
    End Function
End Class

Public Class EDBitMap
    Public Function GetRegion(ByVal pBMPSrce As Bitmap, ByVal pColor As Color) As Region
        On Error GoTo Erreur

        Dim lBMPData As New BitmapBytesRGB24(pBMPSrce)
        lBMPData.LockBitmap(Imaging.ImageLockMode.ReadOnly)
        lBMPData.UnlockBitmap()

        Dim lRgn As New Region

        Dim lRect As New RectangleF
        Dim lbInImage As Boolean = False
        Dim X As Integer

        Dim lnPix As Integer
        Dim lTabColor(2) As Byte
        Dim lnCount As Integer
        Dim lnBorder As Integer
        Dim Xb, Yb, lnPixb As Integer
        Dim Xv, Yv, XvPrev, YvPrev, lnPixv As Integer
        Dim lDirVoisins() As Point = {New Point(1, 0), New Point(1, 1), New Point(0, 1), New Point(-1, 1), New Point(-1, 0), New Point(-1, -1), New Point(0, -1), New Point(1, -1)}
        Dim lDirVoisin As Point
        Dim lGrPath As GraphicsPath
        Dim lGrPaths As Collection
        Dim lPtsChecked(pBMPSrce.Width - 1, pBMPSrce.Height - 1) As Boolean
        Dim lPathPoints As Collection
        Dim dx, dy As Integer
        Dim lMatrix0 As New Matrix

        lTabColor(0) = pColor.R
        lTabColor(1) = pColor.G
        lTabColor(2) = pColor.B
        lnPix = 0
        lnCount = 0

        lGrPaths = New Collection
        lRgn.MakeEmpty()
        lbInImage = False
        For Y As Integer = 0 To pBMPSrce.Height - 1
            X = 0
            Do While (X < pBMPSrce.Width)
                If Not lPtsChecked(X, Y) Then 'pas fait
                    If lTabColor(0) = lBMPData.ImageBytes(lnPix) _
                AndAlso lTabColor(1) = lBMPData.ImageBytes(lnPix + 1) _
                AndAlso lTabColor(2) = lBMPData.ImageBytes(lnPix + 2) Then
                        '1er point
                        'Bord?
                        If X = 0 OrElse Y = 0 OrElse X = pBMPSrce.Width - 1 OrElse Y = pBMPSrce.Height - 1 Then
                            lnBorder = 2
                        Else
                            lnBorder = 0
                            For Xb = X - 1 To X + 1
                                For Yb = Y - 1 To Y + 1
                                    If Xb <> X OrElse Yb <> Y Then
                                        lnPixb = (Yb * pBMPSrce.Width + Xb) * 3
                                        If Not (lTabColor(0) = lBMPData.ImageBytes(lnPixb) _
                                    AndAlso lTabColor(1) = lBMPData.ImageBytes(lnPixb + 1) _
                                    AndAlso lTabColor(2) = lBMPData.ImageBytes(lnPixb + 2)) Then
                                            lnBorder += 1
                                            If lnBorder > 1 Then Exit For
                                        End If
                                    End If
                                Next
                                If lnBorder > 1 Then Exit For
                            Next
                        End If
                        lPtsChecked(X, Y) = True
                        If lnBorder > 1 Then
                            lPathPoints = New Collection
                            lPathPoints.Add(New PointF(X, Y))
                            'Parcours les voisins
                            XvPrev = X
                            YvPrev = Y
                            Do
                                For Each lDirVoisin In lDirVoisins
                                    lnBorder = 0
                                    'Voisin
                                    Xv = XvPrev + lDirVoisin.X
                                    Yv = YvPrev + lDirVoisin.Y
                                    If Xv < 0 OrElse Yv < 0 OrElse Xv >= pBMPSrce.Width OrElse Yv >= pBMPSrce.Height Then
                                    ElseIf lPtsChecked(Xv, Yv) Then
                                        'Déjà parcouru, voisin suivant
                                    Else
                                        lnPixv = (Yv * pBMPSrce.Width + Xv) * 3
                                        If lTabColor(0) = lBMPData.ImageBytes(lnPixv) _
                                    AndAlso lTabColor(1) = lBMPData.ImageBytes(lnPixv + 1) _
                                    AndAlso lTabColor(2) = lBMPData.ImageBytes(lnPixv + 2) Then
                                            'Le voisin est de la bonne couleur
                                            lPtsChecked(Xv, Yv) = True
                                            'Est ce un bord?
                                            If Xv = 0 OrElse Yv = 0 OrElse Xv = pBMPSrce.Width - 1 OrElse Yv = pBMPSrce.Height - 1 Then
                                                lnBorder = 2
                                            Else
                                                For Xb = Xv - 1 To Xv + 1
                                                    For Yb = Yv - 1 To Yv + 1
                                                        If Xb <> Xv OrElse Yb <> Yv Then
                                                            lnPixb = (Yb * pBMPSrce.Width + Xb) * 3
                                                            If Not (lTabColor(0) = lBMPData.ImageBytes(lnPixb) _
                                                        AndAlso lTabColor(1) = lBMPData.ImageBytes(lnPixb + 1) _
                                                        AndAlso lTabColor(2) = lBMPData.ImageBytes(lnPixb + 2)) Then
                                                                lnBorder += 1
                                                                If lnBorder > 1 Then Exit For
                                                            End If
                                                        End If
                                                    Next
                                                    If lnBorder > 1 Then Exit For
                                                Next
                                            End If
                                        End If

                                        'Bord trouvé
                                        If lnBorder > 1 Then Exit For
                                    End If
                                Next
                                If lnBorder <= 1 AndAlso lPathPoints.Count > 2 Then
                                    'Pas de voisin, on a fini le parcours
                                    'TO DO : Pas de voisin peut signifier qu'on est sur une pointe

                                    lGrPath = New GraphicsPath
                                    Dim lnPtChange As Integer = 1
                                    Dim lnPt As Integer
                                    dx = lPathPoints(2).x - lPathPoints(1).x
                                    dy = lPathPoints(2).y - lPathPoints(1).y
                                    For lnPt = 3 To lPathPoints.Count
                                        If lPathPoints.Item(lnPt).x - lPathPoints.Item(lnPt - 1).x <> dx _
                                    OrElse lPathPoints.Item(lnPt).y - lPathPoints.Item(lnPt - 1).y <> dy Then
                                            'direction différente
                                            lGrPath.AddLine(lPathPoints.Item(lnPtChange), lPathPoints.Item(lnPt - 1))
                                            dx = lPathPoints(lnPt).x - lPathPoints(lnPt - 1).x
                                            dy = lPathPoints(lnPt).y - lPathPoints(lnPt - 1).y
                                            lnPtChange = lnPt - 1
                                        End If
                                    Next
                                    'Origine
                                    lGrPath.AddLine(lPathPoints.Item(lnPtChange), lPathPoints.Item(1))
                                    lRgn.Union(lGrPath)
                                    'Centre
                                    lRect = lGrPath.GetBounds(lMatrix0)
                                    lGrPaths.Add(lGrPath)
                                    Exit Do
                                End If
                                lnCount += 1
                                lPathPoints.Add(New PointF(Xv, Yv))
                                XvPrev = Xv
                                YvPrev = Yv
                            Loop
                        End If

                    End If
                End If

                X += 1
                lnPix += 3
            Loop
        Next Y
        Return lRgn

        GoTo Fin
Erreur:
        MsgBox(Err.Description)
        If False Then
            Resume
        End If
Fin:
    End Function

End Class