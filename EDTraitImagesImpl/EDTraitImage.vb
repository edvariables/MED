Imports System.Windows.Forms
Imports System.Drawing
Imports EDVideoStream
Imports EDID
Imports MED.EDVideoStreamImpl.MED

Public MustInherit Class EDTraitImage
    Implements IDisposable
    Public Enum StateStream
        NotInitialised
        Stopped
        Starting
        Running
        Stopping
    End Enum
    Public Enum TypeTraitImage
        Vibro
        VibroRGB
        VibroRGBCS
        VibroRGBFiltre
        VibroHSL
        ShowDir
        Etourno
        Tissu
        FormPlast
        DrawForm
        SoundMidi
        SoundMulti
        SoundFrq
        ShowDiff
        SndOfImage
        VideoStereo
        EdgeBalls
        Balance
        PersoMouv
        Scenario
        SoundNbre
        ImageOfSnd
        ViewNeurone
        Squares
    End Enum
    Public mState As StateStream
    Protected Friend mBMP As Bitmap
    Protected Friend mBMPData As BitmapBytesRGB24
    Protected Friend mXMax, mYMax As Integer
    Protected Friend mWBCDetect As EDMovDetect

    Public Sub New()

    End Sub
    Public Overridable Sub Dispose() Implements IDisposable.Dispose
        EDTraitImagePrevious = Nothing
        Release()
        GC.SuppressFinalize(Me)
    End Sub

#Region "Propriétés"
    Public MustOverride ReadOnly Property Name() As String
    Public MustOverride ReadOnly Property TypeTraint() As TypeTraitImage

    Public ReadOnly Property Width() As Integer
        Get
            Return mXMax + 1
        End Get
    End Property
    Public ReadOnly Property Height() As Integer
        Get
            Return mYMax + 1
        End Get
    End Property
    Public ReadOnly Property BitMapData() As Byte()
        Get
            On Error Resume Next
            Return mBMPData.ImageBytes
        End Get
    End Property
    Public Overridable Property BitMap() As Bitmap
        Get
            Return mBMP
        End Get
        Set(ByVal pBMP As Bitmap)
            Me.SetBitMap(pBMP, Nothing, Nothing, 0)
        End Set
    End Property
    Public WriteOnly Property WBCDetect() As EDMovDetect
        Set(ByVal Value As EDMovDetect)
            mWBCDetect = Value
        End Set
    End Property
    Private mEDTraitImagePrev As EDTraitImage
    Public Property EDTraitImagePrevious() As EDTraitImage
        Get
            Return mEDTraitImagePrev
        End Get
        Set(ByVal Value As EDTraitImage)
            mEDTraitImagePrev = Value
        End Set
    End Property

    Public Overridable Function Enumeration(ByVal psName As String) As Type

    End Function
    Public Overridable Property Param(ByVal psName As String) As Object
        Get

        End Get
        Set(ByVal Value As Object)

        End Set
    End Property
    Private WithEvents mParams As DataTable
    Public Overridable Property Params() As DataTable
        Get
            If mParams Is Nothing Then
                mParams = New DataTable("Params")
                Dim colItem As DataColumn = New DataColumn("Key", Type.GetType("System.String"))
                colItem.Unique = True
                mParams.Columns.Add(colItem)
                mParams.PrimaryKey = New DataColumn() {colItem}
                colItem = New DataColumn("Libelle", Type.GetType("System.String"))
                colItem.Unique = False
                mParams.Columns.Add(colItem)
                colItem = New DataColumn("Type", Type.GetType("System.String"))
                mParams.Columns.Add(colItem)
                colItem.Unique = False
                colItem = New DataColumn("Value", Type.GetType("System.String"))
                mParams.Columns.Add(colItem)
                colItem.Unique = False
            End If
            Return mParams
        End Get
        Set(ByVal pParams As DataTable)
            mParams = pParams
        End Set
    End Property
    Private Sub mParams_ColumnChanging(ByVal sender As Object, ByVal e As System.Data.DataColumnChangeEventArgs) Handles mParams.ColumnChanging
        Dim lsValue As String
        If e.Column.ColumnName <> "Value" Then Return
        Select Case CStr(e.Row.Item("Type"))
            Case "Single"
                If IsNumeric(e.ProposedValue) Then
                    Param(e.Row.Item("Key")) = CSng(e.ProposedValue)
                End If
            Case "Integer"
                If IsNumeric(e.ProposedValue) Then
                    Param(e.Row.Item("Key")) = CInt(e.ProposedValue)
                End If
            Case "Byte"
                If IsNumeric(e.ProposedValue) Then
                    Param(e.Row.Item("Key")) = CByte(e.ProposedValue)
                End If
            Case "Bool", "Boolean"
                If IsNumeric(e.ProposedValue) Then
                    Param(e.Row.Item("Key")) = CBool(e.ProposedValue)
                End If
            Case Else
                If e.Row.Item("Type").ToString.StartsWith("Enum") Then
                    If IsNumeric(e.ProposedValue) Then
                        Param(e.Row.Item("Key")) = CInt(e.ProposedValue)
                    End If
                Else
                    Param(e.Row.Item("Key")) = e.ProposedValue
                End If
        End Select
    End Sub
    Public Sub ParamsFromEDP(ByVal pEDP As Object)
        'Dim ldbRow As DataRow
        'For Each lParam As EDP.IParam In pEDP
        '    ldbRow = mParams.Rows.Find(lParam.Key())
        '    If Not ldbRow Is Nothing Then ldbRow.Item("Value") = lParam.Data()
        'Next
    End Sub
    Public Sub ParamsToEDP(ByVal pEDP As Object)
        'Dim ldbRow As DataRow
        'For Each ldbRow In mParams.Rows
        '    pEDP.Item(ldbRow.Item("Key"), True).Data = ldbRow.Item("Value")
        'Next
    End Sub
