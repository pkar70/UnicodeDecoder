
Public NotInheritable Class MainPage
    Inherits Page

#Region "user interface"

    Dim bInitDone As Boolean = False

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
                ' uiUnicode.PasteFromClipboard() ' teoretycznie można byłoby i tak
                uiUnicode.Text = sClip
            End If
        End If

    End Sub



#Region "decode tab"
    Private Sub uiUnicode_TextChanged(sender As Object, e As TextChangedEventArgs)
        If Not bInitDone Then Return

        Dim sUni As String = uiUnicode.Text

        uiDecoded.Text = DecodeUnicode(sUni, uiIgnoreASCII.IsChecked, uiFullInfo.IsChecked)
    End Sub

#End Region

#Region "find (encode) tab"
    Private Sub uiUnicodeFind_TextChanged(sender As Object, e As TextChangedEventArgs)
        If Not bInitDone Then Return

        Dim sUni As String = uiUnicodeFind.Text

        If sUni.Length > 3 Then uiUnicodeFindResult.Text = EncodeUnicode(sUni, uiFullInfoFind.IsChecked)
    End Sub

#End Region

#Region "numbers"
    Private Sub uiUnicodeNumber_TextChanged(sender As Object, e As TextChangedEventArgs)
        If Not bInitDone Then Return
        Dim sUni As String = uiUnicodeNumber.Text
        uiUnicodeNumberResult.Text = EncodeNumber(sUni)

    End Sub

#End Region

#Region "letters"

    ' bez sensu, bo np. litera A jest tylko LATIN i NKO, ale NKO ma raczej głoski niż litery
    Private Sub uiUnicodeLetters_TextChanged(sender As Object, e As TextChangedEventArgs)
        If Not bInitDone Then Return
        Dim sUni As String = uiUnicodeLetters.Text
        uiUnicodeLettersResult.Text = EncodeLetters(sUni)

    End Sub

#End Region


    Private Sub uiGetList_Click(sender As Object, e As RoutedEventArgs)
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        NamesListDownload()
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    End Sub

    Private Sub uiReDecode_Checked(sender As Object, e As RoutedEventArgs)
        uiUnicode_TextChanged(Nothing, Nothing)
        uiUnicodeFind_TextChanged(Nothing, Nothing)
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

#Region "operacje zamiany TextBox query na TextBox result"


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

        sSearch = sSearch.ToLowerInvariant

        Dim guard As Integer = 100

        For iLinia = 0 To maLines.GetUpperBound(0)

            Dim sLinia As String = maLines(iLinia)

            If sLinia.StartsWith("@") Then Continue For
            If sLinia.StartsWith(";") Then Continue For

            'czy to jest sekcja nowa?
            If "01234567890ABCDEF".Contains(sLinia.Substring(0, 1)) Then

                ' obsługa starej sekcji
                If bFound Then
                    sRetVal = sRetVal & vbCrLf & sCurrSection

                    sUnicodes &= GetUnicodeChar(sCurrSection)
                    guard -= 1
                    If guard < 0 Then Exit For

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

    Private Shared _Encoder = (New System.Text.UTF32Encoding(Not BitConverter.IsLittleEndian, False, False)).GetDecoder

    Private Shared Function GetUnicodeChar(sCurrSection As String) As String

        Dim sUnicodes As String = ""
        Dim iInd As Integer = sCurrSection.IndexOf(vbTab)
        If iInd < 1 OrElse iInd > 6 Then Return ""

        Dim iHexVal As Long
        If Not Long.TryParse(sCurrSection.Substring(0, iInd),
                             Globalization.NumberStyles.HexNumber,
                             Globalization.CultureInfo.InvariantCulture,
                iHexVal) Then Return ""

        If iHexVal < 33 Then Return ""

        ' *TODO* dodaj do sUnicodes mordkę
        Try
            Dim aChars(5) As Char
            Dim iChars As Integer = _Encoder.GetChars(BitConverter.GetBytes(iHexVal), 0, 4, aChars, 0)
            For i = 0 To iChars - 1
                sUnicodes &= aChars(i)
            Next
            sUnicodes &= " "
        Catch ex As Exception
            ' jeśliby nie było
            sUnicodes = sUnicodes & "X "
        End Try

        Return sUnicodes
    End Function



