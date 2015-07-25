'Preprocess.vb

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module Preprocess

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const SMOOTH_FILTER_SIZE As Integer = 5
    Const ADAPTIVE_THRESH_BLOCK_SIZE As Integer = 19
    Const ADAPTIVE_THRESH_WEIGHT As Integer = 9

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub preprocess(imgOriginal As Image(Of Bgr, Byte), ByRef imgGrayscale As Image(Of Gray, Byte), ByRef imgThresh As Image(Of Gray, Byte))

        imgGrayscale = extractValue(imgOriginal)

        Dim imgMaxContrastGrayscale As Image(Of Gray, Byte) = maximizeContrast(imgGrayscale)

        Dim imgBlurred As Image(Of Gray, Byte) = imgMaxContrastGrayscale.SmoothGaussian(SMOOTH_FILTER_SIZE)

        imgThresh = imgBlurred.ThresholdAdaptive(New Gray(255), ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_GAUSSIAN_C, THRESH.CV_THRESH_BINARY_INV, ADAPTIVE_THRESH_BLOCK_SIZE, New Gray(ADAPTIVE_THRESH_WEIGHT))

    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function extractValue(imgOriginal As Image(Of Bgr, Byte)) As Image(Of Gray, Byte)
        Dim imgHSV As Image(Of Hsv, Byte) = imgOriginal.Convert(Of Hsv, Byte)()
        Dim imgChannels As Image(Of Gray, Byte)() = imgHSV.Split()
        Dim imgValue As Image(Of Gray, Byte) = imgChannels(2)

        Return imgValue
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function maximizeContrast(imgGrayscale As Image(Of Gray, Byte)) As Image(Of Gray, Byte)
        Dim imgTopHat As Image(Of Gray, Byte)
        Dim imgBlackHat As Image(Of Gray, Byte)
        Dim imgGrayscalePlusTopHat As Image(Of Gray, Byte)
        Dim imgGrayscalePlusTopHatMinusBlackHat As Image(Of Gray, Byte)

        Dim structuringElementEx As StructuringElementEx = New StructuringElementEx(3, 3, 1, 1, CV_ELEMENT_SHAPE.CV_SHAPE_ELLIPSE)

        imgTopHat = imgGrayscale.MorphologyEx(structuringElementEx, CV_MORPH_OP.CV_MOP_TOPHAT, 1)
        imgBlackHat = imgGrayscale.MorphologyEx(structuringElementEx, CV_MORPH_OP.CV_MOP_BLACKHAT, 1)

        imgGrayscalePlusTopHat = imgGrayscale.Add(imgTopHat)
        imgGrayscalePlusTopHatMinusBlackHat = imgGrayscalePlusTopHat.Sub(imgBlackHat)

        Return imgGrayscalePlusTopHatMinusBlackHat
    End Function

End Module