#End Region

    Public MustOverride Function Initialiser(ByVal pWidth As Integer, ByVal pHeight As Integer) As Boolean
    Public MustOverride Function Initialiser(ByVal pBMP As Bitmap, ByVal pBMPData() As Byte) As Boolean
    Public MustOverride Function Initialiser(ByVal pWidth As Integer, ByVal pHeight As Integer, ByVal pBMP As Bitmap, ByVal pBMPData() As Byte) As Boolean

    Public Overridable Sub Release()
        mState = StateStream.Stopped
        mBMPData = Nothing
        mBMP = Nothing
    End Sub

    Public Overridable Sub SetBitMap(ByVal pBMP As Bitmap, ByVal pRegion As Region, ByRef pMoveDetect As EDMovDetect, ByVal pIndex As Integer)
        mBMP = pBMP
        If Not mBMP Is Nothing Then
            mXMax = pBMP.Width - 1
            mYMax = pBMP.Height - 1
            mBMPData = New BitmapBytesRGB24(pBMP)
        End If
    End Sub
    Public MustOverride Sub Traitement()

    Public Overridable Function GetStats() As String
    End Function

End Class

'Implements IEDVideoStream for EDTraitImage
Public Class EDTraitImageStream
    Implements IEDVideoStream
    Private mEDTraitImage As EDTraitImage
    Private mWidth As Integer = 160
    Private mHeight As Integer = 120
    Public Function StreamType() As EDVideoStreamGlb.StreamType Implements IEDVideoStream.StreamType
        Return EDVideoStreamGlb.StreamType.TRAITIMAGE
    End Function

    Public Sub New(ByVal pEDTraitImage As EDTraitImage)
        mEDTraitImage = pEDTraitImage
    End Sub

    Public Sub Dispose() Implements IEDVideoStream.Dispose
        If Not mEDTraitImage Is Nothing Then
            mEDTraitImage.Dispose()
            mEDTraitImage = Nothing
        End If
    End Sub

    Public Function OpenFile(ByVal psFile As String) As Boolean Implements IEDVideoStream.OpenFile
        Dim lBMP As Bitmap
        If psFile <> "" Then
            lBMP = New Bitmap(psFile)
            mWidth = lBMP.Width
            mHeight = lBMP.Height
        End If
        mEDTraitImage.Initialiser(mWidth, mHeight, lBMP, Nothing)
        Return True
    End Function

    Public Sub Close() Implements IEDVideoStream.Close
        mEDTraitImage.Release()
    End Sub

    Public Function FrameRate() As Double Implements IEDVideoStream.FrameRate
        Return 25
    End Function

    Public Function FramesCount() As Double Implements IEDVideoStream.FramesCount
        Return 1
    End Function

    Public Function FrameSize() As System.Drawing.Size Implements IEDVideoStream.FrameSize
        Return New Size(mEDTraitImage.Width, mEDTraitImage.Height)
    End Function
    Public Sub ScaleToFitWidth(ByVal pWidthOut As Short) Implements IEDVideoStream.ScaleToFitWidth
        If pWidthOut > 0 Then
            If mWidth > 0 Then
                mHeight = mHeight * pWidthOut / mWidth
                mWidth = pWidthOut
            Else
                mWidth = pWidthOut
                mHeight = 120 * pWidthOut / 160
            End If
        End If
    End Sub

    Public Function GetFrame(ByVal pIndex As Integer) As System.Drawing.Bitmap Implements IEDVideoStream.GetFrame
        If Not mEDTraitImage.mState = EDTraitImage.StateStream.Running Then
            If Not mEDTraitImage.Initialiser(mWidth, mHeight) Then
                Return Nothing
            End If
            Application.DoEvents()
        Else
            mEDTraitImage.Traitement()
        End If
        Return mEDTraitImage.BitMap
    End Function

    Public Sub DeleteCurrentFrame() Implements IEDVideoStream.DeleteCurrentFrame

    End Sub
End Class

Public Interface EDTraitImageForm
    ReadOnly Property Form() As Form
    Property EDTraitImages() As EDTraitImage
    Property State() As EDTraitImage.StateStream
    Sub ShowStats()
    Sub Afficher(ByVal EDP As Object, ByVal pIndex As Byte, ByVal pTop As Integer, ByVal pLeft As Integer, ByVal pEDK8055 As Object, ByRef pEDTraitImPrevious As EDTraitImage)
    Function StartStream(ByVal pWidth As Integer, ByVal pHeight As Integer) As Boolean
    Sub StopStream()
    Function Terminate() As Boolean
End Interface
