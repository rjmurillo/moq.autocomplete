using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Agent.Zorge.Moq
{
    [ExportCompletionProvider(nameof(MoqIsAnyCompletionProvider), LanguageNames.CSharp)]
    public class MoqIsAnyCompletionProvider : CompletionProvider
    {
        private CompletionItemRules _standardCompletionRules;
        private CompletionItemRules _preselectCompletionRules;

        public MoqIsAnyCompletionProvider()
        {
            _preselectCompletionRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect).WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);
            _standardCompletionRules = CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            // wrap it in try catch, because exception for some reason sometimes crash entire VisualStudio
            try
            {
                if (!context.Document.SupportsSemanticModel) return;

                var syntaxRoot = await context.Document.GetSyntaxRootAsync();
                var semanticModel = await context.Document.GetSemanticModelAsync();

                var tokenAtCursor = Helpers.GetCurrentArgumentListSyntaxToken(syntaxRoot, context.Position);
                if (tokenAtCursor.Kind() == SyntaxKind.None) return;
                var mockedMethodArgumentList = tokenAtCursor.Parent as ArgumentListSyntax;
                var mockedMethodInvocation = mockedMethodArgumentList?.Parent as InvocationExpressionSyntax;
                var setupMethodLambda = mockedMethodInvocation?.Parent as LambdaExpressionSyntax;
                var setupMethodArgument = setupMethodLambda?.Parent as ArgumentSyntax;
                var setupMethodArgumentList = setupMethodArgument?.Parent as ArgumentListSyntax;
                var setupMethodInvocation = setupMethodArgumentList?.Parent as InvocationExpressionSyntax;

                if (Helpers.IsMoqSetupMethod(semanticModel, setupMethodInvocation))
                {
                    var matchingMockedMethods = Helpers.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(semanticModel, setupMethodInvocation);
                    
                    // TODO Narrow the list of matching signatures if some arguments are already provided
                    foreach (IMethodSymbol matchingMockedMethodSymbol in matchingMockedMethods.Where(m => m.Parameters.Any()))
                    {
                        // We are at first argument
                        if (tokenAtCursor.IsKind(SyntaxKind.OpenParenToken))
                        {
                            // Generate It.IsAny<>() for the whole signature if we are within first argument
                            var fullMethodHelper = string.Join(", ", matchingMockedMethodSymbol.Parameters.Select(p => "It.IsAny<" + p.Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()"));
                            context.AddItem(CompletionItem.Create(fullMethodHelper, rules: _preselectCompletionRules));
                            // There are more than one argument so suggest completion for first argument only too
                            if (matchingMockedMethodSymbol.Parameters.Length > 1)
                            {
                                var oneArgumentHelper = "It.IsAny<" + matchingMockedMethodSymbol.Parameters[0].Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()";
                                context.AddItem(CompletionItem.Create(oneArgumentHelper, rules: _standardCompletionRules));
                            }
                        }
                        // We are at second+ argument
                        else
                        {
                            // Generate It.IsAny<>() for single argument
                            var allCommaTokens = mockedMethodArgumentList.ChildTokens().Where(t => t.IsKind(SyntaxKind.CommaToken)).ToList();
                            int paramIdx = allCommaTokens.IndexOf(tokenAtCursor) + 1;
                            if (matchingMockedMethodSymbol.Parameters.Length > paramIdx)
                            {
                                var oneArgumentHelper = "It.IsAny<" + matchingMockedMethodSymbol.Parameters[paramIdx].Type.ToMinimalDisplayString(semanticModel, mockedMethodArgumentList.SpanStart) + ">()";
                                context.AddItem(CompletionItem.Create(oneArgumentHelper, rules: _standardCompletionRules));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
