'PossiblePlateStruct.vb

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class PossiblePlate

    ' member variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Public imgPlate As Image(Of Bgr, Byte)
    Public imgGrayscale As Image(Of Gray, Byte)
    Public imgThresh As Image(Of Gray, Byte)

    Public b2dLocationOfPlateInScene As MCvBox2D

    Public strChars As String

    ' constructor '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub New
                                'initialize values
        imgPlate = Nothing
        imgGrayscale = Nothing
        imgThresh = Nothing

        strChars = ""
    End Sub

End Class




