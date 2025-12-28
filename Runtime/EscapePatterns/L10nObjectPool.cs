using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Minerva.Localizations.EscapePatterns
{
    /// <summary>
    /// High-performance object pool for L10n tokenization and evaluation
    /// </summary>
    internal static class L10nObjectPool
    {
        private const int MAX_POOL_SIZE = 64;
        private const int MAX_TOKEN_LIST_CAPACITY = 32;
        private const int MAX_STRING_BUILDER_CAPACITY = 512;

        #region Token Pool

        private static readonly ConcurrentBag<L10nToken> s_tokenPool = new();
        private static int s_tokenPoolCount = 0;

        public static L10nToken RentToken()
        {
            if (s_tokenPool.TryTake(out var token))
            {
                return token;
            }

            return new L10nToken();
        }

        public static void ReturnToken(L10nToken token)
        {
            if (token == null) return;

            // ✅ 递归归还所有子 token 和 children list
            if (token.Children != null)
            {
                foreach (var child in token.Children)
                {
                    ReturnToken(child);
                }

                // ✅ 归还 children list 到池
                ReturnTokenList(token.Children);
                token.Children = null;
            }

            // Reset token
            token.Type = TokenType.Literal;
            token.Content = default;
            token.Metadata = default;
            token.IsTooltip = false;

            // Return to pool if not full
            if (s_tokenPoolCount < MAX_POOL_SIZE)
            {
                s_tokenPool.Add(token);
                System.Threading.Interlocked.Increment(ref s_tokenPoolCount);
            }
        }

        #endregion

        #region Token List Pool

        private static readonly ConcurrentBag<List<L10nToken>> s_tokenListPool = new();
        private static int s_tokenListPoolCount = 0;

        public static List<L10nToken> RentTokenList()
        {
            if (s_tokenListPool.TryTake(out var list))
            {
                return list;
            }

            return new List<L10nToken>(MAX_TOKEN_LIST_CAPACITY);
        }

        public static void ReturnTokenList(List<L10nToken> list)
        {
            if (list == null) return;

            // ❌ 不要在这里归还 tokens！它们由 ReturnToken 处理
            // 只清空 list
            list.Clear();

            // Trim if too large
            if (list.Capacity > MAX_TOKEN_LIST_CAPACITY * 2)
            {
                list.Capacity = MAX_TOKEN_LIST_CAPACITY;
            }

            // Return to pool if not full
            if (s_tokenListPoolCount < MAX_POOL_SIZE)
            {
                s_tokenListPool.Add(list);
                System.Threading.Interlocked.Increment(ref s_tokenListPoolCount);
            }
        }

        #endregion

        #region StringBuilder Pool

        private static readonly ConcurrentBag<StringBuilder> s_stringBuilderPool = new();
        private static int s_stringBuilderPoolCount = 0;

        public static StringBuilder RentStringBuilder()
        {
            if (s_stringBuilderPool.TryTake(out var sb))
            {
                System.Threading.Interlocked.Decrement(ref s_stringBuilderPoolCount);
                return sb;
            }

            return new StringBuilder(MAX_STRING_BUILDER_CAPACITY);
        }

        public static void ReturnStringBuilder(StringBuilder sb)
        {
            if (sb == null) return;

            sb.Clear();

            // Trim if too large
            if (sb.Capacity > MAX_STRING_BUILDER_CAPACITY * 2)
            {
                sb.Capacity = MAX_STRING_BUILDER_CAPACITY;
            }

            // Return to pool if not full
            if (s_stringBuilderPoolCount < MAX_POOL_SIZE)
            {
                s_stringBuilderPool.Add(sb);
                System.Threading.Interlocked.Increment(ref s_stringBuilderPoolCount);
            }
        }

        #endregion

        #region Tokenizer Pool

        private static readonly ConcurrentBag<L10nTokenizer> s_tokenizerPool = new();
        private static int s_tokenizerPoolCount = 0;

        public static L10nTokenizer RentTokenizer(ReadOnlyMemory<char> source)
        {
            if (s_tokenizerPool.TryTake(out var tokenizer))
            {
                System.Threading.Interlocked.Decrement(ref s_tokenizerPoolCount);
                tokenizer.Reset(source);
                return tokenizer;
            }

            return new L10nTokenizer(source);
        }

        public static void ReturnTokenizer(L10nTokenizer tokenizer)
        {
            if (tokenizer == null) return;

            tokenizer.Reset(default);

            // Return to pool if not full
            if (s_tokenizerPoolCount < MAX_POOL_SIZE)
            {
                s_tokenizerPool.Add(tokenizer);
                System.Threading.Interlocked.Increment(ref s_tokenizerPoolCount);
            }
        }

        #endregion

        #region Evaluator Pool

        private static readonly ConcurrentBag<L10nEvaluator> s_evaluatorPool = new();
        private static int s_evaluatorPoolCount = 0;

        public static L10nEvaluator RentEvaluator(EvaluationContext context)
        {
            if (s_evaluatorPool.TryTake(out var evaluator))
            {
                System.Threading.Interlocked.Decrement(ref s_evaluatorPoolCount);
                evaluator.Reset(context);
                return evaluator;
            }

            return new L10nEvaluator(context);
        }

        public static void ReturnEvaluator(L10nEvaluator evaluator)
        {
            if (evaluator == null) return;

            evaluator.Clear();

            // Return to pool if not full
            if (s_evaluatorPoolCount < MAX_POOL_SIZE)
            {
                s_evaluatorPool.Add(evaluator);
                System.Threading.Interlocked.Increment(ref s_evaluatorPoolCount);
            }
        }

        #endregion

        #region Diagnostics

        public static (int tokens, int tokenLists, int stringBuilders, int tokenizers, int evaluators) GetPoolStats()
        {
            return (
                s_tokenPoolCount,
                s_tokenListPoolCount,
                s_stringBuilderPoolCount,
                s_tokenizerPoolCount,
                s_evaluatorPoolCount
            );
        }

        public static void ClearPools()
        {
            while (s_tokenPool.TryTake(out _)) { }
            while (s_tokenListPool.TryTake(out _)) { }
            while (s_stringBuilderPool.TryTake(out _)) { }
            while (s_tokenizerPool.TryTake(out _)) { }
            while (s_evaluatorPool.TryTake(out _)) { }

            s_tokenPoolCount = 0;
            s_tokenListPoolCount = 0;
            s_stringBuilderPoolCount = 0;
            s_tokenizerPoolCount = 0;
            s_evaluatorPoolCount = 0;
        }

        #endregion
    }
}