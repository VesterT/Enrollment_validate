Imports DPUruNet.Fingerbase
Imports DPUruNet
Imports System.Threading
Imports System.Collections.Generic
Imports DPUruNet.Constants
Imports System.IO

Module Module1
    'Declaración de variables globales
    Private _readers As ReaderCollection
    Private count As Integer
    Dim reader As Reader
    Dim fp As String
    Dim preenrollmentFmds As New List(Of Fmd)

    'Metodo que genera las colecciones de huellas
    Public Property Fmds() As Dictionary(Of Int16, Fmd)
        Get
            Return _fmds
        End Get
        Set(ByVal value As Dictionary(Of Int16, Fmd))
            _fmds = value
        End Set
    End Property
    Private _fmds As Dictionary(Of Int16, Fmd) = New Dictionary(Of Int16, Fmd)
    'Metodo que reinicia el lector
    Public Property Reset() As Boolean
        Get
            Return _reset
        End Get
        Set(ByVal value As Boolean)
            _reset = value
        End Set
    End Property
    Private _reset As Boolean
    'Metodo que abre un conexión con el lector
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
    'Metodo que obtiene el estatus de determinado lector de huella
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
                                                   CaptureProcessing.DP_IMG_PROC_DEFAULT, _
                                                    reader.Capabilities.Resolutions(0))

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
    'Funcion que inicia la captura de huella de manera asincrona
    Public Function StartCaptureAsync(ByVal OnCaptured As Reader.CaptureCallback) As Boolean
        AddHandler reader.On_Captured, OnCaptured

        If Not CaptureFingerAsync() Then
            Return False
        End If

        Return True
    End Function
    'Metodo que cancela la captura de huella
    Public Sub CancelCaptureAndCloseReader(ByVal OnCaptured As Reader.CaptureCallback)
        If reader IsNot Nothing Then
            If (Reset) Then
                reader = Nothing
            End If
        End If
    End Sub
    'Metodo principal
    Sub Main()
        'Obtengo la coleccion de lectores conectadosa
        _readers = ReaderCollection.GetReaders
        Dim serial_reader As String
        'Elijo el primer lector, asumiendo que solo existe uno conectado
        serial_reader = _readers(0).Description.SerialNumber
        reader = _readers(0)
        count = 0
        'Abro el lector elegido
        OpenReader()
        'Inicio la captura asincrona
        StartCaptureAsync(AddressOf OnCaptured)
        Console.WriteLine("")
        Console.ReadLine()
    End Sub
    'Metodo que se ejecuta al detectar una captura.
    Public Sub OnCaptured(ByVal captureResult As CaptureResult)
        'Contador para llevar el control de las capturas de las 4 muestras de huella
        count += 1
        'Convierto la huella cpaturada a un objeto para extraer sus propiedades.
        Dim resultConversion As DataResult(Of Fmd) = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Formats.Fmd.DP_PRE_REGISTRATION)
        'Si la huella se capturo correctamente, la agrego a la coleccion de huellas, si no, la descarto
        If resultConversion.ResultCode <> Constants.ResultCode.DP_SUCCESS Then
            Console.WriteLine("no funciona")
        Else
            preenrollmentFmds.Add(resultConversion.Data)
            Console.WriteLine("OK " + count.ToString)
        End If
        'Al recolectar las 4 huellas, en base a la coleccion, genero la huella muestra, la serializo y la escribo en un archivo de texto en la ruta indicada
        If count >= 4 Then
            Dim resultEnrollment As DataResult(Of Fmd) = DPUruNet.Enrollment.CreateEnrollmentFmd(Formats.Fmd.DP_REGISTRATION, preenrollmentFmds)
            fp = Fmd.SerializeXml(resultEnrollment.Data)
            File.WriteAllText("c:\temp\cap_fmd_enrolled.txt", fp)
            Console.WriteLine("Done")
        End If
    End Sub

End Module
