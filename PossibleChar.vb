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
    
                            'constants for checkIfValid, this checks one possible char only (does not compare to another char)
    Const MIN_NUM_OF_COUTOUR_POINTS As Long = 5

    Const MIN_PIXEL_WIDTH As Long = 2
    Const MIN_PIXEL_HEIGHT As Long = 8

    Const MIN_ASPECT_RATIO As Double = 0.25
    Const MAX_ASPECT_RATIO As Double = 1.0

    Const MIN_PIXEL_AREA As Long = 20

                            'constants for comparing two chars
    Const MAX_CHANGE_IN_WIDTH As Double = 0.8
    Const MAX_CHANGE_IN_HEIGHT As Double = 0.2

    Const MAX_CHANGE_IN_AVG As Double = 1.0
    Const MAX_CHANGE_IN_SDV As Double = 0.2

    Const MIN_DIST_FACTOR As Double = 0.3
    Const MAX_DIST_FACTOR As Double = 5.0           '5.0

    Const MAX_ANGLE As Double = 20.0                'try a lower # here ??

    Const MAX_GRADIENT_DIFF As Double = 0.1

    Public contour As Contour(Of Point)

    Public boundingRect As Rectangle

    Public lngCenterX As Long
    Public lngCenterY As Long

    Public dblDiagonalSize As Double
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
    Function checkIfValidAndPopulateData(imgGrayscale As Image(Of Gray, Byte)) As Boolean
        
        If (contour.Count < MIN_NUM_OF_COUTOUR_POINTS) Then
            Return False
        End If

        boundingRect = contour.BoundingRectangle()

        If (boundingRect.Width < MIN_PIXEL_WIDTH) Then' Or boundingRect.Width > MAX_PIXEL_WIDTH) Then
            Return False
        End If

        If (boundingRect.Height < MIN_PIXEL_HEIGHT) Then' Or boundingRect.Height > MAX_PIXEL_HEIGHT) Then
            Return False
        End If

        lngCenterX = CLng((boundingRect.Left + boundingRect.Right) / 2)
        lngCenterY = CLng((boundingRect.Top + boundingRect.Bottom) / 2)

        dblDiagonalSize = Math.Sqrt((boundingRect.Width ^ 2) + (boundingRect.Height ^ 2))

        dblAspectRatio = CDbl(boundingRect.Width) / CDbl(boundingRect.Height)

        If (dblAspectRatio < MIN_ASPECT_RATIO Or dblAspectRatio > MAX_ASPECT_RATIO) Then
            Return False
        End If

        lngArea = boundingRect.Width * boundingRect.Height

        If (lngArea < MIN_PIXEL_AREA) Then
            Return False
        End If
                        'if we get here, we consider the char valid (initially, anyhow, we will compare to other chars later . . .)

        CvInvoke.cvSetImageROI(imgGrayscale, boundingRect)      'temporarily set ROI to current char

        Dim average As Gray
        Dim standardDeviation As MCvScalar

        imgGrayscale.AvgSdv(average, standardDeviation)         'get avg and std dev for ROI

        dblAvg = CDbl(average.Intensity)                        'convert avg and std dev to doubles
        dblStdDev = CDbl(standardDeviation.v0)                  '

        CvInvoke.cvResetImageROI(imgGrayscale)                  'reset ROI

        Return True                 'return that the contour is valid
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
        Dim dblHyp As Double = CDbl(Math.Sqrt((dblAdj ^ 2) + (dblOpp ^ 2)))                     'why is hyp calculated?  why not just use opp & adj ??
        
        Dim dblAngleInRad As Double = Math.Asin(dblOpp / dblHyp) 

        Dim dblAngleInDeg As Double = dblAngleInRad * (180.0 / 3.14159)

        Return dblAngleInDeg
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function gradientTo(otherPossibleChar As PossibleChar) As Double
        Dim greaterXPossibleChar As PossibleChar
        Dim lesserXPossibleChar As PossibleChar

        If (Me.lngCenterX > otherPossibleChar.lngCenterX) Then
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
    Function findListOfMatchingChars(listOfChars As List(Of PossibleChar)) As List(Of PossibleChar)
        Dim listOfMatchingChars As List(Of PossibleChar) = New List(Of PossibleChar)

        For Each possibleChar As PossibleChar In listOfChars

            If (possibleChar.Equals(Me)) Then
                Continue For
            End If

            Dim dblDistanceTo As Double = distanceTo(possibleChar)
            
            Dim dblChangeInWidth As Double = Math.Abs(possibleChar.boundingRect.Width - Me.boundingRect.Width) / Me.boundingRect.Width
            Dim dblChangeInHeight As Double = Math.Abs(possibleChar.boundingRect.Height - Me.boundingRect.Height) / Me.boundingRect.Height

            Dim dblChangeInAvg As Double = Math.Abs((possibleChar.dblAvg - Me.dblAvg) / Me.dblAvg)
            Dim dblChangeInStdDev As Double = Math.Abs((possibleChar.dblStdDev - Me.dblStdDev) / Me.dblStdDev)

            Dim dblGradientTo As Double = Math.Abs(gradientTo(possibleChar))

            If ((Me.dblDiagonalSize * MIN_DIST_FACTOR) < dblDistanceTo And dblDistanceTo < (Me.dblDiagonalSize * MAX_DIST_FACTOR) And _
                dblChangeInWidth < MAX_CHANGE_IN_WIDTH And dblChangeInHeight < MAX_CHANGE_IN_HEIGHT And _
                Me.angleTo(possibleChar) < MAX_ANGLE And _                
                dblChangeInAvg < MAX_CHANGE_IN_AVG And dblChangeInStdDev < MAX_CHANGE_IN_SDV And _
                dblGradientTo < MAX_GRADIENT_DIFF) Then

                listOfMatchingChars.Add(possibleChar)
            End If

        Next

        Return listOfMatchingChars
    End Function

End Class



