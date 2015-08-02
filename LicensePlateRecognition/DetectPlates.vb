'DetectPlates.vb

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module DetectPlates

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const PLATE_WIDTH_PADDING_FACTOR As Double = 1.5
    Const PLATE_HEIGHT_PADDING_FACTOR As Double = 1.5
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectPlatesInScene(imgOriginalScene As Image(Of Bgr, Byte)) As List(Of PossiblePlate)
        Dim imgGrayscaleScene As Image(Of Gray, Byte) = Nothing
        Dim imgThreshScene As Image(Of Gray, Byte) = Nothing
        Dim imgContours As Image(Of Bgr, Byte) = Nothing        'this is only used for showing steps
        Dim random As New Random()                              'this is only used for showing steps

        closePreviousShowStepsWindows()

        If (frmMain.cbShowSteps.Checked = True) Then    
            CvInvoke.cvShowImage("0", imgOriginalScene)
        End If

        Preprocess.preprocess(imgOriginalScene, imgGrayscaleScene, imgThreshScene)

        If (frmMain.cbShowSteps.Checked = True) Then
            CvInvoke.cvShowImage("1a", imgGrayscaleScene)
            CvInvoke.cvShowImage("1b", imgThreshScene)
        End If

        Dim listOfPossibleCharsInScene As List(Of PossibleChar) = findPossibleCharsInScene(imgThreshScene)

        If (frmMain.cbShowSteps.Checked = True) Then
            imgContours = New Image(Of Bgr, Byte)(imgOriginalScene.Size())

            For Each possibleChar As PossibleChar In listOfPossibleCharsInScene
                CvInvoke.cvDrawContours(imgContours, possibleChar.contour, New MCvScalar(255, 255, 255), New MCvScalar(255, 255, 255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
            Next
            CvInvoke.cvShowImage("2b", imgContours)
        End If

        Dim listOfListsOfMatchingCharsInScene As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleCharsInScene)

        If (frmMain.cbShowSteps.Checked = True) Then
            imgContours = New Image(Of Bgr, Byte)(imgOriginalScene.Size())           're-instantiate imgContours to clear it

            For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingCharsInScene
                Dim intRandomBlue = random.Next(0, 256)
                Dim intRandomGreen = random.Next(0, 256)
                Dim intRandomRed = random.Next(0, 256)
                For Each matchingChar As PossibleChar In listOfMatchingChars
                    CvInvoke.cvDrawContours(imgContours, matchingChar.contour, New MCvScalar(intRandomBlue, intRandomGreen, intRandomRed), New MCvScalar(intRandomBlue, intRandomGreen, intRandomRed), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
                Next
            Next
            CvInvoke.cvShowImage("3", imgContours)
        End If

        Dim listOfPossiblePlates As List(Of PossiblePlate) = New List(Of PossiblePlate)         'this will be the return value

        For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingCharsInScene
            Dim possiblePlate = extractPlate(imgOriginalScene, listOfMatchingChars)

            If (Not possiblePlate.imgPlate Is Nothing) Then
                listOfPossiblePlates.Add(possiblePlate)
            End If
        Next

        frmMain.txtInfo.AppendText(vbCrLf + listOfPossiblePlates.Count.ToString + " possible plates found" + vbCrLf)

        If (frmMain.cbShowSteps.Checked = True) Then
            frmMain.txtInfo.AppendText(vbCrLf)
            CvInvoke.cvShowImage("4a", imgContours)

            For i As Integer = 0 To listOfPossiblePlates.Count - 1
                imgContours.Draw(listOfPossiblePlates(i).b2dLocationOfPlateInScene, New Bgr(Color.Red), 2)         'draw red rectangle around plate
                CvInvoke.cvShowImage("4a", imgContours)
                frmMain.txtInfo.AppendText("possible plate " + i.ToString + ", click on any image and press a key to continue . . ." + vbCrLf)
                CvInvoke.cvShowImage("4b", listOfPossiblePlates(i).imgPlate)
                CvInvoke.cvWaitKey(0)
            Next
            frmMain.txtInfo.AppendText(vbCrLf + "plate detection complete, click on any image and press a key to begin char recognition . . ." + vbCrLf + vbCrLf)
            CvInvoke.cvWaitKey(0)
        End If
        
        Return listOfPossiblePlates
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleCharsInScene(imgThresh As Image(Of Gray, Byte)) As List(Of PossibleChar)
        Dim imgContours As New Image(Of Gray, Byte)(imgThresh.Size())       'this is only for showing steps
        Dim intCountOfPossibleChars As Integer = 0                          'this is only for showing steps
        Dim intCountOfValidPossibleChars As Integer = 0                     'this is only for showing steps

        Dim contours As Contour(Of Point) = imgThresh.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST)

        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)()      'this is the return value

        While (Not contours Is Nothing)
            intCountOfPossibleChars = intCountOfPossibleChars + 1
            Dim contour As Contour(Of Point) = contours.ApproxPoly(contours.Perimeter * 0.0001)

            If (frmMain.cbShowSteps.Checked = True)
                CvInvoke.cvDrawContours(imgContours, contour, New MCvScalar(255), New MCvScalar(255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
            End If

            Dim possibleChar As PossibleChar = New PossibleChar(contour)
            
            If (checkIfPossibleChar(possibleChar)) Then
                intCountOfValidPossibleChars = intCountOfValidPossibleChars + 1
                listOfPossibleChars.Add(possibleChar)
            End If

            contours = contours.HNext
        End While

        If (frmMain.cbShowSteps.Checked) Then
            frmMain.txtInfo.AppendText(vbCrLf + "step 2 - intCountOfPossibleChars = " + intCountOfPossibleChars.ToString + vbCrLf)      '2115 with MCLRNF1 image
            frmMain.txtInfo.AppendText("step 2 - intCountOfValidPossibleChars = " + intCountOfValidPossibleChars.ToString + vbCrLf)     '174 with MCLRNF1 image
            CvInvoke.cvShowImage("2a", imgContours)
        End If

        Return listOfPossibleChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function extractPlate(imgOriginal As Image(Of Bgr, Byte), listOfMatchingChars As List(Of PossibleChar)) As PossiblePlate
        Dim possiblePlate As PossiblePlate = New PossiblePlate          'this will be the return value

        listOfMatchingChars.Sort(Function(firstChar, secondChar) firstChar.lngCenterX.CompareTo(secondChar.lngCenterX))

        Dim sngPlateCenterX As Single = CSng(CSng(listOfMatchingChars(0).lngCenterX + listOfMatchingChars(listOfMatchingChars.Count - 1).lngCenterX) / 2.0)
        Dim sngPlateCenterY As Single = CSng(CSng(listOfMatchingChars(0).lngCenterY + listOfMatchingChars(listOfMatchingChars.Count - 1).lngCenterY) / 2.0)

        Dim ptfPlateCenter As PointF = New PointF(sngPlateCenterX, sngPlateCenterY)

        Dim intPlateWidth As Integer = CInt((listOfMatchingChars(listOfMatchingChars.Count-1).boundingRect.Right - listOfMatchingChars(0).boundingRect.Left) * PLATE_WIDTH_PADDING_FACTOR)
        Dim intPlateHeight As Integer = CInt(listOfMatchingChars(0).dblDiagonalSize * PLATE_HEIGHT_PADDING_FACTOR)

        Dim sngOpposite = CSng(listOfMatchingChars(listOfMatchingChars.Count - 1).lngCenterY - listOfMatchingChars(0).lngCenterY)
        Dim sngHypotenuse = CSng(distanceBetweenChars(listOfMatchingChars(0), listOfMatchingChars(listOfMatchingChars.Count - 1)))
        Dim sngAngleInRad = CSng(Math.Asin(sngOpposite / sngHypotenuse))
        Dim sngAngleInDeg As Single = sngAngleInRad * CSng(180.0 / Math.PI)

        possiblePlate.b2dLocationOfPlateInScene = New MCvBox2D(ptfPlateCenter, New SizeF(CSng(intPlateWidth), CSng(intPlateHeight)), sngAngleInDeg)

        possiblePlate.imgPlate = imgOriginal.Copy(possiblePlate.b2dLocationOfPlateInScene)

        Return possiblePlate
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub closePreviousShowStepsWindows()
        CvInvoke.cvDestroyWindow("0")
        CvInvoke.cvDestroyWindow("1a")
        CvInvoke.cvDestroyWindow("1b")
        CvInvoke.cvDestroyWindow("2a")
        CvInvoke.cvDestroyWindow("2b")
        CvInvoke.cvDestroyWindow("3")
        CvInvoke.cvDestroyWindow("4a")
        CvInvoke.cvDestroyWindow("4b")
        CvInvoke.cvDestroyWindow("5a")
        CvInvoke.cvDestroyWindow("5b")
        CvInvoke.cvDestroyWindow("5c")
        CvInvoke.cvDestroyWindow("5d")
        CvInvoke.cvDestroyWindow("6")
        CvInvoke.cvDestroyWindow("7")
        CvInvoke.cvDestroyWindow("8")
        CvInvoke.cvDestroyWindow("9")
        CvInvoke.cvDestroyWindow("10")
    End Sub

End Module


