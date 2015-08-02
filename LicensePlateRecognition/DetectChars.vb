'DetectPlates.vb

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '
Imports Emgu.CV.ML                  '

Imports System.Xml
Imports System.Xml.Serialization    'these imports are for reading Matrix objects from file
Imports System.IO

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module DetectChars

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                                'constants for checkIfPossibleChar, this checks one possible char only (does not compare to another char)
    Const MIN_PIXEL_WIDTH As Long = 2
    Const MIN_PIXEL_HEIGHT As Long = 8

    Const MIN_ASPECT_RATIO As Double = 0.25
    Const MAX_ASPECT_RATIO As Double = 1.0

    Const MIN_PIXEL_AREA As Long = 20

                                'constants for comparing two chars
    Const MIN_DIAG_SIZE_MULTIPLE_AWAY = 0.3
    Const MAX_DIAG_SIZE_MULTIPLE_AWAY As Double = 5.0

    Const MAX_CHANGE_IN_AREA As Double = 0.5

    Const MAX_CHANGE_IN_WIDTH As Double = 0.8
    Const MAX_CHANGE_IN_HEIGHT As Double = 0.2

    Const MAX_ANGLE_BETWEEN_CHARS As Double = 12.0

    Const MIN_NUMBER_OF_MATCHING_CHARS As Integer = 3

    Const RESIZED_CHAR_IMAGE_WIDTH As Integer = 20
    Const RESIZED_CHAR_IMAGE_HEIGHT As Integer = 30

    Dim kNearest As KNearest

    Const MIN_CONTOUR_AREA As Integer = 100

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectCharsInPlates(listOfPossiblePlates As List(Of PossiblePlate)) As List(Of PossiblePlate)
        Dim intPlateCounter As Integer = 0              'this is only for showing steps
        Dim random As New Random()                      'this is only for showing steps
        
        If (listOfPossiblePlates Is Nothing) Then           'if list of possible plates is null,
            Return listOfPossiblePlates                     'return
        ElseIf (listOfPossiblePlates.Count = 0) Then        'if list of possible plates has zero plates
            Return listOfPossiblePlates                     'return
        End If
                        'at this point we can be sure list of possible plates has at least one plate
        
        For Each possiblePlate As PossiblePlate In listOfPossiblePlates
            Preprocess.preprocess(possiblePlate.imgPlate, possiblePlate.imgGrayscale, possiblePlate.imgThresh)

            If (frmMain.cbShowSteps.Checked = True) Then
                CvInvoke.cvShowImage("5a", possiblePlate.imgPlate)
                CvInvoke.cvShowImage("5b", possiblePlate.imgGrayscale)
                CvInvoke.cvShowImage("5c", possiblePlate.imgThresh)
            End If
            
            possiblePlate.imgThresh = possiblePlate.imgThresh.Resize(1.6, INTER.CV_INTER_LINEAR)            'increase size of plate image for easier viewing and char detection
            
                        'threshold image to only black or white (eliminate grayscale)
            CvInvoke.cvThreshold(possiblePlate.imgThresh, possiblePlate.imgThresh, 0, 255, THRESH.CV_THRESH_BINARY Or THRESH.CV_THRESH_OTSU)

            If (frmMain.cbShowSteps.Checked = True) Then
                 CvInvoke.cvShowImage("5d", possiblePlate.imgThresh)
            End If

            Dim listOfPossibleCharsInPlate As List(Of PossibleChar) = findPossibleCharsInPlate(possiblePlate.imgGrayscale, possiblePlate.imgThresh)

            If (frmMain.cbShowSteps.Checked = True) Then
                Dim imgContours As Image(Of Gray, Byte) = New Image(Of Gray, Byte)(possiblePlate.imgThresh.Size())

                For Each possibleChar As PossibleChar In listOfPossibleCharsInPlate
                    CvInvoke.cvDrawContours(imgContours, possibleChar.contour, New MCvScalar(255), New MCvScalar(255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
                Next
                CvInvoke.cvShowImage("6", imgContours)
            End If

            Dim listOfListsOfMatchingCharsInPlate As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleCharsInPlate)

            If (frmMain.cbShowSteps.Checked = True) Then
                Dim imgContours As Image(Of Bgr, Byte) = New Image(Of Bgr, Byte)(possiblePlate.imgThresh.Size())

                For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingCharsInPlate
                    Dim intRandomBlue = random.Next(0, 256)
                    Dim intRandomGreen = random.Next(0, 256)
                    Dim intRandomRed = random.Next(0, 256)
                    For Each matchingChar As PossibleChar In listOfMatchingChars
                        CvInvoke.cvDrawContours(imgContours, matchingChar.contour, New MCvScalar(intRandomBlue, intRandomGreen, intRandomRed), New MCvScalar(intRandomBlue, intRandomGreen, intRandomRed), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
                    Next
                Next
                CvInvoke.cvShowImage("7", imgContours)
            End If

            If (listOfListsOfMatchingCharsInPlate Is Nothing) Then
                If (frmMain.cbShowSteps.Checked = True) Then
                    frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = (none), click on any image and press a key to continue . . ." + vbCrLf)
                    intPlateCounter = intPlateCounter + 1
                    CvInvoke.cvDestroyWindow("8")
                    CvInvoke.cvDestroyWindow("9")
                    CvInvoke.cvDestroyWindow("10")
                    CvInvoke.cvWaitKey(0)
                End If

                possiblePlate.strChars = ""
                Continue For
            ElseIf (listOfListsOfMatchingCharsInPlate.Count = 0) Then
                If (frmMain.cbShowSteps.Checked = True) Then
                    frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = (none), click on any image and press a key to continue . . ." + vbCrLf)
                    intPlateCounter = intPlateCounter + 1
                    CvInvoke.cvDestroyWindow("8")
                    CvInvoke.cvDestroyWindow("9")
                    CvInvoke.cvDestroyWindow("10")
                    CvInvoke.cvWaitKey(0)
                End If

                possiblePlate.strChars = ""
                Continue For
            End If

            For i As Integer = 0 To listOfListsOfMatchingCharsInPlate.Count - 1
                listOfListsOfMatchingCharsInPlate(i).Sort(Function(oneChar, otherChar) oneChar.boundingRect.X.CompareTo(otherChar.boundingRect.X))
                listOfListsOfMatchingCharsInPlate(i) = removeInnerOverlappingChars(listOfListsOfMatchingCharsInPlate(i))
            Next

            If (frmMain.cbShowSteps.Checked = True) Then
                Dim imgContours As Image(Of Bgr, Byte) = New Image(Of Bgr, Byte)(possiblePlate.imgThresh.Size())

                For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingCharsInPlate
                    Dim intRandomBlue = random.Next(0, 256)
                    Dim intRandomGreen = random.Next(0, 256)
                    Dim intRandomRed = random.Next(0, 256)
                    For Each matchingChar As PossibleChar In listOfMatchingChars
                        CvInvoke.cvDrawContours(imgContours, matchingChar.contour, New MCvScalar(intRandomBlue, intRandomGreen, intRandomRed), New MCvScalar(intRandomBlue, intRandomGreen, intRandomRed), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
                    Next
                Next
                CvInvoke.cvShowImage("8", imgContours)
            End If

                    'within each possible plate, suppose the longest list of potential matching chars is the actual list of chars

            Dim intLenOfLongestListOfChars As Integer = 0
            Dim intIndexOfLongestListOfChars As Integer = 0

            For i As Integer = 0 To listOfListsOfMatchingCharsInPlate.Count - 1                         'find index of longest list of matching chars,
                If (listOfListsOfMatchingCharsInPlate(i).Count > intLenOfLongestListOfChars) Then       'we will suppose this is the "best" or "correct" list of chars
                    intLenOfLongestListOfChars = listOfListsOfMatchingCharsInPlate(i).Count
                    intIndexOfLongestListOfChars = i
                End If
            Next

            Dim longestListOfMatchingCharsInPlate As List(Of PossibleChar) = listOfListsOfMatchingCharsInPlate(intIndexOfLongestListOfChars)

            If (frmMain.cbShowSteps.Checked = True) Then
                Dim imgContours As Image(Of Gray, Byte) = New Image(Of Gray, Byte)(possiblePlate.imgThresh.Size())

                For Each matchingChar As PossibleChar In longestListOfMatchingCharsInPlate
                    CvInvoke.cvDrawContours(imgContours, matchingChar.contour, New MCvScalar(255), New MCvScalar(255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
                Next
                CvInvoke.cvShowImage("9", imgContours)
            End If

            possiblePlate.strChars = recognizeCharsInPlate(possiblePlate.imgThresh, longestListOfMatchingCharsInPlate)

            If (frmMain.cbShowSteps.Checked = True) Then
                frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = " + possiblePlate.strChars + ", click on any image and press a key to continue . . ." + vbCrLf)
                intPlateCounter = intPlateCounter + 1
                CvInvoke.cvWaitKey(0)
            End If
        Next

        If (frmMain.cbShowSteps.Checked = True) Then
            frmMain.txtInfo.AppendText(vbCrLf + "char detection complete, click on any image and press a key to continue . . ." + vbCrLf)
            CvInvoke.cvWaitKey(0)
        End If

        Return listOfPossiblePlates
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleCharsInPlate(imgGrayscale As Image(Of Gray, Byte), imgThresh As Image(Of Gray, Byte)) As List(Of PossibleChar)
        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)        'this will be the return value
        Dim imgThreshCopy As Image(Of Gray, Byte)
        Dim contours As Contour(Of Point)
        Dim listOfContours As List(Of Contour(Of Point)) = New List(Of Contour(Of Point))()

        imgThreshCopy = imgThresh.Clone()       'make a copy of the thresh image, this in necessary b/c findContours modifies the image

        contours = imgThreshCopy.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST)

        While (Not contours Is Nothing)                                                     'for each contour
            Dim contour As Contour(Of Point) = contours.ApproxPoly(contours.Perimeter * 0.0001)         'get the current contour, note that the lower the multiplier, the higher the precision
            If (contour.Area >= MIN_CONTOUR_AREA) Then
                listOfContours.Add(contour)
            End If
            contours = contours.HNext                                                                   'move on to next contour
        End While
                                        'sort contours from left to right
        listOfContours.Sort(Function(oneContour, otherContour) oneContour.BoundingRectangle.X.CompareTo(otherContour.BoundingRectangle.X))

        For Each contour As Contour(Of Point) In listOfContours
            Dim possibleChar As PossibleChar = New PossibleChar(contour)
            If (checkIfPossibleChar(possibleChar)) Then
                listOfPossibleChars.Add(possibleChar)
            End If
        Next

        Return listOfPossibleChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function checkIfPossibleChar(possibleChar As PossibleChar) As Boolean
        If (possibleChar.boundingRect.Width > MIN_PIXEL_WIDTH And possibleChar.boundingRect.Height > MIN_PIXEL_HEIGHT And _
            MIN_ASPECT_RATIO < possibleChar.dblAspectRatio And possibleChar.dblAspectRatio < MAX_ASPECT_RATIO And _
            possibleChar.lngArea > MIN_PIXEL_AREA) Then
            Return True
        Else
            Return False
        End If
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfListsOfMatchingChars(listOfPossibleChars As List(Of PossibleChar)) As List(Of List(Of PossibleChar))

        Dim listOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))       'this will be the return value

        For Each possibleChar As PossibleChar In listOfPossibleChars
                                                                'get list of chars that match the current char
            Dim listOfMatchingChars As List(Of PossibleChar) = findListOfMatchingChars(possibleChar, listOfPossibleChars)

            listOfMatchingChars.Add(possibleChar)               'also add the current char to the list of potential matching chars

            If (listOfMatchingChars.Count < MIN_NUMBER_OF_MATCHING_CHARS) Then      'check if the list of chars is long enough to constitute a "group" or "cluster" of matching chars
                Continue For                                                        'if not, continue for, this will go on to the next possible char
            End If
                                                                    'if we get here, the current list passed test as a "group" or "cluster" of matching chars
            listOfListsOfMatchingChars.Add(listOfMatchingChars)     'so add to our list of lists of matching chars

            Dim listOfPossibleCharsWithCurrentMatchesRemoved As List(Of PossibleChar) = listOfPossibleChars.Except(listOfMatchingChars).ToList()

            Dim recursiveListOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))

            recursiveListOfListsOfMatchingChars = findListOfListsOfMatchingChars(listOfPossibleCharsWithCurrentMatchesRemoved)      'recursive call !!

            For Each recursiveListOfMatchingChars As List(Of PossibleChar) In recursiveListOfListsOfMatchingChars
                listOfListsOfMatchingChars.Add(recursiveListOfMatchingChars)
            Next
            Exit For
        Next
        
        Return listOfListsOfMatchingChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfMatchingChars(possibleChar As PossibleChar, listOfChars As List(Of PossibleChar)) As List(Of PossibleChar)
        Dim listOfMatchingChars As List(Of PossibleChar) = New List(Of PossibleChar)            'this will be the return value

        For Each possibleMatchingChar As PossibleChar In listOfChars

            If (possibleMatchingChar.Equals(possibleChar)) Then
                Continue For
            End If

            Dim dblDistanceBetweenChars As Double = distanceBetweenChars(possibleChar, possibleMatchingChar)

            Dim dblAngleBetweenChars As Double = angleBetweenChars(possibleChar, possibleMatchingChar)
            
            Dim dblChangeInArea As Double = Math.Abs(possibleMatchingChar.lngArea - possibleChar.lngArea) / possibleChar.lngArea

            Dim dblChangeInWidth As Double = Math.Abs(possibleMatchingChar.boundingRect.Width - possibleChar.boundingRect.Width) / possibleChar.boundingRect.Width
            Dim dblChangeInHeight As Double = Math.Abs(possibleMatchingChar.boundingRect.Height - possibleChar.boundingRect.Height) / possibleChar.boundingRect.Height

            If (dblDistanceBetweenChars < (possibleChar.dblDiagonalSize * MAX_DIAG_SIZE_MULTIPLE_AWAY) And _
                dblAngleBetweenChars < MAX_ANGLE_BETWEEN_CHARS And _
                dblChangeInArea < MAX_CHANGE_IN_AREA And _
                dblChangeInWidth < MAX_CHANGE_IN_WIDTH And _
                dblChangeInHeight < MAX_CHANGE_IN_HEIGHT) Then

                listOfMatchingChars.Add(possibleMatchingChar)
            End If

        Next

        Return listOfMatchingChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function distanceBetweenChars(firstChar As PossibleChar, secondChar As PossibleChar) As Double
        Dim lngX As Long = Math.Abs(firstChar.lngCenterX - secondChar.lngCenterX)
        Dim lngY As Long = Math.Abs(firstChar.lngCenterY - secondChar.lngCenterY)

        Return Math.Sqrt((lngX ^ 2) + (lngY ^ 2))
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function angleBetweenChars(firstChar As PossibleChar, secondChar As PossibleChar) As Double
        Dim dblAdj As Double = CDbl(Math.Abs(firstChar.lngCenterX - secondChar.lngCenterX))
        Dim dblOpp As Double = CDbl(Math.Abs(firstChar.lngCenterY - secondChar.lngCenterY))

        Dim dblAngleInRad As Double = Math.Atan(dblOpp / dblAdj) 
        
        Dim dblAngleInDeg As Double = dblAngleInRad * (180.0 / Math.PI)

        Return dblAngleInDeg
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function removeInnerOverlappingChars(listOfMatchingChars As List(Of PossibleChar)) As List(Of PossibleChar)
        
        Dim listOfMatchingCharsWithInnerCharRemoved As List(Of PossibleChar) = New List(Of PossibleChar)(listOfMatchingChars)

        For Each currentChar As PossibleChar In listOfMatchingChars
            For Each otherChar As PossibleChar In listOfMatchingChars
                If (Not currentChar.Equals(otherChar)) Then                                     'if current char and other char are not the same char . . .
                                                                                                'if current char and other char have center points at almost the same location . . .
                    If (distanceBetweenChars(currentChar, otherChar) < currentChar.dblDiagonalSize * MIN_DIAG_SIZE_MULTIPLE_AWAY) Then
                                        'if we get in here we have found overlapping chars
                                        'next we identify which char is smaller, then if that char was not already removed on a previous pass, remove it
                        If (currentChar.lngArea < otherChar.lngArea) Then                               'if current char is smaller than other char
                            If (listOfMatchingCharsWithInnerCharRemoved.Contains(currentChar)) Then     'if current char was not already removed on a previous pass . . .
                                listOfMatchingCharsWithInnerCharRemoved.Remove(currentChar)             'then remove current char
                            End If
                        Else                                                                            'else if other char is smaller than current char
                            If (listOfMatchingCharsWithInnerCharRemoved.Contains(otherChar)) Then       'if other char was not already removed on a previous pass . . .
                                listOfMatchingCharsWithInnerCharRemoved.Remove(otherChar)               'then remove other char
                            End If

                        End If
                    End If
                End If
            Next
        Next

        Return listOfMatchingCharsWithInnerCharRemoved
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function recognizeCharsInPlate(imgThresh As Image(Of Gray, Byte), listOfMatchingChars As List(Of PossibleChar)) As String
        Dim strChars As String = ""         'this will be the return value, the chars in the lic plate

        Dim imgThreshColor As Image(Of Bgr, Byte)
        
        listOfMatchingChars.Sort(Function(oneChar, otherChar) oneChar.boundingRect.X.CompareTo(otherChar.boundingRect.X))

        imgThreshColor = imgThresh.Convert(Of Bgr, Byte)()

        For Each currentChar As PossibleChar In listOfMatchingChars
            imgThreshColor.Draw(currentChar.boundingRect, New Bgr(Color.Green), 2)

            Dim imgROI As Image(Of Gray, Byte) = imgThresh.Copy(currentChar.boundingRect)

            Dim imgROIResized As Image(Of Gray, Byte) = imgROI.Resize(RESIZED_CHAR_IMAGE_WIDTH, RESIZED_CHAR_IMAGE_HEIGHT, INTER.CV_INTER_LINEAR)

            Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())
            Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_CHAR_IMAGE_WIDTH * RESIZED_CHAR_IMAGE_HEIGHT)

            CvInvoke.cvConvert(imgROIResized, mtxTemp)

            For intRow As Integer = 0 To RESIZED_CHAR_IMAGE_HEIGHT - 1       'flatten Matrix into one row by RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT number of columns
                For intCol As Integer = 0 To RESIZED_CHAR_IMAGE_WIDTH - 1
                    mtxTempReshaped(0, (intRow * RESIZED_CHAR_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                Next
            Next

            Dim sngCurrentChar As Single = kNearest.FindNearest(mtxTempReshaped, 1, Nothing, Nothing, Nothing, Nothing)

            strChars = strChars + Chr(Convert.ToInt32(sngCurrentChar))
        Next
        
        If (frmMain.cbShowSteps.Checked = True) Then
            CvInvoke.cvShowImage("10", imgThreshColor)
        End If

        Return strChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function loadKNNDataAndTrainKNN() As Boolean

                    'note: we effectively have to read the first XML file twice
                    'first, we read the file to get the number of rows (which is the same as the number of samples)
                    'the first time reading the file we can't get the data yet, since we don't know how many rows of data there are
                    'next, reinstantiate our classifications Matrix and training images Matrix with the correct number of rows
                    'then, read the file again and this time read the data into our resized classifications Matrix and training images Matrix

        Dim mtxClassifications As Matrix(Of Single) = New Matrix(Of Single)(1, 1)       'for the first time through, declare these to be 1 row by 1 column
        Dim mtxTrainingImages As Matrix(Of Single) = New Matrix(Of Single)(1, 1)        'we will resize these when we know the number of rows (i.e. number of training samples)
        
        Dim xmlSerializer As XmlSerializer = New XmlSerializer(mtxClassifications.GetType)          'these variables are for
        Dim streamReader As StreamReader                                                            'reading from the XML files

        Try
            streamReader = new StreamReader("classifications.xml")          'attempt to open classifications file
        Catch ex As Exception                                               'if error is encountered, show error and return
            frmMain.txtInfo.Text = vbCrLf + frmMain.txtInfo.Text + "unable to open 'classifications.xml', error:" + vbCrLf
            frmMain.txtInfo.Text = frmMain.txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return False
        End Try

                'read from the classifications file the 1st time, this is only to get the number of rows, not the actual data
        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Global.Emgu.CV.Matrix(Of Single))
        
        streamReader.Close()            'close the classifications XML file

        Dim intNumberOfTrainingSamples As Integer = mtxClassifications.Rows            'get the number of rows, i.e. the number of training samples

                'now that we know the number of rows, reinstantiate classifications Matrix and training images Matrix with the actual number of rows
        mtxClassifications = New Matrix(Of Single)(intNumberOfTrainingSamples, 1)
        mtxTrainingImages = New Matrix(Of Single)(intNumberOfTrainingSamples, RESIZED_CHAR_IMAGE_WIDTH * RESIZED_CHAR_IMAGE_HEIGHT)

        Try
            streamReader = new StreamReader("classifications.xml")          'reinitialize the stream reader
        Catch ex As Exception                                               'if error is encountered, show error and return
            frmMain.txtInfo.Text = vbCrLf + frmMain.txtInfo.Text + "unable to open 'classifications.xml', error:" + vbCrLf
            frmMain.txtInfo.Text = frmMain.txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return False
        End Try

                    'read from the classifications file again, this time we can get the actual data
        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Global.Emgu.CV.Matrix(Of Single))

        streamReader.Close()            'close the classifications XML file

        xmlSerializer = New XmlSerializer(mtxTrainingImages.GetType)            'reinstantiate file reading variables
        
        Try
            streamReader = New StreamReader("images.xml")
        Catch ex As Exception                                               'if error is encountered, show error and return
            frmMain.txtInfo.Text = vbCrLf + frmMain.txtInfo.Text + "unable to open 'images.xml', error:" + vbCrLf
            frmMain.txtInfo.Text = frmMain.txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return False
        End Try

        mtxTrainingImages = CType(xmlSerializer.Deserialize(streamReader), Global.Emgu.CV.Matrix(Of Single))        'read from training images file
        streamReader.Close()            'close the training images XML file

                ' train '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        
        kNearest = New KNearest()                   'instantiate KNN object
        kNearest.Train(mtxTrainingImages, mtxClassifications, Nothing, False, 3, False)       'call to train

        Return True
    End Function

End Module



