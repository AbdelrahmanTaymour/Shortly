using System.Collections;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace Shortly.Core.Utilities;

public static class Base62Converter
{
    private const string Characters = "qW1r80D2YHsacELuKn5PgfNjZIl6bv3e4ot7zmJAMiRCdVyF9xhGBUXTwSkQ";
    public static string Encode(int id)
    {
        if (id == 0) return Characters[0].ToString();
        var base62 = new Stack<char>();
        while (id > 0)
        {
            base62.Push(Characters[id % 62]);
            id /= 62;
        }

        return new string(base62.ToArray());
    }
    public static long Decode(string shortUrl)
    {
        long id = 0;
        foreach (char c in shortUrl)
        {
            id = id * 62 + Characters.IndexOf(c);
        }
        return id;
    }
}