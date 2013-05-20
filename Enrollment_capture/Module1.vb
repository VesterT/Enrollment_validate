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

    'Public Property CurrentReader() As Reader
    '    Get
    '        Return _currentReader
    '    End Get
    '    Set(ByVal value As Reader)
    '        _currentReader = value
    '        'Console.WriteLine("Error:  " & Action.UpdateReaderState)
    '    End Set
    'End Propertybgu
    'Private _currentReader As Reader

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
                                                   CaptureProcessing.DP_IMG_PROC_DEFAULT, _
                                                    reader.Capabilities.Resolutions(0))

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
        OpenReader()
        StartCaptureAsync(AddressOf OnCaptured)
        Console.WriteLine("")
        Console.ReadLine()
    End Sub

    Public Sub OnCaptured(ByVal captureResult As CaptureResult)
        'Try
        ' Check capture quality and throw an error if bad.

        count += 1

        Dim resultConversion As DataResult(Of Fmd) = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Formats.Fmd.DP_PRE_REGISTRATION)
        If resultConversion.ResultCode <> Constants.ResultCode.DP_SUCCESS Then
            Console.WriteLine("no funciona")
            'Console.ReadLine()
        Else
            fp = Fmd.SerializeXml(resultConversion.Data)
            Console.WriteLine("OK")
            Dim path As String = "c:\temp\cap.txt"
            File.WriteAllText(path, fp)
            'Console.ReadLine()
        End If

        Environment.Exit(0)


        '    preenrollmentFmds.Add(resultConversion.Data)

        '    SendMessage(Action.SendMessage, "A finger was captured.  " & vbCrLf & "Count:  " & (count.ToString()))

        '    If count >= 4 Then
        '        Dim resultEnrollment As DataResult(Of Fmd) = DPUruNet.Enrollment.CreateEnrollmentFmd(Formats.Fmd.ANSI, preenrollmentFmds)

        '        If resultEnrollment.ResultCode = ResultCode.DP_SUCCESS Then
        '            SendMessage(Action.SendMessage, "An enrollment FMD was successfully created.")
        '            SendMessage(Action.SendMessage, "Place a finger on the reader.")
        '            count = 0
        '            preenrollmentFmds.Clear()
        '            Return
        '        ElseIf (resultEnrollment.ResultCode = Constants.ResultCode.DP_ENROLLMENT_INVALID_SET) Then
        '            SendMessage(Action.SendMessage, "Enrollment was unsuccessful.  Please try again.")
        '            SendMessage(Action.SendMessage, "Place a finger on the reader.")
        '            count = 0
        '            preenrollmentFmds.Clear()
        '            Return
        '        End If
        '    End If

        '    SendMessage(Action.SendMessage, "Now place the same finger on the reader.")
        'Catch ex As Exception
        '    SendMessage(Action.SendMessage, "Error:  " & ex.Message)
        'End Try
    End Sub

End Module
