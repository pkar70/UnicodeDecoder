' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class MainPage
    Inherits Page

    Dim maLines As String()

    Private Async Sub uiUnicode_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim sUni As String = uiUnicode.Text

        Dim iLoop As Integer
        Dim sDecod As String = ""



        Dim sMalpa1 As String = ""  ' na pozniej - bo to jednak niezbyt poprawne jest?
        Dim sMalpa2 As String = ""
        Dim sMalpa3 As String = ""

        For iLoop = 0 To sUni.Length - 1
            Dim oChar As System.Char = sUni.ElementAt(iLoop)
            Dim iChar As Int32 = Convert.ToInt32(oChar)

            If uiIgnoreASCII.IsChecked Then
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
                oChar = sUni.ElementAt(iLoop)
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
                    If uiFullInfo.IsChecked Then
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

            ' w trakcie - zeby cos sie dzialo, jakby mialo dlugo trwac
            uiDecoded.Text = sDecod
        Next

        uiDecoded.Text = sDecod
        ' uiIgnoreASCII
        ' uiDecoded
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Dim oFile As StreamReader = File.OpenText("Assets\NamesList.txt")
        Dim sContent As String = oFile.ReadToEnd()
        maLines = sContent.Split(vbLf)
        oFile.Dispose()
    End Sub
End Class
