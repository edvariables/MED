Imports System.Drawing.Imaging
Imports System.Drawing
Public Class EDCalcBMPAvg

    Private mBMPLock As BitmapBytesRGB24
    Private mBMPAvg As Bitmap
    Private mBMPAvgLock As BitmapBytesRGB24
    Private mImSum() As Integer
    Private mImAvg() As Single
    Private mImCount() As Integer
    Private mEDDetect As EDMovDetect
    Private mRect As RectangleF
    Private mMatrix0 As New Drawing2D.Matrix
    '/////////////////////////////
    Public Sub New()
        mEDDetect = New EDMovDetect
        mEDDetect.Limites.SetPreselMoveOnly()
    End Sub
    Public Sub Release()
        If Not mBMPLock Is Nothing Then
            mBMPLock = Nothing
            ReDim mImSum(0)
            mBMPAvgLock = Nothing
            mBMPAvg = Nothing
            mEDDetect.Release()
        End If
    End Sub
    '/////////////////////////////
    Public Property BMPAverage() As Bitmap
        Get
            Return mBMPAvg
        End Get
        Set(ByVal Value As Bitmap)
            mBMPAvg = Value
            If mBMPAvgLock Is Nothing Then mBMPAvgLock = New BitmapBytesRGB24(mBMPAvg)
        End Set
    End Property
    '/////////////////////////////

    'pBMP type is 24RGB
    Public Sub SetNewImage(ByVal pBMP As Bitmap)
        SetNewImage(pBMP, False)
    End Sub
    Public Sub SetNewImage(ByVal pBMP As Bitmap, ByVal pbFirst As Boolean)
        Dim lbFirst As Boolean = pbFirst
        mEDDetect.SetNewImage(pBMP)

        If mBMPLock Is Nothing Then
            lbFirst = True
            mRect = New RectangleF(0, 0, pBMP.Width, pBMP.Height)
            mBMPLock = mEDDetect.mBMPLock
            If mBMPAvg Is Nothing Then
                mBMPAvg = New Bitmap(pBMP.Width, pBMP.Height)
                mBMPAvgLock = New BitmapBytesRGB24(mBMPAvg)
            End If
        Else
            mBMPLock.Bitmap = pBMP
        End If

        If lbFirst Then
            ReDim mImSum(mBMPLock.ImageBytes.GetUpperBound(0))
            ReDim mImAvg(mBMPLock.ImageBytes.GetUpperBound(0))
            ReDim mImCount(mBMPLock.ImageBytes.GetUpperBound(0))
            mBMPAvgLock.LockBitmap(ImageLockMode.ReadWrite, False)
            Array.Copy(mBMPLock.ImageBytes, mBMPAvgLock.ImageBytes, mBMPAvgLock.ImageBytes.GetLength(0))
            lbFirst = False
        Else
            mBMPAvgLock.LockBitmap(ImageLockMode.ReadWrite, False)
            Dim lRegion As Region = New Region(mRect)
            lRegion.Exclude(mEDDetect.RegionDetect)
            Dim lRects() As RectangleF = lRegion.GetRegionScans(mMatrix0)
            CalculateAvg(mBMPLock, mBMPAvgLock, mImSum, mImAvg, mImCount, lRects)
        End If
        mBMPAvgLock.UnlockBitmap()
    End Sub
    Private Sub CalculateAvg(ByRef pBMPLock As BitmapBytesRGB24, ByRef pBMPAvgLock As BitmapBytesRGB24, ByVal pImSum() As Integer, ByVal pImAvg() As Single, ByVal pImCount() As Integer, ByVal pRects() As RectangleF)
        Dim lnPix, lnPixK As Integer
        Dim lRect As RectangleF
        Dim lnRect As Integer
        Dim X, Y As Integer
        Dim k As Integer
        For lnRect = pRects.GetLowerBound(0) To pRects.GetUpperBound(0)
            lRect = pRects(lnRect)
            For X = lRect.Left To lRect.Right - 1
                For Y = lRect.Top To lRect.Bottom - 1
                    lnPix = 3 * (mRect.Width * Y + X)
                    For k = 0 To 2
                        lnPixK = lnPix + k
                        mImCount(lnPixK) += 1
                        pImSum(lnPixK) += pBMPLock.ImageBytes(lnPixK)
                        pImAvg(lnPixK) = mImSum(lnPixK) / mImCount(lnPixK)
                        pBMPAvgLock.ImageBytes(lnPixK) = CByte(mImAvg(lnPixK))
                    Next
                Next
            Next
        Next

        GoTo Fin
Erreur:
        If False Then
            Resume
        End If
Fin:
    End Sub
    

End Class
