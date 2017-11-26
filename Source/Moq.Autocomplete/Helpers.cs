using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Agent.Zorge.Moq
{
    internal class Helpers
    {
        private static Regex setupMethodNamePattern = new Regex("^Moq\\.Mock<.*>\\.Setup\\.*");

        internal static bool IsMoqSetupMethod(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
        {
            var method = invocation.Expression as MemberAccessExpressionSyntax;
            return IsMoqSetupMethod(semanticModel, method);
        }

        internal static bool IsMoqSetupMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
        {
            var methodName = method?.Name.ToString();
            // First fast check before walking semantic model
            if (methodName != "Setup") return false;

            var symbolInfo = semanticModel.GetSymbolInfo(method);
            if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
            {
                return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Any(s => setupMethodNamePattern.IsMatch(s.ToString()));
            }
            else if (symbolInfo.CandidateReason == CandidateReason.None)
            {
                return symbolInfo.Symbol is IMethodSymbol && setupMethodNamePattern.IsMatch(symbolInfo.Symbol.ToString());
            }
            return false;
        }

        internal static SyntaxToken GetCurrentArgumentListSyntaxToken(SyntaxNode node, int currentPosition)
        {
            var allArgumentLists = node.DescendantNodes(n => n.FullSpan.Contains(currentPosition - 1)).OfType<ArgumentListSyntax>().OrderBy(n => n.FullSpan.Length);
            return allArgumentLists.SelectMany(n => n.ChildTokens()
                .Where(t => t.IsKind(SyntaxKind.OpenParenToken) || t.IsKind(SyntaxKind.CommaToken))
                .Where(t => t.FullSpan.Contains(currentPosition - 1))).FirstOrDefault();
        }

        internal static SyntaxToken GetCurrentTypeArgumentList(SyntaxNode node, int currentPosition)
        {
            var allArgumentLists = node.DescendantNodes(n => n.FullSpan.Contains(currentPosition - 1)).OfType<TypeArgumentListSyntax>().OrderBy(n => n.FullSpan.Length);
            return allArgumentLists.SelectMany(n => n.ChildTokens()
                .Where(t => t.IsKind(SyntaxKind.GreaterThanToken))
                .Where(t => t.FullSpan.Contains(currentPosition - 1))).FirstOrDefault();
        }

        internal static bool IsCallbackOrReturnInvocation(SemanticModel semanticModel, InvocationExpressionSyntax callbackOrReturnsInvocation)
        {
            var callbackOrReturnsMethod = callbackOrReturnsInvocation.Expression as MemberAccessExpressionSyntax;
            var methodName = callbackOrReturnsMethod?.Name.ToString();
            // First fast check before walking semantic model
            if (methodName != "Callback" && methodName != "Returns")
            {
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(callbackOrReturnsMethod);
            if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
            {
                return symbolInfo.CandidateSymbols.Any(s => IsCallbackOrReturnSymbol(s));
            }
            else if (symbolInfo.CandidateReason == CandidateReason.None)
            {
                return IsCallbackOrReturnSymbol(symbolInfo.Symbol);
            }
            return false;
        }

        private static bool IsCallbackOrReturnSymbol(ISymbol symbol)
        {
            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol == null) return false;
            var methodName = methodSymbol.ToString();
            return methodName.StartsWith("Moq.Language.ICallback") || methodName.StartsWith("Moq.Language.IReturns");
        }

        internal static InvocationExpressionSyntax FindSetupMethodFromCallbackInvocation(SemanticModel semanticModel, ExpressionSyntax expression)
        {
            var invocation = expression as InvocationExpressionSyntax;
            var method = invocation?.Expression as MemberAccessExpressionSyntax;
            if (method == null) return null;
            if (IsMoqSetupMethod(semanticModel, method)) return invocation;
            return FindSetupMethodFromCallbackInvocation(semanticModel, method.Expression);
        }

        internal static IEnumerable<IMethodSymbol> GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(SemanticModel semanticModel, InvocationExpressionSyntax setupMethodInvocation)
        {
            var setupLambdaArgument = setupMethodInvocation?.ArgumentList.Arguments[0]?.Expression as LambdaExpressionSyntax;
            var mockedMethodInvocation = setupLambdaArgument?.Body as InvocationExpressionSyntax;

            return GetAllMatchingSymbols<IMethodSymbol>(semanticModel, mockedMethodInvocation);
        }

        internal static IEnumerable<T> GetAllMatchingSymbols<T>(SemanticModel semanticModel, ExpressionSyntax expression) where T: class
        {
            var matchingSymbols = new List<T>();
            if (expression != null)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(expression);
                if (symbolInfo.CandidateReason == CandidateReason.None && symbolInfo.Symbol is T)
                {
                    matchingSymbols.Add(symbolInfo.Symbol as T);
                }
                else if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                {
                    matchingSymbols.AddRange(symbolInfo.CandidateSymbols.OfType<T>());
                }
            }
            return matchingSymbols;
        }

        internal static InvocationExpressionSyntax FindMockedMethodInvocationFromSetupMethod(SemanticModel semanticModel, InvocationExpressionSyntax setupInvocation)
        {
            var setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0]?.Expression as LambdaExpressionSyntax;
            return setupLambdaArgument?.Body as InvocationExpressionSyntax;
        }

    }
}
