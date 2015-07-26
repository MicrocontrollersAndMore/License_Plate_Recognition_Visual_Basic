'DetectPlates.vb

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module DetectPlates

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectPlates(imgOriginal As Image(Of Bgr, Byte)) As List(Of PossiblePlate)
        Dim imgGrayscale As Image(Of Gray, Byte) = Nothing
        Dim imgThresh As Image(Of Gray, Byte) = Nothing

        Preprocess.preprocess(imgOriginal, imgGrayscale, imgThresh)

        If (frmMain.ckbShowSteps.Checked = True) Then
            CvInvoke.cvShowImage("just after preprocess of entire image, imgGrayscale", imgGrayscale)
            CvInvoke.cvShowImage("just after preprocess of entire image, imgThresh", imgThresh)
        End If

        Dim listOfPossibleChars As List(Of PossibleChar) = findPossibleChars(imgGrayscale, imgThresh)

        If (frmMain.ckbShowSteps.Checked = True) Then
            Dim imgContours As Image(Of Gray, Byte) = New Image(Of Gray, Byte)(imgOriginal.Size())

            For Each possibleChar As PossibleChar In listOfPossibleChars
                CvInvoke.cvDrawContours(imgContours, possibleChar.contour, New MCvScalar(255), New MCvScalar(255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
            Next
            CvInvoke.cvShowImage("just after findPossibleChars, contours are:", imgContours)
        End If

        Dim listOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleChars)

        Dim listOfPossiblePlates As List(Of PossiblePlate) = New List(Of PossiblePlate)

        For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingChars
            Dim possiblePlate As PossiblePlate = New PossiblePlate

            possiblePlate.imgPlate = extractPlate(imgOriginal, listOfMatchingChars)

            If (Not possiblePlate.imgPlate Is Nothing) Then
                listOfPossiblePlates.Add(possiblePlate)
            End If
        Next
        
        Return listOfPossiblePlates
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function extractPlate(imgOriginal As Image(Of Bgr, Byte), listOfMatchingChars As List(Of PossibleChar)) As Image(Of Bgr, Byte)
        listOfMatchingChars.Sort(Function(firstChar, secondChar) firstChar.lngCenterX.CompareTo(secondChar.lngCenterX))

        Dim dblOpposite = CDbl(listOfMatchingChars(listOfMatchingChars.Count - 1).lngCenterY - listOfMatchingChars(0).lngCenterY)
        Dim dblHypotenuse = CDbl(listOfMatchingChars(0).distanceTo(listOfMatchingChars(listOfMatchingChars.Count - 1)))

        Dim dblAngle = Math.Asin(dblOpposite / dblHypotenuse)

        Dim matrix As Matrix(Of Single) = New RotationMatrix2D(Of Single)

        Dim sngCenterX As Single = CSng(CSng(listOfMatchingChars(0).lngCenterX + listOfMatchingChars(listOfMatchingChars.Count - 1).lngCenterX) / 2.0)
        Dim sngCenterY As Single = CSng(CSng(listOfMatchingChars(0).lngCenterY + listOfMatchingChars(listOfMatchingChars.Count - 1).lngCenterY) / 2.0)

        Dim ptfCenter As PointF = New PointF(sngCenterX, sngCenterY)

        CvInvoke.cv2DRotationMatrix(ptfCenter, dblAngle * 180.0 / Math.PI, 1.0, matrix)

        Dim imgAngleCorrected As Image(Of Bgr, Byte)

        imgAngleCorrected = imgOriginal.WarpAffine(matrix, INTER.CV_INTER_LINEAR, WARP.CV_WARP_DEFAULT, New Bgr(0, 0, 0))

        Dim imgPlate As Image(Of Bgr, Byte) = New Image(Of Bgr, Byte)(CInt(dblHypotenuse + (listOfMatchingChars(0).dblDiagonalSize * 3.0)), CInt(listOfMatchingChars(0).dblDiagonalSize * 1.5))

        CvInvoke.cvGetRectSubPix(imgAngleCorrected, imgPlate, New PointF(sngCenterX, sngCenterY))

        Return imgPlate
    End Function

End Module


