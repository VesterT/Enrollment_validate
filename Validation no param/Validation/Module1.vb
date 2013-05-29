Imports DPUruNet.Fingerbase
Imports DPUruNet
Imports System.Threading
Imports System.Collections.Generic
Imports DPUruNet.Constants
Imports System.IO
Module Module1
    Private _readers As ReaderCollection
    Private count As Integer
    Dim reader As Reader
    Dim fp As String
    Dim Fmds_ver As New List(Of Fmd)
    'Dim sarray() As String

    Public Property Fmds() As Dictionary(Of Int16, Fmd)
        Get
            Return _fmds
        End Get
        Set(ByVal value As Dictionary(Of Int16, Fmd))
            _fmds = value
        End Set
    End Property
    Private _fmds As Dictionary(Of Int16, Fmd) = New Dictionary(Of Int16, Fmd)

    Public Property Reset() As Boolean
        Get
            Return _reset
        End Get
        Set(ByVal value As Boolean)
            _reset = value
        End Set
    End Property
    Private _reset As Boolean

    Public Function OpenReader() As Boolean
        Reset = False
        Dim result As Constants.ResultCode = Constants.ResultCode.DP_DEVICE_FAILURE

        result = reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE)

        If result <> Constants.ResultCode.DP_SUCCESS Then
            Console.WriteLine("Error:  " & result.ToString())
            Reset = True
            Return False
        End If

        Return True
    End Function

    Public Sub GetStatus()
        Dim result = reader.GetStatus()

        If (result <> ResultCode.DP_SUCCESS) Then
            If reader IsNot Nothing Then
                Reset = True
                Throw New Exception("" & result.ToString())
            End If
        End If

        If (reader.Status.Status = ReaderStatuses.DP_STATUS_BUSY) Then
            Thread.Sleep(50)
        ElseIf (reader.Status.Status = ReaderStatuses.DP_STATUS_NEED_CALIBRATION) Then
            reader.Calibrate()
        ElseIf (reader.Status.Status <> ReaderStatuses.DP_STATUS_READY) Then
            Throw New Exception("Reader Status - " & reader.Status.Status.ToString())
        End If
    End Sub

    Public Function CaptureFingerAsync() As Boolean
        Try
            GetStatus()

            Dim captureResult = reader.CaptureAsync(Formats.Fid.ANSI, _
                                                   CaptureProcessing.DP_IMG_PROC_DEFAULT, 500)

            If captureResult <> ResultCode.DP_SUCCESS Then
                reset = True
                Throw New Exception("" + captureResult.ToString())
            End If

            Return True
        Catch ex As Exception
            Console.WriteLine("Error:  " & ex.Message)
            Return False
        End Try
    End Function

    Public Function StartCaptureAsync(ByVal OnCaptured As Reader.CaptureCallback) As Boolean
        AddHandler reader.On_Captured, OnCaptured

        If Not CaptureFingerAsync() Then
            Return False
        End If

        Return True
    End Function

    Public Sub CancelCaptureAndCloseReader(ByVal OnCaptured As Reader.CaptureCallback)
        If reader IsNot Nothing Then
            ' Dispose of reader handle and unhook reader events.

            If (Reset) Then
                reader = Nothing
            End If
        End If
    End Sub

    Sub Main()
        _readers = ReaderCollection.GetReaders
        Dim serial_reader As String
        serial_reader = _readers(0).Description.SerialNumber
        reader = _readers(0)
        count = 0
        'sarray = System.Environment.GetCommandLineArgs()
        OpenReader()
        StartCaptureAsync(AddressOf OnCaptured)
        Console.WriteLine("")
        Console.ReadLine()
    End Sub

    Private Sub OnCaptured(ByVal captureResult As CaptureResult)
        'If (captureResult.Quality = CaptureQuality.DP_QUALITY_CANCELED) Then
        '    Return
        'End If

        'If (captureResult.Quality = CaptureQuality.DP_QUALITY_NO_FINGER _
        '    Or captureResult.Quality = CaptureQuality.DP_QUALITY_TIMED_OUT) Then
        '    'MessageBox.Show("Capture timed out.")
        '    Return
        'End If

        'If (captureResult.Quality = CaptureQuality.DP_QUALITY_FAKE_FINGER) Then
        '    'MessageBox.Show("Possible fake finger was detected.  Try again.")
        '    Return
        'End If

        Dim resultConversion As DataResult(Of Fmd) = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Formats.Fmd.DP_VERIFICATION)

        'If resultConversion.ResultCode <> ResultCode.DP_SUCCESS Then
        '    'MessageBox.Show("Could not create Fmd.  Try again.")
        '    Return
        'End If
        Dim compareResult As DPUruNet.CompareResult

        'For Each fp_a As String In sarray
        compareResult = Comparison.Compare(resultConversion.Data, 0, Fmd.DeserializeXml(File.ReadAllText("c:\temp\cap_fmd_enrolled.txt")), 0)
        Console.WriteLine(IIf(compareResult.Score < (&H7FFFFFFF / 100000), "Usuario Valido", "Usuario Invalido"))
        


        'If compareResult.ResultCode <> Constants.ResultCode.DP_SUCCESS Then

        'End If

        ' Console.WriteLine("Comparison resulted in a dissimilarity score of " & compareResult.Score.ToString() & IIf(compareResult.Score < (&H7FFFFFFF / 100000), " (fingerprints matched)", " (fingerprints did not match)")
    End Sub

End Module
