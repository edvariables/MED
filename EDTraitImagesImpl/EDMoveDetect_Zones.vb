
#Region "ZoneMove"
Imports System.Drawing.Drawing2D
Imports MED.EDMovDetect

Public Class ZoneMove
    Public Key As String
    Public Frame As RectangleF
    Public vX As Single
    Public vY As Single
    Public Region As Region
    Public Sub New(ByVal pFrame As RectangleF)
        Frame = pFrame
        Key = pFrame.X & "_" & pFrame.Y
    End Sub
    Public Function Clone() As ZoneMove
        Dim lClone As ZoneMove = New ZoneMove(New RectangleF(Frame.X, Frame.Y, Frame.Width, Frame.Height))
        lClone.vX = vX
        lClone.vY = vY
        If Not Region Is Nothing Then lClone.Region = Region.Clone
        Return lClone
    End Function
    Public Sub Transform(ByVal pMatrix As Drawing2D.Matrix)
        If Math.Abs(vX) > 5000 OrElse Math.Abs(vY) > 5000 Then
            Err.Raise(-1, "EDMovDetect.Zone.Transform", "To large")
        End If
        With Frame
            .X *= pMatrix.Elements(0)
            .Y *= pMatrix.Elements(3)
            .Width *= pMatrix.Elements(0)
            .Height *= pMatrix.Elements(3)
        End With
        vX *= pMatrix.Elements(0)
        vY *= pMatrix.Elements(3)
        If vX > 10000 OrElse vY > 10000 Then
            Err.Raise(-1, "EDMovDetect.Zone.Transform", "To large")
        End If
    End Sub
End Class
'--------------
Public Class ZonesMove
    Private mCol As Collection
    Public Sub New()
        mCol = New Collection
    End Sub
    Public Sub Add(ByVal pZoneMove As ZoneMove)
        mCol.Add(pZoneMove, pZoneMove.Key)
    End Sub
    Public ReadOnly Property Count() As Integer
        Get
            Return mCol.Count
        End Get
    End Property
    Public Sub Clear()
        mCol = New Collection
    End Sub
    Default Public ReadOnly Property Item(ByVal pIndex As Integer) As Object
        Get
            Return mCol.Item(pIndex)
        End Get
    End Property
    Default Public ReadOnly Property Item(ByVal pKey As String) As Object
        Get
            On Error Resume Next
            Return mCol.Item(pKey)
        End Get
    End Property
    Public Function GetEnumerator() As IEnumerator
        Return mCol.GetEnumerator
    End Function
    Public Sub Transform(ByVal pMatrix As Drawing2D.Matrix)
        Dim lZone As ZoneMove
        For Each lZone In mCol
            lZone.Transform(pMatrix)
        Next
    End Sub
    Public Function GetZoneAt(ByVal x As Integer, ByVal y As Integer) As ZoneMove
        Dim lZone As ZoneMove
        For Each lZone In mCol
            With lZone.Frame
                If .X <= x AndAlso .Y <= y _
                AndAlso .Right > x _
                AndAlso .Bottom > y Then
                    Return lZone
                End If
            End With
        Next
        Return Nothing
    End Function
    Public Function Clone() As ZonesMove
        Dim lClone As New ZonesMove
        Dim lZone As ZoneMove
        For Each lZone In mCol
            lClone.Add(lZone.Clone)
        Next
        Return lClone
    End Function
End Class
#End Region


