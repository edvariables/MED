
Public Class EDMovDetectLimites
    Private mR() As Byte = {&H0, &HFF}
    Private mG() As Byte = {&H0, &HFF}
    Private mB() As Byte = {&H0, &HFF}
    Private mL() As Integer = {&H0, &HFF}
    Private mMvt As Integer = &H10
    Private mSizeMin As Integer = 5
    Public WithRGB As Boolean = True
    Public WithLum As Boolean = True
    Public RectAnalyse As RectangleF = RectangleF.Empty

    Friend Sub New()
    End Sub
    Public Property LMin() As Integer
        Get
            Return mL(0)
        End Get
        Set(ByVal Value As Integer)
            mL(0) = Value
            WithLum = mL(0) > 0 Or mL(1) < &H1FF
        End Set
    End Property
    Public Property LMax() As Integer
        Get
            Return mL(1)
        End Get
        Set(ByVal Value As Integer)
            mL(1) = Value
            WithLum = mL(0) > 0 Or mL(1) < &H1FF
        End Set
    End Property
    Public Property RMin() As Byte
        Get
            Return mR(0)
        End Get
        Set(ByVal Value As Byte)
            mR(0) = Value
            WithRGB = mR(0) > 0 Or mG(0) > 0 Or mB(0) > 0 Or mR(1) < 255 Or mG(1) < 255 Or mB(1) < 255
        End Set
    End Property
    Public Property RMax() As Byte
        Get
            Return mR(1)
        End Get
        Set(ByVal Value As Byte)
            mR(1) = Value
            WithRGB = mR(0) > 0 Or mG(0) > 0 Or mB(0) > 0 Or mR(1) < 255 Or mG(1) < 255 Or mB(1) < 255
        End Set
    End Property
    Public Property GMin() As Byte
        Get
            Return mG(0)
        End Get
        Set(ByVal Value As Byte)
            mG(0) = Value
            WithRGB = mR(0) > 0 Or mG(0) > 0 Or mB(0) > 0 Or mR(1) < 255 Or mG(1) < 255 Or mB(1) < 255
        End Set
    End Property
    Public Property GMax() As Byte
        Get
            Return mG(1)
        End Get
        Set(ByVal Value As Byte)
            mG(1) = Value
            WithRGB = mR(0) > 0 Or mG(0) > 0 Or mB(0) > 0 Or mR(1) < 255 Or mG(1) < 255 Or mB(1) < 255
        End Set
    End Property
    Public Property BMin() As Byte
        Get
            Return mB(0)
        End Get
        Set(ByVal Value As Byte)
            mB(0) = Value
            WithRGB = mR(0) > 0 Or mG(0) > 0 Or mB(0) > 0 Or mR(1) < 255 Or mG(1) < 255 Or mB(1) < 255
        End Set
    End Property
    Public Property BMax() As Byte
        Get
            Return mB(1)
        End Get
        Set(ByVal Value As Byte)
            mB(1) = Value
            WithRGB = mR(0) > 0 Or mG(0) > 0 Or mB(0) > 0 Or mR(1) < 255 Or mG(1) < 255 Or mB(1) < 255
        End Set
    End Property
    Public Property SizeMin() As Integer
        Get
            Return mSizeMin
        End Get
        Set(ByVal Value As Integer)
            mSizeMin = Value
        End Set
    End Property
    Public Property Mvt() As Integer
        Get
            Return mMvt
        End Get
        Set(ByVal Value As Integer)
            mMvt = Value
        End Set
    End Property
    Public Sub SetPreselRed()
        mR(0) = 0
        mR(1) = &HFF
        mG(0) = 0
        mG(1) = 50
        mB(0) = 0
        mB(1) = 50
        WithRGB = True
    End Sub
    Public Sub SetPreselGreen()
        mR(0) = 0
        mR(1) = 50
        mG(0) = 0
        mG(1) = &HFF
        mB(0) = 0
        mB(1) = 50
        WithRGB = True
    End Sub
    Public Sub SetPreselBlue()
        mR(0) = 0
        mR(1) = 50
        mG(0) = 0
        mG(1) = 50
        mB(0) = 0
        mB(1) = &HFF
        WithRGB = True
    End Sub
    Public Sub SetPreselLight(Optional ByVal pLevel As Short = &H170)
        mL(0) = pLevel
        mL(1) = &H1FF
        mR(0) = 0
        mR(1) = &HFF
        mG(0) = 0
        mG(1) = &HFF
        mB(0) = 0
        mB(1) = &HFF
        WithRGB = False
        WithLum = pLevel > 0
    End Sub
    Public Sub SetPreselDark(Optional ByVal pLevel As Short = &H30)
        mL(0) = 0
        mL(1) = pLevel
        mR(0) = 0
        mR(1) = &HFF
        mG(0) = 0
        mG(1) = &HFF
        mB(0) = 0
        mB(1) = &HFF
        WithRGB = False
        WithLum = pLevel < &H1FF
    End Sub
    Public Sub SetPreselMoveOnly(Optional ByVal pMvt As Integer = &H20)
        mL(0) = 0
        mL(1) = &H1FF
        mR(0) = 0
        mR(1) = &HFF
        mG(0) = 0
        mG(1) = &HFF
        mB(0) = 0
        mB(1) = &HFF
        WithRGB = False
        WithLum = False
        mMvt = pMvt
    End Sub
End Class
