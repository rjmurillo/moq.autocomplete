using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Test.Mocks.Edit
{
    [ExportCompletionProvider(nameof(MoqIsAnyCompletionProvider), LanguageNames.CSharp)]
    public class MoqIsAnyCompletionProvider : CompletionProvider
    {

        private static Regex setupMethodNamePattern = new Regex("^Moq\\.Mock<.*>\\.Setup\\.*");
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            // wrap it in try catch, because exception for some reason sometimes crash entire VisualStudio
            try
            {
                if (!context.Document.SupportsSemanticModel) return;

                var syntaxRoot = await context.Document.GetSyntaxRootAsync();
                var semanticModel = await context.Document.GetSemanticModelAsync();

                var tokenAtCursor = GetCurrentArgumentListSyntaxToken(syntaxRoot, context.Position);
                if (tokenAtCursor.Kind() == SyntaxKind.None) return;
                var mockedMethodArgumentList = tokenAtCursor.Parent as ArgumentListSyntax;
                var mockedMethodInvocation = mockedMethodArgumentList?.Parent as InvocationExpressionSyntax;
                var setupMethodLambda = mockedMethodInvocation?.Parent as LambdaExpressionSyntax;
                var setupMethodArgument = setupMethodLambda?.Parent as ArgumentSyntax;
                var setupMethodArgumentList = setupMethodArgument?.Parent as ArgumentListSyntax;
                var setupMethodInvocation = setupMethodArgumentList?.Parent as InvocationExpressionSyntax;
                var setupMethod = setupMethodInvocation?.Expression as MemberAccessExpressionSyntax;
                if (setupMethodInvocation == null) return;


                var setupMethodSymbolInfo = semanticModel.GetSymbolInfo(setupMethod);
                bool isTrueMoqSetupMethod = false;
                if (setupMethodSymbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                {
                    isTrueMoqSetupMethod = setupMethodSymbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Any(s => setupMethodNamePattern.IsMatch(s.ToString()));
                }
                else if (setupMethodSymbolInfo.CandidateReason == CandidateReason.None && setupMethodSymbolInfo.Symbol is IMethodSymbol && setupMethodNamePattern.IsMatch(setupMethodSymbolInfo.Symbol.ToString()))
                {
                    isTrueMoqSetupMethod = true;
                }

                if (isTrueMoqSetupMethod)
                {
                    var matchingMockedMethods = new List<IMethodSymbol>();
                    var mockedMethodsSymbolInfo = semanticModel.GetSymbolInfo(mockedMethodInvocation.Expression);
                    if (mockedMethodsSymbolInfo.CandidateReason == CandidateReason.None && mockedMethodsSymbolInfo.Symbol is IMethodSymbol)
                    {
                        matchingMockedMethods.Add(mockedMethodsSymbolInfo.Symbol as IMethodSymbol);
                    }
                    else if (mockedMethodsSymbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                    {
                        matchingMockedMethods.AddRange(mockedMethodsSymbolInfo.CandidateSymbols.OfType<IMethodSymbol>());
                    }
                    CompletionItemRules rules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect).WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);
                    foreach (IMethodSymbol matchingMockedMethodSymbol in matchingMockedMethods.Where(m => m.Parameters.Any()))
                    {
                        if (tokenAtCursor.Kind() == SyntaxKind.OpenParenToken)
                        {
                            var fullMethodHelper = string.Join(", ", matchingMockedMethodSymbol.Parameters.Select(p => "It.IsAny<" + p.Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()"));
                            context.AddItem(CompletionItem.Create(fullMethodHelper, rules: rules));
                            var oneArgumentHelper = "It.IsAny<" + matchingMockedMethodSymbol.Parameters[0].Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()";
                            context.AddItem(CompletionItem.Create(oneArgumentHelper, rules: rules));
                        }
                        else if (tokenAtCursor.Kind() == SyntaxKind.CommaToken)
                        {
                            var allCommaTokens = mockedMethodArgumentList.ChildTokens().Where(t => t.IsKind(SyntaxKind.CommaToken)).ToList();
                            int paramIdx = allCommaTokens.IndexOf(tokenAtCursor) + 1;
                            if (matchingMockedMethodSymbol.Parameters.Length > paramIdx)
                            {
                                var oneArgumentHelper = "It.IsAny<" + matchingMockedMethodSymbol.Parameters[paramIdx].Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()";
                                context.AddItem(CompletionItem.Create(oneArgumentHelper, rules: rules));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static SyntaxToken GetCurrentArgumentListSyntaxToken(SyntaxNode node, int currentPosition)
        {
            var allArgumentLists = node.DescendantNodes(n => n.FullSpan.Contains(currentPosition - 1)).OfType<ArgumentListSyntax>().OrderBy(n => n.FullSpan.Length);
            return allArgumentLists.SelectMany(n => n.ChildTokens()
                .Where(t => t.IsKind(SyntaxKind.OpenParenToken) || t.IsKind(SyntaxKind.CommaToken))
                .Where(t => t.FullSpan.Contains(currentPosition - 1))).FirstOrDefault();
            // return allNodes.OfType<ArgumentListSyntax>().FirstOrDefault(n => n.OpenParenToken.FullSpan.Contains(currentPosition - 1) || n.ChildTokens().Any(t => t.IsKind(SyntaxKind.CommaToken) && t.FullSpan.Contains(currentPosition - 1)));
        }
    }
}
