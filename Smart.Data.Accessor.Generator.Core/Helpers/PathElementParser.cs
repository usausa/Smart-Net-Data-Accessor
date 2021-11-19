namespace Smart.Data.Accessor.Generator.Helpers;

using System;
using System.Collections.Generic;

internal class PathElementParser
{
    private readonly string path;

    private int index;

    public PathElementParser(string path)
    {
        this.path = path;
    }

    public PathElement[] Parse()
    {
        var list = new List<PathElement>();
        PathElement? element;
        while ((element = Next()) is not null)
        {
            list.Add(element);
        }
        return list.ToArray();
    }

    private PathElement? Next()
    {
        SkipWhitespace();

        // Find token
        var start = index;
        while ((index < path.Length) && IsToken(path[index]))
        {
            index++;
        }

        var end = index;
        if (end == start)
        {
            return null;
        }

        var subPath = path[start..index];

        var indexed = 0;
        while (index < path.Length)
        {
            var c = path[index];
            index++;

            if (c == '.')
            {
                break;
            }

            if (c == '[')
            {
                indexed++;
                index++;
                var nest = 1;
                while ((nest > 0) && (index < path.Length))
                {
                    c = path[index];
                    if (c == '[')
                    {
                        nest++;
                    }
                    else if (c == ']')
                    {
                        nest--;
                    }

                    index++;
                }
            }
        }

        return new PathElement(subPath, indexed);
    }

    private void SkipWhitespace()
    {
        while ((index < path.Length) && Char.IsWhiteSpace(path[index]))
        {
            index++;
        }
    }

    private static bool IsToken(char c) => !Char.IsWhiteSpace(c) && (c != '?') && (c != '[') && (c != '.');
}
