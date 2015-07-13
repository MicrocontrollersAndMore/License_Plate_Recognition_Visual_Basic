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
    Const MAX_CLUSTER_GRADIENT_DIFF As Double = 0.1
    Const MAX_CLUSTER_DIST_FACTOR As Double = 1.5
    Const MIN_CLUSTER_SIZE As Integer = 3

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectPlates(imgOriginal As Image(Of Bgr, Byte)) As List(Of Image(Of Bgr, Byte))
        Dim imgGrayscale As Image(Of Gray, Byte) = Nothing
        Dim imgThresh As Image(Of Gray, Byte) = Nothing

        Preprocess.preprocess(imgOriginal, imgGrayscale, imgThresh)
        


        Dim listOfPossibleChars As List(Of PossibleChar)

        listOfPossibleChars = findPossibleChars(imgGrayscale, imgThresh)
        
        Dim listOfClusters As List(Of List(Of PossibleChar))
        
        'listOfClusters = findClusters(listOfPossibleChars)
        
        Dim listsOfMatchingChars As List(Of List(Of PossibleChar))
        
        listsOfMatchingChars = findListsOfMatchingChars

        
        Dim imgListOfPlates As List(Of Image(Of Bgr, Byte)) = Nothing

        For Each cluster As List(Of PossibleChar) In listOfClusters
            Dim imgPlate As Image(Of Bgr, Byte) = extractPlate(imgOriginal, cluster)



        Next
        

        Return imgListOfPlates
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleChars(imgGrayscale As Image(Of Gray, Byte), imgThresh As Image(Of Gray, Byte)) As List(Of PossibleChar)
        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)()
        Dim listOfContours As List(Of Contour(Of Point)) = New List(Of Contour(Of Point))()

        Dim contours As Contour(Of Point) = imgThresh.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST)

        While (Not contours Is Nothing)
            Dim contour As Contour(Of Point) = contours.ApproxPoly(contours.Perimeter * 0.0001)

            Dim possibleChar As PossibleChar = New PossibleChar(contour)

            If (possibleChar.checkIfValid()) Then
                possibleChar.calcAvgAndStdDev(imgGrayscale)
                listOfPossibleChars.Add(possibleChar)
            End If

            contours = contours.HNext
        End While

        Return listOfPossibleChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findClusters(listOfPossibleChars As List(Of PossibleChar)) As List(Of List(Of PossibleChar))
        Dim listOfClusters As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))





        Return listOfClusters
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function extractPlate(imgOriginal As Image(Of Bgr, Byte), cluster As List(Of PossibleChar)) As List(Of Image(Of Bgr, Byte))
        Dim imgPlate As Image(Of Bgr, Byte)




        Return imgPlate
    End Function










End Module
