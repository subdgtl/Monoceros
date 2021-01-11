Imports Rhino
Imports Rhino.Geometry
Imports Grasshopper
Imports Grasshopper.Kernel
Imports Grasshopper.Kernel.Types
Imports Rhino.DocObjects
Imports System.Runtime.CompilerServices
Imports GH_IO.Serialization
Imports System.Drawing

Public Class GH_Cloud

    Inherits GH_GeometricGoo(Of PointCloud)
    Implements IGH_PreviewData, IGH_BakeAwareData
    Private ReferenceGuid As Guid
    Private DisplayCloud As PointCloud
    Private ScanPos As Plane = Plane.WorldXY

    Private Function CreatePosLines(S As Double) As List(Of Line)
        Dim nl As New List(Of Line)

        For i As Double = -S To S Step 2 * S
            Dim l1 As New Line(New Point3d(-S, i, 0), New Point3d(S, i, 0))
            Dim l2 As New Line(New Point3d(i, -S, 0), New Point3d(i, S, 0))
            Dim t As Transform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, ScanPos)
            l1.Transform(t)
            l2.Transform(t)
            nl.Add(l1)
            nl.Add(l2)
        Next

        Return nl
    End Function

    Private Function CreateAxes(S As Double) As List(Of Line)
        Dim nl As New List(Of Line)
        nl.Add(New Line(New Point3d(0, 0, 0), New Point3d(S * 1.5, 0, 0)))
        nl.Add(New Line(New Point3d(0, 0, 0), New Point3d(0, S * 1.5, 0)))
        nl.Add(New Line(New Point3d(0, 0, 0), New Point3d(0, 0, S * 1.5)))
        Dim t As Transform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, ScanPos)
        For i As Integer = 0 To nl.Count - 1 Step 1
            Dim l As Line = nl(i)
            l.Transform(t)
            nl(i) = l
        Next
        Return nl
    End Function

#Region "Constructors"

    Public Sub New()
        MyBase.New()
        Me.ReferenceGuid = Guid.Empty
        Me.ScanPos = Plane.WorldXY
    End Sub

    Public Sub New(ByVal other As GH_Cloud)
        MyBase.New()
        Me.ReferenceGuid = Guid.Empty

        If (other Is Nothing) Then
            Throw New ArgumentException("other")
        End If
        Me.ReferenceGuid = other.ReferenceGuid

        If (other.m_value IsNot Nothing) Then
            Me.m_value = other.m_value.Duplicate
        End If
        Me.ScanPos = other.ScanPos
    End Sub

    Public Sub New(ByVal c As PointCloud)
        MyBase.New(c)
        Me.ReferenceGuid = Guid.Empty
        Me.ScanPos = Plane.WorldXY
    End Sub

    Public Sub New(ByVal RefGuid As Guid)
        MyBase.New()
        Me.ReferenceGuid = Guid.Empty
        Me.ReferenceGuid = RefGuid
        Me.ScanPos = Plane.WorldXY
    End Sub

    Public Overrides Function Duplicate() As IGH_Goo
        Return Me.DuplicateCloud
    End Function

    Public Function DuplicateCloud() As GH_Cloud
        Return New GH_Cloud(Me)
    End Function

    Public Overrides Function DuplicateGeometry() As IGH_GeometricGoo
        Return Me.DuplicateCloud
    End Function

#End Region

#Region "Casting"

    Public Overrides Function CastFrom(ByVal source As Object) As Boolean
        Dim target As GH_Cloud = Me
        Return GH_CloudConvert.ToGHCloud(RuntimeHelpers.GetObjectValue(source), GH_Conversion.Both, target)
    End Function

    Public Overrides Function CastTo(Of Q)(ByRef target As Q) As Boolean

        If Not GetType(Q).IsAssignableFrom(GetType(PointCloud)) Then
            Return False
        End If

        If (Me.m_value Is Nothing) Then
            Return False
        End If

        Dim obj2 As Object = Me.m_value.DuplicateShallow

        target = DirectCast(obj2, Q)
        Return True

    End Function

#End Region

