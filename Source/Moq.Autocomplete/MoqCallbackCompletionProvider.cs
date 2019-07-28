using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Agent.Zorge.Moq
{
    [ExportCompletionProvider(nameof(MoqCallbackCompletionProvider), LanguageNames.CSharp)]
    public class MoqCallbackCompletionProvider : CompletionProvider
    {
        private CompletionItemRules _standardCompletionRules;
        private CompletionItemRules _preselectCompletionRules;

        public MoqCallbackCompletionProvider()
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
                if (!tokenAtCursor.IsKind(SyntaxKind.OpenParenToken)) return;
                var callbackArgumentList = tokenAtCursor.Parent as ArgumentListSyntax;
                // Ignore if there are already some arguments provided
                if (callbackArgumentList.Arguments.Any()) return;

                var callbackOrReturnsInvocation = callbackArgumentList?.Parent as InvocationExpressionSyntax;
                // Ignore if not Callback() or Returns() method
                if (!Helpers.IsCallbackOrReturnInvocation(semanticModel, callbackOrReturnsInvocation)) return;

                var setupMethodInvocation = Helpers.FindSetupMethodFromCallbackInvocation(semanticModel, callbackOrReturnsInvocation);
                var matchingMockedMethods = Helpers.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(semanticModel, setupMethodInvocation);

                context.AddItem(CompletionItem.Create("() => { }", rules: _standardCompletionRules));
                foreach (IMethodSymbol matchingMockedMethodSymbol in matchingMockedMethods)
                {
                    // Generate It.IsAny<>() for the whole signature if we are within first argument
                    var lambdaParameters = string.Join(", ", matchingMockedMethodSymbol.Parameters.Select(p => p.Type.ToMinimalDisplayString(semanticModel, callbackArgumentList.SpanStart) + " " + p.Name));
                    context.AddItem(CompletionItem.Create("(" + lambdaParameters + ") => { }", rules: _preselectCompletionRules));
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
