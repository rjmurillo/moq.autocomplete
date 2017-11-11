using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Mocks.Edit
{
    [ExportCompletionProvider(nameof(MoqIsAnyCompletionProvider), LanguageNames.CSharp)]
    public class MoqIsAnyCompletionProvider : CompletionProvider
    {
    }
}
