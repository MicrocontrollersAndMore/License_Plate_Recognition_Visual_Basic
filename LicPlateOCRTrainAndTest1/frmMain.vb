'LicPlateOCRTrainAndTest.vb

'using Emgu CV 2.4.10

'add the following components to your form:
'btnOpenTestImage (Button)
'lblChosenFile (Label)
'txtInfo (TextBox)
'ofdOpenFile (OpenFileDialog)

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
Public Class frmMain

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const MIN_CONTOUR_AREA As Integer = 100

    Const RESIZED_IMAGE_WIDTH As Integer = 20
    Const RESIZED_IMAGE_HEIGHT As Integer = 30

    Const MIN_NUMBER_OF_MATCHING_CHARS As Integer = 3

    Dim intNumberOfTrainingSamples As Integer

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenTestImage_Click( sender As Object,  e As EventArgs) Handles btnOpenTestImage.Click

                    'note: we effectively have to read the first XML file twice
                    'first, we read the file to get the number of rows (which is the same as the number of samples)
                    'the first time reading the file we can't get the data yet, since we don't know how many rows of data there are
                    'next, reinstantiate our classifications Matrix and training images Matrix with the correct number of rows
                    'then, read the file again and this time read the data into our resized classifications Matrix and training images Matrix

        Dim mtxClassifications As Matrix(Of Single) = New Matrix(Of Single)(1, 1)       'for the first time through, declare these to be 1 row by 1 column
        Dim mtxTrainingImages As Matrix(Of Single) = New Matrix(Of Single)(1, 1)        'we will resize these when we know the number of rows (i.e. number of training samples)
        
                    'possible chars we are interested in are digits 0 through 9
        Dim intValidChars As New List(Of Integer)(New Integer() { Asc("0"), Asc("1"), Asc("2"), Asc("3"), Asc("4"), Asc("5"), Asc("6"), Asc("7"), Asc("8"), Asc("9") } )
        
        Dim xmlSerializer As XmlSerializer = New XmlSerializer(mtxClassifications.GetType)          'these variables are for
        Dim streamReader As StreamReader                                                            'reading from the XML files

        Try
            streamReader = new StreamReader("classifications.xml")          'attempt to open classifications file
        Catch ex As Exception                                               'if error is encountered, show error and return
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'classifications.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

                'read from the classifications file the 1st time, this is only to get the number of rows, not the actual data
        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Global.Emgu.CV.Matrix(Of Single))
        
        streamReader.Close()            'close the classifications XML file

        intNumberOfTrainingSamples = mtxClassifications.Rows            'get the number of rows, i.e. the number of training samples

                'now that we know the number of rows, reinstantiate classifications Matrix and training images Matrix with the actual number of rows
        mtxClassifications = New Matrix(Of Single)(intNumberOfTrainingSamples, 1)
        mtxTrainingImages = New Matrix(Of Single)(intNumberOfTrainingSamples, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)

        Try
            streamReader = new StreamReader("classifications.xml")          'reinitialize the stream reader
        Catch ex As Exception                                               'if error is encountered, show error and return
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'classifications.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

                    'read from the classifications file again, this time we can get the actual data
        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Global.Emgu.CV.Matrix(Of Single))

        streamReader.Close()            'close the classifications XML file

        xmlSerializer = New XmlSerializer(mtxTrainingImages.GetType)            'reinstantiate file reading variables
        
        Try
            streamReader = New StreamReader("images.xml")
        Catch ex As Exception                                               'if error is encountered, show error and return
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'images.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

        mtxTrainingImages = CType(xmlSerializer.Deserialize(streamReader), Global.Emgu.CV.Matrix(Of Single))        'read from training images file
        streamReader.Close()            'close the training images XML file

                ' train '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        
        Dim kNearest As KNearest = New KNearest()                   'instantiate KNN object
        kNearest.Train(mtxTrainingImages, mtxClassifications, Nothing, False, 3, False)       'call to train

                ' test ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        'Dim imgTestingNumbers As Image(Of Bgr, Byte)        'this is the main input image
        'Dim imgGrayscale As Image(Of Gray, Byte)            '
        'Dim imgBlurred As Image(Of Gray, Byte)              'declare various images
        Dim imgThresh As Image(Of Gray, Byte)               '
        Dim imgThreshCopy As Image(Of Gray, Byte)           '
        Dim imgThreshColor As Image(Of Bgr, Byte)
        Dim imgContours As Image(Of Gray, Byte)

        Dim drChosenFile As DialogResult

        drChosenFile = ofdOpenFile.ShowDialog()                 'open file dialog

        If (drChosenFile <> Windows.Forms.DialogResult.OK Or ofdOpenFile.FileName = "") Then    'if user chose Cancel or filename is blank . . .
            lblChosenFile.Text = "file not chosen"              'show error message on label
            Return                                              'and exit function
        End If

        Try
            imgThresh = New Image(Of Gray, Byte)(ofdOpenFile.FileName)             'open image
        Catch ex As Exception                                                       'if error occurred
            lblChosenFile.Text = "unable to open image, error: " + ex.Message       'show error message on label
            Return                                                                  'and exit function
        End Try
        
        If imgThresh Is Nothing Then                                  'if image could not be opened
            lblChosenFile.Text = "unable to open image"                 'show error message on label
            Return                                                      'and exit function
        End If

        lblChosenFile.Text = ofdOpenFile.FileName           'update label with file name

        Dim contours As Contour(Of Point)

        'imgGrayscale = imgTestingNumbers.Convert(Of Gray, Byte)()           'convert to grayscale

        'imgBlurred = imgGrayscale.SmoothGaussian(5)                         'blur

        '                                        'filter image from grayscale to black and white
        'imgThresh = imgBlurred.ThresholdAdaptive(New Gray(255), ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_GAUSSIAN_C, THRESH.CV_THRESH_BINARY, 11, New Gray(2))

        CvInvoke.cvThreshold(imgThresh, imgThresh, 0, 255, THRESH.CV_THRESH_BINARY Or THRESH.CV_THRESH_OTSU)

        imgThresh = imgThresh.ThresholdBinary(New Gray(128), New Gray(255))

        imgThreshCopy = imgThresh.Clone()       'make a copy of the thresh image, this in necessary b/c findContours modifies the image

                                                'get external countours only
        contours = imgThreshCopy.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST)

        Dim listOfContours As List(Of Contour(Of Point)) = New List(Of Contour(Of Point))()             'declare a list of contours and a list of valid contours,
        Dim listOfValidContours As List(Of Contour(Of Point)) = New List(Of Contour(Of Point))()        'this is necessary for removing invalid contours and sorting from left to right

                                        'populate list of contours
        While (Not contours Is Nothing)                                                     'for each contour
            Dim contour As Contour(Of Point) = contours.ApproxPoly(contours.Perimeter * 0.0001)         'get the current contour, note that the lower the multiplier, the higher the precision
            listOfContours.Add(contour)                                                                 'add to list of contours
            contours = contours.HNext                                                                   'move on to next contour
        End While
                                        'this next loop removes the invalid contours
        For Each contour As Contour(Of Point) In listOfContours                 'for each contour
            If (contour.Area >= MIN_CONTOUR_AREA) Then                                   'if contour is valid
                listOfValidContours.Add(contour)                                'add to list of valid contours
            End If
        Next
                                        'sort contours from left to right
        listOfValidContours.Sort(Function(oneContour, otherContour) oneContour.BoundingRectangle.X.CompareTo(otherContour.BoundingRectangle.X))

        imgContours = New Image(Of Gray, Byte)(imgThresh.Size())
        For Each contour As Contour(Of Point) In listOfValidContours
            CvInvoke.cvDrawContours(imgContours, contour, New MCvScalar(255), New MCvScalar(255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
            'CvInvoke.cvShowImage("imgContours1", imgContours)
            'CvInvoke.cvWaitKey(0)
        Next
        CvInvoke.cvShowImage("imgContours1", imgContours)

        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)

        For Each contour As Contour(Of Point) In listOfValidContours
            Dim possibleChar As PossibleChar = New PossibleChar(contour)
            If (possibleChar.checkIfValid()) Then
                possibleChar.calcAvgAndStdDev(imgThresh)
                listOfPossibleChars.Add(possibleChar)
            End If
        Next

        Dim listOfListOfMatchingChars As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleChars)

        listOfValidContours.Clear()

        For Each matchingChar As PossibleChar In listOfListOfMatchingChars(0)
            listOfValidContours.Add(matchingChar.contour)
        Next

        listOfValidContours.Sort(Function(oneContour, otherContour) oneContour.BoundingRectangle.X.CompareTo(otherContour.BoundingRectangle.X))

        imgContours = New Image(Of Gray, Byte)(imgThresh.Size())
        For Each contour As Contour(Of Point) In listOfValidContours
            CvInvoke.cvDrawContours(imgContours, contour, New MCvScalar(255), New MCvScalar(255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))
            'CvInvoke.cvShowImage("imgContours2", imgContours)
            'CvInvoke.cvWaitKey(0)
        Next
        CvInvoke.cvShowImage("imgContours2", imgContours)

        Dim strFinalString As String = ""           'declare final string, this will have the final number sequence by the end of the program

        Dim intLoopCounter As Integer = 0

        imgThreshColor = imgThresh.Convert(Of Bgr, Byte)()

        For Each contour As Contour(Of Point) In listOfValidContours        'for each contour in list of valid contours
            Dim rect As Rectangle = contour.BoundingRectangle()             'get the bounding rect
            imgThreshColor.Draw(rect, New Bgr(Color.Green), 2)           'draw green rect around the current char

            Dim imgROI As Image(Of Gray, Byte) = imgThresh.Copy(rect)       'get ROI image of bounding rect
            
            'CvInvoke.cvShowImage("imgROI" + intLoopCounter.ToString, imgROI)

                                                                            'resize image, this is necessary for recognition
            Dim imgROIResized As Image(Of Gray, Byte) = imgROI.Resize(RESIZED_IMAGE_WIDTH, RESIZED_IMAGE_HEIGHT, INTER.CV_INTER_LINEAR)

            Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())                  'declare a Matrix of the same dimensions as the Image we are adding to the data structure of training images
            Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)     'declare a flattened (only 1 row) matrix of the same total size

            CvInvoke.cvConvert(imgROIResized, mtxTemp)      'convert Image to a Matrix of Singles with the same dimensions

            For intRow As Integer = 0 To RESIZED_IMAGE_HEIGHT - 1       'flatten Matrix into one row by RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT number of columns
                For intCol As Integer = 0 To RESIZED_IMAGE_WIDTH - 1
                    mtxTempReshaped(0, (intRow * RESIZED_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                Next
            Next

            Dim sngCurrentChar As Single = kNearest.FindNearest(mtxTempReshaped, 1, Nothing, Nothing, Nothing, Nothing)     'finally we can call find_nearest !!!

            strFinalString = strFinalString + Chr(Convert.ToInt32(sngCurrentChar))          'append current char to full string

            intLoopCounter = intLoopCounter + 1
        Next

        txtInfo.AppendText(vbCrLf + txtInfo.Text + "license plate read from image = " + strFinalString + vbCrLf)        'show the full string

        CvInvoke.cvShowImage("imgTestingNumbers", imgThreshColor)        'show input image with green boxes drawn around found digits
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfListsOfMatchingChars(listOfPossibleChars As List(Of PossibleChar)) As List(Of List(Of PossibleChar))

        'intNumTimesInFindListOfListsOfMatchingChars = intNumTimesInFindListOfListsOfMatchingChars + 1
        'Debug.Print("entering recurive call, intNumTimesInFindListOfListsOfMatchingChars = " + intNumTimesInFindListOfListsOfMatchingChars.ToString)
        
        Dim listOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))

        For Each possibleChar As PossibleChar In listOfPossibleChars

            Dim listOfMatchingChars As List(Of PossibleChar) = possibleChar.findListOfMatchingChars(listOfPossibleChars)

            If (listOfMatchingChars Is Nothing) Then
                Continue For
            End If

            listOfMatchingChars.Add(possibleChar)

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

            'Debug.Print ("making recursive call, listOfPossibleCharsWithCurrentMatchesRemoved.Count = " + listOfPossibleCharsWithCurrentMatchesRemoved.Count.ToString)
            
            recursiveListOfListsOfMatchingChars = findListOfListsOfMatchingChars(listOfPossibleCharsWithCurrentMatchesRemoved)

            'Debug.Print ("returned from call")

            For Each recursiveListOfMatchingChars As List(Of PossibleChar) In recursiveListOfListsOfMatchingChars
                listOfListsOfMatchingChars.Add(recursiveListOfMatchingChars)
            Next
            Exit For
        Next

        'Debug.Print("exiting recursive call, listOfListsOfMatchingChars.Count = " + listOfListsOfMatchingChars.Count.ToString)
        
        Return listOfListsOfMatchingChars

    End Function

End Class
