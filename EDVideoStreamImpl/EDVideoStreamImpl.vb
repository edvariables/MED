Imports System.Drawing
Public Class EDVideoStreamGlb
    Public Enum StreamType
        AVI
        MPG
        WEBCAM
        FILES
        BMPCOLLECTION
        IPTRENDS
        TRAITIMAGE
    End Enum
End Class

Public Interface IEDVideoStream
    Function StreamType() As EDVideoStreamGlb.StreamType
    Sub Dispose()
    Function OpenFile(ByVal psFile As String) As Boolean
    Sub Close()
    Function FramesCount() As Double
    Function FrameSize() As Size
    Function FrameRate() As Double
    Function GetFrame(ByVal pIndex As Integer) As Bitmap
    Sub ScaleToFitWidth(ByVal pWidthOut As Short)
    Sub DeleteCurrentFrame()
End Interface