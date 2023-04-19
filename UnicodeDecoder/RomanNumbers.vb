Imports RomanNumerals.Numerals
Imports System.Text

Partial Public Module RomanNumbers

    <Extension>
    Public Function ToRomanNumber(ByVal iNumber As Integer, Optional useUnicode As Boolean = False, Optional numSystem As RomanBigNumberSystem = RomanBigNumberSystem.None, Optional useSubtractive As Boolean = True) As String

        If useUnicode Then
            Return iNumber.ToRomanNumberUnicode(numSystem, useSubtractive)
        Else
            Return iNumber.ToRomanNumberAscii(numSystem, useSubtractive)
        End If

    End Function

    <Extension>
    Public Function ToRomanNumberAscii(ByVal iNumber As Integer, Optional numSystem As RomanBigNumberSystem = RomanBigNumberSystem.None, Optional useSubtractive As Boolean = True) As String
        Return BuildRomanNum(iNumber, numSystem, GetNumberDictionaryAscii(numSystem, useSubtractive))
    End Function

    <Extension>
    Public Function ToRomanNumberUnicode(ByVal iNumber As Integer, Optional numSystem As RomanBigNumberSystem = RomanBigNumberSystem.None, Optional useSubtractive As Boolean = True) As String
        Return BuildRomanNum(iNumber, numSystem, GetNumberDictionaryUnicode(numSystem, useSubtractive))
    End Function

    Private Function GetNumberDictionaryAscii(numSystem As RomanBigNumberSystem, useSubtractive As Boolean) As Dictionary(Of Integer, String)
        Dim ret As New Dictionary(Of Integer, String)

        If numSystem = RomanBigNumberSystem.Apostrophus Then
            ret.Add(100000, "(((I)))")
            ret.Add(50000, "I)))")
            ret.Add(10000, "((I))")
            ret.Add(5000, "I))")
            ret.Add(1000, "(I)")
        Else
            ret.Add(1000, "M")
        End If

        If useSubtractive Then ret.Add(900, "CM")
        ret.Add(500, "D")
        If useSubtractive Then ret.Add(400, "CD")
        ret.Add(100, "C")
        If useSubtractive Then ret.Add(90, "XC")
        ret.Add(50, "L")
        If useSubtractive Then ret.Add(40, "XL")
        ret.Add(10, "X")
        If useSubtractive Then ret.Add(9, "IX")
        ret.Add(5, "V")
        If useSubtractive Then ret.Add(4, "IV")
        ret.Add(1, "I")

        Return ret
    End Function

    Private Function GetNumberDictionaryUnicode(numSystem As RomanBigNumberSystem, useSubtractive As Boolean) As Dictionary(Of Integer, String)
        Dim ret As New Dictionary(Of Integer, String)

        If numSystem = RomanBigNumberSystem.Apostrophus Then
            ret.Add(100000, "ↈ")
            ret.Add(50000, "ↇ")
            ret.Add(10000, "ↂ")
            ret.Add(5000, "ↁ")
            ret.Add(1000, "ↀ")
        Else
            ret.Add(1000, "Ⅿ")
        End If

        If useSubtractive Then ret.Add(900, "ⅭⅯ")
        ret.Add(500, "Ⅾ")
        If useSubtractive Then ret.Add(400, "ⅭⅮ")
        ret.Add(100, "Ⅽ")
        If useSubtractive Then ret.Add(90, "ⅩⅭ")
        ret.Add(50, "Ⅼ")
        If useSubtractive Then ret.Add(40, "ⅩⅬ")
        ret.Add(10, "Ⅹ")
        If useSubtractive Then ret.Add(9, "Ⅸ")
        ret.Add(8, "Ⅷ")
        ret.Add(7, "Ⅶ")
        ret.Add(6, "Ⅵ")
        ret.Add(5, "Ⅴ")
        If useSubtractive Then ret.Add(4, "Ⅳ")
        ret.Add(3, "Ⅲ")
        ret.Add(2, "Ⅱ")
        ret.Add(1, "Ⅰ")

        'Ⅰ Ⅱ Ⅲ Ⅳ Ⅴ Ⅵ Ⅶ Ⅷ Ⅸ Ⅹ Ⅺ Ⅻ Ⅼ Ⅽ Ⅾ Ⅿ ⅰ ⅱ ⅲ ⅳ ⅴ ⅵ ⅶ ⅷ ⅸ ⅹ ⅺ ⅻ ⅼ ⅽ ⅾ ⅿ ↀ ↁ ↂ Ↄ ↅ ↆ ↇ ↈ
        Return ret
    End Function

    Private Function BuildRomanNum(iNumber As Integer, numSystem As RomanBigNumberSystem, dictionary As Dictionary(Of Integer, String)) As String
        If iNumber > 4 * dictionary.ElementAt(0).Key Then Return "(too big number)"

        Dim ret As String = ""

        While iNumber > 0

            For Each oRomanDigit In dictionary
                If iNumber >= oRomanDigit.Key Then
                    ret &= oRomanDigit.Value
                    iNumber -= oRomanDigit.Key
                    Exit For
                End If
            Next

        End While

        Return ret

    End Function

    Public Enum RomanBigNumberSystem
        None
        Vinculum
        Apostrophus
    End Enum

End Module
