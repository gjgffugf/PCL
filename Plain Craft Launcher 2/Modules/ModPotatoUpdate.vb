Imports Newtonsoft.Json.Linq

''' <summary>
''' pcl-potato 的在线更新检查模块。
''' 从三个地址依次尝试获取 update.json，任一成功即可；全部失败则静默跳过，不打扰用户。
''' </summary>
Public Module ModPotatoUpdate

#Region "配置（改这里就够了）"

    ''' <summary>
    ''' 更新信息 JSON 的地址列表。按顺序尝试，前一个失败会自动回退到下一个。
    ''' 想增减镜像源，直接改这个数组。
    ''' </summary>
    Public ReadOnly UpdateUrls As String() = {
        "https://xiaof.hyperphp.com/pcl-potato/update.json",
        "https://f.ffurry.cc.cd/pcl-potato/update.json",
        "https://pcl-potato.xfan.ccwu.cc/update.json",
        "https://gh-proxy.com/https://raw.githubusercontent.com/gjgffugf/pcl/main/update.json"
    }

    ''' <summary>
    ''' 官网地址：更新弹窗的「下载」按钮会打开它。
    ''' 若 JSON 里提供了 downloadUrl，则优先使用 JSON 里的。
    ''' </summary>
    Public Const OfficialSiteUrl As String = "https://xiaof.hyperphp.com/pcl-potato/"

    ''' <summary>
    ''' 单个源的超时时间（毫秒）。三个源都失败才会放弃。
    ''' </summary>
    Public Const UpdateTimeout As Integer = 10000

    '设置项键名：记录已经弹过的是哪个版本，避免同一版本反复弹窗
    Private Const SettingKeyPrompted As String = "PotatoUpdatePromptedCode"

#End Region

#Region "数据结构"

    ''' <summary>
    ''' 从 update.json 解析出来的更新信息。
    ''' </summary>
    Public Class PotatoUpdateInfo
        ''' <summary>程序名，通常为 pcl-potato。</summary>
        Public Property Name As String = "pcl-potato"
        ''' <summary>最新版本的显示名，如 3.0.1。</summary>
        Public Property LatestVersion As String = ""
        ''' <summary>最新版本的整数版本号，与本地 VersionCode 比较。留空或 0 则只按 LatestVersion 文本比较。</summary>
        Public Property LatestVersionCode As Integer = 0
        ''' <summary>弹窗标题。</summary>
        Public Property Title As String = ""
        ''' <summary>弹窗正文，多行用 \n 分隔。</summary>
        Public Property Content As String = ""
        ''' <summary>「下载」按钮打开的网址。</summary>
        Public Property DownloadUrl As String = ""
        ''' <summary>发布日期，仅作展示。</summary>
        Public Property PublishDate As String = ""
        ''' <summary>是否强制提示：为 True 时忽略「不再提示」，每次启动都弹。</summary>
        Public Property ForceUpdate As Boolean = False
    End Class

#End Region

#Region "获取与解析"

    ''' <summary>
    ''' 依次尝试所有源，返回第一个成功拿到的 JSON 文本。全部失败返回 Nothing。
    ''' </summary>
    Private Function FetchUpdateJson() As String
        For Each Url In UpdateUrls
            Try
                Logger.Info($"[pcl-potato] 正在获取更新信息：{Url}")
                Dim Text As String = NetRequestByClient(Url, Timeout:=UpdateTimeout, RequireJson:=True, MakeLog:=False)
                If Text Is Nothing Then Continue For
                Text = Text.Trim()
                If Text.StartsWith("{", StringComparison.Ordinal) AndAlso Text.EndsWith("}", StringComparison.Ordinal) Then
                    Logger.Info($"[pcl-potato] 更新信息获取成功：{Url}")
                    Return Text
                End If
                Logger.Warn($"[pcl-potato] {Url} 返回的内容不是 JSON，尝试下一个源")
            Catch ex As Exception
                Logger.Warn(ex, $"[pcl-potato] 从 {Url} 获取更新信息失败，尝试下一个源")
            End Try
        Next
        Return Nothing
    End Function

    ''' <summary>
    ''' 把 JSON 文本解析为 PotatoUpdateInfo。字段缺失时用默认值兜底，不会因为字段缺失而崩。
    ''' </summary>
    Private Function ParseUpdateInfo(Json As String) As PotatoUpdateInfo
        Dim Info As New PotatoUpdateInfo
        Dim Obj As JObject = JObject.Parse(Json)
        Info.Name = GetJsonString(Obj, "name", "pcl-potato")
        Info.LatestVersion = GetJsonString(Obj, "latestVersion", "")
        Info.LatestVersionCode = GetJsonInteger(Obj, "latestVersionCode", 0)
        Info.Title = GetJsonString(Obj, "title", "")
        Info.Content = GetJsonString(Obj, "content", "")
        Info.DownloadUrl = GetJsonString(Obj, "downloadUrl", "")
        Info.PublishDate = GetJsonString(Obj, "publishDate", "")
        Info.ForceUpdate = GetJsonBoolean(Obj, "forceUpdate", False)
        'JSON 里用 \n 表示换行，转成真正的换行
        Info.Content = Info.Content.Replace("\n", vbCrLf)
        Return Info
    End Function

    Private Function GetJsonString(Obj As JObject, Key As String, DefaultValue As String) As String
        Dim Token = Obj(Key)
        If Token Is Nothing OrElse Token.Type = JTokenType.Null Then Return DefaultValue
        Dim Value = Token.ToString()
        Return If(String.IsNullOrWhiteSpace(Value), DefaultValue, Value)
    End Function
    Private Function GetJsonInteger(Obj As JObject, Key As String, DefaultValue As Integer) As Integer
        Dim Token = Obj(Key)
        If Token Is Nothing OrElse Token.Type = JTokenType.Null Then Return DefaultValue
        Dim Value As Integer
        If Integer.TryParse(Token.ToString(), Value) Then Return Value
        Return DefaultValue
    End Function
    Private Function GetJsonBoolean(Obj As JObject, Key As String, DefaultValue As Boolean) As Boolean
        Dim Token = Obj(Key)
        If Token Is Nothing OrElse Token.Type = JTokenType.Null Then Return DefaultValue
        Dim Value As Boolean
        If Boolean.TryParse(Token.ToString(), Value) Then Return Value
        Return DefaultValue
    End Function

    ''' <summary>
    ''' 判断远端版本是否比本地新。
    ''' 优先比整数版本号；若 JSON 没给 latestVersionCode，则退回比较版本名文本。
    ''' </summary>
    Private Function IsNewer(Info As PotatoUpdateInfo) As Boolean
        If Info.LatestVersionCode > 0 Then Return Info.LatestVersionCode > VersionCode
        If String.IsNullOrWhiteSpace(Info.LatestVersion) Then Return False
        Return Not String.Equals(Info.LatestVersion.Trim(), VersionBaseName.Trim(), StringComparison.OrdinalIgnoreCase)
    End Function

