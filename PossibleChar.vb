'PossibleChar.vb

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
    
                            'constants for checkIfPossibleChar, this checks one possible char only (does not compare to another char)
    Const MIN_PIXEL_WIDTH As Long = 2
    Const MIN_PIXEL_HEIGHT As Long = 8

    Const MIN_ASPECT_RATIO As Double = 0.25
    Const MAX_ASPECT_RATIO As Double = 1.0

    Const MIN_PIXEL_AREA As Long = 20

                            'constants for comparing two chars
    Const MIN_DIAG_SIZE_MULTIPLE_AWAY As Double = 0.3
    Const MAX_DIAG_SIZE_MULTIPLE_AWAY As Double = 5.0

    Const MAX_CHANGE_IN_AREA As Double = 0.5

    Const MAX_CHANGE_IN_WIDTH As Double = 0.8
    Const MAX_CHANGE_IN_HEIGHT As Double = 0.2

    Const MAX_ANGLE_TO As Double = 12.0

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

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function checkIfPossibleChar() As Boolean
        If (boundingRect.Width > MIN_PIXEL_WIDTH And boundingRect.Height > MIN_PIXEL_HEIGHT And _
            MIN_ASPECT_RATIO < dblAspectRatio And dblAspectRatio < MAX_ASPECT_RATIO And _
            lngArea > MIN_PIXEL_AREA) Then
            Return True
        Else
            Return False
        End If
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function distanceTo(otherPossibleChar As PossibleChar) As Double
        Dim lngX As Long = Math.Abs(Me.lngCenterX - otherPossibleChar.lngCenterX)
        Dim lngY As Long = Math.Abs(Me.lngCenterY - otherPossibleChar.lngCenterY)

        Return Math.Sqrt((lngX ^ 2) + (lngY ^ 2))
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function angleTo(otherPossibleChar As PossibleChar) As Double
        Dim dblAdj As Double = CDbl(Math.Abs(Me.lngCenterX - otherPossibleChar.lngCenterX))
        Dim dblOpp As Double = CDbl(Math.Abs(Me.lngCenterY - otherPossibleChar.lngCenterY))

        Dim dblAngleInRad As Double = Math.Atan(dblOpp / dblAdj) 
        
        Dim dblAngleInDeg As Double = dblAngleInRad * (180.0 / Math.PI)

        Return dblAngleInDeg
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfMatchingChars(listOfChars As List(Of PossibleChar)) As List(Of PossibleChar)
        Dim listOfMatchingChars As List(Of PossibleChar) = New List(Of PossibleChar)

        For Each possibleChar As PossibleChar In listOfChars

            If (possibleChar.Equals(Me)) Then
                Continue For
            End If

            Dim dblDistanceToOtherChar As Double = distanceTo(possibleChar)
            
            Dim dblChangeInArea As Double = Math.Abs(possibleChar.lngArea - Me.lngArea) / Me.lngArea

            Dim dblChangeInWidth As Double = Math.Abs(possibleChar.boundingRect.Width - Me.boundingRect.Width) / Me.boundingRect.Width
            Dim dblChangeInHeight As Double = Math.Abs(possibleChar.boundingRect.Height - Me.boundingRect.Height) / Me.boundingRect.Height

            If (dblDistanceToOtherChar > (Me.dblDiagonalSize * MIN_DIAG_SIZE_MULTIPLE_AWAY) And _
                dblDistanceToOtherChar < (Me.dblDiagonalSize * MAX_DIAG_SIZE_MULTIPLE_AWAY) And _
                dblChangeInArea < MAX_CHANGE_IN_AREA And _
                dblChangeInWidth < MAX_CHANGE_IN_WIDTH And dblChangeInHeight < MAX_CHANGE_IN_HEIGHT And _
                Me.angleTo(possibleChar) < MAX_ANGLE_TO) Then

                listOfMatchingChars.Add(possibleChar)
            End If

        Next

        Return listOfMatchingChars
    End Function

End Class



