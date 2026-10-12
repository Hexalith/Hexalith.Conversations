namespace Hexalith.Conversations.TestSupport;

/// <summary>Conservatively recognizes unconditional C# declaration tokens without evaluating code.</summary>
internal static class CSharpSourceDeclarations
{
    /// <summary>Omits comments, nonliteral strings, characters, and all conditional directive regions.</summary>
    internal static IReadOnlyList<string> UnconditionalTokens(string source)
    {
        List<string> tokens = [];
        int index = 0;
        int conditionalDepth = 0;
        while (index < source.Length)
        {
            char current = source[index];
            if (char.IsWhiteSpace(current)) { index++; continue; }
            int lineStart = source.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
            if (current == '#' && source.AsSpan(lineStart, index - lineStart).Trim().IsEmpty)
            {
                int end = source.IndexOf('\n', index);
                if (end < 0) end = source.Length;
                string directive = source[(index + 1)..end].TrimStart().Split([' ', '\t', '\r'], 2)[0];
                if (directive == "if") conditionalDepth++;
                else if (directive == "endif" && conditionalDepth > 0) conditionalDepth--;
                index = end;
                continue;
            }
            if (current == '/' && index + 1 < source.Length && source[index + 1] == '/')
            {
                int end = source.IndexOf('\n', index + 2);
                index = end < 0 ? source.Length : end;
                continue;
            }
            if (current == '/' && index + 1 < source.Length && source[index + 1] == '*')
            {
                int end = source.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = end < 0 ? source.Length : end + 2;
                continue;
            }
            int start = index;
            int quote = index;
            while (quote < source.Length && source[quote] is '$' or '@') quote++;
            if (quote < source.Length && source[quote] == '"')
            {
                index = ReadString(source, start, quote, out bool literal);
                if (conditionalDepth == 0) tokens.Add(literal ? source[start..index] : "<string>");
                continue;
            }
            if (current == '\'')
            {
                index++;
                while (index < source.Length)
                {
                    char character = source[index++];
                    if (character == '\\') index = Math.Min(source.Length, index + 1);
                    else if (character == '\'') break;
                }
                if (conditionalDepth == 0) tokens.Add("<character>");
                continue;
            }
            if (char.IsLetter(current) || current == '_')
            {
                index++;
                while (index < source.Length && (char.IsLetterOrDigit(source[index]) || source[index] == '_')) index++;
            }
            else index++;
            if (conditionalDepth == 0) tokens.Add(source[start..index]);
        }
        return tokens;
    }

    private static int ReadString(string source, int start, int quote, out bool literal)
    {
        bool verbatim = source.AsSpan(start, quote - start).Contains('@');
        int dollars = source.AsSpan(start, quote - start).Count('$');
        int quotes = 0;
        while (quote + quotes < source.Length && source[quote + quotes] == '"') quotes++;
        bool raw = quotes >= 3;
        literal = start == quote && !raw;
        int index = quote + (raw ? quotes : 1);
        while (index < source.Length)
        {
            if (dollars > 0 && source[index] == '{')
            {
                int braces = 0;
                while (index + braces < source.Length && source[index + braces] == '{') braces++;
                if (raw && braces >= dollars)
                { index = SkipInterpolation(source, index + braces, dollars); continue; }
                if (!raw && braces % 2 != 0)
                { index = SkipInterpolation(source, index + braces, 1); continue; }
                index += braces;
                continue;
            }
            if (source[index] == '"')
            {
                if (raw)
                {
                    int count = 0;
                    while (index + count < source.Length && source[index + count] == '"') count++;
                    if (count >= quotes) return index + quotes;
                    index += count;
                    continue;
                }
                if (verbatim && index + 1 < source.Length && source[index + 1] == '"')
                { index += 2; continue; }
                return index + 1;
            }
            if (!raw && !verbatim && source[index] == '\\')
            { literal = false; index = Math.Min(source.Length, index + 2); }
            else index++;
        }
        literal = false;
        return index;
    }

    private static int SkipInterpolation(string source, int index, int closingBraces)
    {
        int depth = 0;
        while (index < source.Length)
        {
            if (source[index] == '{') { depth++; index++; continue; }
            if (source[index] == '}')
            {
                int count = 0;
                while (index + count < source.Length && source[index + count] == '}') count++;
                if (depth == 0 && count >= closingBraces) return index + closingBraces;
                if (depth > 0) depth--;
                index++;
                continue;
            }
            if (source[index] == '/' && index + 1 < source.Length && source[index + 1] is '/' or '*')
            {
                bool line = source[index + 1] == '/';
                int end = line ? source.IndexOf('\n', index + 2) : source.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = end < 0 ? source.Length : end + (line ? 0 : 2);
                continue;
            }
            int quote = index;
            while (quote < source.Length && source[quote] is '$' or '@') quote++;
            if (quote < source.Length && source[quote] == '"')
            { index = ReadString(source, index, quote, out _); continue; }
            if (source[index] == '\'')
            {
                index++;
                while (index < source.Length)
                {
                    char character = source[index++];
                    if (character == '\\') index = Math.Min(source.Length, index + 1);
                    else if (character == '\'') break;
                }
                continue;
            }
            index++;
        }
        return index;
    }

    /// <summary>Finds an exact contiguous declaration sequence in the measured tokens.</summary>
    internal static bool ContainsSequence(IReadOnlyList<string> tokens, IReadOnlyList<string> declaration)
    {
        for (int index = 0; index <= tokens.Count - declaration.Count; index++)
        {
            bool matches = true;
            for (int offset = 0; offset < declaration.Count; offset++)
                matches &= tokens[index + offset] == declaration[offset];
            if (matches) return true;
        }
        return false;
    }
}
