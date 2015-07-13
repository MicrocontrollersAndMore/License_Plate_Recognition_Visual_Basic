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
    Const MIN_PIXELS As Long = 8             'contour filter criteria
    Const MIN_WIDTH As Long = 2
    Const MIN_HEIGHT As Long = 8
    Const MAX_WIDTH As Long = 50
    Const MAX_HEIGHT As Long = 50
    Const MIN_ASPECT As Double = 0.1
    Const MAX_ASPECT As Double = 3.0
    Const MIN_AREA As Long = 20
    Const MIN_DENSITY As Double = 0.01

    Const MAX_HEIGHT_DIFF As Long = 8        'similarity criteria
    Const MAX_WIDTH_DIFF As Long = 8
    Const MAX_AVG_FACTOR As Double = 0.2
    Const MAX_SDV_FACTOR As Double = 0.2

    Const MIN_DIST_FACTOR As Double = 0.3       'match criteria
    Const MAX_DIST_FACTOR As Double = 10.0
    Const MAX_ANGLE As Double = Math.PI / 4

    Dim contour As Contour(Of Point)
    Dim boundingRect As Rectangle

    Dim lngCenterX As Long
    Dim lngCenterY As Long

    Dim dblDiameter As Double
    Dim dblAspectRatio As Double
    Dim lngArea As Long
    Dim dblDensity As Double
     
    Dim dblAvg As Double
    Dim dblStdDev As Double

    ' constructor '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub New(_contour As Contour(Of Point))
        contour = _contour
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function checkIfValid() As Boolean
        If (contour.Area >= MIN_PIXELS) Then
            Return False
        End If

        boundingRect = contour.BoundingRectangle()

        If (boundingRect.Width < MIN_WIDTH Or boundingRect.Width > MAX_WIDTH) Then
            Return False
        End If

        If (boundingRect.Height < MIN_HEIGHT Or boundingRect.Height > MAX_HEIGHT) Then
            Return False
        End If

        lngCenterX = (boundingRect.Left + boundingRect.Right) / 2
        lngCenterY = (boundingRect.Top + boundingRect.Bottom) / 2

        dblDiameter = Math.Sqrt((boundingRect.Width ^ 2) + (boundingRect.Height ^ 2))

        dblAspectRatio = CDbl(boundingRect.Width) / CDbl(boundingRect.Height)
        
        If (dblAspectRatio < MIN_ASPECT Or dblAspectRatio > MAX_ASPECT) Then
            Return False
        End If

        lngArea = boundingRect.Width * boundingRect.Height

        If (lngArea < MIN_AREA) Then
            Return False
        End If

        dblDensity = CDbl(contour.Count) / CDbl(lngArea)

        If (dblDensity < MIN_DENSITY) Then
            Return False
        End If

        Return True
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub calcAvgAndStdDev(imgGrayscale As Image(Of Gray, Byte))
        CvInvoke.cvSetImageROI(imgGrayscale, boundingRect)

        Dim average As Gray
        Dim standardDeviation As MCvScalar

        imgGrayscale.AvgSdv(average, standardDeviation)

        dblAvg = CDbl(average.Intensity)
        dblStdDev = CDbl(standardDeviation.v0)

        CvInvoke.cvResetImageROI(imgGrayscale)
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function distanceTo(otherPossibleChar As PossibleChar) As Double

        Dim lngX As Long = lngCenterX - otherPossibleChar.lngCenterX
        Dim lngY As Long = lngCenterY - otherPossibleChar.lngCenterY

        Return Math.Sqrt((lngX ^ 2) + (lngY ^ 2))
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function gradientTo(otherPossibleChar As PossibleChar) As Double
        Dim greaterXPossibleChar As PossibleChar
        Dim lesserXPossibleChar As PossibleChar

        If (lngCenterX > otherPossibleChar.lngCenterX) Then
            greaterXPossibleChar = Me
            lesserXPossibleChar = otherPossibleChar
        Else
            greaterXPossibleChar = otherPossibleChar
            lesserXPossibleChar = Me
        End If

        Dim dblXDifference As Double = CDbl(greaterXPossibleChar.lngCenterX - lesserXPossibleChar.lngCenterX)
        Dim dblYDifference As Double = CDbl(greaterXPossibleChar.lngCenterY - lesserXPossibleChar.lngCenterY)

        Return dblYDifference / dblXDifference
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function angleTo(otherPossibleChar As PossibleChar) As Double
        Dim dblAdj As Double = CDbl(Math.Abs(lngCenterX - otherPossibleChar.lngCenterX))
        Dim dblOpp As Double = CDbl(Math.Abs(lngCenterY - otherPossibleChar.lngCenterY))
        Dim dblHyp As Double = CDbl(Math.Sqrt((dblAdj ^ 2) + (dblOpp ^ 2)))

        Return Math.Asin(dblOpp / dblHyp)
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPotentialMatches(listOfPossibleChars As List(Of PossibleChar)) As List(Of MatchingPossibleCharAndDistanceTo)

        Dim listOfMatchingPossibleCharsAndDistancesTo As List(Of MatchingPossibleCharAndDistanceTo) = Nothing

        For Each otherPossibleChar As PossibleChar In listOfPossibleChars
            If (otherPossibleChar IsNot Me) Then
                
                Dim dblDistanceTo As Double = distanceTo(otherPossibleChar)

                If (dblDistanceTo > (Me.dblDiameter * MIN_DIST_FACTOR) And _
                    dblDistanceTo < (Me.dblDiameter * MAX_DIST_FACTOR) And _
                    Math.Abs(Me.boundingRect.Width - otherPossibleChar.boundingRect.Width) < MAX_WIDTH_DIFF And _
                    Math.Abs(Me.boundingRect.Height - otherPossibleChar.boundingRect.Height) < MAX_HEIGHT_DIFF And _
                    ((Me.dblAvg - otherPossibleChar.dblAvg) / Me.dblAvg) < MAX_AVG_FACTOR And _
                    ((Me.dblStdDev - otherPossibleChar.dblStdDev) / Me.dblStdDev) < MAX_SDV_FACTOR And _
                    Me.angleTo(otherPossibleChar) < MAX_ANGLE) Then
                                                                'if all the above are true, then self and otherPossibleChar are a "match",
                                                                'so add it to our list, also add distance between self and otherAugmentedContour
                    Dim matchingPossibleCharAndDistanceTo As MatchingPossibleCharAndDistanceTo
                    matchingPossibleCharAndDistanceTo.possibleChar = otherPossibleChar
                    matchingPossibleCharAndDistanceTo.dblDistanceTo = dblDistanceTo
                    listOfMatchingPossibleCharsAndDistancesTo.Add(matchingPossibleCharAndDistanceTo)
                End If
            End If
        Next

        Return listOfMatchingPossibleCharsAndDistancesTo
    End Function

End Class
























