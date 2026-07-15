Imports System.Drawing
Imports System.Drawing.Graphics
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices


Public Class BitmapBytesRGB24
        'ATTENTION : La largeur doit être avoir un arrondi entier à 4 octets
        ' Provide public access to the picture's byte data.
        Public ImageBytes() As Byte
        Public BytesPerPixel As Integer = 3
        Private mBounds As Rectangle
        Public mTotal_size As Integer
        Private mbLocked As Boolean
        ' A reference to the Bitmap.
        Private m_Bitmap As Bitmap
        ' Bitmap data.
        Private m_BitmapData As BitmapData
        Public mPtrScan0 As IntPtr
        Private mLockMode As ImageLockMode
        Public Sub New(ByVal bm As Bitmap)
            Bitmap = bm
            If Not bm Is Nothing AndAlso bm.PixelFormat = PixelFormat.Format32bppArgb Then BytesPerPixel = 4
        End Sub
        Public Property Bitmap() As Bitmap
            Get
                Return m_Bitmap
            End Get
            Set(ByVal bm As Bitmap)
                m_Bitmap = bm
                If Not bm Is Nothing Then
                    mBounds = New Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height)
                End If
            End Set
        End Property

        ' Lock the bitmap's data.
        Public Sub LockBitmap(Optional ByVal pLock As ImageLockMode = ImageLockMode.ReadWrite,
                            Optional ByVal pbCopyData As Boolean = True)
            ' Lock the bitmap data.
            'If Not pbCopyData Then pLock = ImageLockMode.ReadOnly
            mLockMode = pLock
            m_BitmapData = m_Bitmap.LockBits(mBounds, pLock, PixelFormat.Format24bppRgb)
            mbLocked = True
            If mTotal_size = 0 Then
                ' Allocate room for the data.
                'ATTENTION : Stride est un arrondi à 4 octets
                mTotal_size = m_BitmapData.Stride * m_BitmapData.Height
                ReDim ImageBytes(mTotal_size)
            End If
            ' Copy the data into the ImageBytes array.
            mPtrScan0 = m_BitmapData.Scan0
            If pbCopyData Then
                Marshal.Copy(mPtrScan0, ImageBytes, 0, mTotal_size)
            End If
        End Sub
        Public Sub CopyData(ByVal pBytes() As Byte, ByVal pIndex As Integer, ByVal pCount As Integer)
            Marshal.Copy(IntPtr.op_Explicit(mPtrScan0.ToInt32 + pIndex), pBytes, 0, pCount)
        End Sub
        Public Sub ReadBytes(ByVal pBytes() As Byte, ByVal pIndex As Integer, ByVal pCount As Integer)
            Dim i As Integer
            For i = 0 To pCount - 1
                pBytes(i) = Marshal.ReadByte(mPtrScan0, pIndex + i)
            Next
        End Sub
        Public Function ReadByte(ByVal pIndex As Integer) As Byte
            Return Marshal.ReadByte(mPtrScan0, pIndex)
        End Function
        Public Sub WriteByte(ByVal pIndex As Integer, ByVal pValue As Byte)
            Marshal.WriteByte(mPtrScan0, pIndex, pValue)
        End Sub
        Public Sub WriteByte(ByVal pIndex As Integer, ByVal pValue() As Byte, ByVal pLen As Integer)
            Marshal.Copy(pValue, pIndex, mPtrScan0, pLen)
        End Sub
        ' Copy the data back into the Bitmap
        ' and release resources.
        Public Sub UnlockBitmap()
            If Not mbLocked Then Exit Sub
            If m_BitmapData Is Nothing Then Return
            If m_Bitmap Is Nothing Then Return
            ' Copy the data back into the bitmap.
            If mLockMode <> ImageLockMode.ReadOnly Then Marshal.Copy(ImageBytes, 0, m_BitmapData.Scan0, mTotal_size)
            ' Unlock the bitmap.
            m_Bitmap.UnlockBits(m_BitmapData)
            mbLocked = False
        End Sub
End Class