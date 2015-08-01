'frmMain.vb

'using Emgu CV 2.4.10

'add the following components to your form:
'tableLayoutPanel (TableLayoutPanel)
'btnOpenFile (Button)
'lblChosenFile (Label)
'ibOriginal (TextBox)
'ckbShowSteps (CheckBox)
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
    Const IMAGE_BOX_PCT_SHOW_STEPS_NOT_CHECKED As Single = 75
    Const TEXT_BOX_PCT_SHOW_STEPS_NOT_CHECKED  As Single = 25

    Const IMAGE_BOX_PCT_SHOW_STEPS_CHECKED As Single = 55
    Const TEXT_BOX_PCT_SHOW_STEPS_CHECKED As Single = 45

    'Dim listOfPossiblePlates As List(Of PossiblePlate) = New List(Of PossiblePlate)

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub frmMain_Load( sender As Object,  e As EventArgs) Handles MyBase.Load
        ckbShowSteps_CheckedChanged(New Object, New EventArgs)
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub ckbShowSteps_CheckedChanged( sender As Object,  e As EventArgs) Handles ckbShowSteps.CheckedChanged
        If (ckbShowSteps.Checked = False) Then
            tableLayoutPanel.RowStyles.Item(1).Height = IMAGE_BOX_PCT_SHOW_STEPS_NOT_CHECKED
            tableLayoutPanel.RowStyles.Item(2).Height = TEXT_BOX_PCT_SHOW_STEPS_NOT_CHECKED
        ElseIf (ckbShowSteps.Checked = True) Then
            tableLayoutPanel.RowStyles.Item(1).Height = IMAGE_BOX_PCT_SHOW_STEPS_CHECKED
            tableLayoutPanel.RowStyles.Item(2).Height = TEXT_BOX_PCT_SHOW_STEPS_CHECKED
        End If
    End Sub

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

        CvInvoke.cvDestroyWindow("final imgPlate")
        CvInvoke.cvDestroyWindow("final imgThresh")

        ibOriginal.Image = imgOriginal              'show original image on main form

        Dim listOfPossiblePlates As List(Of PossiblePlate) = DetectPlates.detectPlates(imgOriginal)

        Dim blnKNNTrainingSuccessful As Boolean = loadKNNDataAndTrainKNN()

        If (blnKNNTrainingSuccessful = False) Then
            txtInfo.AppendText(vbCrLf + "error: KNN traning was not successful" + vbCrLf)
            Return
        End If

        listOfPossiblePlates = DetectChars.detectChars(listOfPossiblePlates)

        If (listOfPossiblePlates Is Nothing) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        ElseIf (listOfPossiblePlates.Count = 0) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        Else
                    'if we get in here list of possible plates has at leat one plate

                                'sort the list of possible plates in DESCENDING order (most number of chars to least number of chars)
            listOfPossiblePlates.Sort(Function(onePlate, otherPlate) otherPlate.strChars.Length.CompareTo(onePlate.strChars.Length))

                                                                            'suppose the plate with the most recognized chars
            Dim licPlate As PossiblePlate = listOfPossiblePlates(0)         '(the first plate in sorted by string length descending order)
                                                                            'is the actual plate

            CvInvoke.cvShowImage("final imgPlate", licPlate.imgPlate)              'show the final color plate image 
            CvInvoke.cvShowImage("final imgThresh", licPlate.imgThresh)            'show the final thresh plate image
            CvInvoke.cvSaveImage("imgThresh.png", licPlate.imgThresh, Nothing)      'save thresh image to file
            
            If (licPlate.strChars.Length = 0) Then                                                          'if no chars are present in the lic plate,
                txtInfo.AppendText(vbCrLf + "no characters were detected" + licPlate.strChars + vbCrLf)     'update info text box
                Return                                                                                      'and return
            End If

            imgOriginal.Draw(licPlate.b2dLocationOfPlateInScene, New Bgr(Color.Red), 2)         'draw red rectangle around plate

            txtInfo.AppendText(vbCrLf + "license plate read from image = " + licPlate.strChars + vbCrLf)        'update info text box with license plate read
            txtInfo.AppendText(vbCrLf + "----------------------------------------" + vbCrLf)

            Dim font As MCvFont = New MCvFont(CvEnum.FONT.CV_FONT_HERSHEY_SIMPLEX, 2.0, 2.0)        'use plane jane font
            font.thickness = 3                                                                      'make text bold

            Dim textSize As Size
            Dim intBaseline As Integer

            CvInvoke.cvGetTextSize(licPlate.strChars, font, textSize, intBaseline)

            Dim ptBottomLeftOfFirstChar As New Point(CInt(licPlate.b2dLocationOfPlateInScene.center.X - CSng(textSize.Width) / 2.0), _
                                                     CInt(licPlate.b2dLocationOfPlateInScene.center.Y + licPlate.b2dLocationOfPlateInScene.size.Height + CSng(textSize.Height)))

            imgOriginal.Draw(licPlate.strChars, font, ptBottomLeftOfFirstChar, New Bgr(Color.Yellow))       'write text of license plate on the image

            ibOriginal.Image = imgOriginal              'update form with updated original image
        End If

    End Sub

End Class