#Region "Rhino connection"

    Public Function BakeGeometry(ByVal doc As RhinoDoc, ByVal att As ObjectAttributes, ByRef obj_guid As Guid) As Boolean Implements IGH_BakeAwareData.BakeGeometry
        If Not Me.IsValid Then
            Return False
        End If
        obj_guid = doc.Objects.AddPointCloud(MyBase.m_value, att)
        Return True
    End Function

    Public Overrides Function LoadGeometry(ByVal doc As RhinoDoc) As Boolean
        Dim obj2 As RhinoObject = doc.Objects.Find(Me.ReferenceID)
        If (Not obj2 Is Nothing) Then
            Select Case obj2.Geometry.ObjectType
                Case ObjectType.PointSet
                    If TypeOf obj2.Geometry Is PointCloud Then
                        MyBase.m_value = DirectCast(obj2.Geometry, PointCloud).DuplicateShallow
                        Me.ScanPos = Plane.WorldXY
                        Return True
                    End If
                    Exit Select
            End Select
        End If
        Return False
    End Function

    Public Overrides Sub ClearCaches()
        If Me.IsReferencedGeometry Then
            MyBase.m_value = Nothing
        End If
    End Sub

    Public Overrides Function EmitProxy() As IGH_GooProxy
        Return New GH_CloudProxy(Me)
    End Function

    Public Overrides Property ReferenceID As Guid
        Get
            Return Me.ReferenceGuid
        End Get
        Set(ByVal value As Guid)
            Me.ReferenceGuid = value
        End Set
    End Property

    Public Overrides ReadOnly Property IsGeometryLoaded As Boolean
        Get
            Return (Not MyBase.m_value Is Nothing)
        End Get
    End Property

#End Region

#Region "Drawing"

    Public Sub DrawViewportWires(ByVal args As GH_PreviewWireArgs) Implements IGH_PreviewData.DrawViewportWires
        If (Not MyBase.m_value Is Nothing) Then

            If Settings_Global.DisplayPositions Then
                args.Pipeline.DrawLines(CreatePosLines(Grasshopper.CentralSettings.PreviewPlaneRadius), args.Color, args.Thickness * 2)
                Dim l As List(Of Line) = CreateAxes(Grasshopper.CentralSettings.PreviewPlaneRadius)

                Dim curcol As Color = Grasshopper.Instances.ActiveCanvas.Document.PreviewColourSelected
                curcol = Color.FromArgb(255, curcol.R, curcol.G, curcol.B)

                If curcol = args.Color Then
                    args.Pipeline.DrawLine(l(0), args.Color, args.Thickness * 2)
                    args.Pipeline.DrawLine(l(1), args.Color, args.Thickness * 2)
                    args.Pipeline.DrawLine(l(2), args.Color, args.Thickness * 2)
                Else
                    args.Pipeline.DrawLine(l(0), Rhino.ApplicationSettings.AppearanceSettings.GridXAxisLineColor, args.Thickness * 2)
                    args.Pipeline.DrawLine(l(1), Rhino.ApplicationSettings.AppearanceSettings.GridYAxisLineColor, args.Thickness * 2)
                    args.Pipeline.DrawLine(l(2), Rhino.ApplicationSettings.AppearanceSettings.GridZAxisLineColor, args.Thickness * 2)
                End If
            End If

            If Settings_Global.DisplayDynamic Then
                If args.Pipeline.IsDynamicDisplay Then
                    ResolveDisplay()
                    args.Pipeline.DrawPointCloud(DisplayCloud, Settings_Global.DisplayRadius + 1, args.Color)
                Else
                    args.Pipeline.DrawPointCloud(MyBase.m_value, Settings_Global.DisplayRadius, args.Color)
                End If
            Else
                args.Pipeline.DrawPointCloud(MyBase.m_value, Settings_Global.DisplayRadius, args.Color)
            End If

        End If
    End Sub

    Private Sub ResolveDisplay()
        Dim rnd As New Random()

        DisplayCloud = Nothing
        DisplayCloud = New PointCloud()

        Select Case m_value.Count
            Case Is < 1000
                DisplayCloud.AddRange(m_value.GetPoints)
            Case Else
                For i As Integer = 0 To m_value.Count - 1 Step 1000
                    Dim rndint64 As Int64 = rnd.Next(0, m_value.Count - 1)
                    DisplayCloud.Add(m_value.Item(rndint64).Location)
                    DisplayCloud(DisplayCloud.Count - 1).Color = m_value.Item(rndint64).Color
                Next
        End Select

    End Sub

    Public Sub DrawViewportMeshes(args As GH_PreviewMeshArgs) Implements IGH_PreviewData.DrawViewportMeshes
    End Sub

    Public Overrides Function GetBoundingBox(xform As Transform) As BoundingBox
        If (MyBase.m_value Is Nothing) Then
            Return BoundingBox.Empty
        End If
        Return MyBase.m_value.GetBoundingBox(xform)
    End Function

    Public Overrides ReadOnly Property Boundingbox As BoundingBox
        Get
            If (MyBase.m_value Is Nothing) Then
                Return BoundingBox.Empty
            End If
            Return MyBase.m_value.GetBoundingBox(True)
        End Get
    End Property

    Public ReadOnly Property ClippingBox As BoundingBox Implements IGH_PreviewData.ClippingBox
        Get
            Return Me.Boundingbox
        End Get
    End Property

