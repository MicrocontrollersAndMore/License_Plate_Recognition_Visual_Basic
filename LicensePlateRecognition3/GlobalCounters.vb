'GlobalCounters.vb

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module GlobalCounters
    
    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Public intNumTimesInCheckIfValid As Integer = 0
    Public intNumTimesPastFirstIf As Integer = 0
    Public intNumTimesPastWidthAndHeight As Integer = 0
    Public intNumTimesInFindListOfListsOfMatchingChars As Integer = 0

End Module
