



Imports claslibtest14

Public NotInheritable Class MainPage
    Inherits Page

#Region "user interface"

    Dim bInitDone As Boolean = False

    Private Sub uiUnicode_TextChanged(sender As Object, e As TextChangedEventArgs)
        If Not bInitDone Then Return

        Dim sUni As String = uiUnicode.Text

        If uiSwitchEncode.IsChecked Then
            If sUni.Length > 3 Then uiDecoded.Text = EncodeUnicode(sUni, uiFullInfo.IsChecked)
        Else
            uiDecoded.Text = DecodeUnicode(sUni, uiIgnoreASCII.IsChecked, uiFullInfo.IsChecked)
        End If
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ProgRingInit(True, False)
        Await NamesListInit()
        bInitDone = True

        Class1.getenvvartest()
        Module1.cosik1()
        Dim sCOs = Environment.GetEnvironmentVariables '(EnvironmentVariableTarget.User)
        'Dim scosss = Environment.GetCommandLineArgs

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
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        NamesListDownload()
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    End Sub

    Private Sub uiReDecode_Checked(sender As Object, e As RoutedEventArgs)
        uiUnicode_TextChanged(Nothing, Nothing)
    End Sub

    Private Sub uiSwitchRole_Click(sender As Object, e As RoutedEventArgs)

        ' interesuje nas tylko włączenie, wyłączenie nie działa
        Dim oTB As ToggleButton = TryCast(sender, ToggleButton)
        If oTB Is Nothing Then Return
        If Not oTB.IsChecked Then
            oTB.IsChecked = True
            Return
        End If

        ' włączenie jednego to wyłączenie drugiego
        If oTB.Name = "uiSwitchDecode" Then
            uiSwitchEncode.IsChecked = False
        Else
            uiSwitchDecode.IsChecked = False
        End If

        ' ustalenie pozostałych parametrów
        If oTB.Name = "uiSwitchDecode" Then
            uiUnicode.Header = "Unicode text:"
            uiUnicode.PlaceholderText = "(enter/paste unicode text)"
            uiIgnoreASCII.Visibility = Visibility.Visible
        Else
            uiUnicode.Header = "Search symbols for:"
            uiUnicode.PlaceholderText = "(enter what I should search for (min. 4 letters)"
            uiIgnoreASCII.Visibility = Visibility.Collapsed
        End If

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

            maLines = aPage

        End If

        ' gdy wiemy że nowsza wersja, albo gdy nie było poprzedniej wersji
        File.WriteAllText(NamesListGetPathname, sPage)

    End Function

    Private Async Function GetHtmlPage(ByVal sUrl As String) As Task(Of String)
        DumpCurrMethod(sUrl)

        Using oHttp As New Net.Http.HttpClient()
            Dim oUri As New Uri(sUrl)

            Using oResp = Await oHttp.GetAsync(oUri)

                If Not oResp.IsSuccessStatusCode Then Return ""

                Return Await oResp.Content.ReadAsStringAsync()
            End Using
        End Using
    End Function

#End Region

#Region "dekodowanie"


    Private Function DecodeUnicode(sUnicode As String, bIgnoreASCII As Boolean, bFullInfo As Boolean) As String
        Dim iLoop As Integer
        Dim sDecod As String = ""


        'Dim sMalpa1 As String = ""  ' na pozniej - bo to jednak niezbyt poprawne jest?
        'Dim sMalpa2 As String = ""
        'Dim sMalpa3 As String = ""

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
                iLoop += 1
                oChar = sUnicode.ElementAt(iLoop)
                iChar = Convert.ToInt32(oChar)
                sDecod = sDecod & "  next: " & iChar.ToString("X4") & vbTab & " = "

                iX = (iX Or (iChar And &H3FF))
                iW += 1

                sChar = iW.ToString("X") & iX.ToString("X4")
                ' sDecod = sDecod & 
            End If


            sDecod &= "U+"

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

    Private Function EncodeUnicode(sSearch As String, bFullInfo As Boolean) As String

        Dim sRetVal As String = ""
        Dim sCurrSection As String = ""
        Dim bFound As Boolean = False
        Dim sUnicodes As String = ""

        Dim oEncoder = (New System.Text.UTF32Encoding(Not BitConverter.IsLittleEndian, False, False)).GetDecoder

        sSearch = sSearch.ToLowerInvariant

        For iLinia = 0 To maLines.GetUpperBound(0)

            Dim sLinia As String = maLines(iLinia)

            If sLinia.StartsWith("@") Then Continue For
            If sLinia.StartsWith(";") Then Continue For

            'czy to jest sekcja nowa?
            If "01234567890ABCDEF".Contains(sLinia.Substring(0, 1)) Then

                ' obsługa starej sekcji
                If bFound Then
                    sRetVal = sRetVal & vbCrLf & sCurrSection

                    Dim iInd As Integer = sCurrSection.IndexOf(vbTab)
                    If iInd > 0 And iInd < 7 Then
                        Dim iHexVal As Long
                        If Long.TryParse(sCurrSection.Substring(0, iInd),
                                         Globalization.NumberStyles.HexNumber,
                                         Globalization.CultureInfo.InvariantCulture,
                            iHexVal) Then

                            If iHexVal > 32 Then
                                ' *TODO* dodaj do sUnicodes mordkę
                                Try
                                    Dim aChars(5) As Char
                                    Dim iChars As Integer = oEncoder.GetChars(BitConverter.GetBytes(iHexVal), 0, 4, aChars, 0)
                                    For i = 0 To iChars - 1
                                        sUnicodes &= aChars(i)
                                    Next
                                    sUnicodes &= " "
                                Catch ex As Exception
                                    ' jeśliby nie było
                                    sUnicodes = sUnicodes & "X "
                                End Try
                            End If

                        End If
                    End If

                    bFound = False
                End If

                sCurrSection = sLinia
                If sLinia.ToLowerInvariant.Contains(sSearch) Then bFound = True
            Else

                ' zwykła linia
                If bFullInfo Then sCurrSection = sCurrSection & vbCrLf & sLinia

                ' a może to jakiś alias?
                If sLinia.StartsWith(vbTab & "=") Then
                    If sLinia.ToLowerInvariant.Contains(sSearch) Then
                        ' gdy nie kopiujemy całości, to przynajmniej tą linię "trafioną" musimy skopiować
                        If Not bFullInfo Then sCurrSection = sCurrSection & vbCrLf & sLinia
                        bFound = True
                    End If
                End If

            End If
        Next

        Return sUnicodes & vbCrLf & sRetVal
    End Function

#End Region



End Class