#Region "number coding"

    Dim _DigitSystems As List(Of String)

    Private Function EncodeNumber(sUni As String) As String

        ' *TODO* może być potem jako ogólniej, tzn. że każda cyfra z innego zestawu :)
        Dim sNumSystem As String = RecognizeNumberSystem(sUni)
        If sNumSystem = "" Then Return "(unrecognized numbering system)"

        Dim iNumber As Integer = NumberFromText(sUni)

        Dim sRet As String = $"Recognized as {iNumber} in {sNumSystem}" & vbCrLf & vbCrLf

        sRet = sRet & "Roman (archaic, ASCII): " & iNumber.ToRomanNumber(useSubtractive:=False) & vbCrLf
        sRet = sRet & "Roman (modified, ASCII): " & iNumber.ToRomanNumber & vbCrLf
        sRet = sRet & "Roman (apostrophus, ASCII): " & iNumber.ToRomanNumber(numSystem:=RomanBigNumberSystem.Apostrophus) & vbCrLf
        sRet = sRet & "Roman (archaic): " & iNumber.ToRomanNumber(True, useSubtractive:=False) & vbCrLf
        sRet = sRet & "Roman (modified): " & iNumber.ToRomanNumber(True) & vbCrLf
        sRet = sRet & "Roman (apostrophus): " & iNumber.ToRomanNumber(True, RomanBigNumberSystem.Apostrophus) & vbCrLf & vbCrLf

        ' *TODO* greek, hebrew

        If _DigitSystems Is Nothing Then _DigitSystems = RetrieveDigitSystems()

        For Each digitSystem As String In _DigitSystems
            sRet = sRet & digitSystem & ":" & vbTab & EncodeNumber(iNumber, digitSystem) & vbCrLf
        Next

        Return sRet

    End Function

    Private Function RetrieveDigitSystems() As List(Of String)
        Dim aDigitSystems As New List(Of String)

        For Each linia As String In maLines
            If linia.Contains(" DIGIT ONE") Then
                Dim numSystem As String = linia.TrimBefore(vbTab).Substring(1)
                Dim iInd As Integer = numSystem.IndexOf(" DIGIT ONE")
                aDigitSystems.Add(numSystem.Substring(0, iInd))
            End If
        Next

        ' aDigitSystems.Add("KAYAH LI")

        Return aDigitSystems

    End Function

    Private Function EncodeNumber(iNumber As Integer, digitSystem As String) As String

        Dim ret As String = ""

        Dim sNumber As String = iNumber.ToString
        For Each digit As Char In sNumber

            Dim sQuery As String = vbTab & digitSystem & " DIGIT "

            Select Case digit
                Case "0"
                    sQuery &= "ZERO"
                Case "1"
                    sQuery &= "ONE"
                Case "2"
                    sQuery &= "TWO"
                Case "3"
                    sQuery &= "THREE"
                Case "4"
                    sQuery &= "FOUR"
                Case "5"
                    sQuery &= "FIVE"
                Case "6"
                    sQuery &= "SIX"
                Case "7"
                    sQuery &= "SEVEN"
                Case "8"
                    sQuery &= "EIGHT"
                Case "9"
                    sQuery &= "NINE"
                Case Else
                    ret &= "?"
                    Continue For
            End Select

            For iLinia = 0 To maLines.GetUpperBound(0)
                If maLines(iLinia).Contains(sQuery) Then
                    ret &= GetUnicodeChar(maLines(iLinia))
                    Exit For
                End If
            Next

        Next

        Return ret
    End Function


    Private Function RecognizeNumberSystem(sUni As String) As String
        If sUni.Length < 1 Then Return ""

        Dim iChar As Int32 = Convert.ToInt32(sUni.ElementAt(0))
        Dim sChar As String = iChar.ToString("X4") & vbTab   ' tab wazny - bo nie poczatek, a calosc kodu!

        For iLinia = 0 To maLines.GetUpperBound(0)
            If maLines(iLinia).StartsWith(sChar) Then
                Dim sCharName As String = maLines(iLinia)
                Dim iInd As Integer = sCharName.IndexOf(vbTab)
                If iInd < 2 Then Return ""
                sCharName = sCharName.Substring(iInd + 1)
                If sCharName.StartsWith("DIGIT") Then Return "WESTERN"
                iInd = sCharName.IndexOf(" DIGIT ")
                If iInd < 2 Then Return ""
                Return sCharName.Substring(0, iInd)

            End If
        Next

        Return ""

    End Function

    Private Function NumberFromText(sUni As String) As Integer

        Dim decoded As String = DecodeUnicode(sUni, False, False)

        Dim wartosc As Integer = 0
        For Each oDigit As String In decoded.Split(vbCrLf)

            If Not oDigit.Contains("DIGIT") Then Continue For

            wartosc *= 10
            If oDigit.Contains(" ONE") Then wartosc += 1
            If oDigit.Contains(" TWO") Then wartosc += 2
            If oDigit.Contains(" THREE") Then wartosc += 3
            If oDigit.Contains(" FOUR") Then wartosc += 4
            If oDigit.Contains(" FIVE") Then wartosc += 5
            If oDigit.Contains(" SIX") Then wartosc += 6
            If oDigit.Contains(" SEVEN") Then wartosc += 7
            If oDigit.Contains(" EIGHT") Then wartosc += 8
            If oDigit.Contains(" NINE") Then wartosc += 9
        Next

        Return wartosc

    End Function