#End Region

#Region "Transformations"

    Public Overrides Function Transform(ByVal xform As Transform) As IGH_GeometricGoo
        If Not Me.IsValid Then
            Return Nothing
        End If

        m_value.Transform(xform)

        If m_value.ContainsNormals Then
            For Each pci As PointCloudItem In m_value
                Dim norm As Vector3d = pci.Normal
                norm.Transform(xform)
                pci.Normal = norm
            Next
        End If

        Me.ReferenceID = Guid.Empty
        Me.ScanPos.Transform(xform)
        Return Me
    End Function

    Public Overrides Function Morph(ByVal xmorph As SpaceMorph) As IGH_GeometricGoo
        If Not Me.IsValid Then
            Return Nothing
        End If

        Dim d As Double = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 1000
        Dim yypl As Plane = Plane.WorldXY
        Dim xxpl As Plane = New Plane(New Point3d(0, 0, 0), New Vector3d(0, -1, 0), New Vector3d(1, 0, 0))
        Dim orig As Point3d = Me.ScannerPosition.Origin

        yypl.Translate(New Vector3d(-d, 0, 0))
        xxpl.Translate(New Vector3d(0, d, 0))

        Dim cyy As New Circle(yypl, d)
        Dim cxx As New Circle(xxpl, d)

        cyy.Transform(Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, Me.ScannerPosition))
        cxx.Transform(Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, Me.ScannerPosition))

        Dim ccy As Curve = cyy.ToNurbsCurve
        Dim ccx As Curve = cxx.ToNurbsCurve

        xmorph.Morph(ccy)
        xmorph.Morph(ccx)
        orig = xmorph.MorphPoint(orig)

        Me.ScannerPosition = New Plane(orig, ccx.TangentAt(0), ccy.TangentAt(0))

        xmorph.Morph(MyBase.m_value)
        Me.ReferenceID = Guid.Empty

        Return Me
    End Function

#End Region

#Region "Other"

    Public Overrides Function ToString() As String

        If m_value.UserDictionary.ContainsKey("VolvoxFilePath") Then Return ("Cloud will be loaded from: " & m_value.UserDictionary.GetString("VolvoxFilePath"))

        If Me.IsReferencedGeometry Then
            If m_value IsNot Nothing Then Return ("Referenced Point Cloud with " & m_value.Count & " points")
        Else
            If m_value IsNot Nothing Then Return ("Point Cloud with " & m_value.Count & " points")
        End If

        Return "Null Cloud"
    End Function

    Public Overrides ReadOnly Property IsValid As Boolean
        Get
            If (MyBase.m_value Is Nothing) Then
                Return False
            End If
            Return MyBase.m_value.IsValid
        End Get
    End Property

    Public Overrides ReadOnly Property IsValidWhyNot As String
        Get
            Return ("I really don't know, sorry.")
        End Get
    End Property

    Public Overrides ReadOnly Property TypeDescription As String
        Get
            Return "Point Cloud wrapper"
        End Get
    End Property

    Public Overrides ReadOnly Property TypeName As String
        Get
            Return "Cloud"
        End Get
    End Property

#End Region

#Region "Properties"

    Public Property ScannerPosition As Plane
        Get
            Return Me.ScanPos
        End Get
        Set(ByVal value As Plane)
            Me.ScanPos = value
        End Set
    End Property

#End Region

