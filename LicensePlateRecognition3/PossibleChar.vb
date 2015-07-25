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
    Const MIN_NUM_OF_COUTOUR_POINTS As Long = 5         '8
    Const MIN_WIDTH As Long = 2
    Const MIN_HEIGHT As Long = 8
    Const MAX_WIDTH As Long = 50
    Const MAX_HEIGHT As Long = 50
    Const MIN_ASPECT As Double = 0.25                '0.1
    Const MAX_ASPECT As Double = 1.0                '3.0
    Const MIN_AREA As Long = 20
    Const MIN_DENSITY As Double = 0.01

    Const MAX_HEIGHT_DIFF As Long = 8
    Const MAX_WIDTH_DIFF As Long = 8
    Const MAX_AVG_FACTOR As Double = 1.0
    Const MAX_SDV_FACTOR As Double = 0.2

    Const MIN_DIST_FACTOR As Double = 0.3           '0.3
    Const MAX_DIST_FACTOR As Double = 3.0          '10.0
    Const MAX_ANGLE As Double = 15.0         'Math.PI / 4

    Const MAX_GRADIENT_DIFF As Double = 0.1

    Public contour As Contour(Of Point)
    Public boundingRect As Rectangle

    Public lngCenterX As Long
    Public lngCenterY As Long

    Public dblDiameter As Double
    Public dblAspectRatio As Double
    Public lngArea As Long
    Public dblDensity As Double
     
    Public dblAvg As Double
    Public dblStdDev As Double

    ' constructor '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub New(_contour As Contour(Of Point))
        contour = _contour
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function checkIfValid() As Boolean
        
        'Debug.Print("--------------------------------")

        'intNumTimesInCheckIfValid = intNumTimesInCheckIfValid + 1
        'Debug.Print("intNumTimesInCheckIfValid = " + intNumTimesInCheckIfValid.ToString)

        If (contour.Count < MIN_NUM_OF_COUTOUR_POINTS) Then
            Return False
        End If
        
        'intNumTimesPastFirstIf = intNumTimesPastFirstIf + 1
        'Debug.Print("intNumTimesPastFirstIf = " + intNumTimesPastFirstIf.ToString)

        boundingRect = contour.BoundingRectangle()

        If (boundingRect.Width < MIN_WIDTH Or boundingRect.Width > MAX_WIDTH) Then
            Return False
        End If

        If (boundingRect.Height < MIN_HEIGHT Or boundingRect.Height > MAX_HEIGHT) Then
            Return False
        End If

        'intNumTimesPastWidthAndHeight = intNumTimesPastWidthAndHeight + 1
        'Debug.Print("intNumTimesPastWidthAndHeight = " + intNumTimesPastWidthAndHeight.ToString)

        lngCenterX = CLng((boundingRect.Left + boundingRect.Right) / 2)
        lngCenterY = CLng((boundingRect.Top + boundingRect.Bottom) / 2)

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
        Dim dblAdj As Double = CDbl(Math.Abs(Me.lngCenterX - otherPossibleChar.lngCenterX))
        Dim dblOpp As Double = CDbl(Math.Abs(Me.lngCenterY - otherPossibleChar.lngCenterY))
        Dim dblHyp As Double = CDbl(Math.Sqrt((dblAdj ^ 2) + (dblOpp ^ 2)))

        'Dim dblAngleInRads As Double = Math.Atan(dblOpp / dblAdj)

        Dim dblAngleInRad As Double = Math.Asin(dblOpp / dblHyp) 

        Dim dblAngleInDeg As Double = dblAngleInRad * (180.0 / 3.14159)

        Return dblAngleInDeg
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    'Function findPotentialMatches(listOfPossibleChars As List(Of PossibleChar)) As List(Of MatchingPossibleCharAndDistanceTo)

    '    Dim listOfMatchingPossibleCharsAndDistancesTo As List(Of MatchingPossibleCharAndDistanceTo) = Nothing

    '    For Each otherPossibleChar As PossibleChar In listOfPossibleChars
    '        If (otherPossibleChar IsNot Me) Then
                
    '            Dim dblDistanceTo As Double = distanceTo(otherPossibleChar)

    '            If (dblDistanceTo > (Me.dblDiameter * MIN_DIST_FACTOR) And _
    '                dblDistanceTo < (Me.dblDiameter * MAX_DIST_FACTOR) And _
    '                Math.Abs(Me.boundingRect.Width - otherPossibleChar.boundingRect.Width) < MAX_WIDTH_DIFF And _
    '                Math.Abs(Me.boundingRect.Height - otherPossibleChar.boundingRect.Height) < MAX_HEIGHT_DIFF And _
    '                ((Me.dblAvg - otherPossibleChar.dblAvg) / Me.dblAvg) < MAX_AVG_FACTOR And _
    '                ((Me.dblStdDev - otherPossibleChar.dblStdDev) / Me.dblStdDev) < MAX_SDV_FACTOR And _
    '                Me.angleTo(otherPossibleChar) < MAX_ANGLE) Then
    '                                                            'if all the above are true, then self and otherPossibleChar are a "match",
    '                                                            'so add it to our list, also add distance between self and otherAugmentedContour
    '                Dim matchingPossibleCharAndDistanceTo As MatchingPossibleCharAndDistanceTo
    '                matchingPossibleCharAndDistanceTo.possibleChar = otherPossibleChar
    '                matchingPossibleCharAndDistanceTo.dblDistanceTo = dblDistanceTo
    '                listOfMatchingPossibleCharsAndDistancesTo.Add(matchingPossibleCharAndDistanceTo)
    '            End If
    '        End If
    '    Next

    '    Return listOfMatchingPossibleCharsAndDistancesTo
    'End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfMatchingChars(listOfChars As List(Of PossibleChar)) As List(Of PossibleChar)
        Dim listOfMatchingChars As List(Of PossibleChar) = New List(Of PossibleChar)

        For Each possibleChar As PossibleChar In listOfChars
            Dim dblDistanceTo As Double = distanceTo(possibleChar)
            Dim dblGradientTo As Double = Math.Abs(gradientTo(possibleChar))

            Dim dblChangeInAvg As Double = Math.Abs((Me.dblAvg - possibleChar.dblAvg) / Me.dblAvg)
            Dim dblChangeInStdDev As Double = Math.Abs((Me.dblStdDev - possibleChar.dblStdDev) / Me.dblStdDev)

            If (dblDistanceTo > (Me.dblDiameter * MIN_DIST_FACTOR) And dblDistanceTo < (Me.dblDiameter * MAX_DIST_FACTOR) And _
                Math.Abs(Me.boundingRect.Width - possibleChar.boundingRect.Width) < MAX_WIDTH_DIFF And Math.Abs(Me.boundingRect.Height - possibleChar.boundingRect.Height) < MAX_HEIGHT_DIFF And _
                dblChangeInAvg < MAX_AVG_FACTOR And dblChangeInStdDev < MAX_SDV_FACTOR And _
                Me.angleTo(possibleChar) < MAX_ANGLE And _
                dblGradientTo < MAX_GRADIENT_DIFF) Then

                listOfMatchingChars.Add(possibleChar)

                If (listOfMatchingChars.Count >= 3) Then
                    Dim dummy1 As Integer
                    dummy1 = dummy1 + 1
                End If

            End If
        Next

        Return listOfMatchingChars
    End Function

End Class
























