using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tiny
{
    internal class TinyTokenizer
    {
        private static readonly List<TokenDefinition> definitions = new List<TokenDefinition>()
        {
            new TokenDefinition(TokenType.Indent, "^(  )+", 1, captureGroup: 0),

            new TokenDefinition(TokenType.PropertyQuoted, @"""((?:[^""\\]|\\.)*)"" *:", 4),
            new TokenDefinition(TokenType.WordQuoted, @"""((?:[^""\\]|\\.)*)""", 5),

            new TokenDefinition(TokenType.ArrayIndicator, "- ", 10),

            new TokenDefinition(TokenType.Property, "([^\\s:-][^\\s:]*) *:", 20),
            new TokenDefinition(TokenType.Word, "[^\\s:]+", 21),

            new TokenDefinition(TokenType.EndLine, "\n", 100),
        };

        public enum TokenType
        {
            Indent,
            PropertyQuoted,
            Property,
            WordQuoted,
            Word,
            ArrayIndicator,
            EndLine,
        }

        public static IEnumerable<Token> Tokenize(TextReader reader)
        {
            string line;
            int lineNumber = 1;
            while ((line = reader.ReadLine()) != null)
            {
                foreach (var token in Tokenize(line))
                {
                    token.LineNumber = lineNumber;
                    yield return token;
                }

                lineNumber++;
            }
        }

        public static IEnumerable<Token> Tokenize(string content)
        {
            var matches = definitions
                .SelectMany(d => d.FindMatches(content));

            var byStartGroups = matches
                .GroupBy(m => m.StartIndex)
                .OrderBy(g => g.Key);

            TokenDefinition.Match previousMatch = null;
            foreach (var byStartGroup in byStartGroups)
            {
                var bestMatch = byStartGroup
                    .OrderBy(m => m.Priority)
                    .First();

                if (previousMatch != null && bestMatch.StartIndex < previousMatch.EndIndex)
                    continue;

                yield return new Token(bestMatch.Type, bestMatch.Value);
                previousMatch = bestMatch;
            }

            yield return new Token(TokenType.EndLine);
        }

        public class Token
        {
            public TokenType TokenType;
            public string Value;
            public int LineNumber;

            public Token(TokenType tokenType, string value = null)
            {
                TokenType = tokenType;
                Value = value;
            }

            public override string ToString() => $"{TokenType} <{Value}> (line {LineNumber})";
        }

        private class TokenDefinition
        {
            private Regex regex;
            private readonly TokenType matchType;
            private readonly int priority;
            private readonly int captureGroup;

            public TokenDefinition(TokenType matchType, string regexPattern, int priority, int captureGroup = 1)
            {
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                this.matchType = matchType;
                this.priority = priority;
                this.captureGroup = captureGroup;
            }

            public IEnumerable<Match> FindMatches(string input)
            {
                var matches = regex.Matches(input);
                foreach (System.Text.RegularExpressions.Match match in matches)
                    yield return new Match()
                    {
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length,
                        Priority = priority,
                        Type = matchType,
                        Value = match.Groups.Count > captureGroup ?
                            match.Groups[captureGroup].Value :
                            match.Value,
                    };
            }

            public override string ToString() => $"regex:{regex}, matchType:{matchType}, priority:{priority}, captureGroup:{captureGroup}";

            public class Match
            {
                public int StartIndex;
                public int EndIndex;
                public int Priority;
                public TokenType Type;
                public string Value;

                public override string ToString() => $"{Type} <{Value}> from {StartIndex} to {EndIndex}, priority:{Priority}";
            }
        }
    }
}
