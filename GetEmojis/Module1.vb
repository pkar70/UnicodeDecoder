Imports System.Net.Http
Imports HtmlAgilityPack

Module Module1

    Sub Main()

        Console.WriteLine("GetEmojis")

        Dim targetDir As String = IO.Path.Combine(Environment.CurrentDirectory, "unicode")
        Dim themeFile As String = IO.Path.Combine(targetDir, "theme")

        'If IO.Directory.Exists(targetDir) Then
        '    Console.WriteLine("Folder Unicode already exists, exiting")
        '    Exit Sub
        'End If


        IO.Directory.CreateDirectory(targetDir)

        ThemeFileHeader(themeFile)        ' oficjalne 
        ' GetOfficialEmojis(targetDir)

        ' z pomijaniem już istniejących
        GetProposedEmojis(targetDir, themeFile)

    End Sub

    Private Sub ThemeFileHeader(themeFile As String)

        IO.File.Delete(themeFile)
        IO.File.WriteAllText(themeFile,
$"Name=Unicode {Date.Now.ToString("yyyyMMdd")} 
Description=Emoji from Unicode data file
Icon=1fae2.png
Author=pkar

[default]
"
)
    End Sub

    Private _http As New HttpClient

    Private Sub GetOfficialEmojis(targetDir As String)
        ' już istniejące Emoji

        Dim sPage As String = _http.GetStringAsync("http://www.unicode.org/emoji/charts/emoji-list.html").Result

    End Sub

    Private Sub GetProposedEmojis(targetDir As String, themeFile As String)
        ' propozycje, ale na liście są też oficjalne (niektóre?), więc je pomijamy... a może w ogóle wszystkie tu są?

        Dim sPage As String = _http.GetStringAsync("http://www.unicode.org/emoji/charts/emoji-proposals.html").Result

        If String.IsNullOrWhiteSpace(sPage) Then Return

        Dim oHtml As New HtmlDocument()
        oHtml.LoadHtml(sPage)

        Dim oTable As HtmlNode = oHtml.DocumentNode.SelectNodes("//table")(1)

        Dim counter As Integer = 0

        For Each oRow As HtmlNode In oTable.SelectNodes("tr")
            Dim oRows = oRow.SelectNodes("td")
            If oRows Is Nothing Then Continue For

            Dim emoji As String = oRows(0).InnerText

            Dim sFilename As String = emoji.Replace(" ", "-").ToLower & ".png"

            Dim sIcon = oRows(1).SelectSingleNode("img").Attributes("src").Value
            ' data:image/png;base64,zzzzz....

            If Not sIcon.StartsWith("data:image/png;base64,") Then
                Console.WriteLine("Inny img src dla " & sFilename)
            Else
                Dim decoded = System.Convert.FromBase64String(sIcon.Substring("data:image/png;base64,".Length))
                IO.File.WriteAllBytes(IO.Path.Combine(targetDir, sFilename), decoded)

                Dim sLinia As String = sFilename & vbTab
                Dim codepoints = emoji.Split(" ")
                For Each cdpn In codepoints
                    Dim utf32 As Int32 = Int32.Parse(cdpn, Globalization.NumberStyles.AllowHexSpecifier)
                    sLinia &= Char.ConvertFromUtf32(utf32)
                Next

                IO.File.AppendAllText(themeFile, sLinia & vbCrLf)

                counter += 1
            End If

        Next

        Console.WriteLine("Emojis count: " & counter)

    End Sub

End Module
