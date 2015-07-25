'Detect.vb

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module DetectPlates

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const MAX_CHAR_DIST_FACTOR As Double = 1.5
    Const MAX_CHAR_GRADIENT_DIFF As Double = 0.1
    Const MIN_NUMBER_OF_MATCHING_CHARS As Integer = 3

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectPlates(imgOriginal As Image(Of Bgr, Byte)) As List(Of Image(Of Bgr, Byte))
        Dim imgGrayscale As Image(Of Gray, Byte) = Nothing
        Dim imgThresh As Image(Of Gray, Byte) = Nothing

        Preprocess.preprocess(imgOriginal, imgGrayscale, imgThresh)
        
        'CvInvoke.cvShowImage("imgOriginal", imgOriginal)
        'CvInvoke.cvShowImage("imgGrayscale", imgGrayscale)
        'CvInvoke.cvShowImage("imgThresh", imgThresh)
        'CvInvoke.cvWaitKey(0)

        '-----------------------------------------------

        Dim listOfPossibleChars As List(Of PossibleChar) = findPossibleChars(imgGrayscale, imgThresh)

        'frmMain.txtInfo.Text = frmMain.txtInfo.Text + vbCrLf + "len of listOfPossibleChars = " + listOfPossibleChars.Count.ToString + vbCrLf   '289

        'Dim imgContours As Image(Of Gray, Byte) = New Image(Of Gray, Byte)(imgOriginal.Size)

        'For Each possibleChar As PossibleChar In listOfPossibleChars
        '    CvInvoke.cvDrawContours(imgContours, possibleChar.contour, New MCvScalar(255), New MCvScalar(255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
        'Next

        'CvInvoke.cvShowImage("imgContours", imgContours)
        'CvInvoke.cvWaitKey(0)

        '-----------------------------------------------

        Dim listOfListOfMatchingChars As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleChars)

        'frmMain.txtInfo.Text = frmMain.txtInfo.Text + vbCrLf + "len of listOfListOfMatchingChars = " + listOfListOfMatchingChars.Count.ToString + vbCrLf
        
        Dim imgListOfPlates As List(Of Image(Of Bgr, Byte)) = New List(Of Image(Of Bgr, Byte))

        For Each listOfMatchingChars As List(Of PossibleChar) In listOfListOfMatchingChars
            Dim imgPlate As Image(Of Bgr, Byte) = extractPlate(imgOriginal, listOfMatchingChars)

            If (Not imgPlate Is Nothing) Then
                imgListOfPlates.Add(imgPlate)
            End If
        Next
        
        Return imgListOfPlates
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleChars(imgGrayscale As Image(Of Gray, Byte), imgThresh As Image(Of Gray, Byte)) As List(Of PossibleChar)
        Dim contours As Contour(Of Point) = imgThresh.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST)

        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)()

        Dim intCountOfPossibleChars As Integer = 0
        Dim intCountOfValidPossibleChars As Integer = 0

        While (Not contours Is Nothing)

            intCountOfPossibleChars = intCountOfPossibleChars + 1

            Dim contour As Contour(Of Point) = contours.ApproxPoly(contours.Perimeter * 0.0001)

            Dim possibleChar As PossibleChar = New PossibleChar(contour)

            If (possibleChar.checkIfValid()) Then

                intCountOfValidPossibleChars = intCountOfValidPossibleChars + 1

                possibleChar.calcAvgAndStdDev(imgGrayscale)
                listOfPossibleChars.Add(possibleChar)
            End If

            contours = contours.HNext
        End While

        'intCountOfPossibleChars = 2115
        'intCountOfValidPossibleChars = 289

        Return listOfPossibleChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfListsOfMatchingChars(listOfPossibleChars As List(Of PossibleChar)) As List(Of List(Of PossibleChar))

        intNumTimesInFindListOfListsOfMatchingChars = intNumTimesInFindListOfListsOfMatchingChars + 1
        Debug.Print("entering recurive call, intNumTimesInFindListOfListsOfMatchingChars = " + intNumTimesInFindListOfListsOfMatchingChars.ToString)
        
        Dim listOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))

        For Each possibleChar As PossibleChar In listOfPossibleChars

            Dim listOfMatchingChars As List(Of PossibleChar) = possibleChar.findListOfMatchingChars(listOfPossibleChars)

            If (listOfMatchingChars Is Nothing) Then
                Continue For
            End If

            If (listOfMatchingChars.Count < MIN_NUMBER_OF_MATCHING_CHARS) Then
                Continue For
            End If

            listOfListsOfMatchingChars.Add(listOfMatchingChars)

            Dim listOfPossibleCharsWithCurrentMatchesRemoved As List(Of PossibleChar) = New List(Of PossibleChar)

            For Each nonMatchingChar As PossibleChar In listOfPossibleChars
                If (Not listOfMatchingChars.Contains(nonMatchingChar)) Then
                    listOfPossibleCharsWithCurrentMatchesRemoved.Add(nonMatchingChar)
                End If
            Next

            Dim recursiveListOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))

            Debug.Print ("making recursive call, listOfPossibleCharsWithCurrentMatchesRemoved.Count = " + listOfPossibleCharsWithCurrentMatchesRemoved.Count.ToString)
            
            recursiveListOfListsOfMatchingChars = findListOfListsOfMatchingChars(listOfPossibleCharsWithCurrentMatchesRemoved)

            Debug.Print ("returned from call")

            For Each recursiveListOfMatchingChars As List(Of PossibleChar) In recursiveListOfListsOfMatchingChars
                listOfListsOfMatchingChars.Add(recursiveListOfMatchingChars)
            Next
            Exit For
        Next

        Debug.Print("exiting recursive call, listOfListsOfMatchingChars.Count = " + listOfListsOfMatchingChars.Count.ToString)
        
        Return listOfListsOfMatchingChars

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

        Dim imgPlate As Image(Of Bgr, Byte) = New Image(Of Bgr,Byte)(CInt(dblHypotenuse + (listOfMatchingChars(0).dblDiameter * 3.0)), CInt(listOfMatchingChars(0).dblDiameter * 1.5))

        CvInvoke.cvGetRectSubPix(imgAngleCorrected, imgPlate, New PointF(sngCenterX, sngCenterY))

        Return imgPlate
    End Function

End Module






