using System;
using System.Linq;
using System.Reflection;

namespace BuildTools
{
    internal static class ProjectConfigExtensions
    {
        public static bool HasFeature(this IProjectConfigProvider configProvider, Feature feature) =>
            configProvider.Config.HasFeature(feature);

        public static bool HasFeature(this ProjectConfig config, Feature feature)
        {
            //No features specified; allow all features
            if (config.Features == null)
                return true;

            //Feature "System" is always allowed
            if (feature == Feature.System)
                return true;

            if (config.Features.Length == 0)
                return false;

            return config.Features.Contains(feature);
        }

        public static bool HasCommand(this ProjectConfig config, Type type)
        {
            var attrib = type.GetCustomAttribute<BuildCommandAttribute>();

            if (attrib == null)
                throw new InvalidOperationException($"Cmdlet '{type.Name}' is missing a '{nameof(BuildCommandAttribute)}'.");

            if (config.Features == null)
            {
                //All features are (implicitly) allowed

                if (config.Commands == null)
                {
                    //All commands are (implicitly) allowed
                    return true;
                }

                if (config.Commands.Length == 0)
                {
                    //Explicitly no commands are allowed. You can't have no commands, this will generate an error
                    return false;
                }

                if (attrib.Feature == Feature.System)
                {
                    //System feature is (implicitly) allowed
                    return true;
                }

                //Only certain commands are allowed, check if this is one of them
                return config.Commands.Contains(attrib.Kind);
            }

            if (config.Features.Length == 0)
            {
                //Explicitly no features are allowed. This means the System feature too!

                if (config.Commands == null)
                {
                    //All commands are (implicitly) allowed, but the explicit ban on features wins
                    return false;
                }

                if (config.Commands.Length == 0)
                {
                    //Explicitly no features or commands are allowed
                    return false;
                }

                //If the command was explicitly allowed, allow it
                return config.Commands.Contains(attrib.Kind);
            }

            if (config.Features.Contains(attrib.Feature))
            {
                //The feature is allowed
                return true;
            }
            else
            {
                //The feature is not allowed, but is the command allowed?

                if (attrib.Feature == Feature.System)
                {
                    //System feature is (implicitly) allowed. The only way to blacklist System is to explicitly
                    //exclude all features and define the commands you want manually
                    return true;
                }

                if (config.Commands == null)
                {
                    //No commands were explicitly allowed
                    return false;
                }

                if (config.Commands.Length == 0)
                {
                    //Explicitly no commands are allowed
                    return false;
                }

                //If the command was explicitly allowed, allow it
                return config.Commands.Contains(attrib.Kind);
            }
        }
    }
}