#Region "Proxy"

    Public Class GH_CloudProxy
        Inherits GH_GooProxy(Of GH_Cloud)
        ' Methods
        Public Sub New(ByVal owner As GH_Cloud)
            MyBase.New(owner)
        End Sub

        Public Overrides Sub Construct()
            Try
                Instances.DocumentEditorFadeOut()
                Dim cloud As GH_Cloud = Nothing

                If (Not cloud Is Nothing) Then
                    Me.Owner.m_value = cloud.m_value
                    Me.Owner.ReferenceGuid = cloud.ReferenceGuid
                    Me.Owner.LoadGeometry()
                    Me.Owner.ScanPos = Plane.WorldXY
                End If
            Finally
                Instances.DocumentEditorFadeIn()
            End Try
        End Sub

        Public Overrides Function FromString(ByVal [in] As String) As Boolean
            Return False
        End Function

        Public Property ObjectID As String
            Get
                If Me.Owner.IsReferencedGeometry Then
                    Return String.Format("{0}", Me.Owner.ReferenceID)
                End If
                Return "none"
            End Get
            Set(ByVal value As String)
                If Me.Owner.IsReferencedGeometry Then
                    Try
                        Dim guid As New Guid(value)
                        Me.Owner.ReferenceID = guid
                        Me.Owner.ClearCaches()
                        Me.Owner.LoadGeometry()
                        Me.Owner.ScanPos = Plane.WorldXY
                    Catch exception1 As Exception
                        'ProjectData.SetProjectError(exception1)
                        'Dim exception As Exception = exception1
                        'ProjectData.ClearProjectError()
                    End Try
                End If
            End Set
        End Property

        Public ReadOnly Property Type As String
            Get
                If (Me.Owner.Value Is Nothing) Then
                    Return "No cloud"
                End If
                If TypeOf Me.Owner.Value Is PointCloud Then
                    Return "Point Cloud"
                End If

                Return "Other"
            End Get
        End Property

    End Class

#End Region

#Region "Serialize"

    Public Overrides Function Read(ByVal reader As GH_IReader) As Boolean
        Me.ReferenceGuid = Guid.Empty
        MyBase.m_value = Nothing
        Me.ReferenceGuid = reader.GetGuid("RefID")

        Dim nghpl As New GH_IO.Types.GH_Plane
        nghpl = reader.GetPlane("ScannerPosition")

        Dim pto As GH_IO.Types.GH_Point3D = nghpl.Origin
        Dim ptx As GH_IO.Types.GH_Point3D = nghpl.XAxis
        Dim pty As GH_IO.Types.GH_Point3D = nghpl.YAxis
        Me.ScanPos = New Plane(New Point3d(pto.x, pto.y, pto.z), New Point3d(ptx.x, ptx.y, ptx.z), New Point3d(pty.x, pty.y, pty.z))

        If reader.ItemExists("ON_Data") Then
            Dim byteArray As Byte() = reader.GetByteArray("ON_Data")
            MyBase.m_value = GH_Convert.ByteArrayToCommonObject(Of PointCloud)(byteArray)
        End If
        Return True
    End Function

    Public Overrides Function Write(ByVal writer As GH_IWriter) As Boolean
        writer.SetGuid("RefID", Me.ReferenceGuid)

        Dim ptx As New Point3d(Me.ScanPos.Origin + Me.ScanPos.XAxis)
        Dim pty As New Point3d(Me.ScanPos.Origin + Me.ScanPos.YAxis)
        Dim xp As New GH_IO.Types.GH_Point3D(ptx.X, ptx.Y, ptx.Z)
        Dim yp As New GH_IO.Types.GH_Point3D(pty.X, pty.Y, pty.Z)
        Dim op As New GH_IO.Types.GH_Point3D(Me.ScanPos.Origin.X, Me.ScanPos.Origin.Y, Me.ScanPos.Origin.Z)
        writer.SetPlane("ScannerPosition", New GH_IO.Types.GH_Plane(op, xp, yp))

        If ((Me.ReferenceID = Guid.Empty) AndAlso (Not MyBase.m_value Is Nothing)) Then
            Dim buffer As Byte() = GH_Convert.CommonObjectToByteArray(MyBase.m_value)
            If (Not buffer Is Nothing) Then
                writer.SetByteArray("ON_Data", buffer)
            End If
        End If

        Return True
    End Function

#End Region

End Class