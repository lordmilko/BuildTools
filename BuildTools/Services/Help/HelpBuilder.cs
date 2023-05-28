using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace BuildTools
{
    class HelpBuilder
    {
        private const string CreateHelpMethodName = "CreateHelp";

        private IServiceProvider serviceProvider;

        public HelpBuilder(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public ScriptBlock CreateBlock(CmdletInfo cmdletInfo)
        {
            var createHelp = cmdletInfo.ImplementingType.BaseType.GetMethod(CreateHelpMethodName, BindingFlags.Static | BindingFlags.Public);

            if (createHelp == null)
                throw new MissingMemberException(cmdletInfo.ImplementingType.BaseType.Name, CreateHelpMethodName);

            var helpConfig = InvokeCreateHelp(cmdletInfo, createHelp);

            ValidateHelp(helpConfig);

            var comment = BuildComment(helpConfig);

            return ScriptBlock.Create(comment);
        }

        private HelpConfig InvokeCreateHelp(CmdletInfo cmdletInfo, MethodInfo createHelp)
        {
            var helpConfig = new HelpConfig(cmdletInfo.Name);

            var parameters = createHelp.GetParameters();

            var args = new List<object>();

            foreach (var parameter in parameters)
            {
                object value;

                if (parameter.ParameterType == typeof(HelpConfig))
                    value = helpConfig;
                else if (parameter.ParameterType == typeof(ProjectConfig))
                    value = serviceProvider.GetService<IProjectConfigProvider>().Config;
                else
                    value = serviceProvider.GetService(parameter.ParameterType);

                args.Add(value);
            }

            createHelp.Invoke(null, args.ToArray());

            return helpConfig;
        }

        private void ValidateHelp(HelpConfig helpConfig)
        {
            if (helpConfig.Synopsis == null)
                throw new InvalidOperationException($"A synopsis for command '{helpConfig.Command}' was not set");
        }

        private string BuildComment(HelpConfig helpConfig)
        {
            var builder = new StringBuilder();

            builder.AppendLine("<#");

            builder.AppendLine(".SYNOPSIS");
            builder.AppendLine(helpConfig.Synopsis.Trim());

            if (!string.IsNullOrWhiteSpace(helpConfig.Description))
            {
                builder.AppendLine();
                builder.AppendLine(".DESCRIPTION");
                builder.AppendLine(helpConfig.Description.Trim());
            }

            if (helpConfig.Parameters != null)
            {
                bool shouldIncludeParameter(HelpParameter hp)
                {
                    if (hp is ConditionalHelpParameter c)
                    {
                        var predicateParams = c.Predicate.Method.GetParameters().Select(p => serviceProvider.GetService(p.ParameterType)).ToArray();

                        var result = (bool) c.Predicate.DynamicInvoke(predicateParams);

                        return result;
                    }

                    return true;
                }

                foreach (var parameter in helpConfig.Parameters)
                {
                    if (!shouldIncludeParameter(parameter))
                        continue;

                    builder.AppendLine();
                    builder.Append(".PARAMETER").Append(" ").AppendLine(parameter.Name);
                    builder.AppendLine(parameter.Description);
                }
            }

            if (helpConfig.Examples != null)
            {
                foreach (var example in helpConfig.Examples)
                {
                    builder.AppendLine();
                    builder.AppendLine(".EXAMPLE");
                    builder.Append("C:\\> ").AppendLine(example.Command);
                    builder.AppendLine(example.Description);
                }
            }

            if (helpConfig.RelatedLinks != null && helpConfig.RelatedLinks.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine(".LINK");

                foreach (var item in helpConfig.RelatedLinks)
                    builder.AppendLine(item.Name);
            }

            builder.AppendLine("#>");

            return builder.ToString();
        }
    }
}