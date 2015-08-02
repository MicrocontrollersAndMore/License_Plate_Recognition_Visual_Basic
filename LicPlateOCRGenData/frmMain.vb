'LicPlateOCRGenData.vb
'
'Emgu CV 2.4.10
'
'add the following components to your form:
'tableLayoutPanel (TableLayoutPanel)
'btnOpenTrainingImage (Button)
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

Imports System.Xml                  '
Imports System.Xml.Serialization    'these imports are for writing Matrix objects to file, see end of program
Imports System.IO                   '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const MIN_CONTOUR_AREA As Integer = 100

    Const RESIZED_IMAGE_WIDTH As Integer = 20
    Const RESIZED_IMAGE_HEIGHT As Integer = 30

    Dim intNumberOfTrainingSamples As Integer

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenTrainingImage_Click( sender As Object,  e As EventArgs) Handles btnOpenTrainingImage.Click
        Dim drChosenFile As DialogResult

        drChosenFile = ofdOpenFile.ShowDialog()                 'open file dialog

        If (drChosenFile <> Windows.Forms.DialogResult.OK Or ofdOpenFile.FileName = "") Then    'if user chose Cancel or filename is blank . . .
            lblChosenFile.Text = "file not chosen"              'show error message on label
            Return                                              'and exit function
        End If

        Dim imgTrainingNumbers As Image(Of Bgr, Byte)           'this is the main input image

        Try
            imgTrainingNumbers = New Image(Of Bgr, Byte)(ofdOpenFile.FileName)             'open image
        Catch ex As Exception                                                       'if error occurred
            lblChosenFile.Text = "unable to open image, error: " + ex.Message       'show error message on label
            Return                                                                  'and exit function
        End Try
        
        If imgTrainingNumbers Is Nothing Then                                  'if image could not be opened
            lblChosenFile.Text = "unable to open image"                 'show error message on label
            Return                                                      'and exit function
        End If

        lblChosenFile.Text = ofdOpenFile.FileName           'update label with file name

        Dim imgGrayscale As Image(Of Gray, Byte)            '
        Dim imgBlurred As Image(Of Gray, Byte)              'declare various images
        Dim imgThresh As Image(Of Gray, Byte)               '
        Dim imgThreshCopy As Image(Of Gray, Byte)           '
        Dim imgContours As Image(Of Gray, Byte)             '

        Dim contours As Contour(Of Point)
        Dim listOfContours As List(Of Contour(Of Point)) = New List(Of Contour(Of Point))

                                'possible chars we are interested in are digits 0 through 9, put these in list intValidChars
        Dim intValidChars As New List(Of Integer)(New Integer() { Asc("0"), Asc("1"), Asc("2"), Asc("3"), Asc("4"), Asc("5"), Asc("6"), Asc("7"), Asc("8"), Asc("9"), _
                                                                  Asc("A"), Asc("B"), Asc("C"), Asc("D"), Asc("E"), Asc("F"), Asc("G"), Asc("H"), Asc("I"), Asc("J"), _
                                                                  Asc("K"), Asc("L"), Asc("M"), Asc("N"), Asc("O"), Asc("P"), Asc("Q"), Asc("R"), Asc("S"), Asc("T"), _
                                                                  Asc("U"), Asc("V"), Asc("W"), Asc("X"), Asc("Y"), Asc("Z") } )

        imgGrayscale = imgTrainingNumbers.Convert(Of Gray, Byte)()             'convert to grayscale

        imgBlurred = imgGrayscale.SmoothGaussian(5)         'blur

        imgBlurred = imgGrayscale.SmoothGaussian(5)         'blur

                                                            'filter image from grayscale to black and white
        imgThresh = imgBlurred.ThresholdAdaptive(New Gray(255), ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_GAUSSIAN_C, THRESH.CV_THRESH_BINARY_INV, 11, New Gray(2))

        CvInvoke.cvShowImage("imgThresh", imgThresh)            'show threshold image for reference

        imgThreshCopy = imgThresh.Clone()       'make a copy of the thresh image, this in necessary b/c findContours modifies the image

                                                'get external countours only
        contours = imgThreshCopy.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_EXTERNAL)

                                                    'next we count the contours
        intNumberOfTrainingSamples = 0          'init number of contours (i.e. training samples) to zero

        While (Not contours Is Nothing)
            intNumberOfTrainingSamples = intNumberOfTrainingSamples + 1
            contours = contours.HNext
        End While

        contours = imgThreshCopy.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_EXTERNAL)       'get contours again to go back to beginning

        imgContours = New Image(Of Gray, Byte)(imgThresh.Size())        'instantiate contours image

                                                                        'draw contours onto contours image
        CvInvoke.cvDrawContours(imgContours, contours, New MCvScalar(255), New MCvScalar(255), 100, 1, LINE_TYPE.CV_AA, New Point(0, 0))

        CvInvoke.cvShowImage("imgContours", imgContours)        'show contours image for reference

                                'this is our classifications data structure
        Dim mtxClassifications As Matrix(Of Single) = New Matrix(Of Single)(intNumberOfTrainingSamples, 1)

                                'this is our training images data structure, note we will have to perform some conversions to write to this later
        Dim mtxTrainingImages As Matrix(Of Single) = New Matrix(Of Single)(intNumberOfTrainingSamples, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)

                                                        'this keeps track of which row we are on in both classifications and training images,
        Dim intTrainingDataRowToAdd As Integer = 0      'note that each sample will correspond to one row in
                                                        'both the classifications XML file and the training images XML file

        While(Not contours Is Nothing)                      'for each contour
            Dim contour As Contour(Of Point) = contours.ApproxPoly(contours.Perimeter * 0.0001)         'get the current contour, note that the lower the multiplier, the higher the precision
            If (contour.Area >= MIN_CONTOUR_AREA) Then                                   'if contour is big enough to consider
                Dim rect As Rectangle = contour.BoundingRectangle()             'get the bounding rect
                imgTrainingNumbers.Draw(rect, New Bgr(Color.Red), 2)            'draw red rectangle around each contour as we ask user for input
                Dim imgROI As Image(Of Gray, Byte) = imgThresh.Copy(rect)       'get ROI image of current char
                
                                                'resize image, this is necessary for recognition and storage
                Dim imgROIResized As Image(Of Gray, Byte) = imgROI.Resize(RESIZED_IMAGE_WIDTH, RESIZED_IMAGE_HEIGHT, INTER.CV_INTER_LINEAR)

                CvInvoke.cvShowImage("imgROI", imgROI)                              'show ROI image for reference
                CvInvoke.cvShowImage("imgROIResized", imgROIResized)                'show resized ROI image for reference
                CvInvoke.cvShowImage("imgTrainingNumbers", imgTrainingNumbers)      'show training numbers image, this will now have red rectangles drawn on it

                Dim intChar As Integer = CvInvoke.cvWaitKey(0)          'get key press

                If (intChar = 27) Then              'if esc key was pressed
                    Return                          'exit the function
                ElseIf (intValidChars.Contains(intChar)) Then       'else if the char is in the list of chars we are looking for . . .

                    mtxClassifications(intTrainingDataRowToAdd, 0) = Convert.ToSingle(intChar)      'write classification char to classifications Matrix

                                'now add the training image (some conversion is necessary first) . . .
                                'note that we have to covert the images to Matrix(Of Single) type, this is necessary to pass into the KNearest object call to train
                    Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())                  'declare a Matrix of the same dimensions as the Image we are adding to the data structure of training images
                    Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)     'declare a flattened (only 1 row) matrix of the same total size

                    CvInvoke.cvConvert(imgROIResized, mtxTemp)      'convert Image to a Matrix of Singles with the same dimensions
                    
                    For intRow As Integer = 0 To RESIZED_IMAGE_HEIGHT - 1       'flatten Matrix into one row by RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT number of columns
                        For intCol As Integer = 0 To RESIZED_IMAGE_WIDTH - 1
                            mtxTempReshaped(0, (intRow * RESIZED_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                        Next
                    Next

                    For intCol As Integer = 0 To (RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT) - 1       'write flattened Matrix into one row of training images Matrix
                        mtxTrainingImages(intTrainingDataRowToAdd, intCol) = mtxTempReshaped(0, intCol)
                    Next

                    intTrainingDataRowToAdd = intTrainingDataRowToAdd + 1       'increment which row, i.e. sample we are on
                End If
            End If
            contours = contours.HNext                   'move on to next contour
        End While

        txtInfo.Text = txtInfo.Text + "training complete !!" + vbCrLf + vbCrLf

                'save classifications to file '''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim xmlSerializer As XmlSerializer = New XmlSerializer(mtxClassifications.GetType)
        Dim streamWriter As StreamWriter

        Try
            streamWriter = new StreamWriter("classifications.xml")          'attempt to open classifications file
        Catch ex As Exception                                               'if error is encountered, show error and return
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'classifications.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

        xmlSerializer.Serialize(streamWriter, mtxClassifications)
        streamWriter.Close()
        
                'save training images to file '''''''''''''''''''''''''''''''''''''''''''''''''''''

        xmlSerializer = New XmlSerializer(mtxTrainingImages.GetType)
        
        Try
            streamWriter = new StreamWriter("images.xml")                   'attempt to open images file
        Catch ex As Exception                                               'if error is encountered, show error and return
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'images.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

        xmlSerializer.Serialize(streamWriter, mtxTrainingImages)
        streamWriter.Close()

        txtInfo.Text = vbCrLf + txtInfo.Text + "file writing done" + vbCrLf





















    End Sub
End Class
