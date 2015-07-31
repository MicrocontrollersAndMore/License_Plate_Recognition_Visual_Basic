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
    Function detectPlates(imgOriginal As Image(Of Bgr, Byte)) As List(Of PossiblePlate)
        Dim imgGrayscale As Image(Of Gray, Byte) = Nothing
        Dim imgThresh As Image(Of Gray, Byte) = Nothing
        Dim imgContours As Image(Of Bgr, Byte) = Nothing        'this is only used for showing steps
        Dim random As New Random()                              'this is only used for showing steps

        If (frmMain.ckbShowSteps.Checked = True) Then
            CvInvoke.cvShowImage("1 - imgOriginal at beginning", imgOriginal)
        End If

        Preprocess.preprocess(imgOriginal, imgGrayscale, imgThresh)

        If (frmMain.ckbShowSteps.Checked = True) Then
            CvInvoke.cvShowImage("2a - just after preprocess of entire image, imgGrayscale", imgGrayscale)
            CvInvoke.cvShowImage("2b - just after preprocess of entire image, imgThresh", imgThresh)
        End If

        Dim listOfPossibleChars As List(Of PossibleChar) = findPossibleCharsInScene(imgThresh)

        If (frmMain.ckbShowSteps.Checked = True) Then
            imgContours = New Image(Of Bgr, Byte)(imgOriginal.Size())

            For Each possibleChar As PossibleChar In listOfPossibleChars
                CvInvoke.cvDrawContours(imgContours, possibleChar.contour, New MCvScalar(255, 255, 255), New MCvScalar(255, 255, 255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
            Next
            CvInvoke.cvShowImage("3 - just after findPossibleCharsInScene, listOfPossibleChars contours are:", imgContours)
        End If

        Dim listOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleChars)

        If (frmMain.ckbShowSteps.Checked = True) Then
            imgContours = New Image(Of Bgr, Byte)(imgOriginal.Size())           're-instantiate imgContours to clear it

            For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingChars
                Dim intRandomBlue = random.Next(0, 256)
                Dim intRandomGreen = random.Next(0, 256)
                Dim intRandomRed = random.Next(0, 256)
                For Each matchingChar As PossibleChar In listOfMatchingChars
                    CvInvoke.cvDrawContours(imgContours, matchingChar.contour, New MCvScalar(intRandomBlue, intRandomGreen, intRandomRed), New MCvScalar(intRandomBlue, intRandomGreen, intRandomRed), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
                Next
            Next
            CvInvoke.cvShowImage("4 - just got listOfListsOfMatchingChars for scene, contours are:", imgContours)
        End If

        Dim listOfPossiblePlates As List(Of PossiblePlate) = New List(Of PossiblePlate)

        For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingChars
            Dim possiblePlate = extractPlate(imgOriginal, listOfMatchingChars)

            If (Not possiblePlate.imgPlate Is Nothing) Then
                listOfPossiblePlates.Add(possiblePlate)
            End If
        Next

        If (frmMain.ckbShowSteps.Checked = True) Then
            For Each possiblePlate As PossiblePlate In listOfPossiblePlates
                imgContours.Draw(possiblePlate.b2dLocationOfPlateInScene, New Bgr(Color.Red), 2)         'draw red rectangle around plate
            Next
            CvInvoke.cvShowImage("5a - just got listOfPossiblePlates, contours are:", imgContours)

            For i As Integer = 0 To listOfPossiblePlates.Count - 1
                CvInvoke.cvShowImage("5b - possible plate " + i.ToString, listOfPossiblePlates(i).imgPlate)
            Next

        End If
        
        Return listOfPossiblePlates
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleCharsInScene(imgThresh As Image(Of Gray, Byte)) As List(Of PossibleChar)
        Dim contours As Contour(Of Point) = imgThresh.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST)

        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)()      'this is the return value

        Dim intCountOfPossibleChars As Integer = 0
        Dim intCountOfValidPossibleChars As Integer = 0

        While (Not contours Is Nothing)
            intCountOfPossibleChars = intCountOfPossibleChars + 1
            Dim contour As Contour(Of Point) = contours.ApproxPoly(contours.Perimeter * 0.0001)
            Dim possibleChar As PossibleChar = New PossibleChar(contour)
            
            If (possibleChar.checkIfPossibleChar()) Then
                intCountOfValidPossibleChars = intCountOfValidPossibleChars + 1
                listOfPossibleChars.Add(possibleChar)
            End If

            contours = contours.HNext
        End While

        If (frmMain.ckbShowSteps.Checked) Then
            frmMain.txtInfo.AppendText(vbCrLf + "3 - intCountOfPossibleChars = " + intCountOfPossibleChars.ToString + vbCrLf)                 '2115 with MCLRNF1 image
            frmMain.txtInfo.AppendText("3 - intCountOfValidPossibleChars = " + intCountOfValidPossibleChars.ToString + vbCrLf + vbCrLf)       '222 with MCLRNF1 image
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
        Dim sngHypotenuse = CSng(listOfMatchingChars(0).distanceTo(listOfMatchingChars(listOfMatchingChars.Count - 1)))
        Dim sngAngleInRad = CSng(Math.Asin(sngOpposite / sngHypotenuse))
        Dim sngAngleInDeg As Single = sngAngleInRad * CSng(180.0 / Math.PI)

        possiblePlate.b2dLocationOfPlateInScene = New MCvBox2D(ptfPlateCenter, New SizeF(CSng(intPlateWidth), CSng(intPlateHeight)), sngAngleInDeg)

        possiblePlate.imgPlate = imgOriginal.Copy(possiblePlate.b2dLocationOfPlateInScene)

        Return possiblePlate
    End Function

End Module


