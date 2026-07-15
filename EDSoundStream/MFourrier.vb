
    Public Class Fourier2

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
        Private Const PI2 As Double = 2 * Math.PI

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
                dAngl = PI2 / Taille
                Alpha = 2 * (System.Math.Sin(dAngl / 2) ^ 2)
                Beta = System.Math.Sin(dAngl)

                I = 0
                Do While (I < Nbr)
                    AR = 1.0# ' Cosinus(0)
                    AI = 0.0# ' Sinus(0)

                    J = I
                    For L = 1 To Fin
                        k = J + Fin
                        TR = AR * Transformée(k).Réel - AI * Transformée(k).Imaginaire
                        Ti = AI * Transformée(k).Réel + AR * Transformée(k).Imaginaire
                        Transformée(k).Réel = Transformée(J).Réel - TR
                        Transformée(k).Imaginaire = Transformée(J).Imaginaire - Ti
                        Transformée(J).Réel += TR
                        Transformée(J).Imaginaire += Ti
                        dAr = Alpha * AR + Beta * AI
                        AI -= (Alpha * AI - Beta * AR)
                        AR -= dAr
                        J += 1
                    Next L
                    I += Taille
                Loop
                Fin = Taille
                Taille *= 2
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
            Dim I As Byte
            Ivrs = 0

            For I = 1 To P2
                Ivrs = (Ivrs * 2) Or (IdPuls And 1)
                IdPuls \= 2
            Next I

        End Function

        Public Function Puissance(ByVal Entier As Integer) As Byte
            Return CByte(Math.Log(Entier) / Math.Log(2))
        End Function

    End Class
