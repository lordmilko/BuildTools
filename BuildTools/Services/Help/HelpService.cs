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

        static HelpService()
        {
            InitializeHelpCommentsParser();
        }

        #region Reflection

        private static MethodInfo HelpCommentsParser_GetHelpCommentTokens;
        private static MethodInfo HelpCommentsParser_CreateFromComments;

        //Instance Fields
        private static FieldInfo HelpCommentsParser_ScriptBlock;
        private static FieldInfo HelpCommentsParser_CommandName;

        //Instance Methods
        private static ConstructorInfo HelpCommentsParser_Ctor;
        private static MethodInfo HelpCommentsParser_AnalyzeCommentBlock;

        private static void InitializeHelpCommentsParser()
        {
            var typeName = "System.Management.Automation.HelpCommentsParser";

            var helpCommentsParserType = typeof(PSCmdlet).Assembly.GetType(typeName);

            if (helpCommentsParserType == null)
                throw new InvalidOperationException($"Failed to find type '{typeName}'.");

            //Static Methods
            HelpCommentsParser_GetHelpCommentTokens = helpCommentsParserType.GetStaticInternalMethod("GetHelpCommentTokens");
            HelpCommentsParser_CreateFromComments = helpCommentsParserType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(m => m.Name == "CreateFromComments" && m.GetParameters().Length == 4);

            //Instance Fields
            HelpCommentsParser_ScriptBlock = helpCommentsParserType.GetInternalFieldInfo("scriptBlock");
            HelpCommentsParser_CommandName = helpCommentsParserType.GetInternalFieldInfo("commandName");

            //Instance Methods
            HelpCommentsParser_Ctor = helpCommentsParserType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single(c => c.GetParameters().Length == 2);
            HelpCommentsParser_AnalyzeCommentBlock = helpCommentsParserType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == "AnalyzeCommentBlock" && m.GetParameters().Single().ParameterType == typeof(List<Token>));
        }

        #endregion

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

            foreach (var commandInfo in commands)
            {
                using (new HelpMetadataHolder(commandInfo))
                {
                    var helpBlock = helpBuilder.CreateBlock(commandInfo);
                    var helpInfo = GetHelpInfo(commandInfo, context, helpBlock, scriptBlockTokenCache);

                    addToCommandCache.Invoke(
                        provider,
                        new[] { commandInfo.ModuleName, commandInfo.Name, helpInfo }
                    );
                }
            }
        }

        private object GetHelpInfo(CommandInfo commandInfo, object context, ScriptBlock sb, Dictionary<Ast, Token[]> scriptBlockTokenCache)
        {
            var ast = sb.Ast;

            var commentTokens = (Tuple<List<Token>, List<string>>) HelpCommentsParser_GetHelpCommentTokens.Invoke(null, new object[]{ast, scriptBlockTokenCache});
            var comments = commentTokens.Item1;
            var parameterDescriptions = commentTokens.Item2;

            var helpCommentsParser = HelpCommentsParser_Ctor.Invoke(new object[] {commandInfo, parameterDescriptions});
            HelpCommentsParser_ScriptBlock.SetValue(helpCommentsParser, sb);
            HelpCommentsParser_CommandName.SetValue(helpCommentsParser, commandInfo.Name);
            HelpCommentsParser_AnalyzeCommentBlock.Invoke(helpCommentsParser, new object[] {comments});

            var helpInfo = HelpCommentsParser_CreateFromComments.Invoke(
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
