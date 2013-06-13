Imports DPUruNet.Fingerbase
Imports DPUruNet
Imports System.Threading
Imports System.Collections.Generic
Imports DPUruNet.Constants
Imports System.IO
Module Module1

    'Declaro variables globales
    Private _readers As ReaderCollection
    Private count As Integer
    Dim reader As Reader
    Dim fp As String
    Dim Fmds_ver As New List(Of Fmd)

    'Metodo que almacena la coleccion de huellas
    Public Property Fmds() As Dictionary(Of Int16, Fmd)
        Get
            Return _fmds
        End Get
        Set(ByVal value As Dictionary(Of Int16, Fmd))
            _fmds = value
        End Set
    End Property
    Private _fmds As Dictionary(Of Int16, Fmd) = New Dictionary(Of Int16, Fmd)
    'Metodo que reinicia el lector de huella
    Public Property Reset() As Boolean
        Get
            Return _reset
        End Get
        Set(ByVal value As Boolean)
            _reset = value
        End Set
    End Property
    Private _reset As Boolean
    'Metodo que abre determinado lector de huella
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
    'Metodo que regresa el status del lector de huella
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
    'Metodo que captura la huella asincronamente
    Public Function CaptureFingerAsync() As Boolean
        Try
            GetStatus()

            Dim captureResult = reader.CaptureAsync(Formats.Fid.ANSI, _
                                                   CaptureProcessing.DP_IMG_PROC_DEFAULT, 500)

            If captureResult <> ResultCode.DP_SUCCESS Then
                Reset = True
                Throw New Exception("" + captureResult.ToString())
            End If

            Return True
        Catch ex As Exception
            Console.WriteLine("Error:  " & ex.Message)
            Return False
        End Try
    End Function
    'metodo que inicia la captura asincrona
    Public Function StartCaptureAsync(ByVal OnCaptured As Reader.CaptureCallback) As Boolean
        AddHandler reader.On_Captured, OnCaptured

        If Not CaptureFingerAsync() Then
            Return False
        End If

        Return True
    End Function

    Public Sub CancelCaptureAndCloseReader(ByVal OnCaptured As Reader.CaptureCallback)
        If reader IsNot Nothing Then
            'Metodo para cerrar el lector de huellas

            If (Reset) Then
                reader = Nothing
            End If
        End If
    End Sub

    Sub Main()
        'Obtengo la coleccion de lectores de huella conectados al equipo, asumo que solo hay uno conectado y lo elijo.
        _readers = ReaderCollection.GetReaders
        Dim serial_reader As String
        serial_reader = _readers(0).Description.SerialNumber
        reader = _readers(0)
        count = 0
        'Abro el lector elejido e inicio el proceso de captura asincrona
        OpenReader()
        StartCaptureAsync(AddressOf OnCaptured)
        Console.WriteLine("")
        Console.ReadLine()
    End Sub
    'Metodo que se ejcuta en cuanto se captura una huella
    Private Sub OnCaptured(ByVal captureResult As CaptureResult)
        'Genero el objecto con la huella en base a la huella capturada
        Dim resultConversion As DataResult(Of Fmd) = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Formats.Fmd.DP_VERIFICATION)

        'Genero un objeto para la clase de comnparacion de huellas
        Dim compareResult As DPUruNet.CompareResult

        'Comparo la huella capturada con la huella guardada en la ruta especificada, de-serializandola previamente
        compareResult = Comparison.Compare(resultConversion.Data, 0, Fmd.DeserializeXml(File.ReadAllText("c:\temp\cap_fmd_enrolled.txt")), 0)
        'Salida indicando si el usuario es valido o no
        Console.WriteLine(IIf(compareResult.Score < (&H7FFFFFFF / 100000), "Usuario Valido", "Usuario Invalido"))
    End Sub

End Module
