'PossibleChar.vb
'
'Emgu CV 2.4.10

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports System.Math

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class PossibleChar

    ' member variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Public contour As Contour(Of Point)

    Public boundingRect As Rectangle

    Public lngCenterX As Long
    Public lngCenterY As Long

    Public dblDiagonalSize As Double
    Public dblAspectRatio As Double
    Public lngArea As Long

    ' constructor '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub New(_contour As Contour(Of Point))
        contour = _contour

        boundingRect = contour.BoundingRectangle()

        lngCenterX = CLng((boundingRect.Left + boundingRect.Right) / 2)
        lngCenterY = CLng((boundingRect.Top + boundingRect.Bottom) / 2)

        dblDiagonalSize = Math.Sqrt((boundingRect.Width ^ 2) + (boundingRect.Height ^ 2))

        dblAspectRatio = CDbl(boundingRect.Width) / CDbl(boundingRect.Height)

        lngArea = boundingRect.Width * boundingRect.Height
    End Sub

End Class

