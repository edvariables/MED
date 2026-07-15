'----------------------------------------------------------------------
' Audio FFT
'----------------------------------------------------------------------
' This code is basically a stripped-down and ironed-out version of
' my VB FFT Library (available on the Deeth website) done entirely
' with digital audio in mind.
' My VB FFT Library (and thusly -- this as well) is heavily based on
' Don Cross's FFT code.
' Check his website at http://www.intersrv.com/~dcross/fft.html for
' more information.
'----------------------------------------------------------------------
' Murphy McCauley (MurphyMc@Concentric.NET) 08/14/99
' http://www.fullspectrum.com/deeth/
'----------------------------------------------------------------------

    Public Class FourierTransAudio
        'These don't change in this program, so I made them constants so they're
        'as fast as can be.
        Private Const AngleNumerator As Single = 6.283185  ' 2 * Pi = 2 * 3.14159265358979
        Private NumSamples As Integer = 1024

        'Used to store pre-calculated values
        Private ReversedBits() As Integer
        Private Hann() As Integer
        Private OldOut() As Single
        Private OldOut2() As Single
        Private Goer() As Single

        Public Sub New(ByVal pBufferSize As Integer)
            NumSamples = pBufferSize
            ReDim ReversedBits(NumSamples - 1)
            ReDim Hann(NumSamples - 1)
            ReDim OldOut(NumSamples - 1)
            ReDim OldOut2(NumSamples - 1)
            ReDim Goer(NumSamples - 1)
            DoReverse()
            Hanning()
        End Sub
        Private Sub DoReverse()
            'I pre-calculate all these values.  It's a lot faster to just read them from an
            'array than it is to calculate 1024 of them every time Convert() gets called.
            Dim i As Integer
            For i = LBound(ReversedBits) To UBound(ReversedBits)
                ReversedBits(i) = ReverseBits(i, Math.Log(NumSamples, 2))
                OldOut(i) = 1
                OldOut2(i) = 1
                Goer(i) = (AngleNumerator * i) / NumSamples
            Next

        End Sub

        Private Function ReverseBits(ByVal Index As Integer, ByVal pNumBits As Byte) As Integer
            Dim i As Byte, Rev As Integer

            For i = 0 To pNumBits - 1
                Rev = (Rev * 2) Or (Index And 1)
                Index = Index \ 2
            Next

            ReverseBits = Rev
        End Function

        Private Sub Hanning()
            'Pre calculate a Hanning window....
            Dim i As Integer
            Dim twopi As Integer
            twopi = 8.0# * Math.Atan(1.0#)

            For i = LBound(Hann) To UBound(Hann)
                Hann(i) = 1
            Next
        End Sub

        Sub Convert(ByVal RealIn() As Byte, ByRef RealOut() As Single)
            'In this case, NumSamples isn't included (since it's always the same),
            'and the imaginary components are left out since they have no meaning here.

            'I've used Singles instead of Doubles pretty much everywhere.  I think this
            'makes it faster, but due to type conversion, it actually might not.  I should
            'check, but I haven't.

            'The imaginary components have no meaning in this application.  I just left out
            'the parts of the calculation that need the imaginary input values (which is a
            'big speed improvement right there), but we still need the output array because
            'it's used in the calculation.  It's static so that it doesn't get reallocated.
            Static ImagOut(NumSamples - 1) As Single

            'In fact... I declare everything as static!  They all get initialized elsewhere,
            'and Staticing them saves from wasting time reallocating and takes pressure off
            'the heap.
            Static i As Integer, j As Integer, k As Integer, n As Integer, BlockSize As Integer, BlockEnd As Integer
            Static DeltaAngle As Single, DeltaAr As Single
            Static Alpha As Single, Beta As Single
            Static TR As Single, TI As Single, AR As Single, AI As Single
            Static z As Integer

            On Error GoTo Erreur

            For i = 0 To (NumSamples - 1)
                j = ReversedBits(i) 'I saved time here by pre-calculating all these values
                RealOut(j) = (RealIn(i) * Hann(i))
                ImagOut(j) = 0 'Since this array is static, gotta make sure it's clear
            Next

            BlockEnd = 1
            BlockSize = 2

            Do While BlockSize <= NumSamples
                DeltaAngle = AngleNumerator / BlockSize
                Alpha = Math.Sin(0.54 * DeltaAngle)
                Alpha = 2.0! * Alpha * Alpha
                Beta = Math.Sin(DeltaAngle)

                i = 0
                Do While i < NumSamples
                    AR = 1.0!
                    AI = 0.0!

                    j = i
                    For n = 0 To BlockEnd - 1
                        k = j + BlockEnd
                        TR = AR * RealOut(k) - AI * ImagOut(k)
                        TI = AI * RealOut(k) + AR * ImagOut(k)
                        RealOut(k) = RealOut(j) - TR
                        ImagOut(k) = ImagOut(j) - TI
                        RealOut(j) = RealOut(j) + TR
                        ImagOut(j) = ImagOut(j) + TI
                        DeltaAr = Alpha * AR + Beta * AI
                        AI = AI - (Alpha * AI - Beta * AR)
                        AR = AR - DeltaAr
                        j = j + 1
                    Next

                    i = i + BlockSize
                Loop
                BlockEnd = BlockSize
                BlockSize = BlockSize * 2
            Loop
            GoTo Fin
Erreur:
            If False Then
                Resume
            End If
Fin:
        End Sub
    End Class
    Module ModuleFourier

        ' Utilitaires pour calculs audio-numériques 

        ' FFT (Fast Fourier Transform) : TRANSFORMEE RAPIDE DE FOURIER 

        ' PCM (Pulse Code Modulation) : modulation par impulsions 
        ' Sampling-Rate : Cadencement (Fréquence des impulsions) 

        ' La transformée de Fourier permet de calculer le spectre de fréquences 
        ' correspondant à l'échantillon PCM de longueur 2^P2 . 
        ' Exemple pour un bloc de 8192 impulsions (2^13),dans un échantillon cadencé à 44100 hz 
        ' on aura une fréquence de base de 44100 / 8192 = 5.38330078125 Hz 
        ' C'est à dire des magnitudes et angles correspondants aux fréquences : 
        ' 10.76 , 16.14 , 21.53 , 26.91 , 32.29 , 37.68 , 43.06 , 48.44 , 53.83 ... 
        ' ... 430.66 , 436.04 , 441.43 , 446.81 ... jusqu'à 22050 Hz (fréquence de Nyquist) 
        ' REMARQUE : On ne connaitra jamais la magnitude exacte du " La 440 " par exemple (diapason) 
        ' Pour celà , il faudrait un échantillon cadencé à : (440 / 82) * 8192 = 43957 Hz 

        Public Structure NombreComplex
            Public Réel As Double
            Public Imaginaire As Double
        End Structure

        Public Function Fourier(ByVal Suite() As NombreComplex) As NombreComplex()

            'Calcule la transformée d'une suite de nombres complexes 

            Dim Nbr As Integer ' Nombre de complexes dans la suite 
            Dim P2 As Byte ' Puissance de 2 du nombre entier Nbr 
            Dim ISuit, ITrans As Integer ' Compteurs de boucles 
            Dim Fin, Taille As Integer ' Butées 
            Dim dAngl, dAr As Double
            Dim Alpha, Beta As Double
            Dim k, I, J, L As Integer
            Dim AR, TR, Ti, AI As Double

            ' Nbr est la taille de la suite à transformer 
            Nbr = UBound(Suite) + 1 ' Obligatoirement: une puissance de 2 
            P2 = Puissance(Nbr)

            Dim Transformée(Nbr - 1) As NombreComplex

            For ISuit = 0 To Nbr - 1
                ITrans = Ivrs(ISuit, P2)
                Transformée(ITrans).Réel = Suite(ISuit).Réel
                Transformée(ITrans).Imaginaire = Suite(ISuit).Imaginaire
            Next ISuit

            Fin = 1
            Taille = 2
            Do While (Taille <= Nbr)
                dAngl = 2 * Math.PI / Taille
                Alpha = 2 * ((System.Math.Sin(0.5 * dAngl)) ^ 2)
                Beta = System.Math.Sin(dAngl)

                I = 0
                Do While (I < Nbr)
                    AR = 1 ' Cosinus(0) 
                    AI = 0 ' Sinus(0) 

                    J = I
                    For L = 1 To Fin
                        k = J + Fin
                        TR = AR * Transformée(k).Réel - AI * Transformée(k).Imaginaire
                        Ti = AI * Transformée(k).Réel + AR * Transformée(k).Imaginaire
                        Transformée(k).Réel = Transformée(J).Réel - TR
                        Transformée(k).Imaginaire = Transformée(J).Imaginaire - Ti
                        Transformée(J).Réel = Transformée(J).Réel + TR
                        Transformée(J).Imaginaire = Transformée(J).Imaginaire + Ti
                        dAr = Alpha * AR + Beta * AI
                        AI = AI - (Alpha * AI - Beta * AR)
                        AR = AR - dAr
                        J = J + 1
                    Next L
                    I = I + Taille
                Loop
                Fin = Taille
                Taille = Taille * 2
            Loop
            Return Transformée
        End Function

        Public Function Inverse(ByVal Transformée() As NombreComplex) As NombreComplex()

            'Calcule la transformée inverse d'une suite de nombres complexes 

            Dim Nbr As Integer ' Nombre de complexes dans la suite 
            Dim P2 As Byte ' Puissance de 2 du nombre entier Nbr 
            Dim ISuit, ITrans As Integer ' Compteurs de boucles 
            Dim Fin, Taille As Integer ' Butées 
            Dim dAngl, dAr As Double
            Dim Alpha, Beta As Double
            Dim k, I, J, L As Integer
            Dim AR, TR, Ti, AI As Double

            ' Nbr est la taille de la Transformée à transformer 
            Nbr = UBound(Transformée) + 1 ' Obligatoirement: une puissance de 2 
            P2 = Puissance(Nbr)

            Dim Suite(Nbr - 1) As NombreComplex

            For ISuit = 0 To Nbr - 1
                ITrans = Ivrs(ISuit, P2)
                Suite(ITrans).Réel = Transformée(ISuit).Réel
                Suite(ITrans).Imaginaire = Transformée(ISuit).Imaginaire
            Next ISuit

            Fin = 1
            Taille = 2
            Do While (Taille <= Nbr)
                dAngl = -2 * Math.PI / Taille
                Alpha = 2 * ((System.Math.Sin(0.5 * dAngl)) ^ 2)
                Beta = System.Math.Sin(dAngl)

                I = 0
                Do While (I < Nbr)
                    AR = 1 ' Cosinus(0) 
                    AI = 0 ' Sinus(0) 

                    J = I
                    For L = 1 To Fin
                        k = J + Fin
                        TR = AR * Suite(k).Réel - AI * Suite(k).Imaginaire
                        Ti = AI * Suite(k).Réel + AR * Suite(k).Imaginaire
                        Suite(k).Réel = Suite(J).Réel - TR
                        Suite(k).Imaginaire = Suite(J).Imaginaire - Ti
                        Suite(J).Réel = Suite(J).Réel + TR
                        Suite(J).Imaginaire = Suite(J).Imaginaire + Ti
                        dAr = Alpha * AR + Beta * AI
                        AI = AI - (Alpha * AI - Beta * AR)
                        AR = AR - dAr
                        J = J + 1
                    Next L
                    I = I + Taille
                Loop
                Fin = Taille
                Taille = Taille * 2
            Loop
            For I = 0 To UBound(Suite)
                Suite(I).Réel = 2 * Suite(I).Réel \ Nbr
                Suite(I).Imaginaire = 2 * Suite(I).Imaginaire \ Nbr
            Next I
            Return Suite
        End Function

        Public Function Magnitude(ByVal Transformée() As NombreComplex) As Double()
            Dim Id As Integer
            Id = UBound(Transformée) \ 2
            Dim M(Id) As Double
            For Id = 0 To UBound(M)
                M(Id) = Math.Sqrt(Math.Pow(Transformée(Id).Réel, 2) + Math.Pow(Transformée(Id).Imaginaire, 2)) / 2
            Next
            Return M
        End Function

        Public Function Ivrs(ByVal IdPuls As Integer, ByVal P2 As Byte) As Integer
            Dim I As Short
            Ivrs = 0

            For I = 1 To P2
                Ivrs = (Ivrs * 2) Or (IdPuls And 1)
                IdPuls = IdPuls \ 2
            Next I

        End Function

        Public Function Puissance(ByVal Entier As Integer) As Byte
            Return Int(Math.Log(Entier) / Math.Log(2))
        End Function

    End Module
