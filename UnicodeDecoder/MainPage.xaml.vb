
' początek: 2020.01
' ale jako app, wraz ze Store i aktualizacją pliku names, 2022.03
' STORE: 2022.03

' 2022.03.29: gdy na appStart jest w Clipboard krotki tekst (<10 chars), to robi jego decode od razu



Public NotInheritable Class MainPage
    Inherits Page

#Region "user interface"

    Dim bInitDone As Boolean = False

    Private Sub uiUnicode_TextChanged(sender As Object, e As TextChangedEventArgs)
        If bInitDone Then DecodeUnicode()
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ProgRingInit(True, False)
        Await NamesListInit()
        bInitDone = True

        ' 2022.03.29
        Await Task.Delay(1000)  ' bo AccessDenied gdy idzie bez zwłoki, na DataTransfer.Clipboard.GetContent
        Dim sClip As String
        Try
            sClip = Await ClipGetAsync()
        Catch ex As Exception
            sClip = ""
        End Try

        If Not String.IsNullOrEmpty(sClip) Then
            If sClip.Length < 10 Then
                uiUnicode.Text = sClip
            End If
        End If

    End Sub

    Private Sub uiGetList_Click(sender As Object, e As RoutedEventArgs)
        NamesListDownload()
    End Sub

    Private Sub uiReDecode_Checked(sender As Object, e As RoutedEventArgs)
        If bInitDone Then DecodeUnicode()
    End Sub
#End Region

#Region "plik z nazwami"

    Dim maLines As String() = {""}

    Private Function NamesListGetPathname() As String
        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder
        Return Path.Combine(oFold.Path, "NamesList.txt")
    End Function

    Private Async Function NamesListInit() As Task
        If Not File.Exists(NamesListGetPathname) Then
            File.Copy("Assets\NamesList.txt", NamesListGetPathname)
            Await NamesListDownload()
        End If

        maLines = File.ReadAllLines(NamesListGetPathname)
    End Function

    Private Async Function NamesListDownload() As Task
        If Not Await DialogBoxYNAsync("Do you want to check if new standard exist on The Unicode Consortium site?") Then Return

        ProgRingShow(True)
        Dim sPage As String = Await GetHtmlPage("https://unicode.org/Public/UNIDATA/NamesList.txt")
        ProgRingShow(False)
        If sPage = "" Then
            Await DialogBoxAsync("ERROR: cannot download file")
            Return
        End If
        If sPage.Length < 1000 * 1024 Then
            Await DialogBoxAsync("ERROR: file seems too short (" & sPage.Length & " bytes only, expected > 1 MB)")
            Return
        End If

        If maLines.Length > 5 Then
            ' jest poprzednia wersja, możemy sprawdzić

            '; charset=UTF-8
            '@@@	The Unicode Standard 14.0.0
            '@@@+	U14M210804.lst

            Dim sOldVers As String = ""
            For iLoop As Integer = 0 To 10
                Dim sLine As String = maLines(iLoop)
                If sLine.Contains("The Unicode Standard") Then
                    Dim iInd As Integer = sLine.IndexOf("The Unicode Standard")
                    sOldVers = sLine.Substring(iInd + "The Unicode Standard".Length).Trim
                    Exit For
                End If
            Next

            Dim sNewVers As String = ""
            ' szkoda robić większą aPage, wystarczy parę linijek
            Dim aPage As String() = sPage.Substring(0, 3000).Split(vbLf)
            For iLoop As Integer = 0 To 10
                Dim sLine As String = aPage(iLoop)
                If sLine.Contains("The Unicode Standard") Then
                    Dim iInd As Integer = sLine.IndexOf("The Unicode Standard")
                    sNewVers = sLine.Substring(iInd + "The Unicode Standard".Length).Trim
                    Exit For
                End If
            Next

            If sOldVers = sNewVers Then
                Await DialogBoxAsync("There is no newer version (current: " & sOldVers & ")")
                Return
            End If

            Await DialogBoxAsync("Got newer version " & sNewVers & " (previous: " & sOldVers & ")")

        End If

        ' gdy wiemy że nowsza wersja, albo gdy nie było poprzedniej wersji
        File.WriteAllText(NamesListGetPathname, sPage)

    End Function

    Private Async Function GetHtmlPage(ByVal sUrl As String) As Task(Of String)
        DumpCurrMethod(sUrl)

        Using oHttp As Net.Http.HttpClient = New Net.Http.HttpClient()
            Dim oUri As Uri = New Uri(sUrl)

            Using oResp = Await oHttp.GetAsync(oUri)

                If Not oResp.IsSuccessStatusCode Then Return ""

                Return Await oResp.Content.ReadAsStringAsync()
            End Using
        End Using
    End Function

#End Region

#Region "dekodowanie"

    Private Sub DecodeUnicode()
        Dim sUni As String = uiUnicode.Text
        uiDecoded.Text = DecodeUnicode(sUni, uiIgnoreASCII.IsChecked, uiFullInfo.IsChecked)
    End Sub

    Private Function DecodeUnicode(sUnicode As String, bIgnoreASCII As Boolean, bFullInfo As Boolean) As String
        Dim iLoop As Integer
        Dim sDecod As String = ""


        Dim sMalpa1 As String = ""  ' na pozniej - bo to jednak niezbyt poprawne jest?
        Dim sMalpa2 As String = ""
        Dim sMalpa3 As String = ""

        For iLoop = 0 To sUnicode.Length - 1
            Dim oChar As System.Char = sUnicode.ElementAt(iLoop)
            Dim iChar As Int32 = Convert.ToInt32(oChar)

            If bIgnoreASCII Then
                If iChar > &H1F And iChar < &H7F Then Continue For
                If iChar = 10 Then Continue For
                If iChar = 13 Then Continue For
                If iChar = 7 Then Continue For
            End If


            Dim sChar As String = iChar.ToString("X4") & vbTab   ' tab wazny - bo nie poczatek, a calosc kodu!

            ' D83D
            If iChar >= &HD800 And iChar <= &HDBFF Then

                Dim iW As Integer
                Dim iX As Integer
                iW = (iChar And &H3C0) >> 6
                iX = (iChar And &H3F) << 10

                sDecod = sDecod & sChar & "high-surrogate code point:" & vbCrLf
                iLoop = iLoop + 1
                oChar = sUnicode.ElementAt(iLoop)
                iChar = Convert.ToInt32(oChar)
                sDecod = sDecod & "  next: " & iChar.ToString("X4") & vbTab & " = "

                iX = (iX Or (iChar And &H3FF))
                iW += 1

                sChar = iW.ToString("X") & iX.ToString("X4")
                ' sDecod = sDecod & 
            End If


            sDecod = sDecod & "U+"

            For iLinia = 0 To maLines.GetUpperBound(0)
                If maLines(iLinia).StartsWith(sChar) Then
                    sDecod = sDecod & maLines(iLinia) & vbCrLf
                    If bFullInfo Then
                        Do
                            iLinia += 1
                            If Not maLines(iLinia).StartsWith(vbTab) Then Exit Do
                            sDecod = sDecod & maLines(iLinia) & vbCrLf
                        Loop
                    End If
                    Exit For
                End If
            Next

            If uiFullInfo.IsChecked Then sDecod &= vbCrLf   ' skoro duzo info o jednym znaku, dawaj dodatkowe linie

            '' w trakcie - zeby cos sie dzialo, jakby mialo dlugo trwac
            'uiDecoded.Text = sDecod
        Next

        Return sDecod

    End Function

#End Region



End Class