#End Region

#Region "对外入口"

    ''' <summary>
    ''' 在后台线程检查更新，若有新版本则弹窗。失败静默，不弹任何错误。
    ''' 启动时调用一次即可。
    ''' </summary>
    Public Sub PotatoUpdateCheck(Optional Silent As Boolean = True)
        RunInNewThread(
        Sub()
            Try
                Dim Json As String = FetchUpdateJson()
                If Json Is Nothing Then
                    Logger.Info("[pcl-potato] 三个源均未能获取更新信息，跳过本次检查")
                    Exit Sub
                End If
                Dim Info As PotatoUpdateInfo = ParseUpdateInfo(Json)
                If Not IsNewer(Info) Then
                    Logger.Info($"[pcl-potato] 当前已是最新版本（本地 {VersionBaseName} / {VersionCode}）")
                    Exit Sub
                End If
                '已经弹过这个版本就不再弹（forceUpdate 除外）
                If Not Info.ForceUpdate AndAlso Settings.Get(Of Integer)(SettingKeyPrompted) >= Info.LatestVersionCode AndAlso Info.LatestVersionCode > 0 Then
                    Logger.Info("[pcl-potato] 该版本的更新提示已经显示过，跳过")
                    Exit Sub
                End If
                ShowUpdateDialog(Info)
            Catch ex As Exception
                Logger.Warn(ex, "[pcl-potato] 检查更新失败")
                If Not Silent Then Hint("检查更新失败，请稍后再试……", HintType.Red)
            End Try
        End Sub, "pcl-potato Update Check")
    End Sub

    ''' <summary>
    ''' 显示更新弹窗。点「下载」按钮会打开浏览器访问官网。
    ''' </summary>
    Private Sub ShowUpdateDialog(Info As PotatoUpdateInfo)
        Dim Title As String = If(String.IsNullOrWhiteSpace(Info.Title),
                                 $"pcl-potato 已发布新版本 {Info.LatestVersion}", Info.Title)
        Dim Content As String = If(String.IsNullOrWhiteSpace(Info.Content),
                                   $"新版本 {Info.LatestVersion} 已经发布啦！", Info.Content)
        If Not String.IsNullOrWhiteSpace(Info.PublishDate) Then
            Content = Content & vbCrLf & vbCrLf & "发布日期：" & Info.PublishDate
        End If
        Dim Url As String = If(String.IsNullOrWhiteSpace(Info.DownloadUrl), OfficialSiteUrl, Info.DownloadUrl)

        Logger.Info($"[pcl-potato] 发现新版本 {Info.LatestVersion}，弹窗提示")
        'MyMsgBox 返回点击的按钮序号（从 1 开始）：1 = 下载，2 = 关闭
        If MyMsgBox(Content, Title, "下载", "关闭") = 1 Then
            OpenWebsite(Url)
        End If
        If Info.LatestVersionCode > 0 Then Settings.Set(SettingKeyPrompted, Info.LatestVersionCode)
    End Sub

    ''' <summary>
    ''' 用户手动点「检查更新」时的入口：无论如何都会给出反馈。
    ''' </summary>
    Public Sub PotatoUpdateCheckManual()
        RunInNewThread(
        Sub()
            Try
                Dim Json As String = FetchUpdateJson()
                If Json Is Nothing Then
                    Hint("无法连接到更新服务器，请检查网络后再试……", HintType.Red)
                    Exit Sub
                End If
                Dim Info As PotatoUpdateInfo = ParseUpdateInfo(Json)
                If Not IsNewer(Info) Then
                    MyMsgBox($"当前已经是最新版本啦！{vbCrLf}版本：{VersionDisplay}", "pcl-potato 检查更新", "确定")
                    Exit Sub
                End If
                ShowUpdateDialog(Info)
            Catch ex As Exception
                Logger.Warn(ex, "[pcl-potato] 手动检查更新失败")
                Hint("检查更新失败，请稍后再试……", HintType.Red)
            End Try
        End Sub, "pcl-potato Update Check Manual")
    End Sub

#End Region

End Module