#End Region

#Region "letters coding"

    Dim _LettersSystems As List(Of String)

    Private Function EncodeLetters(sUni As String) As String

        Dim lLetters As New List(Of String)

        ' U+0061	LATIN SMALL LETTER A
        ' U+004B	LATIN CAPITAL LETTER K
        For Each oChar As String In DecodeUnicode(sUni, False, False).Split(vbCrLf)
            If Not oChar.Contains(" LETTER ") Then Continue For
            Dim iInd As Integer = oChar.IndexOf(" SMALL LETTER")
            If iInd < 0 Then iInd = oChar.IndexOf(" CAPITAL LETTER")
            If iInd < 0 Then iInd = oChar.IndexOf(" LETTER")
            lLetters.Add(oChar.Substring(iInd + 1))
        Next

        ' może być różne dla różnych, bo różny zakres
        Dim lSystems As List(Of String) = RetrieveLettersSystems(lLetters)

        Dim sRet As String = ""

        For Each letterSystem As String In lSystems
            sRet = sRet & letterSystem & ":" & vbTab & EncodeLetters(lLetters, letterSystem) & vbCrLf
        Next

        Return sRet

    End Function

    Private Function EncodeLetters(letters As List(Of String), letterSystem As String) As String

        Dim ret As String = ""

        For Each letter As String In letters
            Dim query As String = vbTab & letterSystem & " " & letter
            For iLinia = 0 To maLines.GetUpperBound(0)
                If maLines(iLinia).EndsWith(query) Then
                    ret &= GetUnicodeChar(maLines(iLinia))
                    Exit For
                End If
            Next
        Next

        Return ret
    End Function

    Private Function RetrieveLettersSystems(letters As List(Of String)) As List(Of String)
        Dim aDigitSystems As New List(Of String)

        'For Each linia As String In maLines
        '    If linia.Contains(" DIGIT ONE") Then
        '        Dim numSystem As String = linia.TrimBefore(vbTab).Substring(1)
        '        Dim iInd As Integer = numSystem.IndexOf(" DIGIT ONE")
        '        aDigitSystems.Add(numSystem.Substring(0, iInd))
        '    End If
        'Next

        ' aDigitSystems.Add("KAYAH LI")

        Return aDigitSystems

    End Function

#End Region


#End Region



End Class
