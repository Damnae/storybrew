using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tiny
{
    internal class TinyTokenParser
    {
        public static TinyToken Parse(IEnumerable<TinyTokenizer.Token> tokens)
        {
            TinyToken result = null;
            var context = new ParseContext(tokens, r => result = r);

            TinyTokenizer.Token previousToken = null;
            while (context.CurrentToken != null)
            {
                if (context.CurrentToken != previousToken)
                {
                    //Debug.Print($"{context.CurrentToken}");
                    previousToken = context.CurrentToken;
                }

                switch (context.CurrentToken.TokenType)
                {
                    case TinyTokenizer.TokenType.Indent:
                        context.Indent(context.CurrentToken.Value.Length / 2);
                        context.ConsumeToken();
                        continue;

                    case TinyTokenizer.TokenType.EndLine:
                        context.NewLine();
                        context.ConsumeToken();
                        continue;
                }
                //Debug.Print($"  - {context.Parser.GetType().Name} ({context.ParserCount})");
                context.Parser.Parse(context);
            }

            while (context.Parser != null)
            {
                context.Parser.End();
                context.PopParser();
            }

            return result;
        }

        private class ParseContext
        {
            private readonly IEnumerator<TinyTokenizer.Token> tokenEnumerator;

            public TinyTokenizer.Token CurrentToken;
            public TinyTokenizer.Token LookaheadToken;

            private readonly Stack<Parser> parserStack;
            public Parser Parser => parserStack.Count > 0 ? parserStack.Peek() : null;
            public int ParserCount => parserStack.Count;

            public int IndentLevel { get; private set; }

            public ParseContext(IEnumerable<TinyTokenizer.Token> tokens, Action<TinyToken> callback)
            {
                tokenEnumerator = tokens.GetEnumerator();
                initializeCurrentAndLookahead();

                parserStack = new Stack<Parser>();
                parserStack.Push(new AnyParser(callback));
            }

            public void PopParser()
            {
                parserStack.Pop();
            }

            public void PushParser(Parser parser)
            {
                parserStack.Push(parser);
            }

            public void ReplaceParser(Parser parser)
            {
                parserStack.Pop();
                parserStack.Push(parser);
            }

            public void Indent(int level)
            {
                IndentLevel = level;
            }

            public void NewLine()
            {
                IndentLevel = 0;
            }

            public void ConsumeToken()
            {
                CurrentToken = LookaheadToken;
                LookaheadToken = tokenEnumerator.MoveNext() ? tokenEnumerator.Current : null;
            }

            private void initializeCurrentAndLookahead()
            {
                ConsumeToken();
                ConsumeToken();
            }
        }

        private abstract class Parser
        {
            protected readonly Action<TinyToken> Callback;
            protected readonly int VirtualIndent;

            public Parser(Action<TinyToken> callback, int virtualIndent)
            {
                Callback = callback;
                VirtualIndent = virtualIndent;
            }

            public abstract void Parse(ParseContext context);
            public abstract void End();
        }

        private abstract class MultilineParser : Parser
        {
            private int? indent = null;

            protected abstract int ResultCount { get; }

            public MultilineParser(Action<TinyToken> callback, int virtualIndent) : base(callback, virtualIndent)
            {
            }

            protected bool CheckIndent(ParseContext context)
            {
                indent = indent ?? context.IndentLevel + VirtualIndent;
                var lineIndent = ResultCount == 0 ? context.IndentLevel + VirtualIndent : context.IndentLevel;
                if (lineIndent != indent)
                {
                    if (lineIndent > indent)
                        throw new InvalidDataException($"Unexpected indent: {lineIndent}, expected: {indent}, token: {context.CurrentToken}");

                    context.PopParser();
                    return true;
                }
                return false;
            }
        }

        private class ObjectParser : MultilineParser
        {
            private readonly TinyObject result = new TinyObject();

            protected override int ResultCount => result.Count;

            public ObjectParser(Action<TinyToken> callback, int virtualIndent = 0) : base(callback, virtualIndent)
            {
                callback(result);
            }

            public override void Parse(ParseContext context)
            {
                if (CheckIndent(context))
                    return;

                switch (context.LookaheadToken.TokenType)
                {
                    case TinyTokenizer.TokenType.ArrayIndicator:
                    case TinyTokenizer.TokenType.Property:
                    case TinyTokenizer.TokenType.PropertyQuoted:
                        throw new InvalidDataException("Unexpected token: " + context.LookaheadToken + ", after: " + context.CurrentToken);
                }

                switch (context.CurrentToken.TokenType)
                {
                    case TinyTokenizer.TokenType.Property:
                    case TinyTokenizer.TokenType.PropertyQuoted:

                        var key = context.CurrentToken.Value;
                        if (context.CurrentToken.TokenType == TinyTokenizer.TokenType.PropertyQuoted)
                            key = TinyUtil.UnescapeString(key);

                        switch (context.LookaheadToken.TokenType)
                        {
                            case TinyTokenizer.TokenType.Word:
                            case TinyTokenizer.TokenType.WordQuoted:
                                context.PushParser(new ValueParser(r => result.Add(key, r)));
                                break;
                            case TinyTokenizer.TokenType.EndLine:
                                context.PushParser(new EmptyProperyParser(r => result.Add(key, r), context.IndentLevel + 1));
                                break;
                            default:
                                throw new InvalidDataException("Unexpected token: " + context.LookaheadToken + ", after: " + context.CurrentToken);
                        }
                        context.ConsumeToken();
                        return;
                }
                throw new InvalidDataException("Unexpected token: " + context.CurrentToken);
            }

            public override void End()
            {
            }
        }

        private class ArrayParser : MultilineParser
        {
            private readonly TinyArray result = new TinyArray();

            protected override int ResultCount => result.Count;

            public ArrayParser(Action<TinyToken> callback, int virtualIndent = 0) : base(callback, virtualIndent)
            {
                callback(result);
            }

            public override void Parse(ParseContext context)
            {
                if (CheckIndent(context))
                    return;

                switch (context.CurrentToken.TokenType)
                {
                    case TinyTokenizer.TokenType.ArrayIndicator:
                        context.PushParser(new AnyParser(r => result.Add(r), result.Count == 0 ? VirtualIndent + 1 : 1));
                        context.ConsumeToken();
                        return;
                }

                throw new InvalidDataException("Unexpected token: " + context.CurrentToken);
            }

            public override void End()
            {
            }
        }

        private class ValueParser : Parser
        {
            private static readonly Regex floatRegex = new Regex("^[-+]?[0-9]*\\.[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            private static readonly Regex integerRegex = new Regex("^[-+]?\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            private static readonly Regex boolRegex = new Regex($"^{TinyValue.BooleanTrue}|{TinyValue.BooleanFalse}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            public ValueParser(Action<TinyToken> callback) : base(callback, 0)
            {
            }

            public override void Parse(ParseContext context)
            {
                switch (context.LookaheadToken.TokenType)
                {
                    case TinyTokenizer.TokenType.EndLine:
                        break;
                    default:
                        throw new InvalidDataException("Unexpected token: " + context.LookaheadToken + ", after: " + context.CurrentToken);
                }

                switch (context.CurrentToken.TokenType)
                {
                    case TinyTokenizer.TokenType.Word:
                        {
                            var value = context.CurrentToken.Value;
                            Match match;
                            if ((match = floatRegex.Match(value)).Success)
                                Callback(new TinyValue(value, TinyTokenType.Float));
                            else if ((match = integerRegex.Match(value)).Success)
                                Callback(new TinyValue(value, TinyTokenType.Integer));
                            else if ((match = boolRegex.Match(value)).Success)
                                Callback(new TinyValue(value == TinyValue.BooleanTrue));
                            else
                                Callback(new TinyValue(value));
                            context.ConsumeToken();
                            context.PopParser();
                        }
                        return;

                    case TinyTokenizer.TokenType.WordQuoted:
                        {
                            var value = TinyUtil.UnescapeString(context.CurrentToken.Value);
                            Callback(new TinyValue(value));
                            context.ConsumeToken();
                            context.PopParser();
                        }
                        return;
                }

                throw new InvalidDataException("Unexpected token: " + context.CurrentToken);
            }

            public override void End()
            {
            }
        }

        private class AnyParser : Parser
        {
            public AnyParser(Action<TinyToken> callback, int virtualIndent = 0) : base(callback, virtualIndent)
            {
            }

            public override void Parse(ParseContext context)
            {
                switch (context.CurrentToken.TokenType)
                {
                    case TinyTokenizer.TokenType.Property:
                    case TinyTokenizer.TokenType.PropertyQuoted:
                        context.ReplaceParser(new ObjectParser(Callback, VirtualIndent));
                        return;
                    case TinyTokenizer.TokenType.ArrayIndicator:
                        context.ReplaceParser(new ArrayParser(Callback, VirtualIndent));
                        return;
                    case TinyTokenizer.TokenType.Word:
                    case TinyTokenizer.TokenType.WordQuoted:
                        context.ReplaceParser(new ValueParser(Callback));
                        return;
                }
                throw new InvalidDataException("Unexpected token: " + context.CurrentToken);
            }

            public override void End()
            {
            }
        }

        private class EmptyProperyParser : Parser
        {
            private readonly int expectedIndent;

            public EmptyProperyParser(Action<TinyToken> callback, int expectedIndent, int virtualIndent = 0) : base(callback, virtualIndent)
            {
                this.expectedIndent = expectedIndent;
            }

            public override void Parse(ParseContext context)
            {
                if (context.IndentLevel < expectedIndent)
                {
                    Callback(new TinyValue(null, TinyTokenType.Null));
                    context.PopParser();
                    return;
                }

                if (context.IndentLevel == expectedIndent)
                {
                    context.ReplaceParser(new AnyParser(Callback));
                    return;
                }

                throw new InvalidDataException($"Unexpected indent: {context.IndentLevel}, expected: {expectedIndent}, token: {context.CurrentToken}");
            }

            public override void End()
            {
                Callback(new TinyValue(null, TinyTokenType.Null));
            }
        }
    }
}