using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using BuildTools.PowerShell;
using BuildTools.Reflection;

namespace BuildTools
{
    class HelpService : IHelpService
    {
        private IPowerShellService powerShell;
        private HelpBuilder helpBuilder;

        public HelpService(
            IPowerShellService powerShell,
            HelpBuilder helpBuilder)
        {
            this.powerShell = powerShell;
            this.helpBuilder = helpBuilder;
        }

        public void RegisterHelp(IPowerShellModule module)
        {
            var cmdlet = ((PowerShellService) powerShell).ActiveCmdlet;

            var context = cmdlet.GetInternalProperty("Context");
            var helpProviders = ((IEnumerable) context.GetInternalProperty("HelpSystem").GetInternalProperty("HelpProviders")).Cast<object>();
            var provider = helpProviders.Single(p => p.GetType().Name == "CommandHelpProvider");
            var addToCommandCache = provider.GetInternalMethod("AddToCommandCache");

            var scriptBlockTokenCache = new Dictionary<Ast, Token[]>();
            var commands = ((PowerShellModule)module).Module.ExportedCmdlets.Values;

            foreach (var commandInfo in commands.Take(1))
            {
                var helpBlock = helpBuilder.CreateBlock(commandInfo);
                var helpInfo = GetHelpInfo(commandInfo, context, helpBlock, scriptBlockTokenCache);

                addToCommandCache.Invoke(
                    provider,
                    new[] { commandInfo.ModuleName, commandInfo.Name, helpInfo }
                );
            }
        }

        private object GetHelpInfo(CommandInfo commandInfo, object context, ScriptBlock sb, Dictionary<Ast, Token[]> scriptBlockTokenCache)
        {
            var typeName = "System.Management.Automation.HelpCommentsParser";

            var helpCommentsParserType = typeof(PSCmdlet).Assembly.GetType(typeName);

            if (helpCommentsParserType == null)
                throw new InvalidOperationException($"Failed to find type '{typeName}'.");

            //Static Methods
            var getHelpCommentTokensMethod = helpCommentsParserType.GetStaticInternalMethod("GetHelpCommentTokens");
            var createFromCommentsMethod = helpCommentsParserType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(m => m.Name == "CreateFromComments" && m.GetParameters().Length == 4);

            //Instance Fields
            var helpCommentsParserScriptBlock = helpCommentsParserType.GetInternalFieldInfo("scriptBlock");
            var helpCommentsParserCommandName = helpCommentsParserType.GetInternalFieldInfo("commandName");

            //Instance Methods
            var helpCommentsParserCtor = helpCommentsParserType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single(c => c.GetParameters().Length == 2);
            var analyzeCommentBlockMethod = helpCommentsParserType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == "AnalyzeCommentBlock" && m.GetParameters().Single().ParameterType == typeof(List<Token>));

            var ast = sb.Ast;

            var commentTokens = (Tuple<List<Token>, List<string>>) getHelpCommentTokensMethod.Invoke(null, new object[]{ast, scriptBlockTokenCache});
            var comments = commentTokens.Item1;
            var parameterDescriptions = commentTokens.Item2;

            var helpCommentsParser = helpCommentsParserCtor.Invoke(new object[] {commandInfo, parameterDescriptions});
            helpCommentsParserScriptBlock.SetValue(helpCommentsParser, sb);
            helpCommentsParserCommandName.SetValue(helpCommentsParser, commandInfo.Name);
            analyzeCommentBlockMethod.Invoke(helpCommentsParser, new object[] {comments});

            var helpInfo = createFromCommentsMethod.Invoke(
                null,
                new[]
                {
                    context,
                    commandInfo,
                    helpCommentsParser,
                    true
                }
            );

            return helpInfo;
        }
    }
}
