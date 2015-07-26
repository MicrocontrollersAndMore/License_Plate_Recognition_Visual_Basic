'frmMain.vb

'using Emgu CV 2.4.10

'add the following components to your form:
'btnOpenFile (Button)
'lblChosenFile (Label)
'ibOriginal (TextBox)
'ofdOpenFile (OpenFileDialog)

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Dim listOfPossiblePlates As List(Of PossiblePlate) = New List(Of PossiblePlate)

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenFile_Click( sender As Object,  e As EventArgs) Handles btnOpenFile.Click
        Dim drChosenFile As DialogResult

        drChosenFile = ofdOpenFile.ShowDialog()                 'open file dialog

        If (drChosenFile <> Windows.Forms.DialogResult.OK Or ofdOpenFile.FileName = "") Then    'if user chose Cancel or filename is blank . . .
            lblChosenFile.Text = "file not chosen"              'show error message on label
            Return                                              'and exit function
        End If

        Dim imgOriginal As Image(Of Bgr, Byte)           'this is the main input image

        Try
            imgOriginal = New Image(Of Bgr, Byte)(ofdOpenFile.FileName)             'open image
        Catch ex As Exception                                                       'if error occurred
            lblChosenFile.Text = "unable to open image, error: " + ex.Message       'show error message on label
            Return                                                                  'and exit function
        End Try
        
        If imgOriginal Is Nothing Then                                  'if image could not be opened
            lblChosenFile.Text = "unable to open image"                 'show error message on label
            Return                                                      'and exit function
        End If

        lblChosenFile.Text = ofdOpenFile.FileName           'update label with file name

        For i As Integer = 0 To listOfPossiblePlates.Count - 1                  'close any windows that were open from the
            CvInvoke.cvDestroyWindow("imgThresh" + i.ToString)          'previous time this function was called
        Next

        ibOriginal.Image = imgOriginal              'show original image on main form

        listOfPossiblePlates = DetectPlates.detectPlates(imgOriginal)

        If (listOfPossiblePlates Is Nothing) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        ElseIf (listOfPossiblePlates.Count = 0) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        Else
            'txtInfo.AppendText(vbCrLf + "plate detection complete, " + listOfPossiblePlates.Count.ToString + " possible plates found" + vbCrLf)

            Dim blnKNNTrainingSuccessful = loadKNNDataAndTrainKNN()

            If (blnKNNTrainingSuccessful = False) Then
                txtInfo.AppendText(vbCrLf + "error: KNN traning was not successful" + vbCrLf)
                Return
            End If

            For Each possiblePlate As PossiblePlate In listOfPossiblePlates
                Preprocess.preprocess(possiblePlate.imgPlate, possiblePlate.imgGrayscale, possiblePlate.imgThresh)
                
                possiblePlate.imgThresh = possiblePlate.imgThresh.Resize(1.6, INTER.CV_INTER_LINEAR)            'increase size of plate image for easier viewing and char detection

                'CvInvoke.cvShowImage("imgThresh" + i.ToString, listOfPossiblePlates(i).imgThresh)
                'CvInvoke.cvSaveImage("imgThresh" + i.ToString + ".png", listOfPossiblePlates(i).imgThresh, Nothing)

                possiblePlate.strChars = getCharsFromPlate(possiblePlate.imgThresh)

                'If possiblePlate.strChars = "" Then
                '    Continue For
                'End If

                'txtInfo.AppendText(vbCrLf + "license plate read from image = " + possiblePlate.strChars + vbCrLf)
            Next

                                            'sort plates from most # of chars to least # of chars
            listOfPossiblePlates.Sort(Function(onePlate, otherPlate) otherPlate.strChars.Length.CompareTo(onePlate.strChars.Length))

            Dim licPlate As PossiblePlate = listOfPossiblePlates(0)         'suppose the possible plate with the most # of chars is the plate

            CvInvoke.cvShowImage("name here later", licPlate.imgPlate)
            CvInvoke.cvShowImage("name here later2", licPlate.imgThresh)
            CvInvoke.cvSaveImage("imgThresh.png", licPlate.imgThresh, Nothing)

            If (licPlate.strChars.Length = 0) Then
                txtInfo.AppendText(vbCrLf + "no characters were detected" + licPlate.strChars + vbCrLf)
                Return
            End If

            txtInfo.AppendText(vbCrLf + "license plate read from image = " + licPlate.strChars + vbCrLf)

        End If

    End Sub

End Class
