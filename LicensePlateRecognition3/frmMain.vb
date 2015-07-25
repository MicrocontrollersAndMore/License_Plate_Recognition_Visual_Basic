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
Imports Emgu.CV.OCR

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain
    
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
        
        ibOriginal.Image = imgOriginal

        Dim imgListOfPlates As List(Of Image(Of Bgr, Byte)) = DetectPlates.detectPlates(imgOriginal)

        Dim intPlateCounter As Integer = 0

        If (imgListOfPlates Is Nothing) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        ElseIf (imgListOfPlates.Count = 0) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        Else
            txtInfo.AppendText(vbCrLf + "plate detection complete, " + imgListOfPlates.Count.ToString + " possible plates found" + vbCrLf)

            For Each imgPlate As Image(Of Bgr, Byte) In imgListOfPlates
                Dim imgGrayscale As Image(Of Gray, Byte) = Nothing
                Dim imgThresh As Image(Of Gray, Byte) = Nothing
                
                Preprocess.preprocess(imgPlate, imgGrayscale, imgThresh)

                imgThresh = imgThresh.Resize(1.6, INTER.CV_INTER_LINEAR)

                CvInvoke.cvShowImage("imgThresh" + intPlateCounter.ToString, imgThresh)
                CvInvoke.cvSaveImage("imgThresh" + intPlateCounter.ToString + ".png", imgThresh, Nothing)

                'Dim strLicPlateChars As String = ReadCharacters.readCharacters(imgThresh)

                'Dim tess As Tesseract

                'Try
                '    tess = New Tesseract("tessdata", "eng", Tesseract.OcrEngineMode.OEM_DEFAULT)
                'Catch ex As Exception
                '    txtInfo.AppendText(vbCrLf + "ERROR INSTANTIATING TESSERACT OBJECT" + vbCrLf)
                '    Return
                'End Try
                
                'tess.Recognize(imgThresh)
                
                'Dim strLicPlateChars As String = tess.GetText()

                'txtInfo.AppendText(vbCrLf + "License Plate Characters are " + strLicPlateChars + vbCrLf)

                intPlateCounter = intPlateCounter + 1
            Next
            
        End If
    End Sub
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub frmMain_FormClosing( sender As Object,  e As FormClosingEventArgs) Handles MyBase.FormClosing
        '
    End Sub

End Class
